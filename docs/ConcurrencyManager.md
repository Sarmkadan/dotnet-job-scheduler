# ConcurrencyManager

The `ConcurrencyManager` class provides centralized control over concurrent job execution limits, both at the global level and per-job. It tracks active job executions, enforces concurrency thresholds, and synchronizes state with persistent storage to ensure consistency across application restarts.

## API

### `ConcurrencyManager`
Initializes a new instance of the concurrency manager. The instance coordinates concurrency tracking for jobs and global execution limits.

### `async Task<bool> CanExecuteAsync(string jobName, int maxConcurrency)`
Determines whether a job with the specified name can be executed given its maximum allowed concurrency.

- **Parameters**
  - `jobName` (string): The unique identifier of the job.
  - `maxConcurrency` (int): The maximum number of concurrent executions allowed for the job.

- **Return Value**
  Returns `true` if the job can be executed without exceeding its concurrency limit; otherwise, `false`.

- **Exceptions**
  Throws `ArgumentNullException` if `jobName` is `null`.
  Throws `ArgumentOutOfRangeException` if `maxConcurrency` is less than 1.

### `async Task EnsureCanExecuteAsync(string jobName, int maxConcurrency)`
Ensures that a job can be executed by waiting until concurrency conditions are met, if necessary.

- **Parameters**
  - `jobName` (string): The unique identifier of the job.
  - `maxConcurrency` (int): The maximum number of concurrent executions allowed for the job.

- **Return Value**
  Returns a `Task` that completes when the job can be executed or when cancellation is requested.

- **Exceptions**
  Throws `ArgumentNullException` if `jobName` is `null`.
  Throws `ArgumentOutOfRangeException` if `maxConcurrency` is less than 1.

### `void IncrementConcurrencyCount(string jobName)`
Increments the concurrency count for the specified job, indicating a new execution has started.

- **Parameters**
  - `jobName` (string): The unique identifier of the job.

- **Exceptions**
  Throws `ArgumentNullException` if `jobName` is `null`.

### `void DecrementConcurrencyCount(string jobName)`
Decrements the concurrency count for the specified job, indicating an execution has completed.

- **Parameters**
  - `jobName` (string): The unique identifier of the job.

- **Exceptions**
  Throws `ArgumentNullException` if `jobName` is `null`.

### `int GetJobConcurrencyCount(string jobName)`
Gets the current number of active executions for the specified job.

- **Parameters**
  - `jobName` (string): The unique identifier of the job.

- **Return Value**
  Returns the current concurrency count for the job.

- **Exceptions**
  Throws `ArgumentNullException` if `jobName` is `null`.

### `int GetGlobalConcurrencyCount()`
Gets the total number of currently active job executions across all jobs.

- **Return Value**
  Returns the total concurrency count.

### `async Task SynchronizeWithDatabaseAsync()`
Synchronizes the in-memory concurrency state with the persistent storage, ensuring consistency after restarts or crashes.

- **Return Value**
  Returns a `Task` that completes when synchronization is finished.

### `Dictionary<string, int> GetConcurrencyStats()`
Retrieves a snapshot of concurrency statistics for all tracked jobs.

- **Return Value**
  Returns a dictionary mapping job names to their current concurrency counts.

## Usage

### Example 1: Enforcing job concurrency
