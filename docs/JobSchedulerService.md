# JobSchedulerService

The `JobSchedulerService` is the primary orchestration component within the `dotnet-job-scheduler` project, responsible for the lifecycle management, scheduling, execution, and monitoring of background jobs. It provides a comprehensive asynchronous API to create, update, suspend, resume, and delete jobs, while also handling the immediate execution of due jobs, managing retry logic for failed attempts, and retrieving detailed statistical data regarding scheduler performance and job history.

## API

### `JobSchedulerService`
The public constructor used to initialize a new instance of the service. It typically requires dependency injection of underlying repositories, logging services, and time providers to function correctly.

### `CreateJobAsync`
Creates a new job definition within the scheduler.
*   **Parameters:** Accepts configuration details required to define the job (e.g., cron expression, payload, type).
*   **Returns:** `Task<Job>` containing the newly created job entity with assigned identifiers and initial status.
*   **Throws:** May throw exceptions if the job configuration is invalid, if a duplicate ID is detected, or if database persistence fails.

### `ExecuteDueJobsAsync`
Scans the job store for jobs scheduled to run at or before the current time and executes them.
*   **Parameters:** None.
*   **Returns:** `Task<IEnumerable<JobExecution>>` representing the list of execution records generated during this invocation.
*   **Throws:** May throw if the underlying execution engine fails or if concurrent access conflicts occur during status updates.

### `ProcessRetriesAsync`
Identifies jobs that have failed previously and are eligible for retry based on their retry policies, then attempts to re-execute them.
*   **Parameters:** None.
*   **Returns:** `Task<IEnumerable<JobExecution>>` containing the records of retry attempts processed.
*   **Throws:** May throw if retry logic encounters critical system errors or data inconsistencies.

### `UpdateJobScheduleAsync`
Modifies the scheduling configuration (e.g., cron expression or next run time) of an existing job without altering its payload or type.
*   **Parameters:** Requires the job identifier and the new schedule definition.
*   **Returns:** `Task<Job>` with the updated schedule information.
*   **Throws:** Throws if the job ID does not exist or if the new schedule format is invalid.

### `SuspendJobAsync`
Temporarily halts the scheduling and execution of a specific job without deleting it.
*   **Parameters:** Requires the job identifier.
*   **Returns:** `Task<Job>` reflecting the suspended state.
*   **Throws:** Throws if the job is not found or is already in a terminal state.

### `ResumeJobAsync`
Re-enables a previously suspended job, allowing it to be picked up by the scheduler again.
*   **Parameters:** Requires the job identifier.
*   **Returns:** `Task<Job>` reflecting the active state.
*   **Throws:** Throws if the job is not found or was not in a suspended state.

### `DeleteJobAsync` (Void Return)
Permanently removes a job definition and potentially its associated history from the scheduler.
*   **Parameters:** Requires the job identifier.
*   **Returns:** `Task` completing when the deletion is finalized.
*   **Throws:** Throws if the job ID does not exist.

### `GetJobDetailsAsync`
Retrieves comprehensive metadata and status information for a specific job.
*   **Parameters:** Requires the job identifier.
*   **Returns:** `Task<JobDetailsDto>` containing detailed view-model data.
*   **Throws:** Throws if the job ID is not found.

### `GetSchedulerStatisticsAsync`
Aggregates operational metrics specific to the scheduler's internal state (e.g., queue depth, active workers).
*   **Parameters:** None.
*   **Returns:** `Task<SchedulerStatisticsDto>` with current scheduler metrics.
*   **Throws:** Unlikely to throw unless the metrics aggregation subsystem fails.

### `GetSystemStatisticsAsync`
Retrieves broader system-level statistics related to job processing, potentially including resource usage or global throughput.
*   **Parameters:** None.
*   **Returns:** `Task<SchedulerStatisticsDto>` with system-wide metrics. Note this member returns a non-async wrapper task.
*   **Throws:** Unlikely to throw unless underlying system monitors are unavailable.

### `GetExecutionHistoryAsync`
Fetches a list of past execution records, typically filtered by time range or status.
*   **Parameters:** May accept filtering criteria (e.g., job ID, date range, success/failure status).
*   **Returns:** `Task<IEnumerable<JobExecution>>` listing historical execution events.
*   **Throws:** Throws if query parameters are malformed.

### `GetJobByIdAsync`
Attempts to retrieve a single job entity by its unique identifier.
*   **Parameters:** Requires the job identifier.
*   **Returns:** `Task<Job?>` containing the job if found, or `null` otherwise.
*   **Throws:** Generally does not throw for missing IDs; returns `null` instead.

### `GetJobsAsync`
Retrieves a collection of jobs, supporting pagination or filtering.
*   **Parameters:** May accept pagination tokens or filter predicates.
*   **Returns:** `Task<IEnumerable<Job>>` containing the matched jobs.
*   **Throws:** Throws if query constraints are invalid.

### `GetTotalJobCountAsync`
Calculates the total number of jobs currently registered in the system.
*   **Parameters:** None.
*   **Returns:** `Task<int>` representing the count.
*   **Throws:** Unlikely to throw unless the data store is inaccessible.

