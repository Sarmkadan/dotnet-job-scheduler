# JobResponse

`JobResponse` is a data transfer object that encapsulates the full state and metadata of a scheduled job within the `dotnet-job-scheduler` system. It is returned by query and management operations to provide a snapshot of a job’s configuration, execution history, and current status. Instances of this type are immutable after creation from the scheduler’s internal storage; consumers should treat them as read-only views.

## API

The following public properties are defined on `JobResponse`. All properties are read-write at the API level, but in practice the scheduler sets them during construction and consumers typically only read them.

| Member | Type | Description |
|--------|------|-------------|
| `Id` | `Guid` | The unique identifier of the job. |
| `Name` | `string` | A human-readable name for the job. |
| `Description` | `string` | A textual description of the job’s purpose. |
| `CronExpression` | `string` | The cron expression that defines the job’s schedule. |
| `TimeZoneId` | `string?` | The IANA time zone identifier used to interpret the cron expression; `null` means the system’s local time zone is used. |
| `Priority` | `string` | The job’s priority level (e.g., `"Low"`, `"Normal"`, `"High"`). |
| `Status` | `string` | The current execution status (e.g., `"Idle"`, `"Running"`, `"Paused"`, `"Failed"`). |
| `HandlerType` | `string` | The fully qualified type name of the job handler that executes the job’s logic. |
| `ExecutionTimeoutSeconds` | `int` | The maximum number of seconds a single execution is allowed to run before being considered timed out. |
| `IsActive` | `bool` | Indicates whether the job is currently active (scheduled to run) or disabled. |
| `CreatedAt` | `DateTime` | The UTC timestamp when the job was first created. |
| `UpdatedAt` | `DateTime?` | The UTC timestamp of the last modification to the job’s configuration; `null` if never updated. |
| `LastExecutedAt` | `DateTime?` | The UTC timestamp when the job last started execution; `null` if never executed. |
| `NextExecutionAt` | `DateTime?` | The UTC timestamp of the next scheduled execution; `null` if the job is not scheduled to run again. |
| `TotalExecutions` | `int` | The total number of execution attempts (including retries). |
| `SuccessfulExecutions` | `int` | The number of executions that completed successfully. |
| `FailedExecutions` | `int` | The number of executions that failed or timed out. |
| `SuccessRate` | `double` | The ratio of successful executions to total executions, expressed as a value between 0.0 and 1.0. |
| `MaxRetries` | `int` | The maximum number of automatic retry attempts allowed after a failed execution. |
| `MaxConcurrentExecutions` | `int` | The maximum number of concurrent executions allowed for this job (0 means unlimited). |

None of these properties throw exceptions when read. Setting a property may throw `ArgumentNullException` if a non-nullable string is set to `null`, or `ArgumentOutOfRangeException` if a numeric value is outside an expected range (e.g., negative `ExecutionTimeoutSeconds`). However, the scheduler typically validates these values before constructing a `JobResponse`.

## Usage

The following examples demonstrate typical usage of `JobResponse` in a consumer application.

**Example 1: Inspecting job status after retrieval**

```csharp
using dotnet_job_scheduler;

// Assume jobResponse is obtained from a scheduler query
JobResponse job = scheduler.GetJob(jobId);

Console.WriteLine($"Job: {job.Name} (ID: {job.Id})");
Console.WriteLine($"Status: {job.Status}");
Console.WriteLine($"Next execution: {job.NextExecutionAt?.ToString("u") ?? "not scheduled"}");
Console.WriteLine($"Success rate: {job.SuccessRate:P1}");

if (job.FailedExecutions > 0)
{
    Console.WriteLine($"Warning: {job.FailedExecutions} failures out of {job.TotalExecutions} attempts.");
}
```

**Example 2: Comparing job configurations**

```csharp
using dotnet_job_scheduler;

void PrintJobDifferences(JobResponse before, JobResponse after)
{
    if (before.CronExpression != after.CronExpression)
        Console.WriteLine($"Schedule changed from '{before.CronExpression}' to '{after.CronExpression}'.");

    if (before.IsActive != after.IsActive)
        Console.WriteLine($"Active state changed from {before.IsActive} to {after.IsActive}.");

    if (before.MaxRetries != after.MaxRetries)
        Console.WriteLine($"Max retries changed from {before.MaxRetries} to {after.MaxRetries}.");

    if (before.ExecutionTimeoutSeconds != after.ExecutionTimeoutSeconds)
        Console.WriteLine($"Timeout changed from {before.ExecutionTimeoutSeconds}s to {after.ExecutionTimeoutSeconds}s.");
}
```

## Notes

- **Thread safety**: `JobResponse` is a plain data object with no synchronization. Reading its properties concurrently from multiple threads is safe because the object is effectively immutable after it is fully constructed by the scheduler. However, if a consumer modifies a property (e.g., sets `IsActive`), that write is not atomic across all properties and may cause inconsistent reads in other threads. In practice, consumers should treat `JobResponse` as read-only and use dedicated scheduler methods to update job state.
- **Nullability**: `TimeZoneId`, `UpdatedAt`, `LastExecutedAt`, and `NextExecutionAt` are nullable. Code that accesses these properties should check for `null` before using the value, especially when formatting timestamps or passing the time zone identifier to APIs that do not accept null.
- **Success rate precision**: `SuccessRate` is computed as `SuccessfulExecutions / TotalExecutions` (using floating-point division). When `TotalExecutions` is zero, the value is `0.0`. Consumers should be aware of potential floating-point rounding when comparing against exact thresholds.
- **Edge cases**: If `MaxConcurrentExecutions` is zero, the scheduler treats it as unlimited. A negative value is invalid and will be rejected during job creation or update. Similarly, `ExecutionTimeoutSeconds` must be a positive integer; zero or negative values are not allowed.
