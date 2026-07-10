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
/// Extension methods for DatabaseLeaderElectionServiceTests providing additional test scenarios
/// and helper methods for working with leader election services.
/// </summary>
public static class DatabaseLeaderElectionServiceTestsExtensions
{
    /// <summary>
    /// Creates a new DatabaseLeaderElectionService instance with a fresh in-memory database context.
    /// </summary>
    /// <param name="test">The test instance to create the service for.</param>
    /// <param name="instanceId">Optional instance identifier. If null, uses "test-node-{Guid}".</param>
    /// <param name="leaseDurationSeconds">Lease duration in seconds. Defaults to 30.</param>
    /// <returns>A new DatabaseLeaderElectionService instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if test is null.</exception>
    public static DatabaseLeaderElectionService CreateIsolatedService(
        this DatabaseLeaderElectionServiceTests test,
        string? instanceId = null,
        int leaseDurationSeconds = 30)
    {
        ArgumentNullException.ThrowIfNull(test);

        var options = new DbContextOptionsBuilder<JobSchedulerContext>()
            .UseInMemoryDatabase($"LeaderElectionTest_Isolated_{Guid.NewGuid()}")
            .Options;

        var context = new JobSchedulerContext(options);
        return new DatabaseLeaderElectionService(context, instanceId ?? $"test-node-{Guid.NewGuid()}", leaseDurationSeconds);
    }

    /// <summary>
    /// Creates a pair of DatabaseLeaderElectionService instances sharing the same database context.
    /// Useful for testing scenarios where services need to coordinate through the same database.
    /// </summary>
    /// <param name="test">The test instance to create the services for.</param>
    /// <param name="instanceId1">Instance ID for the first service. Defaults to "node-1".</param>
    /// <param name="instanceId2">Instance ID for the second service. Defaults to "node-2".</param>
    /// <param name="leaseDurationSeconds">Lease duration in seconds. Defaults to 30.</param>
    /// <returns>A tuple containing both DatabaseLeaderElectionService instances.</returns>
    /// <exception cref="ArgumentNullException">Thrown if test is null.</exception>
    public static (DatabaseLeaderElectionService service1, DatabaseLeaderElectionService service2) CreateServicePair(
        this DatabaseLeaderElectionServiceTests test,
        string? instanceId1 = null,
        string? instanceId2 = null,
        int leaseDurationSeconds = 30)
    {
        ArgumentNullException.ThrowIfNull(test);

        var options = new DbContextOptionsBuilder<JobSchedulerContext>()
            .UseInMemoryDatabase($"LeaderElectionTest_Pair_{Guid.NewGuid()}")
            .Options;

        var context = new JobSchedulerContext(options);
        var service1 = new DatabaseLeaderElectionService(context, instanceId1 ?? "node-1", leaseDurationSeconds);
        var service2 = new DatabaseLeaderElectionService(context, instanceId2 ?? "node-2", leaseDurationSeconds);

        return (service1, service2);
    }

    /// <summary>
    /// Gets the current leader lock information from the database.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <param name="context">The database context to query.</param>
    /// <returns>The leader lock row if it exists, null otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown if test or context is null.</exception>
    public static async Task<SchedulerLeaderLock?> GetCurrentLeaderLockAsync(
        this DatabaseLeaderElectionServiceTests test,
        JobSchedulerContext context)
    {
        ArgumentNullException.ThrowIfNull(test);
        ArgumentNullException.ThrowIfNull(context);

        return await context.SchedulerLeaderLocks
            .FirstOrDefaultAsync(l => l.LockName == SchedulerLeaderLock.DefaultLockName);
    }

    /// <summary>
    /// Gets all leader lock information from the database.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <param name="context">The database context to query.</param>
    /// <returns>A read-only list of all leader lock rows.</returns>
    /// <exception cref="ArgumentNullException">Thrown if test or context is null.</exception>
    public static async Task<IReadOnlyList<SchedulerLeaderLock>> GetAllLeaderLocksAsync(
        this DatabaseLeaderElectionServiceTests test,
        JobSchedulerContext context)
    {
        ArgumentNullException.ThrowIfNull(test);
        ArgumentNullException.ThrowIfNull(context);

        return await context.SchedulerLeaderLocks
            .ToListAsync();
    }

