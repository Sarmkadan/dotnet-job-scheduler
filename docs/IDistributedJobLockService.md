# `IDistributedJobLockService`

`IDistributedJobLockService`, defined in `src/JobScheduler.Core/Services/IDistributedJobLockService.cs`, provides distributed, database-backed per-job locking for multi-instance deployments. It prevents two scheduler nodes from executing the same job concurrently when their in-process `ConcurrencyManager` state is not shared.

The interface is registered with a scoped lifetime in `DependencyInjectionExtensions.AddJobScheduler`:

```csharp
services.AddScoped<IDistributedJobLockService, DistributedJobLockService>();
```

## Methods

### `TryAcquireLockAsync`

```csharp
Task<bool> TryAcquireLockAsync(
    Guid jobId,
    string holderInstanceId,
    TimeSpan lockDuration,
    CancellationToken cancellationToken = default);
```

> Attempts to acquire an exclusive lock for `jobId`. Returns `true` when the lock is granted; `false` when another instance already holds a valid (non-expired) lock.

Parameters:

- `jobId`: The job to lock.
- `holderInstanceId`: Unique ID of the calling scheduler instance. `DistributedJobLockService` rejects null, empty, or whitespace values with `ArgumentException`.
- `lockDuration`: How long the lock remains valid without renewal. `DistributedJobLockService` requires a value greater than zero and throws `ArgumentException` otherwise.
- `cancellationToken`: Cancellation token passed to the database operations. Defaults to `default`.

Returns a `Task<bool>` whose result is `true` if the caller acquired the lock, renewed its own existing row, or took over an expired lock. It returns `false` if another holder has a non-expired lock or if a concurrent database insert causes a `DbUpdateException`.

### `ReleaseLockAsync`

```csharp
Task ReleaseLockAsync(
    Guid jobId,
    string holderInstanceId,
    CancellationToken cancellationToken = default);
```

> Releases the lock held by `holderInstanceId` for `jobId`. No-ops when the caller does not hold the lock or the lock has already expired.

Parameters:

- `jobId`: The job whose lock should be released.
- `holderInstanceId`: Unique ID of the calling scheduler instance.
- `cancellationToken`: Cancellation token passed to the database operations. Defaults to `default`.

Returns a non-generic `Task` that completes after the matching lock row is removed and the change is saved. If no row exists or the holder does not match, the implementation returns without changing the database. The current implementation removes a matching holder's row even when that row is expired; it does not perform a separate expiry check during release.

### `IsLockedAsync`

```csharp
Task<bool> IsLockedAsync(
    Guid jobId,
    CancellationToken cancellationToken = default);
```

> Returns `true` when a valid (non-expired) lock exists for `jobId`.

Parameters:

- `jobId`: The job whose lock state is queried.
- `cancellationToken`: Cancellation token passed to the database query. Defaults to `default`.

Returns a `Task<bool>` whose result is `true` only when a stored lock exists and its expiry is later than the current UTC time; otherwise it returns `false`.

### `RenewLockAsync`

```csharp
Task<bool> RenewLockAsync(
    Guid jobId,
    string holderInstanceId,
    TimeSpan lockDuration,
    CancellationToken cancellationToken = default);
```

> Renews the expiry of an existing lock held by `holderInstanceId`. Returns `true` when the renewal succeeded; `false` when the lock does not exist or belongs to a different holder.

Parameters:

- `jobId`: The job whose lock should be renewed.
- `holderInstanceId`: Unique ID of the scheduler instance that must currently own the lock.
- `lockDuration`: Duration added to the current UTC time to calculate the new expiry.
- `cancellationToken`: Cancellation token passed to the database operations. Defaults to `default`.

Returns a `Task<bool>` whose result is `true` after the expiry is updated and saved. It returns `false` if the lock is missing, belongs to another holder, or is already expired. Unlike `TryAcquireLockAsync`, the current implementation does not validate that `holderInstanceId` is nonblank or that `lockDuration` is positive.

### `GetActiveLocksAsync`

```csharp
Task<IReadOnlyList<DistributedJobLock>> GetActiveLocksAsync(
    CancellationToken cancellationToken = default);
```

> Returns all active (non-expired) locks in the system.

Parameters:

- `cancellationToken`: Cancellation token passed to the database query. Defaults to `default`.

Returns a `Task<IReadOnlyList<DistributedJobLock>>`. The result contains rows whose `ExpiresAt` is later than the current UTC time, ordered by `AcquiredAt` in ascending order. Each item exposes the job ID, holder instance ID, acquisition time, and expiry time.

### `CleanExpiredLocksAsync`

```csharp
Task<int> CleanExpiredLocksAsync(
    CancellationToken cancellationToken = default);
```

> Removes all expired lock entries from the database. Should be called periodically to prevent table growth.

Parameters:

- `cancellationToken`: Cancellation token passed to the database operations. Defaults to `default`.

Returns a `Task<int>` whose result is the number of expired locks that were removed. Locks with `ExpiresAt` equal to or earlier than the current UTC time are expired. If none exist, the method returns `0` without calling `SaveChangesAsync`.

## `DistributedJobLockService` implementation

`DistributedJobLockService` uses the scoped `JobSchedulerContext` and an optional `ILogger<DistributedJobLockService>`. Lock timestamps are calculated with `DateTime.UtcNow`, and lock state is stored in the context's `DistributedJobLocks` set.

Acquisition first queries by `JobId`:

- If no row exists, it inserts a lock containing the job, holder, acquisition time, and expiry.
- If the row already belongs to the caller, it extends the expiry and returns `true`.
- If another holder's row is expired, it transfers ownership, resets `AcquiredAt`, and sets a new expiry.
- If another holder's row is still valid, it returns `false`.

The database's per-job uniqueness constraint arbitrates concurrent inserts. `DbUpdateException` during acquisition is treated as a lost acquisition race and produces `false`. Other database exceptions propagate. Renewal and release both require the stored holder ID to match the caller. Active-lock queries ignore expired rows, while cleanup physically deletes those rows.

Because the service is scoped with its EF Core context, each scheduler operation should resolve it within an appropriate dependency-injection scope. All scheduler instances must use the same scheduler database for the lock to be distributed across them.

## Lock, renew, and release example

Use a stable, unique instance ID for the scheduler process. Always release in a `finally` block after successful acquisition, and renew before the current lease expires when work may outlast it.

```csharp
public static async Task RunJobAsync(
    IDistributedJobLockService lockService,
    Guid jobId,
    string schedulerInstanceId,
    CancellationToken cancellationToken)
{
    var leaseDuration = TimeSpan.FromMinutes(2);
    var acquired = await lockService.TryAcquireLockAsync(
        jobId,
        schedulerInstanceId,
        leaseDuration,
        cancellationToken);

    if (!acquired)
    {
        // Another scheduler instance currently owns this job's lock.
        return;
    }

    try
    {
        await PerformFirstStageAsync(jobId, cancellationToken);

        var renewed = await lockService.RenewLockAsync(
            jobId,
            schedulerInstanceId,
            leaseDuration,
            cancellationToken);

        if (!renewed)
        {
            // The lease expired or ownership was lost; do not continue protected work.
            return;
        }

        await PerformSecondStageAsync(jobId, cancellationToken);
    }
    finally
    {
        await lockService.ReleaseLockAsync(
            jobId,
            schedulerInstanceId,
            cancellationToken);
    }
}
```

Choose a lease duration long enough to cover work between renewals. A `true` renewal extends the lease from the renewal time, not from its previous expiry.
