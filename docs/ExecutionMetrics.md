# ExecutionMetrics
The `ExecutionMetrics` type in the `dotnet-job-scheduler` project provides a comprehensive set of metrics for job executions, allowing for the tracking and analysis of job performance and reliability. It encompasses various execution statistics, such as total executions, success and failure rates, execution durations, and retry counts, offering insights into the efficiency and dependability of scheduled jobs.

## API
### Properties
- `Id`: A unique identifier for the metrics instance.
- `JobId`: The identifier of the job these metrics are associated with.
- `TotalExecutions`: The total number of times the job has been executed.
- `SuccessfulExecutions`: The number of successful executions.
- `FailedExecutions`: The number of executions that ended in failure.
- `TimedOutExecutions`: The number of executions that timed out.
- `SkippedExecutions`: The number of executions that were skipped.
- `CancelledExecutions`: The number of executions that were cancelled.
- `AverageDurationMs`: The average duration of job executions in milliseconds.
- `MinDurationMs`: The minimum duration of job executions in milliseconds.
- `MaxDurationMs`: The maximum duration of job executions in milliseconds.
- `SuccessRate`: The rate of successful executions.
- `TotalRetries`: The total number of retries.
- `LastExecutionTime`: The time of the last execution, or null if no executions have occurred.
- `CalculatedAt`: The time at which these metrics were last calculated.
- `IsReliable`: Indicates whether the job is considered reliable based on its execution history.
### Methods
- `CalculateSuccessRate`: Calculates the success rate of the job executions.
- `GetActualFailureCount`: Returns the actual count of failed executions, considering the nature of failures.
- `GetSummary`: Provides a summary of the job execution metrics.
- `HasFailureTrend`: Checks if there is a trend of failures in the job executions.

## Usage
The following examples demonstrate how to utilize the `ExecutionMetrics` type in a C# application:
```csharp
// Example 1: Retrieving and displaying job execution metrics
var metrics = new ExecutionMetrics { JobId = Guid.NewGuid(), TotalExecutions = 100, SuccessfulExecutions = 90 };
Console.WriteLine($"Job ID: {metrics.JobId}, Success Rate: {metrics.SuccessRate}, Total Executions: {metrics.TotalExecutions}");
```

```csharp
// Example 2: Evaluating job reliability based on execution metrics
var metrics = new ExecutionMetrics { JobId = Guid.NewGuid(), TotalExecutions = 100, FailedExecutions = 5 };
if (metrics.IsReliable)
{
    Console.WriteLine("The job is considered reliable.");
}
else
{
    Console.WriteLine("The job is not considered reliable.");
}
```

## Notes
- The `SuccessRate` is calculated based on the `SuccessfulExecutions` and `TotalExecutions`, and it may not reflect the exact success rate if there are skipped or cancelled executions.
- The `HasFailureTrend` method considers the recent execution history to determine if there is a trend of failures, which can be useful for proactive maintenance or job optimization.
- The `ExecutionMetrics` type is designed to be thread-safe, allowing for concurrent access and updates to its properties and methods without compromising data integrity.
- Edge cases, such as division by zero when calculating rates, are handled internally by the type to prevent exceptions and ensure robustness.
