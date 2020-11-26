// ... (rest of README.md content)

## CronExpressionBenchmarksExtensions

The `CronExpressionBenchmarksExtensions` class provides a set of extension methods for benchmarking and testing cron expressions. It allows you to validate complex cron expressions, get next execution times for various scenarios, and test the scheduler's decision logic.

Here's an example usage:

```csharp
var benchmarks = new CronExpressionBenchmarks();
var cronExpression = "0 9 * * *";

// Validate a complex cron expression
bool isValid = benchmarks.IsValidCronExpression_Complex(cronExpression);
Console.WriteLine($"Is '{cronExpression}' valid? {isValid}");

// Get next execution time for a monthly cron expression
DateTime nextTime = benchmarks.GetNextExecutionTime_Monthly(cronExpression);
Console.WriteLine($"Next execution time: {nextTime}");

// Test if a job should execute at a specific time
bool shouldExecute = benchmarks.ShouldExecuteAt_Hit(cronExpression, DateTime.Now);
Console.WriteLine($"Should execute at {DateTime.Now}? {shouldExecute}");

// Get a human-readable description of a complex cron expression
string description = benchmarks.GetCronDescription_Complex(cronExpression);
Console.WriteLine($"Description: {description}");
```

This example demonstrates how to use the `CronExpressionBenchmarksExtensions` class to validate cron expressions, get next execution times, and test the scheduler's decision logic.

## JobExecutionSummaryExtensions

The `JobExecutionSummaryExtensions` class provides extension methods for analyzing job execution statistics, such as calculating failure rates, timeout/cancellation rates, and execution duration metrics. It helps identify performance trends and reliability issues in scheduled jobs.

Example usage:

```csharp
var summary = new JobExecutionSummary
{
    TotalExecutions = 100,
    FailureCount = 5,
    TimedOutCount = 2,
    CancelledCount = 1,
    MinDurationMs = 120,
    MaxDurationMs = 300
};

double failureRate = summary.GetFailureRate();
double timeoutCancelledRate = summary.GetTimeoutCancelledRate();
bool hasFailures = summary.HasFailures();
var durationRange = summary.GetDurationRange();
double stdDev = summary.GetDurationStandardDeviation();

Console.WriteLine($"Failure Rate: {failureRate:F2}%");
Console.WriteLine($"Timeout/Cancelled Rate: {timeoutCancelledRate:F2}%");
Console.WriteLine($"Has Failures: {hasFailures}");
Console.WriteLine($"Duration Range: {durationRange.Min}-{durationRange.Max}ms");
Console.WriteLine($"Duration Standard Deviation: {stdDev:F2}ms");
```

This example demonstrates calculating key metrics from a job's execution history, such as failure rates and duration variability.

// ... (rest of README.md content)
