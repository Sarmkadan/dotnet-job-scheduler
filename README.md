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
