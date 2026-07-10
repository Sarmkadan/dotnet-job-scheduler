# RetryServiceBenchmarks

A benchmarking utility for evaluating different retry strategies in job scheduling scenarios. This class measures the performance and behavior of exponential, linear, and fixed backoff retry mechanisms, including delay calculations, retry decision logic, and cumulative retry timing.

## API

### `Setup`
Initializes the benchmarking environment with default or provided configuration values. Must be called before any other public members to ensure consistent test conditions.

### `CalculateRetryDelay_ExponentialBackoff_FirstAttempt`
Calculates the delay duration for the first retry attempt using exponential backoff.
- **Returns**: `TimeSpan` representing the computed delay.
- **Throws**: `InvalidOperationException` if `BaseDelaySeconds` is non-positive.

### `CalculateRetryDelay_ExponentialBackoff_SecondAttempt`
Calculates the delay duration for the second retry attempt using exponential backoff.
- **Returns**: `TimeSpan` representing the computed delay.
- **Throws**: `InvalidOperationException` if `BaseDelaySeconds` is non-positive.

### `CalculateRetryDelay_ExponentialBackoff_TenthAttempt`
Calculates the delay duration for the tenth retry attempt using exponential backoff.
- **Returns**: `TimeSpan` representing the computed delay.
- **Throws**: `InvalidOperationException` if `BaseDelaySeconds` is non-positive.

### `CalculateRetryDelay_LinearBackoff_FirstAttempt`
Calculates the delay duration for the first retry attempt using linear backoff.
- **Returns**: `TimeSpan` representing the computed delay.
- **Throws**: `InvalidOperationException` if `BaseDelaySeconds` is non-positive.

### `CalculateRetryDelay_LinearBackoff_FifthAttempt`
Calculates the delay duration for the fifth retry attempt using linear backoff.
- **Returns**: `TimeSpan` representing the computed delay.
- **Throws**: `InvalidOperationException` if `BaseDelaySeconds` is non-positive.

### `CalculateRetryDelay_FixedBackoff`
Calculates the delay duration for any retry attempt using a fixed backoff strategy.
- **Returns**: `TimeSpan` representing the fixed delay.
- **Throws**: `InvalidOperationException` if `BaseDelaySeconds` is non-positive.

### `ShouldRetry_WithinMaxRetries`
Determines whether a retry should proceed when the current attempt count is below the maximum allowed retries.
- **Returns**: `true` if retries are permitted; otherwise, `false`.

### `ShouldRetry_ExceededMaxRetries`
Determines whether a retry should proceed when the current attempt count has exceeded the maximum allowed retries.
- **Returns**: `false` if retries are no longer permitted; otherwise, `true`.

### `ShouldRetry_ZeroMaxRetries`
Determines whether a retry should proceed when the maximum retry count is set to zero.
- **Returns**: `false` if no retries are allowed; otherwise, `true`.

### `CalculateTotalRetryTime_Exponential`
Computes the cumulative time spent in retries using exponential backoff.
- **Returns**: `int` representing the total delay in seconds across all retry attempts.

### `CalculateTotalRetryTime_Linear`
Computes the cumulative time spent in retries using linear backoff.
- **Returns**: `int` representing the total delay in seconds across all retry attempts.

### `CalculateTotalRetryTime_Fixed`
Computes the cumulative time spent in retries using fixed backoff.
- **Returns**: `int` representing the total delay in seconds across all retry attempts.

### `FormatRetryMessage`
Generates a formatted message describing the retry attempt, including attempt number, delay, and strategy.
- **Returns**: `string` containing the formatted retry message.

### `MaxAttempts`
Gets or sets the maximum number of retry attempts allowed.
- **Type**: `public int`
- **Default**: `3`

### `BackoffStrategy`
Gets or sets the retry backoff strategy to use.
- **Type**: `public JobRetryBackoffStrategy`
- **Default**: `JobRetryBackoffStrategy.Exponential`

### `BaseDelaySeconds`
Gets or sets the base delay in seconds used for backoff calculations.
- **Type**: `public int`
- **Default**: `5`

## Usage
