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
}
