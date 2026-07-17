#nullable enable

using FluentAssertions;
using JobScheduler.Core.Data;
using JobScheduler.Core.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DotnetJobScheduler.Tests;

/// <summary>
/// Extension methods for <see cref="DistributedJobLockServiceTests"/> that provide additional test utilities
/// and convenience methods for testing distributed job lock scenarios.
/// </summary>
public static class DistributedJobLockServiceTestsExtensions
{

    /// <summary>
    /// Creates a service instance with a fresh in-memory database context.
    /// </summary>
    /// <param name="service">The test service instance.</param>
    /// <returns>A new <see cref="DistributedJobLockService"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> is <see langword="null"/>.</exception>
    public static DistributedJobLockService CreateFreshService(this DistributedJobLockServiceTests service)
    {
        ArgumentNullException.ThrowIfNull(service);
        return new DistributedJobLockService(CreateFreshContext());
    }

    /// <summary>
    /// Creates a fresh in-memory database context for testing.
    /// </summary>
    /// <returns>A new <see cref="JobSchedulerContext"/> instance.</returns>
    public static JobSchedulerContext CreateFreshContext()
    {
        var options = new DbContextOptionsBuilder<JobSchedulerContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobSchedulerContext(options);
    }

    /// <summary>
    /// Creates a service instance and acquires a lock, returning the lock entity for further assertions.
    /// </summary>
    /// <param name="service">The test service instance (for extension method syntax).</param>
    /// <param name="jobId">The job ID to acquire.</param>
    /// <param name="holderId">The holder instance ID.</param>
    /// <param name="duration">The lock duration.</param>
    /// <returns>The acquired lock entity.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="holderId"/> is <see langword="null"/> or empty.</exception>
    /// <exception cref="Xunit.Sdk.XunitException">Thrown when lock acquisition fails.</exception>
    public static async Task<DistributedJobLock> ShouldAcquireLockAsync(
        this DistributedJobLockServiceTests _,
        Guid jobId,
        string holderId,
        TimeSpan duration)
    {
        ArgumentNullException.ThrowIfNull(_);
        ArgumentException.ThrowIfNullOrEmpty(holderId);

        using var ctx = CreateFreshContext();
        var service = new DistributedJobLockService(ctx);
        var acquired = await service.TryAcquireLockAsync(jobId, holderId, duration);
        acquired.Should().BeTrue("Lock acquisition should succeed");

        var lockEntity = await ctx.DistributedJobLocks.ToListAsync();
        lockEntity.Should().ContainSingle("Exactly one lock should exist");
        return lockEntity[0];
    }

    /// <summary>
    /// Creates a service instance and attempts to acquire a lock, returning the lock count.
    /// </summary>
    /// <param name="service">The test service instance (for extension method syntax).</param>
    /// <param name="jobId">The job ID to attempt to acquire.</param>
    /// <param name="holderId">The holder instance ID.</param>
    /// <param name="duration">The lock duration.</param>
    /// <returns>The number of locks in the database.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="holderId"/> is <see langword="null"/> or empty.</exception>
    /// <exception cref="Xunit.Sdk.XunitException">Thrown when unexpected lock state occurs.</exception>
    public static async Task<int> ShouldNotAcquireLockAsync(
        this DistributedJobLockServiceTests _,
        Guid jobId,
        string holderId,
        TimeSpan duration)
    {
        ArgumentNullException.ThrowIfNull(_);
        ArgumentException.ThrowIfNullOrEmpty(holderId);

        using var ctx = CreateFreshContext();
        var service = new DistributedJobLockService(ctx);
        var acquired = await service.TryAcquireLockAsync(jobId, holderId, duration);
        acquired.Should().BeFalse("Lock acquisition should fail");

        var lockCount = await ctx.DistributedJobLocks.CountAsync();
        return lockCount;
    }

