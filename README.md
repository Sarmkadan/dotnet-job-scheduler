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

These extension methods keep controller actions concise while providing rich, typed results for downstream processing or logging.


## HealthControllerValidation

The `HealthControllerValidation` class provides validation extension methods for the `HealthController` and related health-check response types. It ensures that health check endpoints return valid, meaningful data by validating controller instances and response objects such as `HealthStatusResponse`, `DatabaseStatus`, `JobsStatus`, `ExecutionsStatus`, and `MemoryStatus`.

Use these methods to validate health check results before returning them to clients, ensuring consistent and reliable health monitoring.

### Usage Example

```csharp
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using JobScheduler.Core.Controllers;
using JobScheduler.Core.Domain.Models;

// Create a HealthController instance (typically injected in real applications)
var controller = new HealthController();

// Validate the controller itself
var controllerErrors = controller.Validate();
if (controllerErrors.Count > 0)
{
    Console.WriteLine("Controller validation failed:");
    foreach (var error in controllerErrors)
    {
        Console.WriteLine($"  - {error}");
    }
}
else
{
    Console.WriteLine("Controller is valid.");
}

// Create a sample HealthStatusResponse
var healthStatus = new HealthStatusResponse
{
    Timestamp = DateTime.UtcNow,
    Version = "1.0.0",
    Status = "OK",
    Database = new DatabaseStatus
    {
        Available = true,
        LastChecked = DateTime.UtcNow,
        ResponseTimeMs = 42
    },
    Jobs = new JobsStatus
    {
        TotalCount = 150,
        ActiveCount = 45
    },
    Executions = new ExecutionsStatus
    {
        TotalCount = 1250,
        SuccessRate = 99.8m,
        FailedCount = 2
    },
    Memory = new MemoryStatus
    {
        UsageMb = 1024,
        Threshold = 2048,
        TotalMemoryMb = 8192
    }
};

// Validate the health status response
var healthErrors = healthStatus.Validate();
if (healthErrors.Count > 0)
{
    Console.WriteLine("Health status validation failed:");
    foreach (var error in healthErrors)
    {
        Console.WriteLine($"  - {error}");
    }
}
else
{
    Console.WriteLine("Health status is valid.");
}

// Use the convenience methods for quick validation
if (healthStatus.IsValid())
{
    Console.WriteLine("Health status passed validation.");
}

// Throw an exception if invalid (alternative approach)
try
{
    healthStatus.EnsureValid();
    Console.WriteLine("Health status is valid (EnsureValid passed).");
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Health status validation failed: {ex.Message}");
}
```

The `HealthControllerValidation` class provides three validation patterns:

* **Validate()** – Returns a list of validation errors (empty if valid)
* **IsValid()** – Returns a boolean indicating validity
* **EnsureValid()** – Throws an exception if invalid, useful for fail-fast scenarios

These methods help maintain consistent health check data quality across the application.
