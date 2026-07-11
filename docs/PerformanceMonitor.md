# PerformanceMonitor

The `PerformanceMonitor` class provides instrumentation for tracking and analyzing the runtime performance of scheduled jobs in the `dotnet-job-scheduler` system. It collects execution metrics such as duration, success rate, throughput, CPU utilization, and memory usage, enabling diagnostics and optimization of job performance over time.

## API

### `public PerformanceMonitor(Guid jobId, string jobName)`
Initializes a new instance of the `PerformanceMonitor` for a specific job.
- **Parameters**:
  - `jobId` (`Guid`): Unique identifier of the job being monitored.
  - `jobName` (`string`): Human-readable name of the job.
- **Throws**:
  - `ArgumentNullException`: If `jobName` is `null` or empty.

### `public void RecordExecutionTime(long executionTimeMs, bool success)`
Records an execution event, including its duration and outcome.
- **Parameters**:
  - `executionTimeMs` (`long`): Duration of the execution in milliseconds.
  - `success` (`bool`): Indicates whether the execution completed successfully.
- **Throws**:
  - `ArgumentOutOfRangeException`: If `executionTimeMs` is negative.

### `public long GetAverageExecutionTime()`
Returns the average execution time of all recorded executions in ticks.
- **Returns**: Average execution time in ticks.
- **Remarks**: Returns `0` if no executions have been recorded.

### `public long GetAverageExecutionTimeMs()`
Returns the average execution time of all recorded executions in milliseconds.
- **Returns**: Average execution time in milliseconds.
- **Remarks**: Returns `0` if no executions have been recorded.

### `public double GetThroughputPerMinute()`
Calculates the number of executions completed per minute, based on the total recorded executions and the time elapsed since the first recorded execution.
- **Returns**: Throughput as executions per minute.
- **Remarks**: Returns `0` if fewer than two executions have been recorded.

### `public double GetSuccessRate()`
Returns the success rate of all recorded executions as a percentage (0.0 to 100.0).
- **Returns**: Success rate percentage.
- **Remarks**: Returns `0` if no executions have been recorded.

### `public long GetPercentileExecutionTime(int percentile)`
Returns the execution time at the specified percentile (e.g., 50 for median, 95 for 95th percentile).
- **Parameters**:
  - `percentile` (`int`): The percentile to query (1-100).
- **Returns**: Execution time in milliseconds at the specified percentile.
- **Throws**:
  - `ArgumentOutOfRangeException`: If `percentile` is outside the 1-100 range.
- **Remarks**: Returns `0` if no executions have been recorded.

### `public double GetCpuUtilization()`
Returns the average CPU utilization percentage during job executions.
- **Returns**: CPU utilization as a percentage (0.0 to 100.0).
- **Remarks**: Returns `0` if no CPU metrics have been recorded or if the system does not support CPU tracking.

### `public long GetMemoryUsageMb()`
Returns the average memory usage in megabytes during job executions.
- **Returns**: Memory usage in MB.
- **Remarks**: Returns `0` if no memory metrics have been recorded or if the system does not support memory tracking.

### `public async Task<List<PerformanceTimelinePoint>> GetPerformanceTimelineAsync()`
Retrieves a timeline of performance metrics over the lifetime of the job, aggregated by time intervals (e.g., per minute or hour).
- **Returns**: A list of `PerformanceTimelinePoint` objects, each representing aggregated metrics for a time interval.
- **Remarks**: The timeline is generated asynchronously and may take longer for jobs with many recorded executions.

### `public void ClearMetrics()`
Resets all collected metrics, clearing execution history and derived statistics.
- **Remarks**: Does not affect the `JobId` or `JobName`.

### `public MetricsSummary GetSummary()`
Generates a summary of all collected metrics, including averages, percentiles, and rates.
- **Returns**: A `MetricsSummary` object containing aggregated performance data.
- **Remarks**: Returns an empty summary if no executions have been recorded.

### `public Guid JobId { get; }`
Gets the unique identifier of the monitored job.
- **Returns**: The job's `Guid`.

### `public string JobName { get; }`
Gets the name of the monitored job.
- **Returns**: The job's name as a `string`.

### `public long ExecutionTimeMs { get; }`
Gets the execution time of the most recent recorded execution in milliseconds.
- **Returns**: Execution time in milliseconds.
- **Remarks**: Returns `0` if no executions have been recorded.

### `public bool Success { get; }`
Gets the success status of the most recent recorded execution.
- **Returns**: `true` if the execution succeeded; otherwise, `false`.
- **Remarks**: Returns `false` if no executions have been recorded.

### `public DateTime Timestamp { get; }`
Gets the timestamp of the most recent recorded execution.
- **Returns**: The execution timestamp as `DateTime`.
- **Remarks**: Returns `DateTime.MinValue` if no executions have been recorded.

### `public int TotalExecutions { get; }`
Gets the total number of executions recorded.
- **Returns**: The count of executions.

### `public int SuccessfulExecutions { get; }`
Gets the number of successful executions recorded.
- **Returns**: The count of successful executions.

## Usage

### Example 1: Monitoring a Scheduled Job
