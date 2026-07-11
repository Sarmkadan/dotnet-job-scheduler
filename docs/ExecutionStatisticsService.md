# ExecutionStatisticsService

The `ExecutionStatisticsService` class provides a centralized interface for querying and analyzing job execution data within the `dotnet-job-scheduler` framework. It exposes aggregated statistics (execution count, average time, success rate, etc.), performance analysis, trend data, and anomaly detection for scheduled jobs. The service is designed to be used as a singleton or scoped dependency, and its methods return data asynchronously.

## API

### `ExecutionStatisticsService()`

Initializes a new instance of the `ExecutionStatisticsService`.  
**Parameters:** None.  
**Returns:** A new `ExecutionStatisticsService` instance.  
**Throws:** None.

### `Date` (DateTime)

Gets or sets the date associated with the current statistics snapshot.  
**Throws:** None.

### `ExecutionCount` (int)

Gets or sets the total number of job executions recorded in the current statistics.  
**Throws:** None.

### `AverageExecutionTimeMs` (long)

Gets or sets the average execution time (in milliseconds) across all recorded executions.  
**Throws:** None.

### `SuccessRate` (double)

Gets or sets the success rate as a value between 0.0 and 1.0 (e.g., 0.95 represents 95% success).  
**Throws:** None.

### `MaxExecutionTimeMs` (long)

Gets or sets the maximum execution time (in milliseconds) observed among the recorded executions.  
**Throws:** None.

### `ExecutionId` (Guid)

Gets or sets the unique identifier for a specific job execution.  
**Throws:** None.

### `Timestamp` (DateTime?)

Gets or sets the timestamp of a specific execution. May be `null` if the timestamp is unavailable.  
**Throws:** None.

### `ExecutionTimeMs` (long)

Gets or sets the execution time (in milliseconds) for a specific job execution.  
**Throws:** None.

### `ExpectedTimeMs` (long)

Gets or sets the expected or baseline execution time (in milliseconds) for comparison.  
**Throws:** None.

### `DeviationFactor` (double)

Gets or sets the factor by which the actual execution time deviates from the expected time (e.g., 1.5 means 50% slower).  
**Throws:** None.

### `AnomalyType` (string)

Gets or sets a string describing the type of anomaly detected (e.g., "SlowExecution", "FailureSpike").  
**Throws:** None.

### `GetJobExecutionStatsAsync()` (async Task<ExecutionStatsResponse?>)

Asynchronously retrieves aggregated execution statistics for all jobs.  
**Parameters:** None.  
**Returns:** A `Task<ExecutionStatsResponse?>` that resolves to an `ExecutionStatsResponse` object, or `null` if no data is available.  
**Throws:** May throw exceptions from the underlying data store (e.g., database connection failures).

### `GetJobPerformanceAnalysisAsync()` (async Task<PerformanceAnalysisResponse?>)

Asynchronously retrieves a performance analysis report for all jobs.  
**Parameters:** None.  
**Returns:** A `Task<PerformanceAnalysisResponse?>` that resolves to a `PerformanceAnalysisResponse` object, or `null` if analysis cannot be produced.  
**Throws:** May throw exceptions from the underlying data store.

### `GetPerformanceTrendAsync()` (async Task<List<PerformanceTrendPoint>>)

Asynchronously retrieves a list of performance trend points over time.  
**Parameters:** None.  
**Returns:** A `Task<List<PerformanceTrendPoint>>` that resolves to a list of trend points. The list may be empty if no trend data exists.  
**Throws:** May throw exceptions from the underlying data store.

### `DetectExecutionAnomaliesAsync()` (async Task<List<ExecutionAnomalyReport>>)

Asynchronously detects anomalies in job execution patterns and returns a list of anomaly reports.  
**Parameters:** None.  
**Returns:** A `Task<List<ExecutionAnomalyReport>>` that resolves to a list of anomaly reports. The list may be empty if no anomalies are found.  
**Throws:** May throw exceptions from the underlying data store.

## Usage

### Example 1: Basic Statistics and Anomaly Detection

```csharp
using dotnet_job_scheduler;

var statsService = new ExecutionStatisticsService();

// Retrieve aggregated statistics
ExecutionStatsResponse? stats = await statsService.GetJobExecutionStatsAsync();
if (stats != null)
{
    Console.WriteLine($"Executions: {stats.ExecutionCount}");
    Console.WriteLine($"Average time: {stats.AverageExecutionTimeMs} ms");
    Console.WriteLine($"Success rate: {stats.SuccessRate:P}");
}

// Detect anomalies
List<ExecutionAnomalyReport> anomalies = await statsService.DetectExecutionAnomaliesAsync();
foreach (var anomaly in anomalies)
{
    Console.WriteLine($"Anomaly: {anomaly.AnomalyType} (Deviation: {anomaly.DeviationFactor:F2})");
}
```

### Example 2: Performance Trend Analysis

```csharp
using dotnet_job_scheduler;

var statsService = new ExecutionStatisticsService();

// Retrieve performance trend
List<PerformanceTrendPoint> trend = await statsService.GetPerformanceTrendAsync();
if (trend.Count > 0)
{
    foreach (var point in trend)
    {
        Console.WriteLine($"{point.Timestamp:yyyy-MM-dd HH:mm}: {point.AverageExecutionTimeMs} ms");
    }
}
else
{
    Console.WriteLine("No trend data available.");
}

// Access service-level properties (if populated by prior operations)
statsService.Date = DateTime.UtcNow;
statsService.ExecutionCount = 150;
Console.WriteLine($"Manual count: {statsService.ExecutionCount}");
```

## Notes

- **Null returns:** Methods `GetJobExecutionStatsAsync` and `GetJobPerformanceAnalysisAsync` may return `null` when no data exists or when the underlying store is unavailable. Always check for `null` before accessing properties.
- **Empty collections:** `GetPerformanceTrendAsync` and `DetectExecutionAnomaliesAsync` return an empty list rather than `null` when no data is available.
- **Property usage:** The instance properties (`Date`, `ExecutionCount`, etc.) are not automatically populated by the service methods. They are intended for manual assignment or for use as a data transfer object when composing custom reports. Their default values are the type defaults (e.g., `DateTime.MinValue`, `0`, `0.0`, `null`).
- **Thread safety:** This class is not thread-safe. If accessed concurrently, external synchronization (e.g., a lock) is required. The asynchronous methods are safe to call concurrently with each other, but reading or writing instance properties concurrently with method calls may lead to inconsistent state.
- **Exceptions:** Methods that access external data stores may throw exceptions such as `InvalidOperationException` or data provider-specific exceptions. Callers should handle these appropriately (e.g., using try-catch or fallback logic).
