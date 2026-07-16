# dotnet-job-scheduler

A .NET job scheduling library for background tasks, cron schedules, retries, pipelines and distributed execution, built on EF Core.

## Architecture

The scheduler is a single package (`JobScheduler.Core`): a polling `BackgroundService` picks up due jobs, `JobExecutorService` dispatches them to your `IJobHandler` implementations with timeout/retry/concurrency handling, and everything persists through EF Core repositories. Multi-node deployments coordinate via database-backed job locks and optional leader election.

Full breakdown - components, data flow, design decisions with trade-offs, extension points and known limitations - in [docs/architecture.md](docs/architecture.md).

## JobExecution

The `JobExecution` entity represents a single execution attempt of a job, tracking its lifecycle, timing, resource usage, and outcome. It captures when the job started and completed, execution duration, attempt number, output, error details, and resource consumption metrics. The entity provides methods to mark executions as completed or failed and determine if retries are appropriate.

Example usage:
```csharp
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Domain.Enums;

// Create a new job execution
var execution = new JobExecution
{
    JobId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
    ExecutorName = "JobProcessor",
    ExecutorInstance = "processor-01",
    AttemptNumber = 1,
    IsRetryable = true,
    MemoryUsageMb = 42,
    CpuUsagePercent = 15.5
};

// Simulate job execution
Console.WriteLine($"Starting execution {execution.Id} for job {execution.JobId}");

// ... execute job logic ...

// Mark as completed successfully
execution.MarkAsCompleted(ExecutionStatus.Success);
Console.WriteLine($"Execution completed in {execution.DurationMilliseconds}ms");
Console.WriteLine($"Total duration: {execution.GetExecutionDuration().TotalMilliseconds}ms");

// Or mark as failed
execution.MarkAsFailed(
    "Connection timeout",
    "System.TimeoutException: Operation timed out...",
    retryable: true
);

// Check if should retry
bool shouldRetry = execution.ShouldRetry(maxRetries: 3);
Console.WriteLine($"Should retry: {shouldRetry}");

// Validate execution data
bool isValid = execution.IsValid();
Console.WriteLine($"Is valid: {isValid}");
```

## JobExecutionSummary

`JobExecutionSummary` is a model that provides aggregated execution statistics for a single job, including success rates, timing metrics, and the status of the most recent execution. It's useful for monitoring job health and performance.

Example usage:

```csharp
using JobScheduler.Core.Domain.Models;
using JobScheduler.Core.Constants;

// Create a JobExecutionSummary for a specific job
var summary = new JobExecutionSummary
{
    JobId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
    JobName = "Data Export Job",
    TotalExecutions = 1000,
    SuccessCount = 950,
    FailureCount = 30,
    TimedOutCount = 15,
    CancelledCount = 5,
    AverageDurationMs = 150,
    MinDurationMs = 50,
    MaxDurationMs = 2500,
    LastExecutedAt = DateTime.UtcNow,
    LastStatus = ExecutionStatus.Success
};

// Display summary statistics
Console.WriteLine($"Job: {summary.JobName} (Id: {summary.JobId})");
Console.WriteLine($"Total executions: {summary.TotalExecutions}");
Console.WriteLine($"Success rate: {summary.SuccessRate}%");
Console.WriteLine($"Successes: {summary.SuccessCount}");
Console.WriteLine($"Failures: {summary.FailureCount}");
Console.WriteLine($"Timeouts: {summary.TimedOutCount}");
Console.WriteLine($"Cancelled: {summary.CancelledCount}");
Console.WriteLine($"Average duration: {summary.AverageDurationMs}ms");
Console.WriteLine($"Min duration: {summary.MinDurationMs}ms");
Console.WriteLine($"Max duration: {summary.MaxDurationMs}ms");
Console.WriteLine($"Last executed: {summary.LastExecutedAt?.ToString("u") ?? "Never"}");
Console.WriteLine($"Last status: {summary.LastStatus}");

// Calculate derived metrics
int failureRate = (int)(100 - summary.SuccessRate);
Console.WriteLine($"Failure rate: {failureRate}%");
```

## ExecutionMetrics

The `ExecutionMetrics` class provides aggregated metrics and statistics for job executions, offering insights into job performance and reliability. It tracks various execution outcomes, such as successes, failures, timeouts, and cancellations, and calculates key performance indicators like average duration and success rate.

Example usage:
```csharp
using JobScheduler.Core.Domain.Entities;

// Create an instance of ExecutionMetrics
var metrics = new ExecutionMetrics
{
    JobId = Guid.NewGuid(),
    TotalExecutions = 100,
    SuccessfulExecutions = 80,
    FailedExecutions = 15,
    TimedOutExecutions = 3,
    SkippedExecutions = 1,
    CancelledExecutions = 1,
    AverageDurationMs = 500,
    MinDurationMs = 100,
    MaxDurationMs = 2000,
    TotalRetries = 5,
    LastExecutionTime = DateTime.UtcNow,
};

// Calculate success rate
double successRate = metrics.CalculateSuccessRate();
Console.WriteLine($"Success Rate: {successRate}%");

// Get a summary of metrics
string summary = metrics.GetSummary();
Console.WriteLine(summary);

// Check reliability
bool isReliable = metrics.IsReliable();
Console.WriteLine($"Is reliable: {isReliable}");

// Get actual failure count
int actualFailureCount = metrics.GetActualFailureCount();
Console.WriteLine($"Actual Failure Count: {actualFailureCount}");

// Check for failure trend
bool hasFailureTrend = metrics.HasFailureTrend();
Console.WriteLine($"Has failure trend: {hasFailureTrend}");
```

## JobSchedulerService

`JobSchedulerService` is the central orchestrator for the job scheduler system. It manages job scheduling, execution, retries, monitoring, and lifecycle operations through a comprehensive API of public methods. The service coordinates between repositories, execution services, retry logic, and concurrency management to provide a unified interface for managing scheduled jobs in a distributed environment.

The service handles:

- Job creation, scheduling, and lifecycle management
- Cron-based execution scheduling with timezone support
- Job execution and retry processing
- Concurrency control and resource management
- Job suspension, resumption, and deletion
- Execution history and statistics tracking
- System-wide monitoring and reporting

Example usage:

```csharp
using JobScheduler.Core.Services;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Setup DI services (typically done in Program.cs)
var services = new ServiceCollection();
services.AddLogging(configure => configure.AddConsole());

// Create repositories (in real app these would be registered with DI)
var jobRepository = new JobRepository(dbContext);
var executionRepository = new ExecutionRepository(dbContext);

// Create required services
var cronService = new CronExpressionService();
var retryService = new RetryService(jobRepository, executionRepository, LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<RetryService>());
var concurrencyManager = new ConcurrencyManager();
var executorService = new JobExecutorService(jobRepository, executionRepository, retryService, concurrencyManager, LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<JobExecutorService>());

// Create JobSchedulerService
var schedulerService = new JobSchedulerService(
    jobRepository,
    executionRepository,
    executorService,
    cronService,
    retryService,
    concurrencyManager,
    LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<JobSchedulerService>()
);

// Create and schedule a new job
var newJob = new Job
{
    Name = "Data Export Job",
    Description = "Exports customer data to external system every hour",
    CronExpression = "0 * * * *",
    TimeZoneId = "America/New_York",
    HandlerType = "JobScheduler.Jobs.DataExportJob, JobScheduler.Jobs",
    HandlerParameters = "{\"format\":\"csv\",\"exportPath\":\"/data/exports\"}",
    Priority = JobPriority.High,
    MaxConcurrentExecutions = 2,
    MaxRetries = 3,
    RetryBackoffSeconds = 60,
    ExecutionTimeoutSeconds = 3600,
    IsActive = true
};

var createdJob = await schedulerService.CreateJobAsync(newJob, "admin@example.com");
Console.WriteLine($"Created job: {createdJob.Name} (Id: {createdJob.Id})");
Console.WriteLine($"Next execution: {createdJob.NextExecutionAt}");

// Execute due jobs (typically called by background service)
var executions = await schedulerService.ExecuteDueJobsAsync();
Console.WriteLine($"Executed {executions.Count()} jobs");

// Process retries for failed executions
var retryExecutions = await schedulerService.ProcessRetriesAsync();
Console.WriteLine($"Created {retryExecutions.Count()} retry executions");

// Get job details
var jobDetails = await schedulerService.GetJobDetailsAsync(createdJob.Id);
Console.WriteLine($"Job {jobDetails.Job.Name} has {jobDetails.TotalExecutions} total executions");

// Update job schedule
var updatedJob = await schedulerService.UpdateJobScheduleAsync(
    createdJob.Id,
    "0 */2 * * *", // Every 2 hours
    "admin@example.com"
);
Console.WriteLine($"Updated schedule: {updatedJob.CronExpression}");

// Suspend and resume a job
await schedulerService.SuspendJobAsync(createdJob.Id, "Maintenance window", "admin@example.com");
Console.WriteLine("Job suspended");

await schedulerService.ResumeJobAsync(createdJob.Id, "admin@example.com");
Console.WriteLine("Job resumed");

// Trigger immediate execution
var execution = await schedulerService.TriggerJobExecutionAsync(createdJob.Id);
Console.WriteLine($"Triggered execution: {execution?.Id}");

// Get system statistics
var stats = await schedulerService.GetSchedulerStatisticsAsync();
Console.WriteLine($"Total jobs: {stats.TotalJobs}");
Console.WriteLine($"Active jobs: {stats.ActiveJobs}");
Console.WriteLine($"Success rate: {stats.AverageSuccessRate}%");

// Get execution history
var history = await schedulerService.GetExecutionHistoryAsync(createdJob.Id, limit: 10);
Console.WriteLine($"Found {history.Count()} execution records");

// Delete a job
await schedulerService.DeleteJobAsync(createdJob.Id);
Console.WriteLine("Job deleted");
```

## JobHistoryService

`JobHistoryService` provides rich querying, filtering, and aggregation capabilities over job execution history. It complements the per-job history exposed by `JobSchedulerService` with system-wide views, time-range filtering, and aggregated statistics for monitoring and analysis purposes.

The service offers both job-specific and system-wide history retrieval with pagination support, allowing you to query execution records by status, date range, and page through results efficiently.

Example usage:

```csharp
using JobScheduler.Core.Services;
using JobScheduler.Core.Domain.Models;
using JobScheduler.Core.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

// Setup DI services (typically done in Program.cs)
var services = new ServiceCollection();
services.AddLogging(configure => configure.AddConsole());

var serviceProvider = services.BuildServiceProvider();

// Create JobHistoryService (requires repositories)
var executionRepository = new ExecutionRepository(dbContext);
var jobRepository = new JobRepository(dbContext);
var historyService = new JobHistoryService(executionRepository, jobRepository);

// Query job-specific execution history with pagination
var jobHistory = await historyService.GetJobHistoryAsync(
    jobId: Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
    query: new JobHistoryQuery
    {
        Status = ExecutionStatus.Success,
        From = DateTime.UtcNow.AddDays(-30),
        To = DateTime.UtcNow,
        PageNumber = 1,
        PageSize = 25
    }
);

Console.WriteLine($"Found {jobHistory.TotalCount} total executions");
Console.WriteLine($"Page {jobHistory.PageNumber} of {jobHistory.TotalPages}");
Console.WriteLine($"Items on page: {jobHistory.Items.Count}");

foreach (var execution in jobHistory.Items)
{
    Console.WriteLine($"Execution {execution.Id}: {execution.GetStatusText()} in {execution.DurationMilliseconds}ms");
}

// Query system-wide execution history
var systemHistory = await historyService.GetSystemHistoryAsync(
    query: new JobHistoryQuery
    {
        PageNumber = 1,
        PageSize = 50
    }
);

Console.WriteLine($"\nSystem history: {systemHistory.TotalCount} total executions across all jobs");

// Get aggregated summary for a specific job
var jobSummary = await historyService.GetJobSummaryAsync(
    jobId: Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
    from: DateTime.UtcNow.AddDays(-7),
    to: DateTime.UtcNow
);

Console.WriteLine($"\nJob Summary:");
Console.WriteLine($"Total executions: {jobSummary.TotalExecutions}");
Console.WriteLine($"Success rate: {jobSummary.SuccessRate}%");
Console.WriteLine($"Average duration: {jobSummary.AverageDurationMs}ms");
Console.WriteLine($"Last executed: {jobSummary.LastExecutedAt?.ToString("u")}");

// Get system-wide aggregated summary
var systemSummary = await historyService.GetSystemSummaryAsync(
    from: DateTime.UtcNow.AddDays(-1),
    to: DateTime.UtcNow
);

Console.WriteLine($"\nSystem Summary:");
Console.WriteLine($"Total executions: {systemSummary.TotalExecutions}");
Console.WriteLine($"Success rate: {systemSummary.SuccessRate}%");
```

## JobDependency

The `JobDependency` entity defines a dependency relationship between two jobs, ensuring that one job (`Job`) only executes after another job (`DependsOnJob`) has completed successfully. It tracks the dependency through foreign keys and provides navigation properties for easy access to both jobs. This is useful for creating workflows where certain operations must run in a specific order.

Example usage:
```csharp
using JobScheduler.Core.Domain.Entities;

// Create a dependency between two jobs
var dependency = new JobDependency
{
    JobId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"),  // The job that depends on another
    DependsOnJobId = Guid.Parse("5fb3e564-5717-4562-b3fc-2c963f66afa7"),  // The job that must complete first
    CreatedBy = "scheduler-service"
};

Console.WriteLine($"Created dependency: Job {dependency.JobId} depends on Job {dependency.DependsOnJobId}");

// Access the related jobs through navigation properties
if (dependency.Job != null)
{
    Console.WriteLine($"Dependent job: {dependency.Job.Name}");
}

if (dependency.DependsOnJob != null)
{
    Console.WriteLine($"Required job: {dependency.DependsOnJob.Name}");
}

// Check when the dependency was created
dependency.CreatedAt = DateTime.UtcNow;
Console.WriteLine($"Dependency created at: {dependency.CreatedAt:u}");
```

