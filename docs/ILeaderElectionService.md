# ILeaderElectionService

`ILeaderElectionService` defines the distributed-leadership contract used to identify the single scheduler node that should fire jobs at each scheduled interval in a multi-instance deployment. The interface is declared in `src/JobScheduler.Core/Services/ILeaderElectionService.cs`.

## Contract

| Member | Semantics |
| --- | --- |
| `bool IsLeader { get; }` | Returns `true` when the current instance presently holds the leader lease. |
| `Task<bool> TryAcquireLeadershipAsync(CancellationToken cancellationToken = default)` | Attempts to acquire or renew the leader lease. The hosting background service is expected to call it periodically. The result indicates whether the instance acquired or retained leadership. |
| `Task ReleaseLeadershipAsync(CancellationToken cancellationToken = default)` | Releases the leader lease so that another node can take over immediately. It is intended to be called during graceful shutdown. |

Both asynchronous methods accept an optional cancellation token.

## DatabaseLeaderElectionService

`DatabaseLeaderElectionService` is the database-backed implementation. It uses `JobSchedulerContext.SchedulerLeaderLocks` and one `SchedulerLeaderLock` row selected by `SchedulerLeaderLock.DefaultLockName`.

The constructor accepts:

- A required `JobSchedulerContext`.
- An optional instance identifier. A null, empty, or whitespace value becomes `Environment.MachineName`.
- A lease duration in seconds. The default is 30 seconds, and a non-positive value also becomes 30 seconds.
- An optional logger.

`TryAcquireLeadershipAsync` reads the default lock row and then follows these code paths:

1. If the row does not exist, it creates one owned by the current instance, with acquisition time set to the current UTC time and expiration set to that time plus the lease duration.
2. If the current instance already owns the row, it renews `LeaseExpiresAt` without changing `AcquiredAt`.
3. If another instance owns an unexpired lease, it returns `false` and clears the in-memory leader flag.
4. If another instance's lease has expired, it replaces the owner, expiration, and acquisition time with values for the current instance.

Successful creation, renewal, or takeover sets `IsLeader` to `true` and returns `true`. An exception during acquisition or renewal is logged as a warning when a logger is available, sets `IsLeader` to `false`, and returns `false` rather than propagating the exception.

`ReleaseLeadershipAsync` returns immediately when the service's in-memory leader flag is already `false`. Otherwise, it looks for the default lock row owned by the current instance and, if found, sets its expiration to one second before the current UTC time. It then clears the in-memory leader flag. Exceptions are logged when possible and are not rethrown; on that exception path, the code does not explicitly clear the flag.

The implementation is registered as a scoped `ILeaderElectionService` by `AddJobScheduler` only when `SchedulerOptions.EnableLeaderElection` is enabled. Registration supplies the configured instance identifier and lease duration, falling back to the machine name for a blank configured identifier.

## SchedulerLeaderLock

`SchedulerLeaderLock` is the EF Core entity representing the distributed leader-lock row.

| Member | Meaning |
| --- | --- |
| `DefaultLockName` | Constant value `"scheduler-leader"`; this is the lock name used by `DatabaseLeaderElectionService`. |
| `Id` | Integer primary key. |
| `LockName` | Identifies the logical lock and defaults to `DefaultLockName`. The EF model makes it required, limits it to 128 characters, and gives it a unique index. |
| `LeaderInstanceId` | Identifies the node that owns the lease. It defaults to an empty string; the EF model makes it required and limits it to 256 characters. |
| `LeaseExpiresAt` | UTC expiration value used to decide whether another instance may take over. |
| `AcquiredAt` | UTC time at which the current ownership was acquired. |

## Use in SchedulerHostedService

As currently written in `src/JobScheduler.Core/Program.cs`, `SchedulerHostedService` does **not** use `ILeaderElectionService`. Its execution loop creates a dependency-injection scope, resolves `JobSchedulerService`, executes due jobs, processes retries, optionally logs statistics, and waits five seconds. It does not resolve the leader-election service, call `TryAcquireLeadershipAsync`, inspect `IsLeader`, gate either scheduling operation on leadership, renew a lease, or call `ReleaseLeadershipAsync` during graceful shutdown.

`Program.CreateHostBuilder` calls `AddJobScheduler` without setting `EnableLeaderElection`, then registers `SchedulerHostedService`. Consequently, no leadership behavior for the hosted-service loop is visible in `Program.cs`; the interface XML documentation describes the intended hosting-service call pattern, but that pattern is not wired into this hosted service in the current code.
