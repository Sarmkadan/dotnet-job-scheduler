// ... (rest of README.md content)

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
Console.WriteLine($"Is Reliable: {isReliable}");

// Get actual failure count
int actualFailureCount = metrics.GetActualFailureCount();
Console.WriteLine($"Actual Failure Count: {actualFailureCount}");

// Check for failure trend
bool hasFailureTrend = metrics.HasFailureTrend();
Console.WriteLine($"Has Failure Trend: {hasFailureTrend}");
```