## DependencyGraphValidationResult

`DependencyGraphValidationResult` represents the outcome of validating the entire job dependency graph for cycles and structural integrity. It indicates whether the graph is a valid Directed Acyclic Graph (DAG) and provides details about any detected cycles, including the nodes involved and a human-readable message.

Example usage:

```csharp
using JobScheduler.Core.Services;
using JobScheduler.Core.Domain.Entities;

// Create a job dependency service (typically injected via DI)
var service = new JobDependencyService(dbContext);

// Validate the dependency graph
var validationResult = await service.ValidateGraphAsync();

// Check if the graph is valid
if (validationResult.IsValid)
{
    Console.WriteLine("✅ Dependency graph is valid - no cycles detected.");
    Console.WriteLine($"Message: {validationResult.Message}");
}
else
{
    Console.WriteLine("❌ Dependency graph validation failed - cycle detected!");
    Console.WriteLine($"Message: {validationResult.Message}");
    
    // Access cycle information
    Console.WriteLine($"Cycle involves {validationResult.CycleNodes.Count} job(s):");
    foreach (var nodeId in validationResult.CycleNodes)
    {
        var job = await dbContext.Jobs.FindAsync(nodeId);
        Console.WriteLine($"  - {job?.Name ?? nodeId.ToString()}");
    }
    
    // The cycle can be visualized as: A → B → C → A
    Console.WriteLine($"Cycle path: {string.Join(" → ", validationResult.CycleNodes)}");
}
```

## Job

The `Job` entity represents a scheduled job in the distributed job scheduler system. It contains the job's configuration, scheduling rules (via cron expression), retry policies, execution limits, and tracking metrics. Jobs can be prioritized, have configurable concurrency limits, and support timezone-aware scheduling.

Example usage:
```csharp
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Domain.Enums;

// Create a new scheduled job
var job = new Job
{
    Name = "Data Export Job",
    Description = "Exports customer data to CSV format every day at midnight",
    CronExpression = "0 0 * * *",
    TimeZoneId = "America/New_York",
    HandlerType = "JobScheduler.Jobs.DataExportJob, JobScheduler.Jobs",
    HandlerParameters = "{\"format\":\"csv\",\"exportPath\":\"/data/exports\"}",
    Priority = JobPriority.High,
    MaxConcurrentExecutions = 2,
    MaxRetries = 3,
    RetryBackoffSeconds = 60,
    ExecutionTimeoutSeconds = 3600,
    IsActive = true,
    CreatedBy = "admin@example.com"
};

// Validate job configuration before scheduling
bool isValid = job.IsValidForScheduling();
Console.WriteLine($"Job is valid for scheduling: {isValid}");

// Update execution metrics after job runs
job.UpdateExecutionMetrics(success: true);
Console.WriteLine($"Success rate: {job.GetSuccessRate()}%");

// Check if job can execute now based on concurrency limits
bool canExecute = job.CanExecuteNow(currentConcurrentCount: 0);
Console.WriteLine($"Can execute now: {canExecute}");

// Mark job as updated by a specific user
job.MarkAsUpdated(updatedBy: "scheduler-service");
```

## JobResponse

`JobResponse` is a serializable model that represents job information for API responses. It provides a clean, read-only view of job data with computed metrics like success rate, making it ideal for returning job information to clients without exposing internal implementation details.

## ExecutionResponse

`ExecutionResponse` is a response model that represents the result of a job execution, containing status information, timing metrics, and error details. It's typically returned by API endpoints that query execution history and is useful for monitoring job performance and debugging execution issues.

Example usage:

```csharp
using JobScheduler.Core.Domain.Models;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Domain.Enums;

// Create an ExecutionResponse from a completed job execution
var execution = new JobExecution
{
    Id = Guid.NewGuid(),
    JobId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
    Status = nameof(ExecutionStatus.Success),
    StartedAt = DateTime.UtcNow.AddMinutes(-5),
    CompletedAt = DateTime.UtcNow,
    DurationMilliseconds = 300000,
    AttemptNumber = 1,
    ExecutionTimeMs = 295000,
    RetryAttempt = 0,
    ErrorMessage = null,
    ExecutorName = "data-processor-01",
    IsRetryable = false,
    CreatedAt = DateTime.UtcNow.AddDays(-1)
};

// Convert to ExecutionResponse
var response = ExecutionResponse.FromExecution(execution);

// Display execution information
Console.WriteLine($"Execution {response.Id} for job {response.JobId}:");
Console.WriteLine($"Status: {response.GetStatusText()}");
Console.WriteLine($"Duration: {response.DurationMilliseconds}ms");
Console.WriteLine($"Started at: {response.StartedAt:u}");
if (response.CompletedAt.HasValue)
{
    Console.WriteLine($"Completed at: {response.CompletedAt.Value:u}");
}
Console.WriteLine($"Attempt: #{response.AttemptNumber}");
Console.WriteLine($"Retry attempt: {response.RetryAttempt}");
Console.WriteLine($"Executor: {response.ExecutorName}");
Console.WriteLine($"Retryable: {response.IsRetryable}");

// Handle errors if present
if (!string.IsNullOrEmpty(response.ErrorMessage))
{
    Console.WriteLine($"Error: {response.ErrorMessage}");
}
```

Example usage:
```csharp
using JobScheduler.Core.Domain.Models;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Domain.Enums;

// Create a job response from a job entity
var job = new Job
{
    Id = Guid.NewGuid(),
    Name = "Data Export Job",
    Description = "Exports customer data to CSV format every day at midnight",
    CronExpression = "0 0 * * *",
    TimeZoneId = "America/New_York",
    HandlerType = "JobScheduler.Jobs.DataExportJob, JobScheduler.Jobs",
    Priority = JobPriority.High,
    MaxConcurrentExecutions = 2,
    MaxRetries = 3,
    ExecutionTimeoutSeconds = 3600,
    IsActive = true,
    CreatedAt = DateTime.UtcNow
};

// Convert to JobResponse
var response = JobResponse.FromJob(job);

Console.WriteLine($"Job: {response.Name} (Id: {response.Id})");
Console.WriteLine($"Status: {response.Status}");
Console.WriteLine($"Next execution: {response.NextExecutionAt}");
Console.WriteLine($"Success rate: {response.SuccessRate}%");
Console.WriteLine($"Concurrency limit: {response.MaxConcurrentExecutions}");
```

## RetryPolicy

`RetryPolicy` defines how a job should be retried after a failure, including the maximum number of attempts, back‑off strategy, and which exception types are considered retryable. It provides helper methods to calculate delay intervals, validate the configuration, and generate human‑readable descriptions of the strategy.

## JobScheduleHistory

The `JobScheduleHistory` entity tracks historical changes to job schedules and configurations, providing a complete audit trail for schedule modifications, status changes, and other property updates. It captures who made the change, when it occurred, the reason for the change, and both old and new values for comparison.

