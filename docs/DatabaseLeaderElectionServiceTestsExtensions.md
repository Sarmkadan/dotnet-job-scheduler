# DatabaseLeaderElectionServiceTestsExtensions

A static utility class providing factory methods and assertion helpers for `DatabaseLeaderElectionService` instances in test scenarios. It simplifies the creation of isolated or paired services with custom lease durations, exposes direct access to the underlying database context for verification, and offers convenience methods to inspect leader lock state and assert leadership acquisition or release.

## API

### CreateIsolatedService
```csharp
public static DatabaseLeaderElectionService CreateIsolatedService(
    this IServiceProvider serviceProvider,
    string instanceId,
    Action<DatabaseLeaderElectionOptions>? configureOptions = null)
```
Creates a single `DatabaseLeaderElectionService` scoped to a unique database context, ensuring no interference with other test instances. Accepts an optional delegate to customize lease duration, renewal intervals, and other election options. Returns a fully initialized service ready for leadership acquisition attempts.

### CreateServicePair
```csharp
public static (DatabaseLeaderElectionService service1, DatabaseLeaderElectionService service2) CreateServicePair(
    this IServiceProvider serviceProvider,
    string instanceId1,
    string instanceId2,
    Action<DatabaseLeaderElectionOptions>? configureOptions = null)
```
Creates two `DatabaseLeaderElectionService` instances sharing the same underlying database context, simulating two competing nodes. Both services are returned as a named tuple. Useful for testing leader election races, failover, and lock contention scenarios.

### GetCurrentLeaderLockAsync
```csharp
public static async Task<SchedulerLeaderLock?> GetCurrentLeaderLockAsync(
    this DatabaseLeaderElectionService service)
```
Queries the database directly for the currently active leader lock record associated with the given service. Returns the `SchedulerLeaderLock` if one exists and has not expired; otherwise returns `null`. Does not attempt to acquire or release any lock.

### GetAllLeaderLocksAsync
```csharp
public static async Task<IReadOnlyList<SchedulerLeaderLock>> GetAllLeaderLocksAsync(
    this DatabaseLeaderElectionService service)
```
Returns all leader lock records currently present in the database, regardless of instance or expiration status. Provides a full snapshot of the leader election table for diagnostics and multi-instance verification.

### CreateShortLeaseService
```csharp
public static DatabaseLeaderElectionService CreateShortLeaseService(
    this IServiceProvider serviceProvider,
    string instanceId)
```
Creates a `DatabaseLeaderElectionService` preconfigured with a very short lease duration, typically on the order of a few seconds. Designed for tests that need to observe lease expiration and automatic leadership release without long waits.

### GetLeadershipHistoryAsync
```csharp
public static async Task<IReadOnlyList<SchedulerLeaderLock>> GetLeadershipHistoryAsync(
    this DatabaseLeaderElectionService service)
```
Retrieves the complete history of leader lock records for the service's scope, including expired and released locks. Returns records ordered by acquisition time, oldest first.

### AssertHasLeadershipAsync
```csharp
public static async Task AssertHasLeadershipAsync(
    this DatabaseLeaderElectionService service)
```
Asserts that the given service currently holds an active, unexpired leader lock. Throws an exception if the service does not hold leadership at the time of invocation. This is a polling-style assertion and does not block waiting for leadership.

### AssertHasReleasedLeadershipAsync
```csharp
public static async Task AssertHasReleasedLeadershipAsync(
    this DatabaseLeaderElectionService service)
```
Asserts that the given service does not currently hold an active leader lock. Throws if a valid lock is still associated with the service. Useful for verifying that a service has successfully stepped down or its lease has expired.

### GetContext
```csharp
public static JobSchedulerContext GetContext(
    this DatabaseLeaderElectionService service)
```
Returns the underlying `JobSchedulerContext` used by the service. This allows direct inspection of the database state, entity tracking, or raw query execution in test code without relying on the service's public abstraction.

## Usage

### Example 1: Verifying leader election between two competing instances
```csharp
[Fact]
public async Task TwoServices_OnlyOneBecomesLeader()
{
    var (service1, service2) = _serviceProvider.CreateServicePair("instance-A", "instance-B");

    await service1.TryBecomeLeaderAsync();
    await service2.TryBecomeLeaderAsync();

    await service1.AssertHasLeadershipAsync();
    await service2.AssertHasReleasedLeadershipAsync();

    var activeLock = await service1.GetCurrentLeaderLockAsync();
    Assert.NotNull(activeLock);
    Assert.Equal("instance-A", activeLock.InstanceId);
}
```

### Example 2: Testing lease expiration with a short lease
```csharp
[Fact]
public async Task ShortLease_ExpiresAndAllowsNewLeader()
{
    var service = serviceProvider.CreateShortLeaseService("ephemeral-instance");
    await service.TryBecomeLeaderAsync();
    await service.AssertHasLeadershipAsync();

    // Wait for the short lease to expire
    await Task.Delay(TimeSpan.FromSeconds(10));

    await service.AssertHasReleasedLeadershipAsync();
    var history = await service.GetLeadershipHistoryAsync();
    Assert.Contains(history, l => l.InstanceId == "ephemeral-instance" && l.IsExpired);
}
```

## Notes

- All factory methods (`CreateIsolatedService`, `CreateServicePair`, `CreateShortLeaseService`) create new database contexts internally. Tests using them do not share state with other tests unless the same database connection string is explicitly reused.
- `CreateServicePair` shares a single database context between the two returned services. Concurrent calls to `TryBecomeLeaderAsync` from both services must be synchronized by the caller if deterministic ordering is required.
- `AssertHasLeadershipAsync` and `AssertHasReleasedLeadershipAsync` perform a single check at the moment of invocation. They do not poll or wait. Use a retry loop or delay in the test if the leadership state is expected to change asynchronously.
- `GetCurrentLeaderLockAsync` and `GetAllLeaderLocksAsync` bypass the service's internal caching and query the database directly. They reflect the committed state at the time of the query.
- `GetContext` exposes the raw `DbContext`. Modifications made through this context will affect the service's view of the database. Use with caution to avoid corrupting the service's internal state.
- None of the methods in this class are thread-safe by design. They are intended for sequential test execution. Concurrent access to the same service instance from multiple threads may produce unpredictable results.
