# JobResponseExtensions

Provides extension methods for `JobResponse` objects to evaluate execution eligibility, compute timing information, and generate status messages.

## API

### `IsDueForExecution(JobResponse job, DateTimeOffset now)`

Determines whether a job is due for execution based on its schedule and last execution time.

- **Parameters**
  - `job`: The `JobResponse` instance to evaluate.
  - `now`: The current timestamp used for comparison.
- **Return value**: `true` if the job should execute now; otherwise, `false`.
- **Throws**: `ArgumentNullException` if `job` is `null`.

### `TimeUntilNextExecution(JobResponse job, DateTimeOffset now)`

Calculates the duration until the next scheduled execution.

- **Parameters**
  - `job`: The `JobResponse` instance to evaluate.
  - `now`: The current timestamp used for comparison.
- **Return value**: A `TimeSpan` representing the time until the next execution. Returns `TimeSpan.Zero` if the job is due.
- **Throws**: `ArgumentNullException` if `job` is `null`.

### `HasExceededMaxRetries(JobResponse job)`

Checks whether a job has exceeded its configured maximum retry attempts.

- **Parameters**
  - `job`: The `JobResponse` instance to evaluate.
- **Return value**: `true` if the job’s retry count exceeds `MaxRetries`; otherwise, `false`.
- **Throws**: `ArgumentNullException` if `job` is `null`.

### `GetStatusMessage(JobResponse job)`

Generates a human-readable status message describing the job’s current state.

- **Parameters**
  - `job`: The `JobResponse` instance to evaluate.
- **Return value**: A `string` containing the formatted status message.
- **Throws**: `ArgumentNullException` if `job` is `null`.

## Usage

```csharp
// Example 1: Check if a job is due and compute time until next execution
var job = new JobResponse
{
    Schedule = "0 0 * * *", // Daily at midnight
    LastExecutionTime = DateTimeOffset.UtcNow.AddHours(-23),
    MaxRetries = 3,
    RetryCount = 0
};

if (JobResponseExtensions.IsDueForExecution(job, DateTimeOffset.UtcNow))
{
    Console.WriteLine("Job is due for execution.");
}
else
{
    var timeUntilNext = JobResponseExtensions.TimeUntilNextExecution(job, DateTimeOffset.UtcNow);
    Console.WriteLine($"Next execution in: {timeUntilNext.TotalMinutes} minutes.");
}

// Example 2: Generate a status message and check retry limits
var status = JobResponseExtensions.GetStatusMessage(job);
Console.WriteLine(status);

if (JobResponseExtensions.HasExceededMaxRetries(job))
{
    Console.WriteLine("Job has exceeded max retries.");
}
```

## Notes

- All methods are thread-safe for concurrent calls on distinct `JobResponse` instances. Avoid sharing mutable `JobResponse` objects across threads without synchronization.
- If `Schedule` is malformed or unparseable, `IsDueForExecution` and `TimeUntilNextExecution` may return unexpected results; ensure schedules are validated before use.
- `MaxRetries` and `RetryCount` are assumed to be non-negative; negative values may lead to incorrect retry evaluations.