Example usage:
```csharp
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Domain.Enums;

// Track a property change on a job
var propertyChange = JobScheduleHistory.CreateChange(
    jobId: Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
    propertyName: "MaxConcurrentExecutions",
    oldValue: "1",
    newValue: "3",
    changeReason: "Increased concurrency to handle load",
    changedBy: "admin@example.com"
);

Console.WriteLine(propertyChange.GetChangeDescription());
Console.WriteLine($"Changed at: {propertyChange.ChangedAt:u}");
Console.WriteLine($"Change reason: {propertyChange.ChangeReason}");

// Track a status change
var statusChange = JobScheduleHistory.CreateStatusChange(
    jobId: Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
    oldStatus: "Inactive",
    newStatus: "Active",
    reason: "Job enabled for production use",
    changedBy: "scheduler-service"
);

Console.WriteLine(statusChange.GetChangeDescription());

// Track a cron expression change
var cronChange = JobScheduleHistory.CreateCronChange(
    jobId: Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
    oldCron: "0 0 * * *",
    newCron: "0 */2 * * *",
    changedBy: "admin@example.com"
);

Console.WriteLine(cronChange.GetChangeDescription());

// Validate history entry
bool isValid = propertyChange.IsValid();
Console.WriteLine($"Is valid: {isValid}");
```

Example usage:
```csharp
using System;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Constants;

// Create a retry policy for a specific job
var policy = new RetryPolicy
{
    JobId = Guid.NewGuid(),
    MaxRetries = 5,
    InitialBackoffSeconds = 10,
    MaxBackoffSeconds = 300,
    Strategy = BackoffStrategy.Exponential,
    BackoffMultiplier = 2.0,
    RetryOnTimeout = true,
    RetryOnCancellation = false,
    RetryableExceptions = "TimeoutException,TransientException"
};

// Validate the policy configuration
if (!policy.IsValid())
{
    Console.WriteLine("Invalid retry policy configuration.");
    return;
}

// Show a description of the chosen back‑off strategy
Console.WriteLine(policy.GetStrategyDescription());

// Calculate the back‑off delay for the third retry attempt
int delaySeconds = policy.CalculateBackoffDelay(attemptNumber: 3);
Console.WriteLine($"Back‑off delay for attempt 3: {delaySeconds}s");

// Determine whether the policy allows a retry for a given exception type
bool shouldRetry = policy.ShouldRetryOnException("System.TimeoutException");
Console.WriteLine($"Should retry on TimeoutException: {shouldRetry}");

// Compute the next scheduled retry time after a failure
DateTime lastFailure = DateTime.UtcNow;
DateTime nextRetry = policy.GetNextRetryTime(lastFailure, attemptNumber: 2);
Console.WriteLine($"Next retry scheduled at: {nextRetry:u}");
```

## PerformanceAnalysisResponse

`PerformanceAnalysisResponse` provides detailed performance metrics and timing analysis for job executions, including percentile data (median, P95, P99), average, minimum, and maximum execution times, as well as timestamps for the fastest and slowest executions. This model is useful for performance monitoring, identifying outliers, and optimizing job execution times.

Example usage:

```csharp
using JobScheduler.Core.Domain.Models;

// Create a PerformanceAnalysisResponse instance with execution metrics
var analysis = new PerformanceAnalysisResponse
{
    JobId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
    AverageExecutionTimeMs = 150,
    MedianExecutionTimeMs = 120,
    P95ExecutionTimeMs = 300,
    P99ExecutionTimeMs = 500,
    SlowestExecutionTimeMs = 2500,
    FastestExecutionTimeMs = 50,
    SlowestExecutionAt = DateTime.UtcNow.AddHours(-1),
    FastestExecutionAt = DateTime.UtcNow.AddDays(-7)
};

// Display performance metrics
Console.WriteLine($"Performance analysis for job {analysis.JobId}:");
Console.WriteLine($"Average execution time: {analysis.AverageExecutionTimeMs}ms");
Console.WriteLine($"Median execution time: {analysis.MedianExecutionTimeMs}ms");
Console.WriteLine($"P95 execution time: {analysis.P95ExecutionTimeMs}ms");
Console.WriteLine($"P99 execution time: {analysis.P99ExecutionTimeMs}ms");
Console.WriteLine($"Slowest execution: {analysis.SlowestExecutionTimeMs}ms at {analysis.SlowestExecutionAt?.ToString("u")}");
Console.WriteLine($"Fastest execution: {analysis.FastestExecutionTimeMs}ms at {analysis.FastestExecutionAt?.ToString("u")}");
```

## ExecutionStatsResponse

`ExecutionStatsResponse` is a response model that provides comprehensive execution statistics for a job, including success rates, execution time metrics, and timing information. It's typically returned by API endpoints that query job performance data and is useful for monitoring, alerting, and performance analysis.

Example usage:

```csharp
using JobScheduler.Core.Domain.Models;

// Create an ExecutionStatsResponse instance from job statistics
var stats = new ExecutionStatsResponse
{
    JobId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
    TotalExecutions = 1000,
    SuccessfulExecutions = 950,
    FailedExecutions = 50,
    SuccessRate = 95.0,
    AverageExecutionTimeMs = 150,
    MinExecutionTimeMs = 50,
    MaxExecutionTimeMs = 2500,
    LastExecutionAt = DateTime.UtcNow
};

// Display statistics
Console.WriteLine($"Job {stats.JobId} statistics:");
Console.WriteLine($"Total executions: {stats.TotalExecutions}");
Console.WriteLine($"Successful: {stats.SuccessfulExecutions} ({stats.SuccessRate}%)");
Console.WriteLine($"Failed: {stats.FailedExecutions}");
Console.WriteLine($"Average execution time: {stats.AverageExecutionTimeMs}ms");
Console.WriteLine($"Min execution time: {stats.MinExecutionTimeMs}ms");
Console.WriteLine($"Max execution time: {stats.MaxExecutionTimeMs}ms");
Console.WriteLine($"Last execution: {stats.LastExecutionAt?.ToString("u") ?? "Never"}");

// Calculate derived metrics
int failureRate = (int)(100 - stats.SuccessRate);
Console.WriteLine($"Failure rate: {failureRate}%");
```

## DatabaseLeaderElectionService

`DatabaseLeaderElectionService` provides distributed leader election for multi-node scheduler deployments using a simple database row as the coordination mechanism. It ensures that only one scheduler instance is active at any time by managing a lease in the `SchedulerLeaderLock` table. This approach requires no external dependencies beyond the existing database and provides automatic failover when the leader instance becomes unavailable.

Example usage:

