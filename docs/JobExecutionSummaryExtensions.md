# JobExecutionSummaryExtensions

The `JobExecutionSummaryExtensions` class provides a set of static extension methods designed to analyze and summarize `JobExecutionSummary` objects within the `dotnet-job-scheduler` library. These utilities simplify the retrieval of statistical metrics, such as failure rates, execution duration ranges, and standard deviations, enabling efficient performance monitoring and diagnostic reporting of job execution histories.

## API

### GetFailureRate
Calculates the ratio of failed job executions to the total number of recorded executions.

*   **Parameters:** `this JobExecutionSummary summary`
*   **Return Value:** `double` representing the failure rate (0.0 to 1.0).
*   **Exceptions:** Throws `ArgumentNullException` if the summary is null.

### GetTimeoutCancelledRate
Calculates the ratio of job executions that were terminated due to a timeout to the total number of recorded executions.

*   **Parameters:** `this JobExecutionSummary summary`
*   **Return Value:** `double` representing the timeout cancellation rate (0.0 to 1.0).
*   **Exceptions:** Throws `ArgumentNullException` if the summary is null.

### HasFailures
Determines whether any job executions in the summary resulted in a failure status.

*   **Parameters:** `this JobExecutionSummary summary`
*   **Return Value:** `bool` - `true` if at least one execution failed; otherwise, `false`.
*   **Exceptions:** Throws `ArgumentNullException` if the summary is null.

### GetDurationRange
Retrieves the minimum and maximum execution durations recorded within the summary.

*   **Parameters:** `this JobExecutionSummary summary`
*   **Return Value:** `(long Min, long Max)` tuple containing the shortest and longest durations in milliseconds.
*   **Exceptions:** Throws `ArgumentNullException` if the summary is null.

### GetDurationStandardDeviation
Calculates the standard deviation of execution durations, indicating the consistency of job performance.

*   **Parameters:** `this JobExecutionSummary summary`
*   **Return Value:** `double` representing the standard deviation of durations.
*   **Exceptions:** Throws `ArgumentNullException` if the summary is null.

## Usage

### Analyzing Job Health
```csharp
var summary = jobService.GetExecutionSummary(jobId);

if (summary.HasFailures())
{
    var failureRate = summary.GetFailureRate();
    Console.WriteLine($"Job {jobId} has a failure rate of {failureRate:P2}.");
}
```

### Reporting Performance Metrics
```csharp
var summary = jobService.GetExecutionSummary(jobId);
var (min, max) = summary.GetDurationRange();
var stdDev = summary.GetDurationStandardDeviation();

Console.WriteLine($"Duration range: {min}ms - {max}ms (StdDev: {stdDev:F2}ms)");
```

## Notes

*   **Thread Safety:** These extension methods are stateless and read-only. They are thread-safe, provided the `JobExecutionSummary` instance being accessed is not concurrently modified by another thread during the calculation.
*   **Edge Cases:** If the `JobExecutionSummary` contains no recorded executions, statistical methods (`GetFailureRate`, `GetTimeoutCancelledRate`, `GetDurationStandardDeviation`) will return `0.0`. `GetDurationRange` will return a tuple of `(0, 0)`.
*   **Input Validation:** Every extension method validates the input `JobExecutionSummary` and will throw an `ArgumentNullException` if the reference is null.
