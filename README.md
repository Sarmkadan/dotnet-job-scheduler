// ... (rest of README.md content)

## CacheServiceBenchmarks

The `CacheServiceBenchmarks` class measures the performance of the `CacheService` operations, including cache hits and misses, cache expiration, concurrent cache access, cache size limit eviction, and cache removal.

Example usage:
```csharp
// Create a new instance of CacheServiceBenchmarks
var benchmarks = new CacheServiceBenchmarks();

// Measure cache miss followed by cache hit for same key
var result = await benchmarks.GetOrAdd_CacheMissThenHit();

// Measure cache expiration and cleanup
await benchmarks.GetOrAdd_WithExpiration();

// Measure concurrent cache access
benchmarks.GetOrAdd_ConcurrentAccess();

// Measure cache size limit eviction
await benchmarks.GetOrAdd_SizeLimitEviction();

// Measure cache hit performance
var cachedValue = await benchmarks.GetOrAdd_CacheHit();

// Remove a cache item
await benchmarks.Remove();

// Clear the entire cache
await benchmarks.Clear();
```

## StringProcessingBenchmarks

The `StringProcessingBenchmarks` class measures string manipulation operations used throughout the scheduler, including slug generation, JSON escaping, truncation, and credential masking.

Example usage:
```csharp
// Create a new instance of StringProcessingBenchmarks
var benchmarks = new StringProcessingBenchmarks();

// Measure slug generation for short, complex, and long names
var shortSlug = benchmarks.ToSlug_Short;
var complexSlug = benchmarks.ToSlug_Complex;
var longSlug = benchmarks.ToSlug_Long;

// Measure JSON escaping for clean and special payloads
var cleanJson = benchmarks.JsonEscape_Clean;
var specialJson = benchmarks.JsonEscape_Special;

// Measure truncation for needed and no-op cases
var neededTruncated = benchmarks.Truncate_Needed;
var noOpTruncated = benchmarks.Truncate_NoOp;

// Measure credential masking for API keys
var maskedApiKey = benchmarks.Mask_ApiKey;
```

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

## JobExecutorServiceBenchmarks

The `JobExecutorServiceBenchmarks` class measures the performance of job execution operations in the `JobExecutorService`. It benchmarks various scenarios including successful job execution, error handling, timeout scenarios, concurrency control, priority handling, and metrics collection to evaluate the efficiency of the job execution pipeline.

Example usage:
```csharp
// Create a new instance of JobExecutorServiceBenchmarks
var benchmarks = new JobExecutorServiceBenchmarks();

// Setup the benchmarks
benchmarks.Setup();

// Measure successful job execution
var successfulResult = await benchmarks.ExecuteJob_Successful();

// Measure failing job execution with retry logic
var failingResult = await benchmarks.ExecuteJob_Failing();

// Measure job execution with timeout enforcement
var timeoutResult = await benchmarks.ExecuteJob_Timeout();

// Measure concurrency limit enforcement
var concurrencyResult = await benchmarks.ExecuteJob_WithConcurrencyLimit();

// Measure priority-based execution ordering
var priorityResult = await benchmarks.ExecuteJob_WithPriority();

// Measure metrics collection during execution
var metricsResult = await benchmarks.ExecuteJob_WithMetricsCollection();
```
