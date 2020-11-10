# DistributedJobLockServiceExtensions

Extension methods for working with distributed job locks in a .NET job scheduler environment. These methods provide a consistent way to acquire, check, and execute operations under distributed locks, with built-in retry logic for transient failures.

## API

### `TryAcquireLockWithRetryAsync`

Attempts to acquire a distributed lock with automatic retry logic. The method will retry according to the configured retry policy if the lock cannot be acquired immediately.

**Parameters:**
- `IDistributedJobLockService service`: The lock service instance.
- `string jobId`: The unique identifier of the job requesting the lock.
- `TimeSpan lockDuration`: The duration for which the lock should be held.
- `CancellationToken cancellationToken`: Optional cancellation token.

**Return value:**
Returns `Task<bool>` where `true` indicates the lock was successfully acquired, `false` otherwise.

**Exceptions:**
- Throws `ArgumentNullException` if `service` or `jobId` is null.
- Throws `ArgumentOutOfRangeException` if `lockDuration` is not positive.

---

### `ExecuteWithLockAsync`

Executes an action under a distributed lock, automatically acquiring and releasing the lock. The action will only execute if the lock can be acquired.

**Parameters:**
- `IDistributedJobLockService service`: The lock service instance.
- `string jobId`: The unique identifier of the job.
- `TimeSpan lockDuration`: The duration for which the lock should be held.
- `Func<CancellationToken, Task> action`: The action to execute under the lock.
- `CancellationToken cancellationToken`: Optional cancellation token.

**Return value:**
Returns `Task<bool>` where `true` indicates the action was executed under the lock, `false` otherwise.

**Exceptions:**
- Throws `ArgumentNullException` if `service`, `jobId`, or `action` is null.
- Throws `ArgumentOutOfRangeException` if `lockDuration` is not positive.

---

### `IsHeldByAsync`

Checks whether a specific job currently holds the distributed lock.

**Parameters:**
- `IDistributedJobLockService service`: The lock service instance.
- `string jobId`: The unique identifier of the job to check.
- `CancellationToken cancellationToken`: Optional cancellation token.

**Return value:**
Returns `Task<bool>` where `true` indicates the specified job holds the lock, `false` otherwise.

**Exceptions:**
- Throws `ArgumentNullException` if `service` or `jobId` is null.

---
### `GetRemainingLockTimeAsync`

Retrieves the remaining time for which the current lock is valid.

**Parameters:**
- `IDistributedJobLockService service`: The lock service instance.
- `string jobId`: The unique identifier of the job holding the lock.
- `CancellationToken cancellationToken`: Optional cancellation token.

**Return value:**
Returns `Task<TimeSpan>` representing the remaining lock duration. Returns `TimeSpan.Zero` if no lock is held by the specified job or if the lock has expired.

**Exceptions:**
- Throws `ArgumentNullException` if `service` or `jobId` is null.

## Usage

### Example 1: Basic lock acquisition with retry
