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


## DashboardControllerExtensions

The `DashboardControllerExtensions` class provides extension methods for analyzing job scheduler health, performance, and failure data. It includes methods to calculate health scores, retrieve formatted statistics, and analyze failing jobs.

### Usage Example

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JobScheduler.Core.Controllers;
using JobScheduler.Core.Domain.Models;

// Example: Retrieve health status for a specific job
var jobId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");
var healthStatus = await DashboardControllerExtensions.GetHealthStatusAsync(jobId);
Console.WriteLine($"Job health status: {healthStatus.Status}, Color: {healthStatus.Color}");

// Example: Calculate health score for a job
var healthScore = await DashboardControllerExtensions.CalculateHealthScoreAsync(jobId);
Console.WriteLine($"Health score: {healthScore}");

// Example: Get formatted statistics for the dashboard
var statistics = await DashboardControllerExtensions.GetFormattedStatisticsAsync();
foreach (var stat in statistics)
{
    Console.WriteLine($"{stat.Key}: {stat.Value}");
}

// Example: Get top failing jobs with analysis
var failingJobs = await DashboardControllerExtensions.GetTopFailingJobsWithAnalysisAsync(5);
foreach (var job in failingJobs)
{
    Console.WriteLine($"Job: {job.JobName}, Failure Rate: {job.FailureRate:P}, " +
                     $"Failed Count: {job.FailedCount}, Impact: {job.FailureImpactScore}");
}
```

The extension methods provide:

* **CalculateHealthScoreAsync** – computes a numeric health score for a job
* **GetHealthStatusAsync** – returns a tuple with the job status and a color code for UI display
* **GetFormattedStatisticsAsync** – returns key-value pairs of formatted statistics for dashboard display
* **GetTopFailingJobsWithAnalysisAsync** – retrieves the most problematic jobs with detailed failure analysis

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
        Console.WriteLine($" - {error}");
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
        Console.WriteLine($" - {error}");
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

## DistributedJobLockServiceTestsExtensions

The `DistributedJobLockServiceTestsExtensions` class provides a comprehensive set of extension methods for testing distributed job lock scenarios. It simplifies the creation of test contexts, lock acquisition, and lock state verification, making it easier to write reliable tests for distributed job scheduling systems.

### Usage Example

```csharp
using System;
using System.Linq;
using System.Threading.Tasks;
using JobScheduler.Core.Data;
using JobScheduler.Core.Services;
using DotnetJobScheduler.Tests;

// Create a fresh in-memory database context and service instance
var context = DistributedJobLockServiceTestsExtensions.CreateFreshContext();
var service = new DistributedJobLockService(context);

// -------------------------------------------------
// 1. Acquire a lock and verify it was acquired
// -------------------------------------------------
var jobId = Guid.NewGuid();
var holderId = "instance-001";
var lockDuration = TimeSpan.FromMinutes(5);

var acquiredLock = await service.ShouldAcquireLockAsync(jobId, holderId, lockDuration);
Console.WriteLine($"Lock acquired: JobId={acquiredLock.JobId}, Holder={acquiredLock.HolderInstanceId}, Expires={acquiredLock.ExpiresAt}");

// -------------------------------------------------
// 2. Verify a job is currently locked
// -------------------------------------------------
var isLocked = await service.ShouldBeLockedAsync(jobId);
Console.WriteLine($"Job is locked: {isLocked.JobId}");

// -------------------------------------------------
// 3. Attempt to acquire a lock that should fail (already held)
// -------------------------------------------------
var failedAcquisitionCount = await service.ShouldNotAcquireLockAsync(jobId, "instance-002", lockDuration);
Console.WriteLine($"Lock acquisition attempts failed as expected (locks in DB: {failedAcquisitionCount})");

// -------------------------------------------------
// 4. Create multiple locks for testing expiration scenarios
// -------------------------------------------------
var now = DateTime.UtcNow;
var locks = await service.CreateLocksAsync(
    (Guid.NewGuid(), "holder-1", now.AddMinutes(10)),
    (Guid.NewGuid(), "holder-2", now.AddMinutes(15))
);
Console.WriteLine($"Created {locks.Count} locks for testing expiration logic");

// -------------------------------------------------
// 5. Check when a specific lock expires
// -------------------------------------------------
var expiryTime = await service.GetLockExpiryAsync(acquiredLock.JobId);
Console.WriteLine($"Lock expires at: {expiryTime}");

// -------------------------------------------------
// 6. Verify a job is not locked
// -------------------------------------------------
var anotherJobId = Guid.NewGuid();
var lockCount = await service.ShouldNotBeLockedAsync(anotherJobId);
Console.WriteLine($"Jobs not locked count: {lockCount}");
```

The extension methods provide a fluent API for common distributed lock testing scenarios:

* **CreateFreshService()** – Creates a new service instance with a fresh in-memory database
* **CreateFreshContext()** – Creates a fresh in-memory database context for testing
* **ShouldAcquireLockAsync()** – Acquires a lock and returns the lock entity for assertions
* **ShouldNotAcquireLockAsync()** – Verifies lock acquisition fails as expected
* **ShouldBeLockedAsync()** – Verifies a job is currently locked
* **ShouldNotBeLockedAsync()** – Verifies a job is not locked
* **CreateLocksAsync()** – Creates multiple lock entities for testing complex scenarios
* **GetLockExpiryAsync()** – Retrieves the expiry time of a specific lock

These methods eliminate boilerplate code and provide FluentAssertions-compatible assertions for testing distributed job lock behavior.

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