```csharp
using JobScheduler.Core.Services;
using JobScheduler.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Setup DI services (typically done in Program.cs)
var services = new ServiceCollection();
services.AddDbContext<JobSchedulerContext>(options =>
    options.UseSqlServer("Server=localhost;Database=JobScheduler;Trusted_Connection=True;"));
services.AddLogging(configure => configure.AddConsole());

var serviceProvider = services.BuildServiceProvider();

// Create DatabaseLeaderElectionService (typically injected via DI)
var electionService = serviceProvider.GetRequiredService<DatabaseLeaderElectionService>();

// Acquire leadership
bool acquired = await electionService.TryAcquireLeadershipAsync();
Console.WriteLine($"Leader election successful: {acquired}");
Console.WriteLine($"Is current instance leader: {electionService.IsLeader}");

// Check leadership status periodically
if (electionService.IsLeader)
{
    Console.WriteLine("This instance is the leader - performing leader tasks...");
    
    // Release leadership when shutting down
    await electionService.ReleaseLeadershipAsync();
}

// Get leader information from the database
using var scope = serviceProvider.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<JobSchedulerContext>();
var leaderLock = await context.SchedulerLeaderLocks
    .FirstOrDefaultAsync(l => l.LockName == "scheduler-leader");

if (leaderLock != null)
{
    Console.WriteLine($"Current leader: {leaderLock.LeaderInstanceId}");
    Console.WriteLine($"Lease expires at: {leaderLock.LeaseExpiresAt:u}");
    Console.WriteLine($"Acquired at: {leaderLock.AcquiredAt:u}");
}
```

## ExternalApiClient

The `ExternalApiClient` is a generic HTTP client for making API calls to external services. It provides methods for GET, POST, PUT, and DELETE requests with built-in timeout management, authentication support, and automatic retry logic for transient failures. The client returns strongly-typed responses through the `ApiResponse<T>` wrapper, which includes success status, error messages, and the deserialized response data.

Example usage:

```csharp
using JobScheduler.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Setup DI services (typically done in Program.cs)
var services = new ServiceCollection();
services.AddHttpClient();
services.AddLogging(configure => configure.AddConsole());

var serviceProvider = services.BuildServiceProvider();

// Create ExternalApiClient (typically injected via DI)
var apiClient = serviceProvider.GetRequiredService<ExternalApiClient>();

// Example: GET request to fetch user data
var userResponse = await apiClient.GetAsync<User>(
    url: "https://api.example.com/users/123",
    authToken: "your-access-token"
);

if (userResponse.Success)
{
    Console.WriteLine($"User: {userResponse.Data?.Name}");
    Console.WriteLine($"Email: {userResponse.Data?.Email}");
}
else
{
    Console.WriteLine($"Error: {userResponse.Error}");
}

// Example: POST request to create a resource
var newUser = new { Name = "John Doe", Email = "john@example.com" };
var createResponse = await apiClient.PostAsync<object, UserResponse>(
    url: "https://api.example.com/users",
    data: newUser,
    authToken: "your-access-token"
);

if (createResponse.Success)
{
    Console.WriteLine($"Created user with ID: {createResponse.Data?.Id}");
}

// Example: PUT request to update a resource
var updateData = new { Name = "John Updated", Email = "john.updated@example.com" };
var updateResponse = await apiClient.PutAsync<object, UserResponse>(
    url: "https://api.example.com/users/123",
    data: updateData,
    authToken: "your-access-token"
);

// Example: DELETE request
var deleteResponse = await apiClient.DeleteAsync(
    url: "https://api.example.com/users/123",
    authToken: "your-access-token"
);

if (deleteResponse.Success)
{
    Console.WriteLine("Resource deleted successfully");
}

// Example: GET with automatic retry
var retryResponse = await apiClient.GetWithRetryAsync<User>(
    url: "https://api.example.com/users/123",
    maxRetries: 3,
    authToken: "your-access-token"
);

// Example: Check API availability
bool isAvailable = await apiClient.IsApiAvailableAsync("https://api.example.com/health");
Console.WriteLine($"API is available: {isAvailable}");
```

## SlackNotificationService

`SlackNotificationService` sends real-time notifications to Slack webhook endpoints when critical job events occur, including job failures, successful completions, and scheduler alerts. It formats messages with color-coded attachments, structured fields, and timestamps for easy parsing in Slack channels.

Example usage:

```csharp
using JobScheduler.Core.Services;
using JobScheduler.Core.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Setup DI services (typically done in Program.cs)
var services = new ServiceCollection();
services.AddHttpClient();
services.AddLogging(configure => configure.AddConsole());

var serviceProvider = services.BuildServiceProvider();

// Create SlackNotificationService (typically injected via DI)
var slackService = serviceProvider.GetRequiredService<SlackNotificationService>();

// Example job and execution
var job = new Job
{
    Id = Guid.NewGuid(),
    Name = "Data Export Job",
    Description = "Exports customer data to external system",
    MaxRetries = 3,
    IsActive = true
};

var execution = new JobExecution
{
    Id = Guid.NewGuid(),
    JobId = job.Id,
    Status = nameof(ExecutionStatus.Success),
    StartedAt = DateTime.UtcNow.AddMinutes(-5),
    CompletedAt = DateTime.UtcNow,
    DurationMilliseconds = 120000,
    ExecutionTimeMs = 118500,
    AttemptNumber = 1,
    RetryAttempt = 0,
    IsRetryable = false,
    ErrorMessage = null
};

// Send success notification
await slackService.SendJobSuccessNotificationAsync(job, execution, "https://hooks.slack.com/services/YOUR/WEBHOOK/URL");

// Send failure notification (example with error)
execution.MarkAsFailed("Connection timeout", "System.TimeoutException: Operation timed out after 30 seconds", retryable: true);
await slackService.SendJobFailureNotificationAsync(job, execution, "https://hooks.slack.com/services/YOUR/WEBHOOK/URL");

// Send scheduler alert
await slackService.SendSchedulerAlertAsync(
    "High Memory Usage Detected",
    "The scheduler is approaching memory limits. Consider scaling up or optimizing job handlers.",
    "Warning",
    "https://hooks.slack.com/services/YOUR/WEBHOOK/URL"
);
```

## PerformanceMonitor

`PerformanceMonitor` collects and analyzes performance metrics for job executions, including execution times, success rates, throughput, CPU utilization, and memory usage. It provides methods to record metrics and retrieve aggregated statistics for monitoring, diagnostics, and dashboard visualization.

Example usage:

