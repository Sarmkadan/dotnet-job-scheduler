# ExecutionRepository

The `ExecutionRepository` provides data‑access operations for `JobExecution` entities within the `dotnet-job-scheduler` application. It wraps a `JobSchedulerContext` and exposes asynchronous methods to query execution records by various criteria such as job, status, date range, and runtime metrics.

## API

### `public ExecutionRepository(JobSchedulerContext context) : base(context)`
- **Purpose**: Initializes a new instance of the repository with the supplied Entity Framework Core context.
- **Parameters**: 
  - `context` – The `JobSchedulerContext` used to interact with the database. Must not be `null`.
- **Return Value**: None (constructor).
- **Exceptions**: 
  - Throws `ArgumentNullException` if `context` is `null`.
  - May propagate any exception thrown by the base class constructor (e.g., `InvalidOperationException` if the context is already disposed).

### `public async Task<JobExecution?> GetLatestExecutionAsync()`
- **Purpose**: Retrieves the most recent `JobExecution` record stored in the database, or `null` if no executions exist.
- **Parameters**: None.
- **Return Value**: A `Task` that completes with the latest `JobExecution` instance, or `null` when the table is empty.
- **Exceptions**: 
  - May throw EF Core exceptions such as `DbUpdateException` or `InvalidOperationException` on query failure.
  - May throw `OperationCanceledException` if a cancellation token is supplied via an overload not shown here.

### `public async Task<IEnumerable<JobExecution>> GetExecutionsByJobAsync()`
- **Purpose**: Returns all `JobExecution` entities associated with a job (the specific job is determined by the repository’s internal state).
- **Parameters**: None.
- **Return Value**: A `Task` that completes with an enumerable collection of `JobExecution` objects.
- **Exceptions**: 
  - May throw EF Core exceptions on database access errors.
  - Returns an empty enumerable if no matching executions are found.

### `public async Task<IEnumerable<JobExecution>> GetExecutionsByStatusAsync()`
- **Purpose**: Retrieves all executions that match a particular status (the status is defined by the repository’s internal configuration).
- **Parameters**: None.
- **Return Value**: A `Task` that completes with an enumerable of `JobExecution` instances having the target status.
- **Exceptions**: 
  - Propagates any data‑access exceptions from the underlying context.
  - Returns an empty collection when no executions with the specified status exist.

### `public async Task<IEnumerable<JobExecution>> GetExecutionsByJobAndStatusAsync()`
- **Purpose**: Returns executions that satisfy both a job filter and a status filter (both criteria are encapsulated within the repository instance).
- **Parameters**: None.
- **Return Value**: A `Task` that completes with an enumerable of `JobExecution` objects meeting the combined criteria.
- **Exceptions**: 
  - May throw exceptions originating from EF Core query execution.
  - Yields an empty sequence if no records match the combined filters.

### `public async Task<int> GetCurrentlyRunningCountAsync()`
- **Purpose**: Provides the number of `JobExecution` records that are presently in a running state.
- **Parameters**: None.
- **Return Value**: A `Task` that completes with an integer count of currently running executions.
- **Exceptions**: 
  - May throw EF Core exceptions if the query cannot be executed.
  - Returns `0` when no executions are running.

### `public async Task<int> GetConcurrentRunningCountAsync()`
- **Purpose**: Returns the maximum number of executions that were running concurrently at any point in time (based on stored timestamps).
- **Parameters**: None.
- **Return Value**: A `Task` that completes with an integer representing the peak concurrent execution count.
- **Exceptions**: 
  - Propagates any query‑related exceptions from the data store.
  - Returns `0` if no execution records exist.

### `public async Task<IEnumerable<JobExecution>> GetRunningExecutionsAsync()`
- **Purpose**: Retrieves all `JobExecution` entities that are currently marked as running.
- **Parameters**: None.
- **Return Value**: A `Task` that completes with an enumerable of running `JobExecution` objects.
- **Exceptions**: 
  - May throw EF Core exceptions on failure.
  - Returns an empty enumerable when no executions are running.

