// ... (rest of README.md content)

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

## RetryPolicy

`RetryPolicy` defines how a job should be retried after a failure, including the maximum number of attempts, back‑off strategy, and which exception types are considered retryable. It provides helper methods to calculate delay intervals, validate the configuration, and generate human‑readable descriptions of the strategy.

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
