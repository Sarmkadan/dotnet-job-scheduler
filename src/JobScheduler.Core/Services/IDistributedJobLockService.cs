#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace JobScheduler.Core.Services;

/// <summary>
/// Provides distributed, database-backed per-job locking for multi-instance deployments.
/// Prevents two scheduler nodes from executing the same job concurrently even when the
/// in-process <see cref="ConcurrencyManager"/> state is not shared between instances.
/// </summary>
public interface IDistributedJobLockService
{
    /// <summary>
    /// Attempts to acquire an exclusive lock for <paramref name="jobId"/>.
    /// Returns <c>true</c> when the lock is granted; <c>false</c> when another instance
    /// already holds a valid (non-expired) lock.
    /// </summary>
    /// <param name="jobId">The job to lock.</param>
    /// <param name="holderInstanceId">Unique ID of the calling scheduler instance.</param>
    /// <param name="lockDuration">How long the lock remains valid without renewal.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<bool> TryAcquireLockAsync(
        Guid jobId,
        string holderInstanceId,
        TimeSpan lockDuration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases the lock held by <paramref name="holderInstanceId"/> for <paramref name="jobId"/>.
    /// No-ops when the caller does not hold the lock or the lock has already expired.
    /// </summary>
    /// <param name="jobId">The job whose lock should be released.</param>
    /// <param name="holderInstanceId">Unique ID of the calling scheduler instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ReleaseLockAsync(
        Guid jobId,
        string holderInstanceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns <c>true</c> when a valid (non-expired) lock exists for <paramref name="jobId"/>.
    /// </summary>
    Task<bool> IsLockedAsync(Guid jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Renews the expiry of an existing lock held by <paramref name="holderInstanceId"/>.
    /// Returns <c>true</c> when the renewal succeeded; <c>false</c> when the lock does not
    /// exist or belongs to a different holder.
    /// </summary>
    Task<bool> RenewLockAsync(
        Guid jobId,
        string holderInstanceId,
        TimeSpan lockDuration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all active (non-expired) locks in the system.
    /// </summary>
    Task<IReadOnlyList<DistributedJobLock>> GetActiveLocksAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all expired lock entries from the database.
    /// Should be called periodically to prevent table growth.
    /// </summary>
    /// <returns>The number of expired locks that were removed.</returns>
    Task<int> CleanExpiredLocksAsync(CancellationToken cancellationToken = default);
}
