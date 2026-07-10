# RetryPolicy

`RetryPolicy` defines the retry behavior for a scheduled job in **dotnet-job-scheduler**. It encapsulates the limits, back‑off configuration, and conditions under which a job execution should be retried after a failure. Instances are identified by `Id` and linked to a specific job via `JobId`.

## API

| Member | Type | Description |
|--------|------|-------------|
| **Id** | `Guid` | Unique identifier of the retry policy record. Assigned when the policy is created and never changes. |
| **JobId** | `Guid` | Identifier of the job to which this policy applies. Must correspond to an existing job record. |
| **MaxRetries** | `int` | Maximum number of retry attempts allowed. A value of `0` disables retries. |
| **InitialBackoffSeconds** | `int` | Number of seconds to wait before the first retry. Must be non‑negative. |
| **MaxBackoffSeconds** | `int` | Upper bound for the back‑off delay (in seconds). The calculated delay will never exceed this value. |
| **Strategy** | `BackoffStrategy` | Enum that selects the back‑off algorithm (e.g., `Fixed`, `Exponential`, `Linear`). Determines how `CalculateBackoffDelay` computes the wait time. |
| **BackoffMultiplier** | `double` | Multiplier used by exponential or linear strategies. Must be greater than `0`. |
| **RetryOnTimeout** | `bool` | When `true`, a timeout exception triggers a retry according to the policy. |
| **RetryOnCancellation** | `bool` | When `true`, an `OperationCanceledException` triggers a retry. |
| **RetryableExceptions** | `string?` | Comma‑separated list of fully‑qualified exception type names that are considered retryable. `null` means no explicit list; other flags (`RetryOnTimeout`, `RetryOnCancellation`) apply. |
| **CreatedAt** | `DateTime` | UTC timestamp when the policy was created. |
| **UpdatedAt** | `DateTime?` | UTC timestamp of the last modification, or `null` if never updated. |
| **CalculateBackoffDelay** | `int` | Returns the delay (in seconds) for the next retry based on `Strategy`, `InitialBackoffSeconds`, `BackoffMultiplier`, and `MaxBackoffSeconds`. Throws `InvalidOperationException` if the policy configuration is invalid (e.g., negative values). |
| **ShouldRetryOnException** | `bool` | Evaluates the current exception context (not passed as a parameter; the method inspects thread‑local state) and returns `true` if the exception matches `RetryableExceptions` or the dedicated flags. Throws `ArgumentNullException` if the examined exception is `null`. |
| **GetNextRetryTime** | `DateTime` | Computes the absolute UTC time when the next retry should be attempted, based on the last failure timestamp and the delay from `CalculateBackoffDelay`. Throws `InvalidOperationException` if `MaxRetries` has been exhausted. |
| **IsValid** | `bool` | Performs a lightweight validation of the policy configuration (e.g., non‑negative back‑off values, `MaxRetries` ≥ 0, `Strategy` defined). Returns `false` when any required property is out of range. |
| **GetStrategyDescription** | `string` | Returns a human‑readable description of the selected back‑off strategy, including multiplier and bounds. Never returns `null`. |

### Member Details

- **CalculateBackoffDelay**  
  - **Purpose:** Compute the delay before the next retry.  
  - **Parameters:** None.  
  - **Return Value:** Delay in seconds (`int`).  
  - **Exceptions:** `InvalidOperationException` if the policy is not valid (`IsValid` is `false`).  

- **ShouldRetryOnException**  
  - **Purpose:** Determine whether the caught exception qualifies for a retry under this policy.  
  - **Parameters:** None (operates on the exception currently being handled).  
  - **Return Value:** `true` if the exception type is listed in `RetryableExceptions` or matches the timeout/cancellation flags.  
  - **Exceptions:** `ArgumentNullException` when the examined exception is `null`.  

- **GetNextRetryTime**  
  - **Purpose:** Provide the exact UTC timestamp for the next retry attempt.  
  - **Parameters:** None.  
  - **Return Value:** `DateTime` (UTC).  
  - **Exceptions:** `InvalidOperationException` if the retry count has reached `MaxRetries`.  

- **IsValid**  
  - **Purpose:** Quick sanity check of the policy configuration.  
  - **Parameters:** None.  
  - **Return Value:** `true` if all numeric properties are within acceptable ranges and required enums are set; otherwise `false`.  

