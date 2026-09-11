# Exception Extensions

This document provides documentation for the extension classes in the `src/JobScheduler.Core/Exceptions/` namespace. These extension methods enhance error handling and diagnostics for various exception types used in the job scheduler.

For documentation on `JobNotFoundExceptionExtensions`, see [JobNotFoundExceptionExtensions.md](./JobNotFoundExceptionExtensions.md).

---

## JobSchedulerExceptionExtensions

Provides extension methods for `JobSchedulerException` to enhance error handling and diagnostics.

### Methods

- `FormatDetails(this JobSchedulerException exception)`
  - Formats the exception details into a human-readable string.
  - Returns a string containing the exception message and error code (if present).
  - Throws `ArgumentNullException` if `exception` is null.

- `IsSpecificError(this JobSchedulerException exception, string errorCode)`
  - Determines whether the exception matches a specific error code.
  - Returns `true` if the exception's error code matches `errorCode` (case-insensitive); otherwise, `false`.
  - Throws `ArgumentNullException` if `exception` is null or `errorCode` is null or empty.

- `GetSummary(this JobSchedulerException exception)`
  - Gets a summary of the exception for logging or diagnostic purposes.
  - Returns a dictionary containing the exception type, message, and error code.
  - Throws `ArgumentNullException` if `exception` is null.

### Example

```csharp
try
{
    // Some operation that might throw JobSchedulerException
}
catch (JobSchedulerException ex)
{
    // Format details for logging
    string details = ex.FormatDetails();
    _logger.LogError(details);

    // Check for a specific error code
    if (ex.IsSpecificError("JS001"))
    {
        // Handle specific error
    }

    // Get summary for diagnostic purposes
    var summary = ex.GetSummary();
    // summary contains Type, Message, and ErrorCode
}
```

---

## ConcurrencyExceptionExtensions

Provides extension methods for `ConcurrencyException` to enhance error handling and diagnostics for concurrency-related issues.

### Methods

- `IsAtMaxConcurrency(this ConcurrencyException exception)`
  - Determines whether the concurrency exception indicates that the job is currently running at maximum allowed concurrency.
  - Returns `true` if the job is currently running at maximum allowed concurrency; otherwise, `false`.
  - Throws `ArgumentNullException` if `exception` is null.

- `GetAvailableConcurrencySlots(this ConcurrencyException exception)`
  - Calculates the number of additional executions that can be started without exceeding the maximum allowed concurrency.
  - Returns the number of additional executions that can be started. Returns 0 if current executions exceed maximum.
  - Throws `ArgumentNullException` if `exception` is null.

### Example

```csharp
try
{
    // Some operation that might throw ConcurrencyException
}
catch (ConcurrencyException ex)
{
    // Check if we are at max concurrency
    if (ex.IsAtMaxConcurrency())
    {
        _logger.Warning("Job is at maximum concurrency.");
    }
    else
    {
        // Get available slots and log
        int availableSlots = ex.GetAvailableConcurrencySlots();
        _logger.Information($"There are {availableSlots} available concurrency slots.");
    }
}
```

---

## CyclicDependencyExceptionExtensions

Provides extension methods for `CyclicDependencyException` to enhance error handling and diagnostics for cyclic dependency detection in job scheduling scenarios.

### Methods

- `GetDescription(this CyclicDependencyException exception)`
  - Gets a human-readable description of the cyclic dependency.
  - Returns a string describing the cyclic dependency.
  - Throws `ArgumentNullException` if `exception` is null.

- `InvolvesJob(this CyclicDependencyException exception, Guid jobId)`
  - Determines whether the cyclic dependency involves a specific job.
  - Returns `true` if the cyclic dependency involves the specified job; otherwise, `false`.
  - Throws `ArgumentNullException` if `exception` is null.

- `FormatDetails(this CyclicDependencyException exception)`
  - Gets a detailed description of the cyclic dependency including both job IDs and the error code.
  - Returns a formatted string containing the dependency details and error code.
  - Throws `ArgumentNullException` if `exception` is null.

- `IsSpecificError(this CyclicDependencyException exception, string errorCode)`
  - Determines whether this cyclic dependency exception matches a specific error code.
  - Returns `true` if the exception's error code matches `errorCode` (case-insensitive); otherwise, `false`.
  - Throws `ArgumentNullException` if `exception` is null or `errorCode` is null or empty.

