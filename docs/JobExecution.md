# JobExecution

Represents the execution record of a scheduled job, including timing metrics, status, and outcome details. Used to track job runs, retries, and resource consumption during execution.

## API

### `public Guid Id`
Unique identifier for this execution record. Assigned at creation and immutable thereafter.

### `public Guid JobId`
Identifier of the `Job` that this execution belongs to. Used to correlate executions with their defining jobs.

### `public ExecutionStatus Status`
Current state of the execution (`Pending`, `Running`, `Completed`, `Failed`, etc.). Updated via `MarkAsCompleted` and `MarkAsFailed`.

### `public DateTime StartedAt`
Timestamp when the execution began. Set when the job is dequeued for execution.

### `public DateTime? CompletedAt`
Timestamp when the execution ended, if completed. `null` if still running or failed. Populated by `MarkAsCompleted` or `MarkAsFailed`.

### `public long DurationMilliseconds`
Total duration of the execution in milliseconds, including retries if applicable. Calculated as the difference between `StartedAt` and `CompletedAt`.

### `public int AttemptNumber`
Ordinal attempt number for this execution (1 for first attempt, 2 for first retry, etc.). Incremented on each retry.

### `public string? Output`
Standard output captured during execution, if any. May be truncated or omitted based on configuration.

### `public string? ErrorMessage`
Error message from the most recent failure, if applicable. Populated by `MarkAsFailed`.

### `public string? StackTrace`
Stack trace from the most recent failure, if applicable. Captured when `MarkAsFailed` is called.

### `public string ExecutorName`
Name of the executor service or host that ran this job. Used for debugging and resource tracking.

### `public string? ExecutorInstance`
Unique identifier of the executor instance (e.g., container ID or machine name) that ran this job. Helps correlate executions across distributed environments.

### `public bool IsRetryable`
Indicates whether this execution is eligible for retry based on job configuration and retry policy. Read-only; set during job scheduling.

### `public DateTime CreatedAt`
Timestamp when the execution record was created. Set at construction and immutable.

### `public long MemoryUsageMb`
Peak memory usage of the job process in megabytes, if measured. `-1` if not tracked.

### `public double CpuUsagePercent`
Peak CPU usage percentage of the job process, if measured. `0.0` if not tracked.

### `public virtual Job Job`
Reference to the `Job` definition associated with this execution. Lazy-loaded or eagerly loaded depending on repository implementation.

### `public void MarkAsCompleted()`
Marks the execution as completed successfully. Sets `Status` to `Completed`, records `CompletedAt`, and calculates `DurationMilliseconds`. Throws `InvalidOperationException` if `Status` is not `Running`.

### `public void MarkAsFailed(string errorMessage, string? stackTrace = null)`
Marks the execution as failed. Sets `Status` to `Failed`, records `CompletedAt`, captures `ErrorMessage` and `StackTrace`, and calculates `DurationMilliseconds`. Throws `ArgumentNullException` if `errorMessage` is `null` or empty. Throws `InvalidOperationException` if `Status` is not `Running`.

### `public TimeSpan GetExecutionDuration()`
Returns the total execution duration as a `TimeSpan`. Equivalent to `TimeSpan.FromMilliseconds(DurationMilliseconds)`. Does not throw; returns `TimeSpan.Zero` if `DurationMilliseconds` is `0`.

## Usage

### Example 1: Tracking a Job Execution
