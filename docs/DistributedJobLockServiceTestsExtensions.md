# DistributedJobLockServiceTestsExtensions

Utility class providing static factory and assertion helpers for testing code that interacts with `DistributedJobLockService`. Designed to simplify test setup, lock acquisition checks, and lock state validation in unit and integration tests.

## API

### `public static DistributedJobLockService CreateFreshService()`

Creates a new `DistributedJobLockService` instance with a fresh, isolated context for testing. The service is configured with default in-memory or test-friendly dependencies suitable for isolated test environments.

- **Parameters**: None
- **Return value**: A new `DistributedJobLockService` instance ready for testing.
- **Exceptions**: Throws if underlying dependencies cannot be initialized (e.g., database connection failure in integration tests).

---

### `public static JobSchedulerContext CreateFreshContext()`

Creates a new `JobSchedulerContext` with default or test-specific configuration. Useful for initializing test scenarios where a fresh context is required.

- **Parameters**: None
- **Return value**: A new `JobSchedulerContext` instance.
- **Exceptions**: Throws if context initialization fails due to invalid configuration or missing dependencies.

---

### `public static async Task<DistributedJobLock> ShouldAcquireLockAsync(DistributedJobLockService service, string jobId, string instanceId, TimeSpan? leaseTime = null)`

Asserts that a lock can be acquired for the given job and instance. Returns the acquired lock for further inspection.

- **Parameters**:
  - `service`: The `DistributedJobLockService` instance to use.
  - `jobId`: The identifier of the job attempting to acquire the lock.
  - `instanceId`: The unique identifier of the job instance.
  - `leaseTime` (optional): The desired lease duration for the lock. If `null`, uses the service default.
- **Return value**: The acquired `DistributedJobLock` instance.
- **Exceptions**: Throws if the lock cannot be acquired (e.g., already held by another instance), or if `service` is `null`.

---

### `public static async Task<int> ShouldNotAcquireLockAsync(DistributedJobLockService service, string jobId, string instanceId, TimeSpan? leaseTime = null)`

Asserts that a lock **cannot** be acquired for the given job and instance. Returns the number of existing locks that prevented acquisition (typically 1 in single-instance scenarios).

- **Parameters**:
  - `service`: The `DistributedJobLockService` instance to use.
  - `jobId`: The identifier of the job attempting to acquire the lock.
  - `instanceId`: The unique identifier of the job instance.
  - `leaseTime` (optional): The desired lease duration for the lock. If `null`, uses the service default.
- **Return value**: The count of conflicting locks that prevented acquisition.
- **Exceptions**: Throws if `service` is `null`.

---

### `public static async Task<DistributedJobLock> ShouldBeLockedAsync(DistributedJobLockService service, string jobId)`

Asserts that a lock currently exists for the specified job. Returns the active lock.

- **Parameters**:
  - `service`: The `DistributedJobLockService` instance to use.
  - `jobId`: The identifier of the job to check.
- **Return value**: The active `DistributedJobLock` instance for the job.
- **Exceptions**: Throws if no lock exists for `jobId`, or if `service` is `null`.

---
### `public static async Task<int> ShouldNotBeLockedAsync(DistributedJobLockService service, string jobId)`

Asserts that no lock exists for the specified job. Returns the count of locks found (expected to be 0).

- **Parameters**:
  - `service`: The `DistributedJobLockService` instance to use.
  - `jobId`: The identifier of the job to check.
- **Return value**: The number of locks found (should be 0).
- **Exceptions**: Throws if `service` is `null`.

---
### `public static async Task<IReadOnlyList<DistributedJobLock>> CreateLocksAsync(DistributedJobLockService service, int count, string jobIdPrefix = "job")`

Creates multiple locks in sequence for testing concurrent or batch scenarios. Useful for simulating multiple job instances or load.

- **Parameters**:
  - `service`: The `DistributedJobLockService` instance to use.
  - `count`: The number of locks to create.
  - `jobIdPrefix` (optional): Prefix for generated job identifiers. Defaults to `"job"`.
- **Return value**: A read-only list of created `DistributedJobLock` instances.
- **Exceptions**: Throws if `service` is `null`, or if lock creation fails due to conflicts or timeouts.

---
### `public static async Task<DateTime> GetLockExpiryAsync(DistributedJobLockService service, string jobId)`

Retrieves the absolute expiry timestamp of the lock for the specified job.

- **Parameters**:
  - `service`: The `DistributedJobLockService` instance to use.
  - `jobId`: The identifier of the job whose lock expiry is to be checked.
- **Return value**: The UTC expiry timestamp of the lock.
- **Exceptions**: Throws if no lock exists for `jobId`, or if `service` is `null`.

## Usage

### Example 1: Basic lock acquisition and validation