```csharp
using JobScheduler.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Setup DI services (typically done in Program.cs)
var services = new ServiceCollection();
services.AddLogging(configure => configure.AddConsole());

var serviceProvider = services.BuildServiceProvider();

// Create PerformanceMonitor (typically injected via DI)
var performanceMonitor = serviceProvider.GetRequiredService<PerformanceMonitor>();

// Record execution metrics after a job completes
var jobId = Guid.NewGuid();
performanceMonitor.RecordExecutionTime(jobId, "Data Export Job", 1500, success: true);
performanceMonitor.RecordExecutionTime(jobId, "Data Export Job", 2500, success: true);
performanceMonitor.RecordExecutionTime(jobId, "Data Export Job", 800, success: true);
performanceMonitor.RecordExecutionTime(jobId, "Data Export Job", 3200, success: false);
performanceMonitor.RecordExecutionTime(Guid.NewGuid(), "Import Job", 1200, success: true);

// Get aggregated metrics for a specific job
var avgTime = performanceMonitor.GetAverageExecutionTime(jobId);
Console.WriteLine($"Average execution time: {avgTime}ms");

var successRate = performanceMonitor.GetSuccessRate(jobId);
Console.WriteLine($"Success rate: {successRate}%");

var p95Time = performanceMonitor.GetPercentileExecutionTime(jobId, 95);
Console.WriteLine($"P95 execution time: {p95Time}ms");

// Get system-wide metrics
var systemAvgTime = performanceMonitor.GetAverageExecutionTimeMs();
Console.WriteLine($"System average execution time: {systemAvgTime}ms");

var throughput = performanceMonitor.GetThroughputPerMinute();
Console.WriteLine($"Throughput: {throughput} executions/minute");

var cpuUsage = performanceMonitor.GetCpuUtilization();
Console.WriteLine($"CPU utilization: {cpuUsage}%");

var memoryUsage = performanceMonitor.GetMemoryUsageMb();
Console.WriteLine($"Memory usage: {memoryUsage}MB");

// Get timeline data for dashboard
var timeline = await performanceMonitor.GetPerformanceTimelineAsync(DateTime.UtcNow.AddHours(-24));
Console.WriteLine($"Timeline points: {timeline.Count}");

foreach (var point in timeline)
{
    Console.WriteLine($"  {point.Timestamp:u}: {point.ExecutionCount} executions ({point.SuccessCount} success, {point.FailureCount} failure), avg {point.AverageExecutionTimeMs}ms");
}

// Get summary statistics
var summary = performanceMonitor.GetSummary();
Console.WriteLine($"Total executions: {summary.TotalExecutions}");
Console.WriteLine($"Success rate: {summary.SuccessRate}%");
Console.WriteLine($"Average time: {summary.AverageExecutionTimeMs}ms");
Console.WriteLine($"Memory usage: {summary.MemoryUsageMb}MB");

// Clear metrics when needed
performanceMonitor.ClearMetrics();
```

## RetryService

The `RetryService` handles job retry logic, backoff strategies, and retry scheduling. It manages exponential, linear, and fixed backoff delays, determines when retries are appropriate, and provides statistics about retry behavior.

Example usage:

```csharp
using JobScheduler.Core.Services;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Constants;
using Microsoft.Extensions.Logging;

// Create repositories (typically injected via DI)
var jobRepository = new JobRepository(dbContext);
var executionRepository = new ExecutionRepository(dbContext);
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

// Create retry service
var retryService = new RetryService(jobRepository, executionRepository, loggerFactory.CreateLogger<RetryService>());

// Example job and execution
var job = new Job
{
    Id = Guid.NewGuid(),
    Name = "Data Processing Job",
    MaxRetries = 3,
    RetryBackoffSeconds = 10,
    ExecutionTimeoutSeconds = 300,
    IsActive = true
};

var failedExecution = new JobExecution
{
    Id = Guid.NewGuid(),
    JobId = job.Id,
    Status = ExecutionStatus.Failed,
    AttemptNumber = 1,
    CompletedAt = DateTime.UtcNow,
    IsRetryable = true
};

// Check if should retry
bool shouldRetry = await retryService.ShouldRetryAsync(job, failedExecution);
Console.WriteLine($"Should retry: {shouldRetry}");

// Calculate backoff delay for next retry
int delaySeconds = retryService.CalculateBackoffDelay(job, failedExecution.AttemptNumber);
Console.WriteLine($"Next retry delay: {delaySeconds} seconds");

// Calculate next retry time
DateTime nextRetryTime = retryService.CalculateNextRetryTime(job, failedExecution);
Console.WriteLine($"Next retry scheduled at: {nextRetryTime:u}");

// Create retry execution
var retryExecution = retryService.CreateRetryExecution(job, failedExecution);
Console.WriteLine($"Created retry execution {retryExecution.Id} (attempt {retryExecution.AttemptNumber})");

// Check retry budget
bool budgetExceeded = await retryService.IsRetryBudgetExceededAsync(job.Id);
Console.WriteLine($"Retry budget exceeded: {budgetExceeded}");

// Get retry statistics
var stats = await retryService.GetRetryStatisticsAsync(job.Id);
Console.WriteLine($"Total executions: {stats.TotalExecutions}");
Console.WriteLine($"Total failures: {stats.TotalFailures}");
Console.WriteLine($"Total retries: {stats.TotalRetries}");
Console.WriteLine($"Average retries per failure: {stats.AverageRetriesPerFailure:F2}");
Console.WriteLine($"Recent failure rate: {stats.RecentFailureRate:F1}%");

// Calculate standalone retry delay
var delay = retryService.CalculateRetryDelay(2, JobRetryBackoffStrategy.Exponential, 5);
Console.WriteLine($"Standalone retry delay for attempt 2: {delay.TotalSeconds}s");

// Check simple retry condition
bool simpleShouldRetry = retryService.ShouldRetry(currentAttempts: 2, maxRetries: 5);
Console.WriteLine($"Simple retry check: {simpleShouldRetry}");

// Calculate total retry time
var totalDelay = retryService.CalculateTotalRetryTime(3, JobRetryBackoffStrategy.Exponential, 5);
Console.WriteLine($"Total delay for 3 retries: {totalDelay.TotalSeconds}s");

// Format retry message
string message = retryService.FormatRetryMessage(2, delay, "processor-01");
Console.WriteLine(message);
```

## WebhookNotificationService

The `WebhookNotificationService` sends real-time notifications to external webhook endpoints when jobs complete or fail. It supports delivery retry with exponential backoff, HMAC signature verification for security, and webhook configuration management including registration, retrieval, and testing.

Example usage:

```csharp
using JobScheduler.Core.Services;
using JobScheduler.Core.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Setup DI services (typically done in Program.cs)
var services = new ServiceCollection();
services.AddHttpClient();
services.AddLogging(configure => configure.AddConsole());
services.AddSingleton<CacheService>();

var serviceProvider = services.BuildServiceProvider();

// Create webhook notification service
var webhookService = serviceProvider.GetRequiredService<WebhookNotificationService>();

// Example job and execution
var job = new Job
{
    Id = Guid.NewGuid(),
    Name = "Data Export Job",
    Description = "Exports customer data to external system"
};

var execution = new JobExecution
{
    Id = Guid.NewGuid(),
    JobId = job.Id,
    Status = ExecutionStatus.Success,
    StartedAt = DateTime.UtcNow.AddMinutes(-5),
    CompletedAt = DateTime.UtcNow,
    DurationMilliseconds = 120000,
    ExecutionTimeMs = 118500,
    AttemptNumber = 1,
    RetryAttempt = 0,
    IsRetryable = false
};

// Register a webhook endpoint
await webhookService.RegisterWebhookAsync(
    jobId: job.Id,
    webhookUrl: "https://api.example.com/webhooks/job-events",
    secret: "your-webhook-secret-key"
);

// Get webhook configuration
var config = await webhookService.GetWebhookConfigAsync(job.Id);
Console.WriteLine($"Webhook registered: {config?.WebhookUrl}");

// Test webhook connectivity
var testResult = await webhookService.TestWebhookAsync(
    webhookUrl: "https://api.example.com/webhooks/job-events",
    secret: "your-webhook-secret-key"
);
Console.WriteLine($"Test result: {(testResult.Success ? "Success" : "Failed")} - {testResult.Message}");

// Send execution notification
if (config != null)
{
    await webhookService.SendExecutionNotificationAsync(job, execution, config);
}

// Unregister webhook when no longer needed
await webhookService.UnregisterWebhookAsync(job.Id);
```

