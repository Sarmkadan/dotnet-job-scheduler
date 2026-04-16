#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using JobScheduler.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobScheduler.Core.Services;

/// <summary>
/// Database-backed implementation of <see cref="IDistributedJobLockService"/>.
/// Uses optimistic concurrency (try/catch on <c>SaveChanges</c>) and per-job unique
/// indexes to ensure that only one holder can own a lock at any given time, even
/// under concurrent writes from multiple scheduler instances.
/// </summary>
public sealed class DistributedJobLockService : IDistributedJobLockService
{
    private readonly JobSchedulerContext _context;
    private readonly ILogger<DistributedJobLockService>? _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="DistributedJobLockService"/>.
    /// </summary>
    /// <param name="context">EF Core context backed by the shared scheduler database.</param>
    /// <param name="logger">Optional structured logger.</param>
    public DistributedJobLockService(
        JobSchedulerContext context,
        ILogger<DistributedJobLockService>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> TryAcquireLockAsync(
        Guid jobId,
        string holderInstanceId,
        TimeSpan lockDuration,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(holderInstanceId))
            throw new ArgumentException("Holder instance ID cannot be empty.", nameof(holderInstanceId));

        if (lockDuration <= TimeSpan.Zero)
            throw new ArgumentException("Lock duration must be positive.", nameof(lockDuration));

        try
        {
            var now = DateTime.UtcNow;
            var expiry = now.Add(lockDuration);

            var existing = await _context.DistributedJobLocks
                .FirstOrDefaultAsync(l => l.JobId == jobId, cancellationToken);

            if (existing is null)
            {
                _context.DistributedJobLocks.Add(new DistributedJobLock
                {
                    JobId = jobId,
                    HolderInstanceId = holderInstanceId,
                    AcquiredAt = now,
                    ExpiresAt = expiry
                });

                await _context.SaveChangesAsync(cancellationToken);
                _logger?.LogDebug("Distributed lock acquired for job {JobId} by instance '{InstanceId}'", jobId, holderInstanceId);
                return true;
            }

            // Current holder renewing its own lock
            if (existing.HolderInstanceId == holderInstanceId)
            {
                existing.ExpiresAt = expiry;
                await _context.SaveChangesAsync(cancellationToken);
                return true;
            }

            // Another holder owns the lock — grant only if it has expired
            if (existing.IsExpired(now))
            {
                existing.HolderInstanceId = holderInstanceId;
                existing.AcquiredAt = now;
                existing.ExpiresAt = expiry;
                await _context.SaveChangesAsync(cancellationToken);
                _logger?.LogInformation(
                    "Distributed lock for job {JobId} taken over by instance '{NewHolder}' (previous lock expired)",
                    jobId, holderInstanceId);
                return true;
            }

            _logger?.LogDebug(
                "Distributed lock for job {JobId} is held by '{HolderInstanceId}' until {Expiry}; acquisition denied",
                jobId, existing.HolderInstanceId, existing.ExpiresAt);
            return false;
        }
        catch (DbUpdateException ex)
        {
            // Unique-index violation means a concurrent writer won the race
            _logger?.LogWarning(ex,
                "Distributed lock acquisition race condition for job {JobId} by instance '{InstanceId}'",
                jobId, holderInstanceId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task ReleaseLockAsync(
        Guid jobId,
        string holderInstanceId,
        CancellationToken cancellationToken = default)
    {
        var existing = await _context.DistributedJobLocks
            .FirstOrDefaultAsync(l => l.JobId == jobId, cancellationToken);

        if (existing is null || existing.HolderInstanceId != holderInstanceId)
            return;

        _context.DistributedJobLocks.Remove(existing);
        await _context.SaveChangesAsync(cancellationToken);

        _logger?.LogDebug("Distributed lock for job {JobId} released by instance '{InstanceId}'", jobId, holderInstanceId);
    }

    /// <inheritdoc />
    public async Task<bool> IsLockedAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var existing = await _context.DistributedJobLocks
            .FirstOrDefaultAsync(l => l.JobId == jobId, cancellationToken);

        return existing is not null && !existing.IsExpired(now);
    }

    /// <inheritdoc />
    public async Task<bool> RenewLockAsync(
        Guid jobId,
        string holderInstanceId,
        TimeSpan lockDuration,
        CancellationToken cancellationToken = default)
    {
        var existing = await _context.DistributedJobLocks
            .FirstOrDefaultAsync(l => l.JobId == jobId, cancellationToken);

        if (existing is null || existing.HolderInstanceId != holderInstanceId || existing.IsExpired())
            return false;

        existing.ExpiresAt = DateTime.UtcNow.Add(lockDuration);
        await _context.SaveChangesAsync(cancellationToken);

        _logger?.LogDebug("Distributed lock for job {JobId} renewed by instance '{InstanceId}'", jobId, holderInstanceId);
        return true;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DistributedJobLock>> GetActiveLocksAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.DistributedJobLocks
            .Where(l => l.ExpiresAt > now)
            .OrderBy(l => l.AcquiredAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CleanExpiredLocksAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var expired = await _context.DistributedJobLocks
            .Where(l => l.ExpiresAt <= now)
            .ToListAsync(cancellationToken);

        if (expired.Count == 0)
            return 0;

        _context.DistributedJobLocks.RemoveRange(expired);
        await _context.SaveChangesAsync(cancellationToken);

        _logger?.LogInformation("Cleaned {Count} expired distributed job lock(s)", expired.Count);
        return expired.Count;
    }
}