### `UpdateJobAsync`
Performs a general update on a job entity, potentially modifying payload, type, or configuration beyond just the schedule.
*   **Parameters:** Requires the job identifier and the updated job data.
*   **Returns:** `Task<Job?>` with the updated entity, or `null` if the job was not found.
*   **Throws:** Throws if validation fails on the updated data.

### `DeleteJobAsync` (Boolean Return)
Attempts to delete a job and reports success via a boolean flag rather than throwing on missing IDs.
*   **Parameters:** Requires the job identifier.
*   **Returns:** `Task<bool>` indicating whether the deletion occurred (`true`) or the job did not exist (`false`).
*   **Throws:** May still throw on database connectivity issues or constraint violations.

### `TriggerJobExecutionAsync`
Manually forces the immediate execution of a specific job, bypassing its scheduled time.
*   **Parameters:** Requires the job identifier.
*   **Returns:** `Task<JobExecution?>` representing the triggered execution record, or `null` if triggering failed.
*   **Throws:** Throws if the job is in a state that prevents execution (e.g., suspended or deleted).

### `GetJobExecutionsAsync`
Retrieves execution records specifically associated with a single job.
*   **Parameters:** Requires the job identifier and optional pagination.
*   **Returns:** `Task<IEnumerable<JobExecution>?>` containing the history, or `null` if the job has no history or does not exist.
*   **Throws:** Throws if the job ID format is invalid.

### `GetJobExecutionCountAsync`
Counts the number of execution records for a specific job.
*   **Parameters:** Requires the job identifier.
*   **Returns:** `Task<int>` representing the number of executions.
*   **Throws:** Throws if the job ID is invalid.

## Usage

### Example 1: Job Lifecycle Management
This example demonstrates creating a job, checking its status, manually triggering it, and then suspending it.

```csharp
public async Task ManageJobLifecycle(JobSchedulerService scheduler)
{
    // Create a new daily cleanup job
    var newJob = await scheduler.CreateJobAsync(new JobDefinition 
    { 
        Name = "DailyCleanup", 
        CronExpression = "0 0 * * *",
        Type = "CleanupTask" 
    });

    Console.WriteLine($"Created job: {newJob.Id}");

    // Manually trigger the job immediately for testing
    var execution = await scheduler.TriggerJobExecutionAsync(newJob.Id);
    if (execution != null)
    {
        Console.WriteLine($"Triggered execution: {execution.Id}");
    }

    // Suspend the job to prevent automatic scheduling
    var suspendedJob = await scheduler.SuspendJobAsync(newJob.Id);
    Console.WriteLine($"Job status is now: {suspendedJob.Status}");
}
```

### Example 2: Monitoring and Retry Processing
This example illustrates retrieving system statistics, processing any pending retries, and fetching the execution history for analysis.

```csharp
public async Task MonitorAndMaintain(JobSchedulerService scheduler)
{
    // Get overall system health metrics
    var stats = await scheduler.GetSystemStatisticsAsync();
    Console.WriteLine($"Active Jobs: {stats.ActiveJobCount}, Failed Last Hour: {stats.FailedCount}");

    // Attempt to retry any jobs that failed previously
    var retriedExecutions = await scheduler.ProcessRetriesAsync();
    Console.WriteLine($"Processed {retriedExecutions.Count()} retry attempts.");

    // Fetch the last 50 executions for audit logging
    var history = await scheduler.GetExecutionHistoryAsync();
    foreach (var record in history.Take(50))
    {
        Console.WriteLine($"Job {record.JobId} executed at {record.StartTime} - Status: {record.Status}");
    }
}
```

## Notes

*   **Thread Safety:** As an asynchronous service interacting with shared state (job queues and databases), `JobSchedulerService` implementations are expected to be thread-safe. However, callers should ensure that sequences of operations requiring atomicity (e.g., "check status then update") are handled via transactional scopes or optimistic concurrency controls provided by the underlying data layer, as individual method calls are atomic but not composite operations.
*   **Null Handling:** Several methods (`GetJobByIdAsync`, `UpdateJobAsync`, `TriggerJobExecutionAsync`, `GetJobExecutionsAsync`) return `null` or `Nullable<T>` to indicate that a resource was not found rather than throwing an exception. Callers must explicitly check for null before accessing properties on the return value.
*   **Method Overloading:** The API exposes two distinct `DeleteJobAsync` signatures: one returning `Task` (which likely throws on failure/missing ID) and one returning `Task<bool>`. Consumers should select the variant that matches their error handling strategy (exception-based vs. result-code-based).
*   **Execution Context:** Methods like `ExecuteDueJobsAsync` and `ProcessRetriesAsync` are designed to be called periodically by a host process (e.g., a background worker or timer). Calling these methods concurrently from multiple instances without distributed locking mechanisms may lead to duplicate job executions.
*   **Asynchronous Consistency:** Note that `GetSystemStatisticsAsync` returns a `Task<SchedulerStatisticsDto>` but is not marked `async` in the signature list, implying it may wrap a synchronous result or delegate directly to a lower-level task without an async state machine, whereas most other methods are explicitly `async`.