- **GetStrategyDescription**  
  - **Purpose:** Generate a textual description of the back‑off strategy for logging or UI.  
  - **Parameters:** None.  
  - **Return Value:** Non‑empty `string`.  

## Usage

### Example 1 – Defining a policy for a job that should retry up to 5 times with exponential back‑off

```csharp
using System;
using DotNetJobScheduler;

var policy = new RetryPolicy
{
    Id = Guid.NewGuid(),
    JobId = jobId,                       // Guid of the scheduled job
    MaxRetries = 5,
    InitialBackoffSeconds = 10,
    MaxBackoffSeconds = 300,
    Strategy = BackoffStrategy.Exponential,
    BackoffMultiplier = 2.0,
    RetryOnTimeout = true,
    RetryOnCancellation = false,
    RetryableExceptions = "System.Net.Http.HttpRequestException,MyApp.TransientException",
    CreatedAt = DateTime.UtcNow
};

if (!policy.IsValid())
{
    throw new InvalidOperationException("RetryPolicy configuration is invalid.");
}

// Later, when handling a failure:
if (policy.ShouldRetryOnException)
{
    var delay = policy.CalculateBackoffDelay();
    var nextRetry = policy.GetNextRetryTime();
    logger.Info($"Retrying in {delay}s (at {nextRetry:u}) using policy {policy.Id}");
    await Task.Delay(TimeSpan.FromSeconds(delay));
    // enqueue retry...
}
```

### Example 2 – Using a fixed back‑off policy for a cancellable job

```csharp
using System;
using DotNetJobScheduler;

var fixedPolicy = new RetryPolicy
{
    Id = Guid.NewGuid(),
    JobId = jobId,
    MaxRetries = 3,
    InitialBackoffSeconds = 30,
    MaxBackoffSeconds = 30,
    Strategy = BackoffStrategy.Fixed,
    BackoffMultiplier = 1.0,   // ignored for Fixed strategy
    RetryOnTimeout = false,
    RetryOnCancellation = true,
    RetryableExceptions = null,
    CreatedAt = DateTime.UtcNow
};

if (!fixedPolicy.IsValid())
    throw new InvalidOperationException("Invalid fixed retry policy.");

// Inside the job execution catch block:
catch (OperationCanceledException) when (fixedPolicy.ShouldRetryOnException)
{
    var delay = fixedPolicy.CalculateBackoffDelay(); // always 30
    var next = fixedPolicy.GetNextRetryTime();
    logger.Warn($"Job cancelled, will retry after {delay}s (at {next:u}).");
    await Task.Delay(TimeSpan.FromSeconds(delay));
    // re‑queue job...
}
```

## Notes

* **Edge Cases**  
  * `MaxRetries` set to `0` disables all retry attempts; `GetNextRetryTime` will immediately throw.  
  * If `InitialBackoffSeconds` exceeds `MaxBackoffSeconds`, the effective delay is clamped to `MaxBackoffSeconds`.  
  * An empty or whitespace `RetryableExceptions` string is treated as “no explicit exception list”. In that case only the `RetryOnTimeout` and `RetryOnCancellation` flags influence `ShouldRetryOnException`.  
  * `BackoffMultiplier` values ≤ 0 are considered invalid; `IsValid` will return `false`.  

* **Thread‑Safety**  
  * `RetryPolicy` is a plain data holder; it does not internally mutate state after construction except when the caller updates mutable properties.  
  * If a single instance is shared across threads, callers must synchronize writes to mutable properties (`UpdatedAt`, etc.) to avoid race conditions.  
  * Methods that compute values (`CalculateBackoffDelay`, `GetNextRetryTime`) are read‑only and safe for concurrent invocation provided the underlying property values are not being modified simultaneously.  

* **Persistence**  
  * The `Id`, `CreatedAt`, and optional `UpdatedAt` fields are intended for storage in the scheduler’s persistence layer. Changing `Id` after creation is not supported and will break referential integrity with the associated job.  

* **Extensibility**  
  * Adding new `BackoffStrategy` values requires updating `CalculateBackoffDelay` and `GetStrategyDescription`. Existing callers that rely on the current enum members remain unaffected.
