// ... (rest of README.md content)

## RetryServiceBenchmarks

The `RetryServiceBenchmarks` class measures performance of retry logic operations in the `RetryService`, including retry delay calculations, retry policy validation, and retry attempt tracking.

Example usage:
```csharp
// Create a new instance of RetryServiceBenchmarks
var benchmarks = new RetryServiceBenchmarks();

// Setup the benchmarks
benchmarks.Setup();

// Measure retry delay calculations
var exponentialFirstAttemptDelay = benchmarks.CalculateRetryDelay_ExponentialBackoff_FirstAttempt();
var linearFifthAttemptDelay = benchmarks.CalculateRetryDelay_LinearBackoff_FifthAttempt();
var fixedBackoffDelay = benchmarks.CalculateRetryDelay_FixedBackoff();

// Evaluate retry policy
var shouldRetryWithinMax = benchmarks.ShouldRetry_WithinMaxRetries();
var shouldRetryExceededMax = benchmarks.ShouldRetry_ExceededMaxRetries();
var shouldRetryZeroMax = benchmarks.ShouldRetry_ZeroMaxRetries();

// Calculate total retry times
var totalExponentialRetryTime = benchmarks.CalculateTotalRetryTime_Exponential();
var totalLinearRetryTime = benchmarks.CalculateTotalRetryTime_Linear();
var totalFixedRetryTime = benchmarks.CalculateTotalRetryTime_Fixed();

// Format retry messages
var retryMessage = benchmarks.FormatRetryMessage();

// Access retry policy properties
var maxAttempts = benchmarks.MaxAttempts;
var backoffStrategy = benchmarks.BackoffStrategy;
var baseDelaySeconds = benchmarks.BaseDelaySeconds;
```