    /// <summary>
    /// Creates a service with a very short lease duration for testing lease expiration scenarios.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <param name="instanceId">Optional instance identifier. Defaults to "short-lease-node".</param>
    /// <param name="leaseDurationSeconds">Lease duration in seconds. Defaults to 1.</param>
    /// <returns>A DatabaseLeaderElectionService with short lease duration.</returns>
    /// <exception cref="ArgumentNullException">Thrown if test is null.</exception>
    public static DatabaseLeaderElectionService CreateShortLeaseService(
        this DatabaseLeaderElectionServiceTests test,
        string? instanceId = null,
        int leaseDurationSeconds = 1)
    {
        ArgumentNullException.ThrowIfNull(test);

        var options = new DbContextOptionsBuilder<JobSchedulerContext>()
            .UseInMemoryDatabase($"LeaderElectionTest_ShortLease_{Guid.NewGuid()}")
            .Options;

        var context = new JobSchedulerContext(options);
        return new DatabaseLeaderElectionService(context, instanceId ?? "short-lease-node", leaseDurationSeconds);
    }

    /// <summary>
    /// Gets the leadership acquisition history for a service instance.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <param name="context">The database context to query.</param>
    /// <param name="instanceId">The instance ID to look up.</param>
    /// <returns>A read-only list of leadership acquisition records.</returns>
    /// <exception cref="ArgumentNullException">Thrown if test or context is null.</exception>
    public static async Task<IReadOnlyList<SchedulerLeaderLock>> GetLeadershipHistoryAsync(
        this DatabaseLeaderElectionServiceTests test,
        JobSchedulerContext context,
        string instanceId)
    {
        ArgumentNullException.ThrowIfNull(test);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrEmpty(instanceId);

        return await context.SchedulerLeaderLocks
            .Where(a => a.LeaderInstanceId == instanceId)
            .OrderBy(a => a.LeaseExpiresAt)
            .ToListAsync();
    }

    /// <summary>
    /// Verifies that a service has successfully acquired leadership.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <param name="service">The service to verify.</param>
    /// <param name="expectedInstanceId">The expected instance ID that should be leader.</param>
    /// <returns>Task for async completion.</returns>
    /// <exception cref="ArgumentNullException">Thrown if test or service is null.</exception>
    /// <exception cref="ArgumentException">Thrown if expectedInstanceId is null or empty.</exception>
    public static async Task AssertHasLeadershipAsync(
        this DatabaseLeaderElectionServiceTests test,
        DatabaseLeaderElectionService service,
        string expectedInstanceId)
    {
        ArgumentNullException.ThrowIfNull(test);
        ArgumentNullException.ThrowIfNull(service);
        ArgumentException.ThrowIfNullOrEmpty(expectedInstanceId);

        var acquired = await service.TryAcquireLeadershipAsync();
        acquired.Should().BeTrue($"Service should be able to acquire leadership");
        service.IsLeader.Should().BeTrue($"Service should be leader after acquisition");

        var context = service.GetContext();
        var lockRow = await test.GetCurrentLeaderLockAsync(context);
        lockRow.Should().NotBeNull($"Leader lock should exist in database");
        lockRow!.LeaderInstanceId.Should().Be(expectedInstanceId, $"Leader instance ID should match expected value");
    }

    /// <summary>
    /// Verifies that a service has released leadership.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <param name="service">The service to verify.</param>
    /// <param name="expectedInstanceId">The instance ID that should no longer be leader.</param>
    /// <returns>Task for async completion.</returns>
    /// <exception cref="ArgumentNullException">Thrown if test or service is null.</exception>
    /// <exception cref="ArgumentException">Thrown if expectedInstanceId is null or empty.</exception>
    public static async Task AssertHasReleasedLeadershipAsync(
        this DatabaseLeaderElectionServiceTests test,
        DatabaseLeaderElectionService service,
        string expectedInstanceId)
    {
        ArgumentNullException.ThrowIfNull(test);
        ArgumentNullException.ThrowIfNull(service);
        ArgumentException.ThrowIfNullOrEmpty(expectedInstanceId);

        await service.ReleaseLeadershipAsync();
        service.IsLeader.Should().BeFalse($"Service should not be leader after releasing leadership");

        var context = service.GetContext();
        var lockRow = await test.GetCurrentLeaderLockAsync(context);
        lockRow.Should().BeNull($"Leader lock should be removed from database after release");
    }

    /// <summary>
    /// Gets the underlying database context from a service.
    /// </summary>
    /// <param name="service">The service instance.</param>
    /// <returns>The JobSchedulerContext instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if service is null.</exception>
    public static JobSchedulerContext GetContext(this DatabaseLeaderElectionService service)
    {
        ArgumentNullException.ThrowIfNull(service);

        // Use reflection to access the private _context field
        var field = typeof(DatabaseLeaderElectionService).GetField("_context", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (JobSchedulerContext)field!.GetValue(service)!;
    }
}