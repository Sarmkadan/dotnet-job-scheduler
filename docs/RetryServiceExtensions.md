# RetryServiceExtensions

The `RetryServiceExtensions` class provides a suite of static utility methods designed to standardize and simplify the implementation of retry logic within the `dotnet-job-scheduler` framework. By encapsulating essential operations—such as calculating backoff intervals, evaluating retry budget limits, and generating consistent logging messages—it ensures that retry behavior remains uniform across different job handlers and configurations.

## API

### CreateRetryExecution
Generates a new `JobExecution` instance designated as a retry attempt for a failing job.

*   **Parameters:**
    *   `job` (`Job`): The job instance to be retried.
    *   `exception` (`Exception`): The exception encountered during the last execution that triggered the retry.
*   **Returns:** A `JobExecution` object configured for the retry operation.
*   **Throws:** `ArgumentNullException` if `job` or `exception` is null.

### CalculateNextRetryTime
Computes the `DateTime` at which the next retry attempt should be executed based on the configured retry policy.

*   **Parameters:**
    *   `job` (`Job`): The job instance.
    *   `retryPolicy` (`RetryPolicy`): The policy defining the backoff strategy.
    *   `attemptCount` (`int`): The current number of failed attempts.
*   **Returns:** A `DateTime` indicating the scheduled time for the next attempt.
*   **Throws:** `ArgumentNullException` if `job` or `retryPolicy` is null.

### IsRetryBudgetExceededAsync
Asynchronously determines if the provided job has exceeded the maximum allowable number of retry attempts defined by its policy.

*   **Parameters:**
    *   `job` (`Job`): The job instance to evaluate.
    *   `retryPolicy` (`RetryPolicy`): The policy containing the retry budget configuration.
*   **Returns:** A `Task<bool>` that completes with `true` if the retry budget is exceeded, otherwise `false`.
*   **Throws:** `ArgumentNullException` if `job` or `retryPolicy` is null.

### FormatRetryMessage
Generates a standardized string message describing the current retry status for logging or notification purposes.

*   **Parameters:**
    *   `job` (`Job`): The job instance.
    *   `retryAttempt` (`int`): The current attempt index.
    *   `exception` (`Exception`): The exception associated with the current failure.
*   **Returns:** A formatted `string` containing details of the retry.
*   **Throws:** `ArgumentNullException` if `job` or `exception` is null.

## Usage

### Example 1: Basic Retry Logic in a Job Handler
```csharp
public async Task HandleAsync(Job job, CancellationToken ct)
{
    try
    {
        await _apiClient.ExecuteAsync(job, ct);
    }
    catch (Exception ex)
    {
        if (!await RetryServiceExtensions.IsRetryBudgetExceededAsync(job, job.RetryPolicy))
        {
            var nextTime = RetryServiceExtensions.CalculateNextRetryTime(job, job.RetryPolicy, job.AttemptCount);
            var execution = RetryServiceExtensions.CreateRetryExecution(job, ex);
            
            _logger.LogWarning(RetryServiceExtensions.FormatRetryMessage(job, job.AttemptCount, ex));
            await _scheduler.ScheduleAsync(execution, nextTime, ct);
        }
    }
}
```

### Example 2: Validating Budget before Scheduling
```csharp
public async Task ProcessFailureAsync(Job job, Exception ex)
{
    bool isExceeded = await RetryServiceExtensions.IsRetryBudgetExceededAsync(job, job.RetryPolicy);
    
    if (isExceeded)
    {
        _logger.LogError("Retry budget exceeded for Job {JobId}.", job.Id);
        await _jobRepository.MarkAsFailedAsync(job);
    }
    else
    {
        // Proceed with scheduling next attempt
    }
}
```

## Notes

*   **Thread Safety:** The methods in this class are stateless and perform pure computations; they are thread-safe and can be called concurrently from multiple threads without external locking.
*   **Asynchronous Operations:** `IsRetryBudgetExceededAsync` is the only asynchronous method. It relies on the underlying storage mechanism to verify the state of the retry budget; ensure that the injected `RetryPolicy` is correctly configured to avoid unexpected database or service calls.
*   **Edge Cases:** Passing `null` for any required argument will result in an `ArgumentNullException`. When calculating `CalculateNextRetryTime`, ensure the `RetryPolicy` does not contain negative values for backoff intervals, as this may result in unexpected `DateTime` results.
