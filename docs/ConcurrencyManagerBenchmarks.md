# ConcurrencyManagerBenchmarks

The `ConcurrencyManagerBenchmarks` class provides benchmarking utilities for testing concurrency limits and execution tracking in the job scheduler. It exposes methods to simulate job execution scenarios, measure concurrency counts, and validate global or per-job execution limits. The class is designed for integration testing and performance measurement of the scheduler's concurrency management system.

## API

### `Setup`
Initializes the benchmarking environment. Must be called before any other methods to ensure consistent test conditions. Does not return a value and does not throw under normal operation.

### `async Task<bool> CanExecuteJob_GlobalLimitNotReached`
Determines whether a job can execute given that the global concurrency limit has not been reached. Returns `true` if execution is permitted, otherwise `false`. Does not throw.

### `async Task<bool> CanExecuteJob_GlobalLimitReached`
Determines whether a job can execute given that the global concurrency limit has been reached. Returns `true` if execution is permitted (e.g., via priority override), otherwise `false`. Does not throw.

### `async Task<bool> CanExecuteJob_PerJobLimit`
Determines whether a job can execute given its per-job concurrency limit. Returns `true` if execution is permitted, otherwise `false`. Does not throw.

### `TrackExecutionStart`
Records the start of a job execution in the concurrency tracker. Increments internal counters. Does not return a value and does not throw.

### `TrackExecutionEnd`
Records the end of a job execution in the concurrency tracker. Decrements internal counters. Does not return a value and does not throw.

### `GetCurrentConcurrencyCount`
Retrieves the current number of actively executing jobs. Returns an `int` representing the count. Does not throw.

### `GetGlobalConcurrencyCount`
Retrieves the total number of jobs currently executing across all job types. Returns an `int` representing the count. Does not throw.

### `Dictionary<string, int> Reset`
Resets all concurrency tracking data and returns a snapshot of the previous state as a dictionary mapping job identifiers to their concurrency counts. The returned dictionary is never `null`.

### `Task<Job?> GetByIdAsync`
Retrieves a job by its unique identifier. Returns the `Job` instance if found, otherwise `null`. Does not throw.

### `Task<IEnumerable<Job>> GetAllAsync`
Retrieves all jobs in the system. Returns an enumerable collection of `Job` instances. The collection may be empty but is never `null`.

### `Task<IEnumerable<Job>> FindAsync`
Searches for jobs matching unspecified criteria. Returns an enumerable collection of `Job` instances. The collection may be empty but is never `null`.

### `Task<Job?> FirstOrDefaultAsync`
Retrieves the first job matching unspecified criteria or returns `null` if none exist. Returns a `Job` instance or `null`. Does not throw.

### `Task<int> CountAsync`
Returns the total number of jobs in the system. Does not throw.

### `Task<Job?> GetByNameAsync`
Retrieves a job by its name. Returns the `Job` instance if found, otherwise `null`. Does not throw.

### `Task<IEnumerable<Job>> GetActiveJobsAsync`
Retrieves all jobs currently marked as active. Returns an enumerable collection of `Job` instances. The collection may be empty but is never `null`.

### `Task<IEnumerable<Job>> GetJobsByStatusAsync`
Retrieves all jobs matching a specific status. Returns an enumerable collection of `Job` instances. The collection may be empty but is never `null`.

### `Task<IEnumerable<Job>> GetJobsByPriorityAsync`
Retrieves all jobs matching a specific priority level. Returns an enumerable collection of `Job` instances. The collection may be empty but is never `null`.

### `Task<IEnumerable<Job>> GetScheduledJobsForExecutionAsync`
Retrieves jobs scheduled for immediate or future execution. Returns an enumerable collection of `Job` instances. The collection may be empty but is never `null`.

### `Task<IEnumerable<Job>> GetFailedJobsAsync`
Retrieves all jobs marked as failed. Returns an enumerable collection of `Job` instances. The collection may be empty but is never `null`.

## Usage