    /// <summary>
    /// Creates a service instance and checks if a lock is currently held, returning the lock entity.
    /// </summary>
    /// <param name="service">The test service instance (for extension method syntax).</param>
    /// <param name="jobId">The job ID to check.</param>
    /// <returns>The lock entity if it exists.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> is <see langword="null"/>.</exception>
    /// <exception cref="Xunit.Sdk.XunitException">Thrown when lock is not held or multiple locks exist.</exception>
    public static async Task<DistributedJobLock> ShouldBeLockedAsync(
        this DistributedJobLockServiceTests _,
        Guid jobId)
    {
        ArgumentNullException.ThrowIfNull(_);

        using var ctx = CreateFreshContext();
        var service = new DistributedJobLockService(ctx);
        var isLocked = await service.IsLockedAsync(jobId);
        isLocked.Should().BeTrue("Job should be locked");

        var activeLocks = await ctx.DistributedJobLocks.ToListAsync();
        activeLocks.Should().ContainSingle("Exactly one lock should exist");
        return activeLocks[0];
    }

    /// <summary>
    /// Creates a service instance and checks if a lock is not currently held.
    /// </summary>
    /// <param name="service">The test service instance (for extension method syntax).</param>
    /// <param name="jobId">The job ID to check.</param>
    /// <returns>The number of active locks.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> is <see langword="null"/>.</exception>
    /// <exception cref="Xunit.Sdk.XunitException">Thrown when lock unexpectedly exists.</exception>
    public static async Task<int> ShouldNotBeLockedAsync(
        this DistributedJobLockServiceTests _,
        Guid jobId)
    {
        ArgumentNullException.ThrowIfNull(_);

        using var ctx = CreateFreshContext();
        var service = new DistributedJobLockService(ctx);
        var isLocked = await service.IsLockedAsync(jobId);
        isLocked.Should().BeFalse("Job should not be locked");

        var activeLocks = await ctx.DistributedJobLocks.ToListAsync();
        return activeLocks.Count;
    }

    /// <summary>
    /// Creates a test scenario with multiple locks and returns them for assertion.
    /// </summary>
    /// <param name="service">The test service.</param>
    /// <param name="locks">Collection of lock configurations to create.</param>
    /// <returns>List of created lock entities.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="service"/> or <paramref name="locks"/> is <see langword="null"/>.</exception>
    public static async Task<IReadOnlyList<DistributedJobLock>> CreateLocksAsync(
        this DistributedJobLockServiceTests service,
        params (Guid JobId, string HolderId, DateTime ExpiresAt)[] locks)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(locks);

        using var ctx = CreateFreshContext();
        var now = DateTime.UtcNow;

        foreach (var (jobId, holderId, expiresAt) in locks)
        {
            ctx.DistributedJobLocks.Add(new DistributedJobLock
            {
                JobId = jobId,
                HolderInstanceId = holderId,
                AcquiredAt = now.AddMinutes(-10),
                ExpiresAt = expiresAt
            });
        }

        await ctx.SaveChangesAsync();

        return await ctx.DistributedJobLocks.ToListAsync();
    }

    /// <summary>
    /// Gets the expiry time of a specific lock.
    /// </summary>
    /// <param name="service">The test service.</param>
    /// <param name="jobId">The job ID to check.</param>
    /// <returns>The expiry time of the lock.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> is <see langword="null"/>.</exception>
    /// <exception cref="Xunit.Sdk.XunitException">Thrown when lock doesn't exist.</exception>
    public static async Task<DateTime> GetLockExpiryAsync(
        this DistributedJobLockServiceTests service,
        Guid jobId)
    {
        ArgumentNullException.ThrowIfNull(service);

        using var ctx = CreateFreshContext();
        var lockEntity = await ctx.DistributedJobLocks
            .FirstOrDefaultAsync(l => l.JobId == jobId);
        lockEntity.Should().NotBeNull("Lock should exist");
        return lockEntity!.ExpiresAt;
    }
}