## RateLimitMiddleware

The `RateLimitMiddleware` is an ASP.NET Core middleware component that implements rate limiting to prevent abuse and ensure fair resource allocation. It uses a sliding window algorithm to track requests per client (IP or authenticated user) and enforces configurable limits on the number of requests allowed within a time window. When the limit is exceeded, the middleware returns HTTP 429 (Too Many Requests) with a `Retry-After` header indicating when requests can be attempted again.

Example usage:

```csharp
using JobScheduler.Core.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.AddConsole();

// Add services to the container
builder.Services.AddControllers();

var app = builder.Build();

// Configure rate limiting with custom settings (100 requests per 30 seconds)
app.UseMiddleware<RateLimitMiddleware>(new RateLimitSettings
{
    RequestsPerWindow = 100,
    WindowSizeSeconds = 30
});

// Configure other middleware
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

## GlobalExceptionMiddleware

The `GlobalExceptionMiddleware` is an ASP.NET Core middleware component that catches all unhandled exceptions during HTTP request processing. It ensures consistent error responses, prevents sensitive error information leakage in production, and logs all errors for audit and debugging purposes. The middleware maps specific exception types to appropriate HTTP status codes and provides detailed error information in development environments.

Example usage:

```csharp
using JobScheduler.Core.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add the middleware to the pipeline
builder.Services.AddControllers();
var app = builder.Build();

// Register GlobalExceptionMiddleware
app.UseMiddleware<GlobalExceptionMiddleware>();

// Configure other middleware
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

Example with custom exception handling:

```csharp
using JobScheduler.Core.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.AddConsole();

// Add services to the container
builder.Services.AddControllers();

var app = builder.Build();

// Use GlobalExceptionMiddleware for consistent error handling
app.UseMiddleware<GlobalExceptionMiddleware>();

// Example endpoint that might throw exceptions
app.MapGet("/jobs/{id}", (Guid id) => 
{
    if (id == Guid.Empty)
    {
        throw new JobValidationException("Job ID cannot be empty");
    }
    
    // Job processing logic here
    return Results.Ok(new { JobId = id });
});

app.Run();
```

## LoggingMiddleware

The `LoggingMiddleware` is an ASP.NET Core middleware component that logs HTTP request details including method, path, query string, headers, request body, response status code, response headers, and response body. It captures timing information and provides a structured way to track request execution for debugging and monitoring purposes.

Example usage:

```csharp
using JobScheduler.Core.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.AddConsole();

// Add services to the container
builder.Services.AddControllers();

var app = builder.Build();

// Use LoggingMiddleware to log HTTP requests and responses
app.UseMiddleware<LoggingMiddleware>();

// Configure other middleware
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

Example with custom configuration:

```csharp
using JobScheduler.Core.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.AddConsole();

// Add services to the container
builder.Services.AddControllers();

var app = builder.Build();

// Use LoggingMiddleware with custom configuration
app.UseMiddleware<LoggingMiddleware>(new LoggingMiddlewareOptions
{
    LogRequestBody = true,
    LogResponseBody = true,
    LogHeaders = true,
    IncludeTiming = true
});

// Configure other middleware
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

## JobHistoryQuery

`JobHistoryQuery` is a model for filtering and paginating job execution history records. It provides optional filters for execution status, time ranges, and pagination controls to retrieve specific subsets of historical job executions.

Example usage:

```csharp
using JobScheduler.Core.Domain.Models;
using JobScheduler.Core.Domain.Enums;

// Create a query to filter job history by status and date range
var query = new JobHistoryQuery
{
    Status = ExecutionStatus.Success,
    From = DateTime.UtcNow.AddDays(-7),
    To = DateTime.UtcNow,
    PageNumber = 1,
    PageSize = 50
};

// Normalize the query (clamps PageSize to 1-200, ensures PageNumber >= 1)
var normalizedQuery = query.Normalize();

Console.WriteLine($"Querying history for status: {normalizedQuery.Status}");
Console.WriteLine($"From: {normalizedQuery.From?.ToString("u")}");
Console.WriteLine($"To: {normalizedQuery.To?.ToString("u")}");
Console.WriteLine($"Page: {normalizedQuery.PageNumber} (size: {normalizedQuery.PageSize})");
```

## IJobHandler

`IJobHandler` is the core interface that your job implementations must implement to define the actual work performed by scheduled jobs. The job scheduler invokes your handler through `JobExecutorService.ExecuteJobAsync()` to execute jobs, validate them for execution, and retrieve execution statistics. Handlers are responsible for implementing the core business logic while the scheduler manages timing, retries, concurrency, and persistence.

Example usage:

```csharp
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Services;
using Microsoft.Extensions.Logging;

// Implement IJobHandler for your specific job type
public class DataExportJob : IJobHandler
{
    private readonly ILogger<DataExportJob> _logger;
    private readonly IJobExecutionRepository _executionRepository;

    public DataExportJob(ILogger<DataExportJob> logger, IJobExecutionRepository executionRepository)
    {
        _logger = logger;
        _executionRepository = executionRepository;
    }

    public Guid JobId { get; } = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");

    public int TotalExecutions => _executionRepository.GetTotalExecutions(JobId);
    public int SuccessfulExecutions => _executionRepository.GetSuccessfulExecutions(JobId);
    public int FailedExecutions => _executionRepository.GetFailedExecutions(JobId);
    public int TimedOutExecutions => _executionRepository.GetTimedOutExecutions(JobId);
    public int SkippedExecutions => _executionRepository.GetSkippedExecutions(JobId);
    public long AverageDurationMs => _executionRepository.GetAverageDurationMs(JobId);
    public double SuccessRate => _executionRepository.GetSuccessRate(JobId);

    public async Task<JobExecution> ExecuteJobAsync(JobExecution execution, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting job execution {ExecutionId} for job {JobId}", execution.Id, JobId);

        try
        {
            // Your job logic here
            await ExportDataAsync(execution, cancellationToken);

            // Mark execution as completed successfully
            execution.MarkAsCompleted(ExecutionStatus.Success);
            _logger.LogInformation("Job execution {ExecutionId} completed successfully in {Duration}ms", 
                execution.Id, execution.DurationMilliseconds);
        }
        catch (OperationCanceledException)
        {
            execution.MarkAsFailed("Job was cancelled", "Operation was cancelled by user", retryable: false);
            _logger.LogWarning("Job execution {ExecutionId} was cancelled", execution.Id);
        }
        catch (Exception ex)
        {
            execution.MarkAsFailed("Export failed", ex.ToString(), retryable: true);
            _logger.LogError(ex, "Job execution {ExecutionId} failed", execution.Id);
        }

        return execution;
    }

    public async Task<(bool CanExecute, string? Reason)> ValidateJobForExecutionAsync(Job job, CancellationToken cancellationToken = default)
    {
        // Check if job is active
        if (!job.IsActive)
        {
            return (false, "Job is not active");
        }

        // Check concurrency limits
        var currentConcurrent = await _executionRepository.GetCurrentConcurrentExecutionsAsync(JobId);
        if (currentConcurrent >= job.MaxConcurrentExecutions)
        {
            return (false, $"Concurrency limit reached ({currentConcurrent}/{job.MaxConcurrentExecutions})");
        }

        // Check execution timeout
        if (job.ExecutionTimeoutSeconds > 0 && job.ExecutionTimeoutSeconds < 30)
        {
            return (false, "Execution timeout is too short (minimum 30 seconds)");
        }

        return (true, null);
    }

    public async Task<ExecutionStatistics> GetExecutionStatisticsAsync(CancellationToken cancellationToken = default)
    {
        return new ExecutionStatistics
        {
            TotalExecutions = TotalExecutions,
            SuccessfulExecutions = SuccessfulExecutions,
            FailedExecutions = FailedExecutions,
            TimedOutExecutions = TimedOutExecutions,
            SkippedExecutions = SkippedExecutions,
            AverageDurationMs = AverageDurationMs,
            SuccessRate = SuccessRate
        };
    }

    private async Task ExportDataAsync(JobExecution execution, CancellationToken cancellationToken)
    {
        // Your actual job implementation
        await Task.Delay(1000, cancellationToken); // Simulate work
    }
}

// Usage with JobExecutorService (typically via dependency injection)
public class JobProcessor
{
    private readonly JobExecutorService _executor;
    private readonly ILogger<JobProcessor> _logger;

    public JobProcessor(JobExecutorService executor, ILogger<JobProcessor> logger)
    {
        _executor = executor;
        _logger = logger;
    }

    public async Task ExecuteJob(Guid jobId, CancellationToken cancellationToken = default)
    {
        try
        {
            var execution = await _executor.ExecuteJobAsync(jobId, cancellationToken);
            _logger.LogInformation("Job {JobId} executed in {Duration}ms", 
                jobId, execution.DurationMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute job {JobId}", jobId);
        }
    }
}
```