- `GetSummary(this CyclicDependencyException exception)`
  - Gets a summary of the cyclic dependency exception for logging or diagnostic purposes.
  - Returns a dictionary containing the exception type, message, error code, and dependency details.
  - Throws `ArgumentNullException` if `exception` is null.

### Example

```csharp
try
{
    // Some operation that might throw CyclicDependencyException
}
catch (CyclicDependencyException ex)
{
    // Get a human-readable description
    string description = ex.GetDescription();
    _logger.LogError(description);

    // Check if the cyclic dependency involves a specific job
    Guid jobIdToCheck = Guid.NewGuid();
    if (ex.InvolvesJob(jobIdToCheck))
    {
        _logger.LogError($"The cyclic dependency involves job {jobIdToCheck}.");
    }

    // Get detailed description with error code
    string details = ex.FormatDetails();
    _logger.LogError(details);

    // Check for a specific error code
    if (ex.IsSpecificError("CD001"))
    {
        // Handle specific error
    }

    // Get summary for diagnostic purposes
    var summary = ex.GetSummary();
    // summary contains Type, Message, ErrorCode, JobId, and DependsOnJobId
}
```

---

## ExecutionExceptionExtensions

Provides extension methods for `ExecutionException` to facilitate logging, diagnostics, and error handling.

### Methods

- `ToLogMessage(this ExecutionException ex)`
  - Creates a single-line log message that contains the most important data from the exception.
  - Returns a formatted log string containing execution ID, job ID, attempt number, and message.
  - Throws `ArgumentNullException` if `ex` is null.

- `IsRetryable(this ExecutionException ex, int maxAttempts)`
  - Determines whether the exception indicates that the job may be retried based on a maximum number of attempts.
  - Returns `true` if `ExecutionException.AttemptNumber` is less than `maxAttempts`; otherwise, `false`.
  - Throws `ArgumentNullException` if `ex` is null.
  - Throws `ArgumentOutOfRangeException` if `maxAttempts` is negative.

- `ToDictionary(this ExecutionException ex)`
  - Returns a read-only dictionary that maps the exception's key properties to their string representations.
  - Returns a dictionary containing the exception data.
  - Throws `ArgumentNullException` if `ex` is null.

- `GetCorrelationInfo(this ExecutionException ex)`
  - Retrieves the correlation identifiers associated with the exception.
  - Returns a tuple containing `ExecutionException.ExecutionId` and `ExecutionException.JobId`.
  - Throws `ArgumentNullException` if `ex` is null.

### Example

```csharp
try
{
    // Some operation that might throw ExecutionException
}
catch (ExecutionException ex)
{
    // Create a log message
    string logMessage = ex.ToLogMessage();
    _logger.LogError(logMessage);

    // Check if the execution is retryable (allowing up to 3 attempts)
    if (ex.IsRetryable(3))
    {
        _logger.Warning("Execution can be retried.");
    }
    else
    {
        _logger.Error("Execution has exceeded the maximum number of attempts.");
    }

    // Convert to dictionary for structured logging
    var dict = ex.ToDictionary();
    // dict contains ExecutionId, JobId, AttemptNumber, and Message

    // Get correlation IDs
    var (executionId, jobId) = ex.GetCorrelationInfo();
    _logger.Information($"Execution ID: {executionId}, Job ID: {jobId}");
}
```

---

## JobValidationExceptionExtensions

Provides extension methods for `JobValidationException`.

### Methods

- `GetFormattedMessage(this JobValidationException exception)`
  - Gets a formatted error message that includes the property name.
  - Returns a formatted error message. If the property name is null or empty, returns the exception message; otherwise, returns a message in the format "Validation error on {PropertyName}: {Message}".
  - Throws `ArgumentNullException` if `exception` is null.

- `Clone(this JobValidationException exception)`
  - Creates a new `JobValidationException` with the same message and property name.
  - Returns a new `JobValidationException` instance.
  - Throws `ArgumentNullException` if `exception` is null.

### Example

```csharp
try
{
    // Some operation that might throw JobValidationException
}
catch (JobValidationException ex)
{
    // Get a formatted message that includes the property name
    string formattedMessage = ex.GetFormattedMessage();
    _logger.LogWarning(formattedMessage);

    // Clone the exception if needed (e.g., to rethrow with additional context)
    JobValidationException clonedEx = ex.Clone();
    // Optionally modify the cloned exception before rethrowing
    throw clonedEx;
}
```