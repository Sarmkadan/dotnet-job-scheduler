#nullable enable

using FluentAssertions;
using JobScheduler.Core.Data;
using JobScheduler.Core.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DotnetJobScheduler.Tests;

/// <summary>
/// Tests for the DistributedJobLockService class.
/// </summary>
public sealed class DistributedJobLockServiceTests
{
    /// <summary>
    /// Creates a new in-memory JobSchedulerContext instance.
    /// </summary>
    /// <returns>A new JobSchedulerContext instance.</returns>
    private static JobSchedulerContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<JobSchedulerContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobSchedulerContext(options);
    }

    /// <summary>
    /// Creates a new DistributedJobLockService instance, optionally using the provided JobSchedulerContext.
    /// </summary>
    /// <param name="ctx">The JobSchedulerContext to use, or null to create a new in-memory context.</param>
    /// <returns>A new DistributedJobLockService instance.</returns>
    private static DistributedJobLockService CreateService(JobSchedulerContext? ctx = null) =>
        new(ctx ?? CreateInMemoryContext());

    /// <summary>
    /// Tests that TryAcquireLockAsync returns true when acquiring a lock for the first time.
    /// </summary>
    /// <returns>A task that completes when the test is finished.</returns>
    [Fact]
    public async Task TryAcquireLockAsync_FirstAcquisition_ReturnsTrue()
    {
        // Arrange
        var ctx = CreateInMemoryContext();
        var service = CreateService(ctx);
        var jobId = Guid.NewGuid();

        // Act
        var acquired = await service.TryAcquireLockAsync(jobId, "node-1", TimeSpan.FromMinutes(5));

        // Assert
        acquired.Should().BeTrue();
        var locks = await ctx.DistributedJobLocks.ToListAsync();
        locks.Should().HaveCount(1);
        locks[0].JobId.Should().Be(jobId);
        locks[0].HolderInstanceId.Should().Be("node-1");
    }

    /// <summary>
    /// Tests that TryAcquireLockAsync renews a lock when the same holder attempts to acquire it again.
    /// </summary>
    /// <returns>A task that completes when the test is finished.</returns>
    [Fact]
    public async Task TryAcquireLockAsync_SameHolderRenewsLock_ReturnsTrue()
    {
        // Arrange
        var ctx = CreateInMemoryContext();
        var service = CreateService(ctx);
        var jobId = Guid.NewGuid();

        await service.TryAcquireLockAsync(jobId, "node-1", TimeSpan.FromMinutes(5));
        var firstExpiry = (await ctx.DistributedJobLocks.FirstAsync()).ExpiresAt;

        // Act — renew
        var renewed = await service.TryAcquireLockAsync(jobId, "node-1", TimeSpan.FromMinutes(10));

        // Assert
        renewed.Should().BeTrue();
        var updatedExpiry = (await ctx.DistributedJobLocks.FirstAsync()).ExpiresAt;
        updatedExpiry.Should().BeAfter(firstExpiry);
    }

    /// <summary>
    /// Tests that TryAcquireLockAsync returns false when a different holder attempts to acquire a lock that is already held by another holder.
    /// </summary>
    /// <returns>A task that completes when the test is finished.</returns>
    [Fact]
    public async Task TryAcquireLockAsync_DifferentHolderOnActiveLock_ReturnsFalse()
    {
        // Arrange
        var ctx = CreateInMemoryContext();
        var service = CreateService(ctx);
        var jobId = Guid.NewGuid();

        await service.TryAcquireLockAsync(jobId, "node-1", TimeSpan.FromMinutes(5));

        // Act
        var acquired = await service.TryAcquireLockAsync(jobId, "node-2", TimeSpan.FromMinutes(5));

        // Assert
        acquired.Should().BeFalse();
    }

    /// <summary>
    /// Tests that TryAcquireLockAsync allows a different holder to acquire a lock after the previous lock has expired.
    /// </summary>
    /// <returns>A task that completes when the test is finished.</returns>
    [Fact]
    public async Task TryAcquireLockAsync_AfterExpiry_DifferentHolderSucceeds()
    {
        // Arrange
        var ctx = CreateInMemoryContext();
        var service = CreateService(ctx);
        var jobId = Guid.NewGuid();

        // Acquire with very short duration so it expires immediately
        await service.TryAcquireLockAsync(jobId, "node-1", TimeSpan.FromMilliseconds(1));
        await Task.Delay(10); // ensure expiry

        // Act
        var acquired = await service.TryAcquireLockAsync(jobId, "node-2", TimeSpan.FromMinutes(5));

        // Assert
        acquired.Should().BeTrue();
        var lockEntry = await ctx.DistributedJobLocks.FirstAsync();
        lockEntry.HolderInstanceId.Should().Be("node-2");
    }

    /// <summary>
    /// Tests that ReleaseLockAsync removes a lock when called by the correct holder.
    /// </summary>
    /// <returns>A task that completes when the test is finished.</returns>
    [Fact]
    public async Task ReleaseLockAsync_ByCorrectHolder_RemovesLock()
    {
        // Arrange
        var ctx = CreateInMemoryContext();
        var service = CreateService(ctx);
        var jobId = Guid.NewGuid();

        await service.TryAcquireLockAsync(jobId, "node-1", TimeSpan.FromMinutes(5));

        // Act
        await service.ReleaseLockAsync(jobId, "node-1");

        // Assert
        var count = await ctx.DistributedJobLocks.CountAsync();
        count.Should().Be(0);
    }

    /// <summary>
    /// Tests that ReleaseLockAsync does not remove a lock when called by an incorrect holder.
    /// </summary>
    /// <returns>A task that completes when the test is finished.</returns>
    [Fact]
    public async Task ReleaseLockAsync_ByWrongHolder_DoesNotRemoveLock()
    {
        // Arrange
        var ctx = CreateInMemoryContext();
        var service = CreateService(ctx);
        var jobId = Guid.NewGuid();

        await service.TryAcquireLockAsync(jobId, "node-1", TimeSpan.FromMinutes(5));

        // Act — wrong holder
        await service.ReleaseLockAsync(jobId, "node-999");

        // Assert — lock still present
        var count = await ctx.DistributedJobLocks.CountAsync();
        count.Should().Be(1);
    }

    /// <summary>
    /// Tests that IsLockedAsync returns true when a lock is active.
    /// </summary>
    /// <returns>A task that completes when the test is finished.</returns>
    [Fact]
    public async Task IsLockedAsync_WithActiveLock_ReturnsTrue()
    {
        // Arrange
        var ctx = CreateInMemoryContext();
        var service = CreateService(ctx);
        var jobId = Guid.NewGuid();

        await service.TryAcquireLockAsync(jobId, "node-1", TimeSpan.FromMinutes(5));

        // Act
        var locked = await service.IsLockedAsync(jobId);

        // Assert
        locked.Should().BeTrue();
    }

    /// <summary>
    /// Tests that IsLockedAsync returns false when no lock is present.
    /// </summary>
    /// <returns>A task that completes when the test is finished.</returns>
    [Fact]
    public async Task IsLockedAsync_WithNoLock_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();

        // Act
        var locked = await service.IsLockedAsync(Guid.NewGuid());

        // Assert
        locked.Should().BeFalse();
    }

    /// <summary>
    /// Tests that RenewLockAsync renews a lock when called by the correct holder.
    /// </summary>
    /// <returns>A task that completes when the test is finished.</returns>
    [Fact]
    public async Task RenewLockAsync_ByCorrectHolder_ExtendsExpiry()
    {
        // Arrange
        var ctx = CreateInMemoryContext();
        var service = CreateService(ctx);
        var jobId = Guid.NewGuid();

        await service.TryAcquireLockAsync(jobId, "node-1", TimeSpan.FromMinutes(1));
        var before = (await ctx.DistributedJobLocks.FirstAsync()).ExpiresAt;

        // Act
        var renewed = await service.RenewLockAsync(jobId, "node-1", TimeSpan.FromMinutes(10));

        // Assert
        renewed.Should().BeTrue();
        var after = (await ctx.DistributedJobLocks.FirstAsync()).ExpiresAt;
        after.Should().BeAfter(before);
    }

    /// <summary>
    /// Tests that RenewLockAsync returns false when called by an incorrect holder.
    /// </summary>
    /// <returns>A task that completes when the test is finished.</returns>
    [Fact]
    public async Task RenewLockAsync_ByWrongHolder_ReturnsFalse()
    {
        // Arrange
        var ctx = CreateInMemoryContext();
        var service = CreateService(ctx);
        var jobId = Guid.NewGuid();

        await service.TryAcquireLockAsync(jobId, "node-1", TimeSpan.FromMinutes(5));

        // Act
        var renewed = await service.RenewLockAsync(jobId, "node-999", TimeSpan.FromMinutes(10));

        // Assert
        renewed.Should().BeFalse();
    }

    /// <summary>
    /// Tests that CleanExpiredLocksAsync removes only expired locks.
    /// </summary>
    /// <returns>A task that completes when the test is finished.</returns>
    [Fact]
    public async Task CleanExpiredLocksAsync_RemovesOnlyExpiredLocks()
    {
        // Arrange
        var ctx = CreateInMemoryContext();
        var service = CreateService(ctx);

        // Add two locks: one active, one already expired
        ctx.DistributedJobLocks.AddRange(
            new DistributedJobLock { JobId = Guid.NewGuid(), HolderInstanceId = "node-1", AcquiredAt = DateTime.UtcNow.AddMinutes(-10), ExpiresAt = DateTime.UtcNow.AddMinutes(5) },
            new DistributedJobLock { JobId = Guid.NewGuid(), HolderInstanceId = "node-2", AcquiredAt = DateTime.UtcNow.AddMinutes(-20), ExpiresAt = DateTime.UtcNow.AddMinutes(-5) }
        );
        await ctx.SaveChangesAsync();

        // Act
        var removed = await service.CleanExpiredLocksAsync();

        // Assert
        removed.Should().Be(1);
        var remaining = await ctx.DistributedJobLocks.CountAsync();
        remaining.Should().Be(1);
    }

    /// <summary>
    /// Tests that GetActiveLocksAsync returns only non-expired locks.
    /// </summary>
    /// <returns>A task that completes when the test is finished.</returns>
    [Fact]
    public async Task GetActiveLocksAsync_ReturnsOnlyNonExpiredLocks()
    {
        // Arrange
        var ctx = CreateInMemoryContext();
        var service = CreateService(ctx);

        ctx.DistributedJobLocks.AddRange(
            new DistributedJobLock { JobId = Guid.NewGuid(), HolderInstanceId = "node-1", AcquiredAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddMinutes(5) },
            new DistributedJobLock { JobId = Guid.NewGuid(), HolderInstanceId = "node-2", AcquiredAt = DateTime.UtcNow.AddMinutes(-20), ExpiresAt = DateTime.UtcNow.AddMinutes(-1) }
        );
        await ctx.SaveChangesAsync();

        // Act
        var active = await service.GetActiveLocksAsync();

        // Assert
        active.Should().HaveCount(1);
        active[0].HolderInstanceId.Should().Be("node-1");
    }

    /// <summary>
    /// Tests that TryAcquireLockAsync throws an ArgumentException when the holder ID is empty.
    /// </summary>
    /// <returns>A task that completes when the test is finished.</returns>
    [Fact]
    public async Task TryAcquireLockAsync_WithEmptyHolderId_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.TryAcquireLockAsync(Guid.NewGuid(), "", TimeSpan.FromMinutes(1)));
    }

    /// <summary>
    /// Tests that TryAcquireLockAsync throws an ArgumentException when the duration is non-positive.
    /// </summary>
    /// <returns>A task that completes when the test is finished.</returns>
    [Fact]
    public async Task TryAcquireLockAsync_WithNonPositiveDuration_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.TryAcquireLockAsync(Guid.NewGuid(), "node-1", TimeSpan.Zero));
    }
}
