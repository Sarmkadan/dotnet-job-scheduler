# RetryPolicyExtensions

`RetryPolicyExtensions` provides a set of static extension methods designed to simplify and enhance the management of retry logic within the `dotnet-job-scheduler` system. These methods allow for the evaluation of retry eligibility, calculation of accumulated delays, generation of human-readable summaries, and fluent configuration of retry parameters.

## API

### ShouldRetry
Determines whether a given operation should be retried based on the current policy configuration and the provided exception context.

- **Parameters:**
    - `Exception exception`: The exception encountered during the operation.
- **Return Value:** `bool`: Returns `true` if the policy permits a retry; otherwise, `false`.
- **Throws:** Does not throw exceptions.

### GetTotalAccumulatedDelay
Calculates the total time delay that would be incurred if all remaining retries authorized by the policy were exhausted.

- **Parameters:** None.
- **Return Value:** `int`: The total delay in milliseconds.
- **Throws:** Does not throw exceptions.

### GetRetrySummary
Generates a descriptive string summarizing the current state and configuration of the retry policy, useful for logging and monitoring.

- **Parameters:** None.
- **Return Value:** `string`: A formatted string containing retry policy details.
- **Throws:** Does not throw exceptions.

### WithAdjustedParameters
Creates a new `RetryPolicy` instance based on the current policy, but with the specified parameters adjusted.

- **Parameters:**
    - `int maxRetries`: The maximum number of retry attempts permitted.
    - `int delayMs`: The delay in milliseconds between retry attempts.
- **Return Value:** `RetryPolicy`: A new `RetryPolicy` instance with the modified parameters.
- **Throws:** Throws `ArgumentOutOfRangeException` if `maxRetries` or `delayMs` are negative.

## Usage

```csharp
// Example 1: Checking if a job should be retried after a failure
var policy = job.GetRetryPolicy();
try 
{
    // Execute job logic
}
catch (Exception ex)
{
    if (policy.ShouldRetry(ex))
    {
        // Schedule retry
    }
}
```

```csharp
// Example 2: Adjusting retry parameters and logging the summary
var basePolicy = job.GetRetryPolicy();
var aggressivePolicy = basePolicy.WithAdjustedParameters(maxRetries: 10, delayMs: 100);

Console.WriteLine($"Policy Summary: {aggressivePolicy.GetRetrySummary()}");
Console.WriteLine($"Potential Total Delay: {aggressivePolicy.GetTotalAccumulatedDelay()}ms");
```

## Notes

- **Extension Methods:** These methods are implemented as extension methods for the `RetryPolicy` type. Ensure the namespace containing `RetryPolicyExtensions` is included in your file.
- **Thread Safety:** The methods `ShouldRetry`, `GetTotalAccumulatedDelay`, and `GetRetrySummary` are designed to be thread-safe, provided the `RetryPolicy` instance being accessed is not concurrently modified by other threads.
- **Immutability:** The `WithAdjustedParameters` method returns a new `RetryPolicy` instance and does not modify the original policy, adhering to functional programming principles to ensure thread safety during policy configuration.