## JobPipeline

`JobPipeline` represents an ordered collection of jobs that are executed sequentially. Each pipeline contains a list of `JobPipelineStep` objects that define which job runs at each position and whether the pipeline should stop if a step fails. Pipelines are useful for modeling linear workflows where later jobs depend on the successful completion of earlier ones.

Example usage:
```csharp
using System;
using System.Collections.Generic;
using JobScheduler.Core.Domain.Entities;

// Create a new pipeline
var pipeline = new JobPipeline
{
    Name = "Data Processing Pipeline",
    Description = "Imports data, then transforms it.",
    CreatedBy = "admin@example.com"
};

// Define steps
var step1 = new JobPipelineStep
{
    PipelineId = pipeline.Id,
    JobId = Guid.NewGuid(), // replace with an existing Job Id
    StepOrder = 0,
    StopOnFailure = true
};

var step2 = new JobPipelineStep
{
    PipelineId = pipeline.Id,
    JobId = Guid.NewGuid(), // replace with an existing Job Id
    StepOrder = 1,
    StopOnFailure = false
};

// Add steps to the pipeline
pipeline.Steps.Add(step1);
pipeline.Steps.Add(step2);

// Set timestamps
pipeline.CreatedAt = DateTime.UtcNow;

// Example output
Console.WriteLine($"Pipeline '{pipeline.Name}' (Id: {pipeline.Id}) has {pipeline.Steps.Count} steps:");
foreach (var step in pipeline.Steps)
{
    Console.WriteLine($"  Step {step.StepOrder}: Job {step.JobId}, StopOnFailure = {step.StopOnFailure}");
}
```

## CreateJobRequest

`CreateJobRequest` is a request model used to create a new scheduled job. It encapsulates all job configuration from client requests including scheduling rules (cron expression), retry policies, execution limits, and handler configuration. The model includes validation logic to ensure required fields are provided and provides a `ToJob()` method to convert the request into a `Job` entity for persistence.

Example usage:

```csharp
using JobScheduler.Core.Domain.Models;
using JobScheduler.Core.Domain.Enums;
using System;

// Create a request to schedule a data export job
var request = new CreateJobRequest
{
    Name = "Daily Customer Data Export",
    Description = "Exports customer data to CSV format every day at midnight",
    CronExpression = "0 0 * * *",
    TimeZoneId = "America/New_York",
    HandlerType = "JobScheduler.Jobs.DataExportJob, JobScheduler.Jobs",
    HandlerParameters = "{\"format\":\"csv\",\"exportPath\":\"/data/exports\"}",
    Priority = JobPriority.High,
    MaxConcurrentExecutions = 2,
    MaxRetries = 3,
    RetryBackoffSeconds = 60,
    ExecutionTimeoutSeconds = 3600,
    IsActive = true
};

// Validate the request before sending to API
bool isValid = request.IsValid();
Console.WriteLine($"Job request is valid: {isValid}");

// Convert to Job entity for persistence
var job = request.ToJob();
Console.WriteLine($"Created job: {job.Name} (Id: {job.Id})");
Console.WriteLine($"Cron expression: {job.CronExpression}");
Console.WriteLine($"Handler type: {job.HandlerType}");
Console.WriteLine($"Max concurrent executions: {job.MaxConcurrentExecutions}");
Console.WriteLine($"Max retries: {job.MaxRetries}");
Console.WriteLine($"Execution timeout: {job.ExecutionTimeoutSeconds}s");
```

## CreatePipelineRequest

`CreatePipelineRequest` is a request model used to create a new job pipeline. It defines the pipeline's name, optional description, and an ordered list of steps (jobs) that will execute sequentially. Each step can be configured to stop the pipeline on failure, enabling you to model linear workflows where jobs depend on the successful completion of previous jobs.

Example usage:

```csharp
using JobScheduler.Core.Domain.Models;
using System;
using System.Collections.Generic;

// Create a pipeline request to import and transform data
var request = new CreatePipelineRequest
{
    Name = "Data Import Pipeline",
    Description = "Imports customer data from CSV and transforms it for analytics",
    Steps = new List<PipelineStepRequest>
    {
        new PipelineStepRequest
        {
            JobId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"), // Data import job
            StopOnFailure = true // Stop pipeline if import fails
        },
        new PipelineStepRequest
        {
            JobId = Guid.Parse("5fb3e564-5717-4562-b3fc-2c963f66afa7"), // Data transformation job
            StopOnFailure = false // Continue even if transformation fails
        },
        new PipelineStepRequest
        {
            JobId = Guid.Parse("7fc8e564-5717-4562-b3fc-2c963f66afa8"), // Data validation job
            StopOnFailure = true // Stop pipeline if validation fails
        }
    }
};

// Validate the request before sending to API
bool isValid = !string.IsNullOrWhiteSpace(request.Name) && request.Steps.Count > 0;
Console.WriteLine($"Pipeline request is valid: {isValid}");
Console.WriteLine($"Pipeline '{request.Name}' has {request.Steps.Count} steps");

// Access step configuration
foreach (var step in request.Steps)
{
    Console.WriteLine($"Step for Job {step.JobId}: StopOnFailure={step.StopOnFailure}");
}
```
