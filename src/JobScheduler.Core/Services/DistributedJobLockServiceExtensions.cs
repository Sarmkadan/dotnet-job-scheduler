#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics;
using JobScheduler.Core.Data;
using Microsoft.Extensions.Logging;

namespace JobScheduler.Core.Services;

/// <summary>
/// Extension methods for <see cref="DistributedJobLockService"/> that provide
/// convenient and commonly-needed operations for distributed job locking scenarios.
/// </summary>
public static class DistributedJobLockServiceExtensions
{
    /// <summary>
    /// Attempts to acquire a lock with automatic retry logic for transient failures.
    /// </summary>
    /// <param name="service">The distributed lock service.</param>
    /// <param name="jobId">The job identifier.</param>
    /// <param name="holderInstanceId">The instance identifier holding the lock.</param>
    /// <param name="lockDuration">The duration for which the lock should be held.</param>
    /// <param name="maxAttempts">Maximum number of retry attempts. Must be at least 1.</param>
    /// <param name="retryDelay">Delay between retry attempts. Defaults to 100ms.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if lock was acquired within the retry limit, false otherwise.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxAttempts"/> is less than 1.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="holderInstanceId"/> is null.</exception>
    public static async Task<bool> TryAcquireLockWithRetryAsync(
        this DistributedJobLockService service,
        Guid jobId,
        string holderInstanceId,
        TimeSpan lockDuration,
        int maxAttempts = 3,
        TimeSpan? retryDelay = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(holderInstanceId);

        if (maxAttempts < 1)
            throw new ArgumentOutOfRangeException(nameof(maxAttempts), "Must be at least 1");

        var delay = retryDelay ?? TimeSpan.FromMilliseconds(100);
        var attempt = 0;

        while (attempt < maxAttempts)
        {
            attempt++;
            var acquired = await service.TryAcquireLockAsync(jobId, holderInstanceId, lockDuration, cancellationToken);

            if (acquired)
                return true;

            if (attempt < maxAttempts)
            {
                await Task.Delay(delay, cancellationToken);
            }
        }

        return false;
    }

    /// <summary>
    /// Executes an action while holding a distributed lock, automatically releasing it when complete.
    /// </summary>
    /// <param name="service">The distributed lock service.</param>
    /// <param name="jobId">The job identifier.</param>
    /// <param name="holderInstanceId">The instance identifier holding the lock.</param>
    /// <param name="lockDuration">The duration for which the lock should be held.</param>
    /// <param name="action">The action to execute while holding the lock.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the action was executed successfully, false if lock could not be acquired.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> is null.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is null.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="holderInstanceId"/> is null.</exception>
    public static async Task<bool> ExecuteWithLockAsync(
        this DistributedJobLockService service,
        Guid jobId,
        string holderInstanceId,
        TimeSpan lockDuration,
        Func<Task> action,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(holderInstanceId);

        var acquired = await service.TryAcquireLockAsync(jobId, holderInstanceId, lockDuration, cancellationToken);

        if (!acquired)
            return false;

        try
        {
            await action();
            return true;
        }
        finally
        {
            await service.ReleaseLockAsync(jobId, holderInstanceId, cancellationToken);
        }
    }

    /// <summary>
    /// Checks if a lock is currently held by a specific instance.
    /// </summary>
    /// <param name="service">The distributed lock service.</param>
    /// <param name="jobId">The job identifier.</param>
    /// <param name="holderInstanceId">The instance identifier to check for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the specified instance holds the lock, false otherwise.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> is null.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="holderInstanceId"/> is null.</exception>
    public static async Task<bool> IsHeldByAsync(
        this DistributedJobLockService service,
        Guid jobId,
        string holderInstanceId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(holderInstanceId);

        var isLocked = await service.IsLockedAsync(jobId, cancellationToken);

        if (!isLocked)
            return false;

        var activeLocks = await service.GetActiveLocksAsync(cancellationToken);
        var lockInfo = activeLocks.FirstOrDefault(l => l.JobId == jobId);

        return lockInfo?.HolderInstanceId == holderInstanceId;
    }

    /// <summary>
    /// Gets the remaining time for a lock if it's currently held.
    /// </summary>
    /// <param name="service">The distributed lock service.</param>
    /// <param name="jobId">The job identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// TimeSpan representing the remaining lock duration, or TimeSpan.Zero if the lock is not held.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> is null.</exception>
    public static async Task<TimeSpan> GetRemainingLockTimeAsync(
        this DistributedJobLockService service,
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(service);

        var now = DateTime.UtcNow;
        var lockInfo = (await service.GetActiveLocksAsync(cancellationToken))
            .FirstOrDefault(l => l.JobId == jobId);

        if (lockInfo is null || lockInfo.IsExpired(now))
            return TimeSpan.Zero;

        var remaining = lockInfo.ExpiresAt - now;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }
}