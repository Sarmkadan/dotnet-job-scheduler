# CreateJobRequest

Represents a request to create a new scheduled job in the job scheduler. This type encapsulates all configuration required to define a job's schedule, execution behavior, retry policy, and handler binding. It also provides a validation flag and a factory method to produce the corresponding `Job` entity for persistence.

## API

### `public string Name`

Gets or sets the unique name of the job. This value is required and must be non-null and non-empty when the request is validated. The name serves as the primary identifier for the job within the scheduler.

### `public string? Description`

Gets or sets an optional human-readable description of the job's purpose. May be `null`. This value is informational only and does not affect scheduling or execution.

### `public string CronExpression`

Gets or sets the cron expression that defines the job's firing schedule. Must be a valid cron string (e.g., `"0 0 * * *"` for midnight daily). Validation of this field occurs when `IsValid` is evaluated.

### `public string? TimeZoneId`

Gets or sets the IANA time zone identifier used to interpret the `CronExpression`. When `null`, the scheduler's default time zone (typically UTC) is assumed. Examples: `"America/New_York"`, `"Europe/London"`.

### `public string HandlerType`

Gets or sets the fully qualified type name of the handler that will execute when the job fires. The type must implement the required handler interface and be resolvable by the scheduler's dependency injection or type-loading mechanism.

### `public string? HandlerParameters`

Gets or sets an optional string containing serialized parameters passed to the handler at execution time. The format is handler-specific (commonly JSON). May be `null` if the handler requires no parameters.

### `public JobPriority Priority`

Gets or sets the priority level assigned to the job. Higher-priority jobs are dequeued and executed before lower-priority jobs when multiple jobs are ready to run simultaneously. Default is typically `JobPriority.Normal`.

### `public int MaxConcurrentExecutions`

Gets or sets the maximum number of instances of this job that may execute concurrently. A value of `1` ensures only one execution at a time; higher values allow parallel execution up to the specified limit. Must be greater than or equal to `1`.

### `public int MaxRetries`

Gets or sets the maximum number of retry attempts after a failed execution. A value of `0` means no retries are attempted. Each retry is subject to the backoff delay defined by `RetryBackoffSeconds`.

### `public int RetryBackoffSeconds`

Gets or sets the delay in seconds between consecutive retry attempts. The delay is applied before each retry. A value of `0` results in immediate retries with no delay.

### `public int ExecutionTimeoutSeconds`

Gets or sets the maximum duration in seconds that a single job execution is allowed to run before being forcibly cancelled. A value of `0` indicates no timeout is enforced.

### `public bool IsActive`

Gets or sets whether the created job should be immediately active upon registration. When `false`, the job is created in a paused state and will not fire until explicitly activated.

### `public bool IsValid`

Gets a value indicating whether the current request state passes all validation rules. Returns `true` when `Name` is non-null and non-empty, `CronExpression` is a valid cron string, `HandlerType` is non-null and non-empty, and numeric fields are within their acceptable ranges. Does not throw; simply returns `false` for invalid state.

### `public Job ToJob`

Creates and returns a new `Job` instance populated with the values from this request. Throws an `InvalidOperationException` if `IsValid` returns `false` at the time of invocation. The returned `Job` is ready for persistence in the scheduler's job store.

## Usage

### Example 1: Creating a simple daily cleanup job

```csharp
var request = new CreateJobRequest
{
    Name = "DailyDatabaseCleanup",
    Description = "Removes expired records from the database each night",
    CronExpression = "0 2 * * *",
    TimeZoneId = "America/Chicago",
    HandlerType = "MyApp.Jobs.DatabaseCleanupHandler, MyApp",
    Priority = JobPriority.Low,
    MaxConcurrentExecutions = 1,
    MaxRetries = 3,
    RetryBackoffSeconds = 60,
    ExecutionTimeoutSeconds = 300,
    IsActive = true
};

if (request.IsValid)
{
    Job job = request.ToJob();
    scheduler.Register(job);
}
```

### Example 2: Creating a parameterized, high-frequency job with no retries

```csharp
var request = new CreateJobRequest
{
    Name = "RealTimeNotificationProcessor",
    CronExpression = "*/5 * * * *",
    HandlerType = "MyApp.Jobs.NotificationHandler, MyApp",
    HandlerParameters = "{\"batchSize\":50,\"queueName\":\"high-priority\"}",
    Priority = JobPriority.High,
    MaxConcurrentExecutions = 4,
    MaxRetries = 0,
    RetryBackoffSeconds = 0,
    ExecutionTimeoutSeconds = 30,
    IsActive = true
};

// Validate before calling ToJob to avoid exceptions
if (!request.IsValid)
{
    throw new InvalidOperationException(
        $"Job request is invalid. Check Name, CronExpression, and HandlerType.");
}

Job job = request.ToJob();
scheduler.Register(job);
```

## Notes

- `IsValid` performs a static snapshot check. Modifying any property after calling `IsValid` or `ToJob` does not retroactively invalidate a previously created `Job` instance.
- `ToJob` throws `InvalidOperationException` when `IsValid` is `false`. Always check `IsValid` before calling `ToJob` to avoid exceptions in control flow.
- The `CronExpression` string is validated for syntax correctness but not for semantic reachability (e.g., a schedule in the distant past is syntactically valid).
- `MaxConcurrentExecutions` values less than `1` cause `IsValid` to return `false`. The scheduler enforces this floor.
- `ExecutionTimeoutSeconds` of `0` disables the timeout; long-running handlers will not be automatically cancelled.
- `RetryBackoffSeconds` applies uniformly to all retries. There is no exponential backoff or jitter built into this request model—such behavior must be implemented in the handler or a custom retry policy if needed.
- This type is not thread-safe. Concurrent modifications to properties while calling `IsValid` or `ToJob` from another thread may yield inconsistent results. External synchronization is required if shared across threads.
