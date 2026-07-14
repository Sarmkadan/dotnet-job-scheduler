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

## CronExpressionBenchmarks

The `CronExpressionBenchmarks` class measures cron expression parsing and schedule evaluation throughput. These benchmarks test operations that execute on the hot path for every scheduled job on each scheduler tick, including validation, next execution time calculation, and schedule description generation.

Example usage:
```csharp
// Create a new instance of CronExpressionBenchmarks
var benchmarks = new CronExpressionBenchmarks();

// Setup the benchmarks
benchmarks.Setup();

// Validate cron expression syntax
var isValid = benchmarks.IsValidCronExpression_Simple();

// Calculate next execution times for different schedules
var everyMinute = benchmarks.GetNextExecutionTime_EveryMinute();
var dailyAt9Am = benchmarks.GetNextExecutionTime_Daily();
var weekdaysAt8 = benchmarks.GetNextExecutionTime_Weekdays();

// Generate multiple upcoming execution times
var nextTenExecutions = benchmarks.GetNextExecutionTimes_10();

// Check if a job should execute at a specific time
var shouldExecute = benchmarks.ShouldExecuteAt_Miss();

// Get human-readable description of cron expression
var description = benchmarks.GetCronDescription_Simple();
```
