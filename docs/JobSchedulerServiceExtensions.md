# JobSchedulerServiceExtensions

The `JobSchedulerServiceExtensions` static class provides a set of extension methods for the `IJobSchedulerService` interface. These methods facilitate streamlined operations for job lifecycle management, including filtering job collections, creating new job definitions, triggering the execution of due jobs within defined timeout constraints, and retrieving aggregate execution statistics.

## API

### GetFilteredJobsAsync

Retrieves a collection of jobs that match the specified filter criteria.

*   **Parameters:**
    *   `IJobSchedulerService service`: The instance of the job scheduler service.
    *   `JobFilter filter`: The filter criteria to apply to the job collection.
    *   `CancellationToken cancellationToken`: A token to monitor for cancellation requests.
*   **Returns:** An `IEnumerable<Job>` containing the jobs that satisfy the provided filter.
*   **Exceptions:** Throws `ArgumentNullException` if `service` or `filter` is null.

### CreateJobAsync

Registers and persists a new job definition within the scheduler.

*   **Parameters:**
    *   `IJobSchedulerService service`: The instance of the job scheduler service.
    *   `CreateJobRequest request`: The details of the job to be created.
    *   `CancellationToken cancellationToken`: A token to monitor for cancellation requests.
*   **Returns:** The created `Job` object.
*   **Exceptions:** Throws `ArgumentNullException` if `service` or `request` is null. Throws `JobCreationException` if the job registration fails.

### ExecuteDueJobsWithTimeoutAsync

Identifies jobs that are currently due for execution and initiates their execution, adhering to a specified timeout for the operation.

*   **Parameters:**
    *   `IJobSchedulerService service`: The instance of the job scheduler service.
    *   `TimeSpan timeout`: The maximum duration allowed for the execution process to initiate.
    *   `CancellationToken cancellationToken`: A token to monitor for cancellation requests.
*   **Returns:** An `IEnumerable<JobExecution>` representing the results of the triggered executions.
*   **Exceptions:** Throws `ArgumentNullException` if `service` is null. Throws `TimeoutException` if the execution process exceeds the specified timeout.

### GetAllJobExecutionStatisticsAsync

Aggregates and retrieves comprehensive execution statistics for all jobs tracked by the scheduler.

*   **Parameters:**
    *   `IJobSchedulerService service`: The instance of the job scheduler service.
    *   `CancellationToken cancellationToken`: A token to monitor for cancellation requests.
*   **Returns:** A `Dictionary<Guid, ExecutionStatistics>` where the key is the unique identifier of the job and the value is the corresponding `ExecutionStatistics` object.
*   **Exceptions:** Throws `ArgumentNullException` if `service` is null.

## Usage

### Example 1: Creating and Filtering Jobs

```csharp
var jobRequest = new CreateJobRequest { Name = "DataBackup", Schedule = "0 0 * * *" };
await schedulerService.CreateJobAsync(jobRequest);

var filter = new JobFilter { Status = JobStatus.Pending };
var pendingJobs = await schedulerService.GetFilteredJobsAsync(filter);

foreach (var job in pendingJobs)
{
    Console.WriteLine($"Pending job: {job.Name}");
}
```

### Example 2: Executing Due Jobs and Reporting Statistics

```csharp
var timeout = TimeSpan.FromSeconds(30);
var executions = await schedulerService.ExecuteDueJobsWithTimeoutAsync(timeout);

var stats = await schedulerService.GetAllJobExecutionStatisticsAsync();
foreach (var kvp in stats)
{
    Console.WriteLine($"Job ID: {kvp.Key}, Success Count: {kvp.Value.SuccessCount}");
}
```

## Notes

*   **Thread Safety:** These extension methods rely entirely on the implementation of `IJobSchedulerService`. While the extension methods themselves are stateless, the underlying service implementation must be thread-safe to handle concurrent calls to these methods correctly.
*   **Asynchronous Operations:** All methods are asynchronous and should be awaited. The `CancellationToken` should be utilized to ensure that long-running operations can be cancelled appropriately.
*   **Timeout Handling:** In `ExecuteDueJobsWithTimeoutAsync`, the `timeout` parameter refers specifically to the duration for initiating the execution process, not the duration of the job execution itself.
*   **Exception Handling:** Callers should be prepared to handle exceptions related to connectivity or underlying storage issues when interacting with the scheduler service.
