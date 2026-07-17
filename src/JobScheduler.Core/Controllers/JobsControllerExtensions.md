# JobsControllerExtensions

Extension methods for `JobsController` that provide bulk operations, status queries, and execution control for job management in the Job Scheduler system.

## API

### `BulkCreateJobs`

Bulk creates multiple jobs from a collection of requests and returns a paginated response containing all created jobs with their IDs.

- **Parameters:**
  - `controller` – The `JobsController` instance.
  - `requests` – Collection of job creation requests.
  - `cultureInfo` – Culture info for consistent formatting (defaults to `CultureInfo.InvariantCulture`).

- **Returns:**
  - `Task<ActionResult<PaginatedResponse<JobResponse>>>` – 200 OK with paginated response of created jobs, or an error response.

- **Exceptions:**
  - Throws `ArgumentNullException` if `controller` or `requests` is `null`.

- **Side Effects:**
  - Adds an `X-Bulk-Create-Errors` header to the response if any job creation fails, containing a pipe-delimited list of error messages.

---

### `JobExists`

Checks whether a job with the specified ID exists.

- **Parameters:**
  - `controller` – The `JobsController` instance.
  - `id` – The job ID to check.

- **Returns:**
  - `Task<ActionResult<bool>>` – 200 OK with `true` if the job exists; 404 NotFound with `false` if it does not; 500 Internal Server Error with `false` on unexpected errors.

- **Exceptions:**
  - Throws `ArgumentNullException` if `controller` is `null`.

---

### `GetJobExecutionStatus`

Retrieves a comprehensive execution status summary for a job, including success rate, execution counts, and recent execution history.

- **Parameters:**
  - `controller` – The `JobsController` instance.
  - `id` – The job ID to query.
  - `limit` – Maximum number of recent executions to include (default: 10).

- **Returns:**
  - `Task<ActionResult<JobExecutionStatusSummary>>` – 200 OK with a populated `JobExecutionStatusSummary` if the job exists; 404 NotFound if the job is not found; 500 Internal Server Error on failure to retrieve execution history.

- **Exceptions:**
  - Throws `ArgumentNullException` if `controller` is `null`.

- **Calculated Fields:**
  - `FailedExecutions` is computed as `TotalExecutions - SuccessfulExecutions`.
  - `SuccessRatePercentage` is computed as `(SuccessfulExecutions / TotalExecutions) * 100` when `TotalExecutions > 0`; otherwise `0`.
  - `AverageExecutionTimeMs` is the arithmetic mean of `ExecutionTimeMs` values across recent executions, treating negative or zero values as `0`.
  - `LastExecutionStatus` and `LastExecutionTime` are derived from the most recent execution in the list.

---

### `BulkSuspendJobs`

Suspends multiple jobs in a single operation and returns a read-only list of results indicating success or failure for each job.

- **Parameters:**
  - `controller` – The `JobsController` instance.
  - `jobIds` – Collection of job IDs to suspend.
  - `reason` – Optional reason for suspension.

- **Returns:**
  - `Task<ActionResult<IReadOnlyList<BulkOperationResult>>>` – 200 OK with a read-only list of `BulkOperationResult` items, each indicating whether the corresponding job was successfully suspended and any error message.

- **Exceptions:**
  - Throws `ArgumentNullException` if `controller` or `jobIds` is `null`.

---

### `BulkOperationResult`

A result object returned by bulk operations (e.g., `BulkSuspendJobs`) that indicates the outcome for a single job.

- **Properties:**
  - `JobId` (`Guid`) – The ID of the job.
  - `Success` (`bool`) – Whether the operation succeeded.
  - `ErrorMessage` (`string?`) – Error message if the operation failed.

---

### `JobExecutionStatusSummary`

A comprehensive summary of a job's execution status and performance metrics.

- **Properties:**
  - `JobId` (`Guid`) – The job ID.
  - `JobName` (`string?`) – The job name.
  - `Status` (`string?`) – The job status.
  - `TotalExecutions` (`int`) – Total number of executions.
  - `SuccessfulExecutions` (`int`) – Number of successful executions.
  - `FailedExecutions` (`int`) – Number of failed executions.
  - `SuccessRatePercentage` (`double`) – Success rate as a percentage.
  - `LastExecutionStatus` (`string?`) – Status of the last execution.
  - `RecentExecutions` (`IReadOnlyList<ExecutionResponse>`) – Recent execution records.
  - `AverageExecutionTimeMs` (`double`) – Average execution time in milliseconds.
  - `LastExecutionTime` (`DateTimeOffset?`) – Time of the last execution.

## Usage

### Bulk create jobs

```csharp
var controller = new JobsController(jobStore, logger);
var requests = new[]
{
    new CreateJobRequest { Name = "data-cleanup", /* ... */ },
    new CreateJobRequest { Name = "report-generator", /* ... */ }
};

var result = await controller.BulkCreateJobs(requests);
if (result.Result is OkObjectResult ok && ok.Value is PaginatedResponse<JobResponse> response)
{
    Console.WriteLine($"Created {response.TotalCount} jobs.");
    foreach (var job in response.Data)
    {
        Console.WriteLine($"Job ID: {job.Id}, Name: {job.Name}");
    }
}
```

### Get job execution summary

```csharp
var controller = new JobsController(jobStore, logger);
var summaryResult = await controller.GetJobExecutionStatus(jobId, limit: 20);

if (summaryResult.Result is OkObjectResult ok && ok.Value is JobExecutionStatusSummary summary)
{
    Console.WriteLine($"Job: {summary.JobName}");
    Console.WriteLine($"Status: {summary.Status}");
    Console.WriteLine($"Success Rate: {summary.SuccessRatePercentage:F2}%");
    Console.WriteLine($"Average Execution Time: {summary.AverageExecutionTimeMs:F2} ms");
    Console.WriteLine($"Last Execution: {summary.LastExecutionTime}");
}
```

## Notes

- **Thread Safety:** All extension methods are designed to be thread-safe when called on the same `JobsController` instance, provided the underlying job store and controller dependencies are thread-safe.
- **Error Handling:** Bulk operations (`BulkCreateJobs`, `BulkSuspendJobs`) continue processing remaining items even if one fails, collecting errors in headers or result objects rather than short-circuiting.
- **Culture Sensitivity:** `BulkCreateJobs` respects the provided `CultureInfo` for any culture-sensitive formatting during job creation; default is invariant culture.
- **Empty Collections:** `BulkCreateJobs` returns a paginated response with zero items if the input collection is empty.
- **Missing Executions:** `GetJobExecutionStatus` returns zero for `AverageExecutionTimeMs` and `null` for `LastExecutionStatus`/`LastExecutionTime` if no execution history is available.
- **Division by Zero:** `SuccessRatePercentage` is computed safely to avoid division by zero when `TotalExecutions` is zero.
- **Header Size:** If many jobs fail during bulk creation, the `X-Bulk-Create-Errors` header may become large; clients should be prepared to handle long header values.