### `public async Task<IEnumerable<JobExecution>> GetFailedExecutionsRequiringRetryAsync()`
- **Purpose**: Returns executions that have failed and are eligible for a retry attempt (according to retry policy logic embedded in the query).
- **Parameters**: None.
- **Return Value**: A `Task` that completes with an enumerable of `JobExecution` instances awaiting retry.
- **Exceptions**: 
  - May throw exceptions from the underlying data access layer.
  - Yields an empty collection if no failed executions meet the retry criteria.

### `public async Task<IEnumerable<JobExecution>> GetExecutionsByDateRangeAsync()`
- **Purpose**: Retrieves all executions whose start or end timestamps fall within a predefined date range (the range is established by the repository’s internal state).
- **Parameters**: None.
- **Return Value**: A `Task` that completes with an enumerable of `JobExecution` objects within the date range.
- **Exceptions**: 
  - May throw EF Core exceptions if the date range query fails.
  - Returns an empty enumerable when no executions match the range.

### `public async Task<long> GetAverageExecutionTimeAsync()`
- **Purpose**: Calculates the average duration (in milliseconds) of all completed `JobExecution` records.
- **Parameters**: None.
- **Return Value**: A `Task` that completes with a `long` representing the average execution time; returns `0` if there are no completed executions.
- **Exceptions**: 
  - May throw EF Core exceptions during aggregation.
  - May throw `InvalidOperationException` if the underlying data contains non‑numeric or null duration values.

### `public async Task<List<JobExecution>> GetByJobIdAsync()`
- **Purpose**: Returns a list of all `JobExecution` entities associated with a specific job identifier (the identifier is maintained within the repository instance).
- **Parameters**: None.
- **Return Value**: A `Task` that completes with a `List<JobExecution>` containing the matching executions; returns an empty list if none are found.
- **Exceptions**: 
  - Propagates any data‑access exceptions from the context.
  - Returns an empty list on query failure only if the exception is swallowed internally; otherwise, the exception is surfaced to the caller.

## Usage

### Example 1: Obtaining the latest execution and checking if it succeeded
```csharp
using var schedulerContext = new JobSchedulerContext(options);
var repo = new ExecutionRepository(schedulerContext);

JobExecution? latest = await repo.GetLatestExecutionAsync();
if (latest is null)
{
    Console.WriteLine("No executions have been recorded yet.");
}
else if (latest.Status == JobStatus.Succeeded)
{
    Console.WriteLine($"Latest execution succeeded in {latest.DurationMs} ms.");
}
else
{
    Console.WriteLine($"Latest execution failed with status {latest.Status}.");
}
```

### Example 2: Retrieving all failed executions that require a retry and re‑queuing them
```csharp
await using var context = new JobSchedulerContext(options);
var repository = new ExecutionRepository(context);

IEnumerable<JobExecution> failedForRetry = await repository.GetFailedExecutionsRequiringRetryAsync();

foreach (var execution in failedForRetry)
{
    // Logic to re‑queue the job for another attempt
    await scheduler.RequeueJobAsync(execution.JobId);
    Console.WriteLine($"Re‑queued job {execution.JobId} (execution {execution.Id}) for retry.");
}
```

## Notes
- All methods are asynchronous and rely on EF Core; they will throw the usual EF‑related exceptions (`DbUpdateException`, `InvalidOperationException`, etc.) if the underlying database is unavailable or the query cannot be translated.
- The repository does **not** maintain mutable state beyond the injected `JobSchedulerContext`. Consequently, multiple threads can safely invoke its methods concurrently **provided** that the shared `JobSchedulerContext` is either thread‑safe (e.g., a scoped context per operation) or externally synchronized. Using a single context instance across threads without proper synchronization may lead to race conditions or undefined behavior.
- Methods that return collections (`IEnumerable<JobExecution>` or `List<JobExecution>`) will never return `null`; they return an empty sequence when no matching records exist.
- The `GetAverageExecutionTimeAsync` method computes the average over executions that have a non‑null duration; executions still in progress or with missing timing data are excluded from the calculation.
- Consumers should consider applying a `CancellationToken` (via overloads not shown in the public signature) if they need to cancel long‑running queries, especially for date‑range or aggregation operations on large data sets.
