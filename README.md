// ... existing content ...

## JobsControllerExtensions

The `JobsControllerExtensions` class adds a set of convenient extension methods to the `JobsController` for bulk job creation, existence checks, execution‑status retrieval, and bulk suspension. These helpers simplify common controller scenarios by handling loops, aggregating results, and returning rich response objects.

### Usage Example

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using JobScheduler.Core.Controllers;
using JobScheduler.Core.Domain.Models;

var services = new ServiceCollection();
services.AddLogging();                     // required by the controller
services.AddScoped<JobsController>();      // the controller itself

var provider = services.BuildServiceProvider();
var controller = provider.GetRequiredService<JobsController>();

// -------------------------------------------------
// 1. Bulk‑create jobs
// -------------------------------------------------
var createRequests = new List<CreateJobRequest>
{
    new CreateJobRequest { Name = "Job A", CronExpression = "0 * * * *" },
    new CreateJobRequest { Name = "Job B", CronExpression = "0 0 * * *" }
};

var bulkCreateResult = await controller.BulkCreateJobs(createRequests);
var createdJobs = bulkCreateResult.Value?.Data ?? Enumerable.Empty<JobResponse>();

// -------------------------------------------------
// 2. Check whether a specific job exists
// -------------------------------------------------
var firstJobId = createdJobs.FirstOrDefault()?.Id ?? Guid.Empty;
var existsResult = await controller.JobExists(firstJobId);
bool exists = existsResult.Value;

// -------------------------------------------------
// 3. Retrieve a detailed execution‑status summary
// -------------------------------------------------
var statusResult = await controller.GetJobExecutionStatus(firstJobId);
var summary = statusResult.Value;

Console.WriteLine($"Job \"{summary?.JobName}\" success rate: {summary?.SuccessRatePercentage}%");
Console.WriteLine($"Last execution status: {summary?.LastExecutionStatus}");
Console.WriteLine($"Average execution time: {summary?.AverageExecutionTimeMs} ms");

// -------------------------------------------------
// 4. Bulk‑suspend jobs (e.g., for maintenance)
// -------------------------------------------------
var jobIdsToSuspend = createdJobs.Select(j => j.Id);
var suspendResult = await controller.BulkSuspendJobs(jobIdsToSuspend, reason: "Planned maintenance");
var suspendResults = suspendResult.Value ?? Array.Empty<BulkOperationResult>();

foreach (var r in suspendResults)
{
    Console.WriteLine($"Job {r.JobId} suspend {(r.Success ? "succeeded" : $"failed: {r.ErrorMessage}")}");
}
```

The example demonstrates how to:

* **BulkCreateJobs** – create several jobs in one call and obtain a `PaginatedResponse<JobResponse>`.
* **JobExists** – verify the presence of a job by its `Guid`.
* **GetJobExecutionStatus** – fetch a `JobExecutionStatusSummary` containing metrics such as total executions, success rate, recent executions, and timing information.
* **BulkSuspendJobs** – suspend multiple jobs at once, receiving a list of `BulkOperationResult` objects that expose `JobId`, `Success`, and an optional `ErrorMessage`.

These extension methods keep controller actions concise while providing rich, typed results for downstream processing or logging.````
