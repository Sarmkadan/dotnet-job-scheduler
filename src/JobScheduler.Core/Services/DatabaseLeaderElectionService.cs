#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using JobScheduler.Core.Data;

namespace JobScheduler.Core.Services;

/// <summary>
/// Database-backed leader election service.
/// Uses an optimistic-lock row in <c>SchedulerLeaderLock</c> to ensure only one
/// scheduler node is active at a time.  This implementation requires no external
/// dependencies beyond the existing database.
/// </summary>
public sealed class DatabaseLeaderElectionService : ILeaderElectionService
{
    private readonly JobSchedulerContext _context;
    private readonly string _instanceId;
    private readonly TimeSpan _leaseDuration;
    private readonly ILogger<DatabaseLeaderElectionService>? _logger;
    private volatile bool _isLeader;

    /// <summary>
    /// Initialises the service.
    /// </summary>
    /// <param name="context">EF Core context used to access the leader-lock table.</param>
    /// <param name="instanceId">
    ///   Unique identifier for this scheduler node.  Defaults to the machine name.
    /// </param>
    /// <param name="leaseDurationSeconds">
    ///   Seconds before an un-renewed lease expires and another node may take over.
    ///   Defaults to 30 s.
    /// </param>
    /// <param name="logger">Optional logger.</param>
    public DatabaseLeaderElectionService(
        JobSchedulerContext context,
        string? instanceId = null,
        int leaseDurationSeconds = 30,
        ILogger<DatabaseLeaderElectionService>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _instanceId = string.IsNullOrWhiteSpace(instanceId) ? Environment.MachineName : instanceId;
        _leaseDuration = TimeSpan.FromSeconds(leaseDurationSeconds > 0 ? leaseDurationSeconds : 30);
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsLeader => _isLeader;

    /// <inheritdoc />
    public async Task<bool> TryAcquireLeadershipAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            var leaseExpiry = now.Add(_leaseDuration);

            // Ensure table row exists.
            var lockRow = await _context.SchedulerLeaderLocks
                .FirstOrDefaultAsync(r => r.LockName == SchedulerLeaderLock.DefaultLockName, cancellationToken);

            if (lockRow is null)
            {
                lockRow = new SchedulerLeaderLock
                {
                    LockName = SchedulerLeaderLock.DefaultLockName,
                    LeaderInstanceId = _instanceId,
                    LeaseExpiresAt = leaseExpiry,
                    AcquiredAt = now
                };
                _context.SchedulerLeaderLocks.Add(lockRow);
                await _context.SaveChangesAsync(cancellationToken);
                _isLeader = true;
                _logger?.LogInformation("Leader election: instance '{InstanceId}' acquired leadership", _instanceId);
                return true;
            }

            // We already own the lease — renew it.
            if (lockRow.LeaderInstanceId == _instanceId)
            {
                lockRow.LeaseExpiresAt = leaseExpiry;
                await _context.SaveChangesAsync(cancellationToken);
                _isLeader = true;
                return true;
            }

            // Another instance holds the lease and it has NOT expired yet.
            if (lockRow.LeaseExpiresAt > now)
            {
                _isLeader = false;
                return false;
            }

            // Lease has expired — take over.
            lockRow.LeaderInstanceId = _instanceId;
            lockRow.LeaseExpiresAt = leaseExpiry;
            lockRow.AcquiredAt = now;
            await _context.SaveChangesAsync(cancellationToken);
            _isLeader = true;
            _logger?.LogInformation(
                "Leader election: instance '{InstanceId}' took over expired lease from '{PrevLeader}'",
                _instanceId, lockRow.LeaderInstanceId);
            return true;
        }
        catch (Exception ex)
        {
            _isLeader = false;
            _logger?.LogWarning(ex, "Leader election: failed to acquire/renew lease for instance '{InstanceId}'", _instanceId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task ReleaseLeadershipAsync(CancellationToken cancellationToken = default)
    {
        if (!_isLeader)
            return;

        try
        {
            var lockRow = await _context.SchedulerLeaderLocks
                .FirstOrDefaultAsync(
                    r => r.LockName == SchedulerLeaderLock.DefaultLockName && r.LeaderInstanceId == _instanceId,
                    cancellationToken);

            if (lockRow is not null)
            {
                // Expire the lease immediately so another node can take over.
                lockRow.LeaseExpiresAt = DateTime.UtcNow.AddSeconds(-1);
                await _context.SaveChangesAsync(cancellationToken);
            }

            _isLeader = false;
            _logger?.LogInformation("Leader election: instance '{InstanceId}' released leadership", _instanceId);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Leader election: error releasing lease for instance '{InstanceId}'", _instanceId);
        }
    }
}

/// <summary>
/// EF Core entity that represents the distributed leader lock row.
/// </summary>
public sealed class SchedulerLeaderLock
{
    public const string DefaultLockName = "scheduler-leader";

    public int Id { get; set; }
    public string LockName { get; set; } = DefaultLockName;
    public string LeaderInstanceId { get; set; } = string.Empty;
    public DateTime LeaseExpiresAt { get; set; }
    public DateTime AcquiredAt { get; set; }
}
