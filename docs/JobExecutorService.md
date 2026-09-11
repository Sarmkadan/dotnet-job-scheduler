# JobExecutorService

The `JobExecutorService` is responsible for executing job handlers and managing the execution lifecycle. It handles timeouts, error capture, concurrency limits, retry logic, and execution state management. This service is used by the scheduler to run jobs and by callers that want to supply custom execution logic.

## API

### `JobExecutorService`
The public constructors used to initialize a new instance of the service. It requires dependency injection of a job repository, execution repository, and concurrency manager. Optional logger and event publisher can be provided.

### `ExecuteJobAsync(Job job, CancellationToken cancellationToken = default)`
Executes a job with timeout, error handling, and state management using the default handler-type dispatch.
*   **Parameters:** 
    - `job`: The job to execute.
    - `cancellationToken`: A token to cancel the operation.
*   **Returns:** `Task<JobExecution>` representing the execution record.
*   **Throws:** 
    - `ArgumentNullException` if `job` is null.
    - `ConcurrencyException` if the job cannot run because a concurrency limit is reached.

### `ExecuteJobAsync(Job job, IJobHandler handler, string executorName, CancellationToken cancellationToken = default)`
Executes a job using an explicitly supplied <see cref="IJobHandler"/> instead of the default handler-type dispatch. Useful for tests and benchmarks that want to inject custom execution logic without registering a handler type.
*   **Parameters:** 
    - `job`: The job to execute.
    - `handler`: The handler to use for execution.
    - `executorName`: The name of the executor (e.g., machine name).
    - `cancellationToken`: A token to cancel the operation.
*   **Returns:** `Task<JobExecution>` representing the execution record.
*   **Throws:** 
    - `ArgumentNullException` if `job` or `handler` is null.
    - `ArgumentException` if `executorName` is null or white space.
    - `ConcurrencyException` if the job cannot run because a concurrency limit is reached.

### `ValidateJobForExecutionAsync(Job job)`
Validates if a job can be executed immediately.
*   **Parameters:** 
    - `job`: The job to validate.
*   **Returns:** `Task<(bool CanExecute, string? Reason)>` indicating whether the job can be executed and a reason if not.
*   **Throws:** Does not throw (returns false with reason instead).

### `GetExecutionStatisticsAsync(Guid jobId)`
Gets execution statistics for a job.
*   **Parameters:** 
    - `jobId`: The identifier of the job.
*   **Returns:** `Task<ExecutionStatistics>` containing statistics about the job's executions.
*   **Throws:** 
    - `JobNotFoundException` if the job with the given ID does not exist.

## ExecutionStatistics

The `ExecutionStatistics` class provides statistics about job execution performance.

### Properties

*   **JobId**: The identifier of the job.
*   **TotalExecutions**: The total number of executions.
*   **SuccessfulExecutions**: The number of executions that succeeded.
*   **FailedExecutions**: The number of executions that failed.
*   **TimedOutExecutions**: The number of executions that timed out.
*   **SkippedExecutions**: The number of executions that were skipped.
*   **AverageDurationMs**: The average duration of successful executions in milliseconds.
*   **SuccessRate**: The success rate as a value between 0 and 1.

### Methods

*   **ToString()**: Returns a human-readable summary of the execution counts and rates.

## Usage

### Example 1: Executing a Job with Default Handler
This example demonstrates executing a job using the service's default handler-type dispatch.

```csharp
public async Task ExecuteJobExample(JobExecutorService executor, Job job)
{
    try
    {
        var execution = await executor.ExecuteJobAsync(job);
        Console.WriteLine($"Job {job.Id} executed with status: {execution.Status}");
    }
    catch (ConcurrencyException ex)
    {
        Console.WriteLine($"Could not execute job due to concurrency limit: {ex.Message}");
    }
}
```

### Example 2: Executing a Job with Custom Handler
This example demonstrates executing a job using a custom <see cref="IJobHandler"/> implementation.

```csharp
public async Task ExecuteJobWithCustomHandler(JobExecutorService executor, Job job, IJobHandler handler)
{
    var execution = await executor.ExecuteJobAsync(job, handler, Environment.MachineName);
    Console.WriteLine($"Job {job.Id} executed by {handler.GetType().Name} with output: {execution.Output}");
}
```

### Example 3: Getting Execution Statistics
This example demonstrates retrieving execution statistics for a job.

```csharp
public async Task GetStatisticsExample(JobExecutorService executor, Guid jobId)
{
    var stats = await executor.GetExecutionStatisticsAsync(jobId);
    Console.WriteLine(stats.ToString());
    // Output example: Job {jobId}: 10 total, 8 succeeded, 1 failed, 1 timed out, 0 skipped, avg 1200ms, success rate 80.0%
}
```

## Notes

*   **Timeout and Retry Interaction:** The service executes each job attempt with a timeout specified by the job's `ExecutionTimeoutSeconds` property. If an attempt fails, the service checks the job's retry policy to determine if a retry should be attempted. Retries are delayed by a backoff period calculated by the retry policy. The service continues retrying until either the job succeeds, the maximum retry count is reached, or the job is cancelled.
*   **Concurrency Management:** The service uses a <see cref="ConcurrencyManager"/> to enforce concurrency limits. Before each execution attempt, it checks if the job can run based on current concurrency counts. If the limit is reached, a <see cref="ConcurrencyException"/> is thrown.
*   **Event Publishing:** If an <see cref="IEventPublisher"/> is provided, the service publishes events for execution start, completion, failure, timeout, and exhaustion. These events can be used for monitoring and logging.
*   **Error Handling:** All exceptions during execution are captured and stored in the execution record. The service distinguishes between timeout, cancellation, and other errors, and applies retry policies accordingly.
*   **Thread Safety:** The service is designed to be used concurrently by multiple callers. Dependencies (repositories, concurrency manager) are expected to be thread-safe.