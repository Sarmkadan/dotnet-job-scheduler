# JobsControllerExtensions

This document describes the public extension methods in `JobsControllerExtensions.cs` that enhance the functionality of the `JobsController` class.

## Extension Methods

| Method | Parameters | Return | Purpose |
|--------|------------|--------|---------|
| `BulkCreateJobs` | `controller`, `requests`, `cultureInfo` | `Task<ActionResult<PaginatedResponse<JobResponse>>>` | Bulk creates multiple jobs from a collection of requests. Returns a paginated response containing all created jobs with their IDs. |
| `JobExists` | `controller`, `id` | `Task<ActionResult<bool>>` | Checks if a job with the specified ID exists. Returns 200 OK with a boolean value if the job exists, 404 NotFound otherwise. |
| `GetJobExecutionStatus` | `controller`, `id`, `limit` | `Task<ActionResult<JobExecutionStatusSummary>>` | Gets the execution status summary for a job, including success rate and recent executions. Returns detailed execution statistics and metrics. |
| `BulkSuspendJobs` | `controller`, `jobIds`, `reason` | `Task<ActionResult<IReadOnlyList<BulkOperationResult>>>` | Bulk suspends multiple jobs with optional reason. Returns a collection of results indicating success/failure for each job. |

## Usage Examples

### Example 1: Bulk Creating Jobs
```csharp
var requests = new[] {
    new CreateJobRequest { Name = "Job A", CronExpression = "0 * * * *" },
    new CreateJobRequest { Name = "Job B", CronExpression = "*/5 * * * *" }
};

var result = await controller.BulkCreateJobs(requests);
if (result.Result is OkObjectResult ok)
{
    var jobs = ((PaginatedResponse<JobResponse>)ok.Value).Data;
    Console.WriteLine($"Created {jobs.Count} jobs.");
}
```

### Example 2: Checking Job Existence and Getting Execution Status
```csharp
// Check if a job exists
bool exists = await controller.JobExists(jobId);
if (exists)
{
    // Get detailed execution status
    var status = await controller.GetJobExecutionStatus(jobId, limit: 10);
    if (status.Result is OkObjectResult statusOk)
    {
        var summary = (JobExecutionStatusSummary)statusOk.Value;
        Console.WriteLine($"Success rate: {summary.SuccessRatePercentage}%");
    }
}
```

## Related Documentation

See [JobsController.md](./JobsController.md) for the base controller implementation that these extension methods enhance.
