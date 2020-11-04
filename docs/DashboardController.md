# DashboardController

Provides endpoints and aggregated metrics for monitoring the state of the job scheduler, including queue status, job performance, failure rates, and system health.

## API

### `DashboardController`
Public controller exposing endpoints for dashboard data retrieval and summary statistics.

### `async Task<ActionResult<DashboardOverview>> GetOverview()`
Returns a high-level overview of the scheduler state including counts of total, active, and failed jobs, success rate, and last update time.

- **Returns**: `ActionResult<DashboardOverview>` containing job counts, success metrics, and timestamp.
- **Throws**: May throw if underlying data stores are unavailable.

### `async Task<ActionResult<QueueStatusResponse>> GetQueueStatus()`
Returns the current distribution of jobs across queue states (pending, running, failed).

- **Returns**: `ActionResult<QueueStatusResponse>` with counts per queue state.
- **Throws**: May throw if queue state cannot be retrieved.

### `async Task<ActionResult<PriorityDistributionResponse>> GetPriorityDistribution()`
Returns the number of jobs grouped by priority level.

- **Returns**: `ActionResult<PriorityDistributionResponse>` mapping priority levels to job counts.
- **Throws**: May throw if priority data is inaccessible.

### `async Task<ActionResult<List<PerformanceTimelinePoint>>> GetPerformanceTimeline()`
Returns a time-series of key performance indicators (e.g., execution counts, success rates) sampled at regular intervals.

- **Returns**: `ActionResult<List<PerformanceTimelinePoint>>` ordered chronologically.
- **Throws**: May throw if historical metrics are unavailable.

### `async Task<ActionResult<List<SlowestJobResponse>>> GetSlowestJobs()`
Returns a list of the slowest currently tracked job executions, sorted by duration descending.

- **Returns**: `ActionResult<List<SlowestJobResponse>>` limited to a fixed number of results.
- **Throws**: May throw if job execution logs are inaccessible.

### `async Task<ActionResult<List<FailingJobResponse>>> GetMostFailingJobs()`
Returns a list of jobs with the highest recent failure counts, sorted by failure count descending.

- **Returns**: `ActionResult<List<FailingJobResponse>>` limited to a fixed number of results.
- **Throws**: May throw if failure tracking data is unavailable.

### `async Task<ActionResult<HealthReportResponse>> GetHealthReport()`
Returns a summary health report including system resource usage, queue depth, and scheduler responsiveness.

- **Returns**: `ActionResult<HealthReportResponse>` with health indicators and messages.
- **Throws**: May throw if system metrics cannot be collected.

### `TotalJobs` (public int)
Gets the total number of jobs registered in the scheduler.

### `ActiveJobs` (public int)
Gets the number of jobs currently in an active state (not completed or failed).

### `RunningExecutions` (public int)
Gets the number of job executions currently in progress.

### `FailedJobsLast24Hours` (public int)
Gets the number of jobs that failed within the last 24 hours.

### `AverageSuccessRate` (public double)
Gets the average success rate of job executions across all tracked history.

### `TotalExecutions` (public int)
Gets the total number of job executions recorded.

### `SuccessfulExecutions` (public int)
Gets the total number of successful job executions.

### `AverageExecutionTimeMs` (public long)
Gets the average execution time of jobs in milliseconds.

### `LastUpdatedAt` (public DateTime)
Gets the timestamp when the dashboard data was last updated.

### `PendingJobs` (public int)
Gets the number of jobs currently in the pending state.

### `RunningJobs` (public int)
Gets the number of jobs currently in the running state.

### `FailedJobs` (public int)
Gets the number of jobs currently in the failed state.

## Usage
