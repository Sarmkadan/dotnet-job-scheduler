# RetryService

The `RetryService` class provides a comprehensive mechanism for managing retry logic in job execution scenarios. It tracks failure statistics, calculates backoff delays, enforces retry budgets, and determines whether a job should be retried based on configurable policies. This service is designed to work within the `dotnet-job-scheduler` framework to handle transient failures gracefully.

## API

### `public RetryService`
- **Purpose**: Initializes a new instance of the `RetryService` for a specific job. The constructor sets up the internal state required for tracking retry statistics and calculating delays.
- **Parameters**: None (instance properties like `JobId` are expected to be set post-construction).
- **Throws**: None.

---

### `public ValueTask<bool> ShouldRetryAsync`
- **Purpose**: Determines whether a job execution should be retried based on retry policies, budget constraints, and failure statistics. This is the primary method for making retry decisions.
- **Parameters**: None.
- **Returns**: A `ValueTask<bool>` that resolves to `true` if the job should be retried, `false` otherwise.
- **Throws**: None.

---

### `public DateTime CalculateNextRetryTime`
- **Purpose**: Computes the absolute timestamp for the next retry attempt by adding the calculated backoff delay to the current time.
- **Parameters**: None.
- **Returns**: A `DateTime` representing the scheduled time for the next retry.
- **Throws**: None.

---

### `public int CalculateBackoffDelay`
- **Purpose**: Calculates the delay (in milliseconds) before the next retry attempt, using an exponential backoff strategy. The delay increases with each subsequent failure.
- **Parameters**: None.
- **Returns**: An `int` representing the delay in milliseconds.
- **Throws**: None.

---

### `public JobExecution CreateRetryExecution`
- **Purpose**: Generates a new `JobExecution` instance configured for the next retry attempt, including the calculated retry delay and next execution time.
- **Parameters**: None.
- **Returns**: A `JobExecution` object representing the retry attempt.
- **Throws**: None.

---

### `public async Task<bool> IsRetryBudgetExceededAsync`
- **Purpose**: Checks whether the retry budget for the job has been exceeded, based on failure rate thresholds or maximum retry limits. This method may involve asynchronous checks (e.g., querying external state).
- **Parameters**: None.
- **Returns**: A `Task<bool>` that resolves to `true` if the retry budget is exceeded, `false` otherwise.
- **Throws**: None.

---

### `public async Task<RetryStatistics> GetRetryStatisticsAsync`
- **Purpose**: Retrieves aggregated statistics about retry attempts, including total executions, failures, retries, and failure rates. This method may involve asynchronous operations to fetch or compute statistics.
- **Parameters**: None.
- **Returns**: A `Task<RetryStatistics>` containing the retry metrics.
- **Throws**: None.

---

### `public TimeSpan CalculateRetryDelay`
- **Purpose**: Computes the delay before the next retry attempt as a `TimeSpan`, using the same logic as `CalculateBackoffDelay` but returning the result in a more convenient format.
- **Parameters**: None.
- **Returns**: A `TimeSpan` representing the delay before the next retry.
- **Throws**: None.

---

### `public bool ShouldRetry`
- **Purpose**: A synchronous alternative to `ShouldRetryAsync` for determining whether a retry should occur. This method does not perform asynchronous checks (e.g., retry budget validation) and relies solely on local state.
- **Parameters**: None.
- **Returns**: `true` if the job should be retried, `false` otherwise.
- **Throws**: None.

---

### `public TimeSpan CalculateTotalRetryTime`
- **Purpose**: Calculates the total elapsed time spent on retry attempts for the job, from the first failure to the most recent retry.
- **Parameters**: None.
- **Returns**: A `TimeSpan` representing the cumulative retry duration.
- **Throws**: None.

---

### `public string FormatRetryMessage`
- **Purpose**: Generates a human-readable message summarizing the retry state, including failure counts, recent failure rate, and next retry time. Useful for logging or diagnostics.
- **Parameters**: None.
- **Returns**: A `string` containing the formatted retry message.
- **Throws**: None.

---

### `public Guid JobId`
- **Purpose**: Gets or sets the unique identifier of the job associated with this `RetryService` instance.
- **Type**: `Guid`.
- **Throws**: None.

---

### `public int TotalExecutions`
- **Purpose**: Gets or sets the total number of execution attempts (including successful and failed attempts) for the job.
- **Type**: `int`.
- **Throws**: None.

---

### `public int TotalFailures`
- **Purpose**: Gets or sets the total number of failed execution attempts for the job.
- **Type**: `int`.
- **Throws**: None.

---

### `public int TotalRetries`
- **Purpose**: Gets or sets the total number of retry attempts for the job.
- **Type**: `int`.
- **Throws**: None.

---

### `public double AverageRetriesPerFailure`
- **Purpose**: Gets or sets the average number of retry attempts per failure, calculated as `TotalRetries / TotalFailures`. Returns `0` if there are no failures.
- **Type**: `double`.
- **Throws**: None.

---

### `public DateTime? LastFailureTime`
- **Purpose**: Gets or sets the timestamp of the most recent failure. Returns `null` if no failures have occurred.
- **Type**: `DateTime?`.
- **Throws**: None.

---

### `public double RecentFailureRate`
- **Purpose**: Gets or sets the failure rate over a recent time window (e.g., the last 5 minutes), expressed as a value between `0` and `1`. This is used to enforce retry budgets.
- **Type**: `double`.
- **Throws**: None.

## Usage

### Example 1: Basic Retry Logic
