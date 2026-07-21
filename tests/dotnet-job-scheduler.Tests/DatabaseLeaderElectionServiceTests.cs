#nullable enable

using FluentAssertions;
using JobScheduler.Core.Data;
using JobScheduler.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotnetJobScheduler.Tests;

/// <summary>
/// Tests for DatabaseLeaderElectionService which manages distributed leadership.
/// </summary>
public sealed class DatabaseLeaderElectionServiceTests : IAsyncLifetime
{
    private readonly DbContextOptions<JobSchedulerContext> _dbOptions;
    private JobSchedulerContext? _context;

    public DatabaseLeaderElectionServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<JobSchedulerContext>()
            .UseInMemoryDatabase("LeaderElectionTest")
            .Options;
    }

    public async Task InitializeAsync()
    {
        _context = new JobSchedulerContext(_dbOptions);
        await _context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        if (_context is not null)
        {
            await _context.Database.EnsureDeletedAsync();
            await _context.DisposeAsync();
        }
    }

    private DatabaseLeaderElectionService CreateService(string? instanceId = null) =>
        new(_context!, instanceId, leaseDurationSeconds: 30);

    [Fact]
    public async Task TryAcquireLeadershipAsync_FirstNode_BecamesLeader()
    {
        // Arrange
        var service = CreateService("node-1");

        // Act
        var acquired = await service.TryAcquireLeadershipAsync();

        // Assert
        acquired.Should().BeTrue();
        service.IsLeader.Should().BeTrue();
    }

    [Fact]
    public async Task TryAcquireLeadershipAsync_FirstLeaderRetains_LeadershipOnRenew()
    {
        // Arrange
        var service1 = CreateService("node-1");

        // Act - Node 1 acquires leadership
        var firstAcquisition = await service1.TryAcquireLeadershipAsync();

        // Node 1 renews leadership (lease not expired)
        var renewed = await service1.TryAcquireLeadershipAsync();

        // Assert
        firstAcquisition.Should().BeTrue();
        renewed.Should().BeTrue();
        service1.IsLeader.Should().BeTrue();
    }

    [Fact]
    public async Task TryAcquireLeadershipAsync_SecondNode_CannotBecomeLeaderWhileFirstHolds()
    {
        // Arrange
        var service1 = CreateService("node-1");
        var service2 = CreateService("node-2");

        // Act
        var node1Acquired = await service1.TryAcquireLeadershipAsync();
        var node2Acquired = await service2.TryAcquireLeadershipAsync();

        // Assert
        node1Acquired.Should().BeTrue();
        node2Acquired.Should().BeFalse();
        service1.IsLeader.Should().BeTrue();
        service2.IsLeader.Should().BeFalse();
    }

    [Fact]
    public async Task IsLeader_ReflectsAcquisitionResult()
    {
        // Arrange
        var service = CreateService("node-1");

        // Act - Before acquisition
        var beforeAcquisition = service.IsLeader;

        // Acquire leadership
        await service.TryAcquireLeadershipAsync();
        var afterAcquisition = service.IsLeader;

        // Assert
        beforeAcquisition.Should().BeFalse();
        afterAcquisition.Should().BeTrue();
    }

    [Fact]
    public async Task ReleaseLeadershipAsync_AllowsOtherNodeToBecomeLeader()
    {
        // Arrange
        var service1 = CreateService("node-1");
        var service2 = CreateService("node-2");

        // Act
        await service1.TryAcquireLeadershipAsync();
        await service1.ReleaseLeadershipAsync();

        var node2Acquired = await service2.TryAcquireLeadershipAsync();

        // Assert
        node2Acquired.Should().BeTrue();
        service2.IsLeader.Should().BeTrue();
    }

    [Fact]
    public async Task LeaseExpiration_AllowsNewLeaderElection()
    {
        // Arrange - Create context with very short lease duration
        var shortLeaseContext = new JobSchedulerContext(_dbOptions);
        var service1 = new DatabaseLeaderElectionService(shortLeaseContext, "node-1", leaseDurationSeconds: 1);
        var service2 = new DatabaseLeaderElectionService(_context!, "node-2", leaseDurationSeconds: 1);

        // Act
        await service1.TryAcquireLeadershipAsync();

        // Wait for lease to expire
        await Task.Delay(1500);

        var service2Acquired = await service2.TryAcquireLeadershipAsync();

        // Assert
        service2Acquired.Should().BeTrue();
        service2.IsLeader.Should().BeTrue();

        await shortLeaseContext.DisposeAsync();
    }

    [Fact]
    public async Task Constructor_WithCustomInstanceId_UsesProvidedId()
    {
        // Arrange
        var customInstanceId = "custom-instance-123";
        var service = CreateService(customInstanceId);

        // Act
        var acquired = await service.TryAcquireLeadershipAsync();

        // Assert
        acquired.Should().BeTrue();
        var lockRow = await _context!.SchedulerLeaderLocks
            .FirstOrDefaultAsync(r => r.LockName == SchedulerLeaderLock.DefaultLockName);
        lockRow?.LeaderInstanceId.Should().Be(customInstanceId);
    }

    [Fact]
    public async Task Constructor_WithoutInstanceId_UsesEnvironmentMachineName()
    {
        // Arrange
        var service = CreateService(null);

        // Act
        var acquired = await service.TryAcquireLeadershipAsync();

        // Assert
        acquired.Should().BeTrue();
        var lockRow = await _context!.SchedulerLeaderLocks
            .FirstOrDefaultAsync(r => r.LockName == SchedulerLeaderLock.DefaultLockName);
        lockRow?.LeaderInstanceId.Should().Be(Environment.MachineName);
    }

    [Fact]
    public async Task MultipleNodes_SerialAcquisitionAndRelease()
    {
        // Arrange
        var services = Enumerable.Range(1, 3)
            .Select(i => CreateService($"node-{i}"))
            .ToList();

        // Act & Assert
        foreach (var (service, index) in services.Select((s, i) => (s, i)))
        {
            // Previous node should release
            if (index > 0)
            {
                await services[index - 1].ReleaseLeadershipAsync();
            }

            // Current node should acquire
            var acquired = await service.TryAcquireLeadershipAsync();
            acquired.Should().BeTrue();
            service.IsLeader.Should().BeTrue();
        }
    }

    [Fact]
    public async Task LeadershipLock_PersistsInDatabase()
    {
        // Arrange
        var service = CreateService("persistent-node");

        // Act
        await service.TryAcquireLeadershipAsync();

        // Assert
        var lockRow = await _context!.SchedulerLeaderLocks
            .FirstOrDefaultAsync(r => r.LockName == SchedulerLeaderLock.DefaultLockName);

        lockRow.Should().NotBeNull();
        lockRow!.LeaderInstanceId.Should().Be("persistent-node");
        lockRow.LeaseExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task ConcurrentAcquisitionAttempts_OnlyOneSucceeds()
    {
        // Arrange
        var services = Enumerable.Range(1, 5)
            .Select(i => CreateService($"node-{i}"))
            .ToList();

        // Act - All nodes try to acquire simultaneously
        var acquisitionTasks = services
            .Select(s => s.TryAcquireLeadershipAsync())
            .ToList();

        var results = await Task.WhenAll(acquisitionTasks);

        // Assert - Only one should succeed
        var successCount = results.Count(r => r);
        successCount.Should().Be(1);

        // Exactly one node should have IsLeader = true
        var leadersCount = services.Count(s => s.IsLeader);
        leadersCount.Should().Be(1);
    }

    [Fact]
    public async Task LeaderLeaseExpiry_AllowsAnotherNodeToTakeOver()
    {
        // Arrange - Create two services with very short lease duration using separate contexts
        var shortLeaseContext1 = new JobSchedulerContext(_dbOptions);
        var shortLeaseContext2 = new JobSchedulerContext(_dbOptions);
        var service1 = new DatabaseLeaderElectionService(shortLeaseContext1, "node-1", leaseDurationSeconds: 1);
        var service2 = new DatabaseLeaderElectionService(shortLeaseContext2, "node-2", leaseDurationSeconds: 1);

        // Act - Node 1 acquires leadership
        var node1Acquired = await service1.TryAcquireLeadershipAsync();
        node1Acquired.Should().BeTrue();
        service1.IsLeader.Should().BeTrue();

        // Wait for lease to expire
        await Task.Delay(1500);

        // Node 2 should be able to take over
        var node2Acquired = await service2.TryAcquireLeadershipAsync();
        node2Acquired.Should().BeTrue();
        service2.IsLeader.Should().BeTrue();

        // Verify in database that node 2 is now the leader
        var lockRow = await shortLeaseContext2.SchedulerLeaderLocks
            .FirstOrDefaultAsync(r => r.LockName == SchedulerLeaderLock.DefaultLockName);
        lockRow.Should().NotBeNull();
        lockRow!.LeaderInstanceId.Should().Be("node-2");

        await shortLeaseContext1.DisposeAsync();
        await shortLeaseContext2.DisposeAsync();
    }

    [Fact]
    public async Task CurrentLeaderRenewsBeforeExpiry_KeepsLeadership()
    {
        // Arrange - Create service with short lease duration
        var shortLeaseContext = new JobSchedulerContext(_dbOptions);
        var service = new DatabaseLeaderElectionService(shortLeaseContext, "node-1", leaseDurationSeconds: 5);

        // Act - Node acquires leadership
        var firstAcquisition = await service.TryAcquireLeadershipAsync();
        firstAcquisition.Should().BeTrue();
        service.IsLeader.Should().BeTrue();

        // Get initial lease expiry
        var initialLockRow = await shortLeaseContext.SchedulerLeaderLocks
            .FirstOrDefaultAsync(r => r.LockName == SchedulerLeaderLock.DefaultLockName);
        initialLockRow.Should().NotBeNull();
        var initialExpiry = initialLockRow!.LeaseExpiresAt;

        // Wait but before lease expires
        await Task.Delay(1000);

        // Renew leadership
        var renewed = await service.TryAcquireLeadershipAsync();
        renewed.Should().BeTrue();
        service.IsLeader.Should().BeTrue();

        // Verify lease was extended
        var updatedLockRow = await shortLeaseContext.SchedulerLeaderLocks
            .FirstOrDefaultAsync(r => r.LockName == SchedulerLeaderLock.DefaultLockName);
        updatedLockRow.Should().NotBeNull();
        updatedLockRow!.LeaseExpiresAt.Should().BeAfter(initialExpiry);

        await shortLeaseContext.DisposeAsync();
    }

    [Fact]
    public async Task FollowerRepeatedlyFailingToAcquire_StaysFollower()
    {
        // Arrange - Create multiple services
        var service1 = CreateService("node-1");
        var service2 = CreateService("node-2");
        var service3 = CreateService("node-3");

        // Act - Node 1 acquires leadership
        var node1Acquired = await service1.TryAcquireLeadershipAsync();
        node1Acquired.Should().BeTrue();
        service1.IsLeader.Should().BeTrue();
        service2.IsLeader.Should().BeFalse();
        service3.IsLeader.Should().BeFalse();

        // Act - Nodes 2 and 3 repeatedly try to acquire but fail
        for (int i = 0; i < 5; i++)
        {
            var node2Attempt = await service2.TryAcquireLeadershipAsync();
            var node3Attempt = await service3.TryAcquireLeadershipAsync();

            node2Attempt.Should().BeFalse();
            node3Attempt.Should().BeFalse();
            service2.IsLeader.Should().BeFalse();
            service3.IsLeader.Should().BeFalse();
        }
    }

    [Fact]
    public async Task LosingLeadership_IsObservableViaIsLeaderFlag()
    {
        // Arrange
        var service1 = CreateService("node-1");
        var service2 = CreateService("node-2");

        // Act - Node 1 acquires leadership
        var node1Acquired = await service1.TryAcquireLeadershipAsync();
        node1Acquired.Should().BeTrue();
        service1.IsLeader.Should().BeTrue();

        // Node 2 cannot acquire while node 1 is leader
        var node2Attempt = await service2.TryAcquireLeadershipAsync();
        node2Attempt.Should().BeFalse();
        service2.IsLeader.Should().BeFalse();

        // Node 1 releases leadership
        await service1.ReleaseLeadershipAsync();
        service1.IsLeader.Should().BeFalse();

        // Node 2 can now acquire
        var node2Acquired = await service2.TryAcquireLeadershipAsync();
        node2Acquired.Should().BeTrue();
        service2.IsLeader.Should().BeTrue();
    }

    [Fact]
    public async Task LeaderElection_WithExpiredLease_UpdatesLeaderInstanceId()
    {
        // Arrange - Create context with expired lease
        var expiredContext = new JobSchedulerContext(_dbOptions);
        var expiredLock = new SchedulerLeaderLock
        {
            LockName = SchedulerLeaderLock.DefaultLockName,
            LeaderInstanceId = "old-leader",
            LeaseExpiresAt = DateTime.UtcNow.AddSeconds(-10), // Already expired
            AcquiredAt = DateTime.UtcNow.AddMinutes(-1)
        };
        expiredContext.SchedulerLeaderLocks.Add(expiredLock);
        await expiredContext.SaveChangesAsync();

        var service1 = new DatabaseLeaderElectionService(expiredContext, "new-leader", leaseDurationSeconds: 30);

        // Act - New leader should take over expired lease
        var acquired = await service1.TryAcquireLeadershipAsync();
        acquired.Should().BeTrue();
        service1.IsLeader.Should().BeTrue();

        // Assert - Leader instance ID should be updated
        var lockRow = await expiredContext.SchedulerLeaderLocks
            .FirstOrDefaultAsync(r => r.LockName == SchedulerLeaderLock.DefaultLockName);
        lockRow.Should().NotBeNull();
        lockRow!.LeaderInstanceId.Should().Be("new-leader");
        lockRow.LeaseExpiresAt.Should().BeAfter(DateTime.UtcNow);

        await expiredContext.DisposeAsync();
    }

    [Fact]
    public async Task LeadershipTransition_UpdatesLeadershipFlagsCorrectly()
    {
        // Arrange
        var service1 = CreateService("node-1");
        var service2 = CreateService("node-2");

        // Act - Node 1 becomes leader
        var node1Acquired = await service1.TryAcquireLeadershipAsync();
        node1Acquired.Should().BeTrue();
        service1.IsLeader.Should().BeTrue();
        service2.IsLeader.Should().BeFalse();

        // Node 2 attempts to acquire (should fail)
        var node2Attempt = await service2.TryAcquireLeadershipAsync();
        node2Attempt.Should().BeFalse();
        service2.IsLeader.Should().BeFalse();

        // Node 1 releases
        await service1.ReleaseLeadershipAsync();
        service1.IsLeader.Should().BeFalse();

        // Node 2 acquires
        var node2Acquired = await service2.TryAcquireLeadershipAsync();
        node2Acquired.Should().BeTrue();
        service2.IsLeader.Should().BeTrue();

        // Node 1 attempts to acquire again (should fail)
        var node1SecondAttempt = await service1.TryAcquireLeadershipAsync();
        node1SecondAttempt.Should().BeFalse();
        service1.IsLeader.Should().BeFalse();
    }
}
