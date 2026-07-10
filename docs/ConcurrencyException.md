# ConcurrencyException

The `ConcurrencyException` is thrown when a job scheduling operation fails because the maximum allowed number of concurrent executions for a job has been reached or exceeded. It carries information about the job that caused the violation, the current number of concurrent executions, and the configured limit, enabling callers to inspect or log the exact concurrency state at the time of the failure.

## API

### `ConcurrencyException()`

Initializes a new instance of the `ConcurrencyException` class. All properties default to their type’s default value (`Guid.Empty` for `JobId`, `0` for `CurrentConcurrentExecutions` and `MaxAllowed`).

### `Guid JobId`

Gets or sets the identifier of the job that exceeded the concurrency limit. This property is typically set after construction using an object initializer or by the code that throws the exception.

### `int CurrentConcurrentExecutions`

Gets or sets the number of concurrent executions of the job that were observed at the time the exception was thrown. This value reflects the actual count that caused the limit to be exceeded.

### `int MaxAllowed`

Gets or sets the maximum number of concurrent executions allowed for the job. This value represents the configured limit that was violated.

## Usage

The following example demonstrates throwing a `ConcurrencyException` when a job’s concurrency limit is exceeded.

```csharp
public void TryScheduleJob(Guid jobId, int currentExecutions, int maxAllowed)
{
    if (currentExecutions >= maxAllowed)
    {
        throw new ConcurrencyException
        {
            JobId = jobId,
            CurrentConcurrentExecutions = currentExecutions,
            MaxAllowed = maxAllowed
        };
    }
    // Proceed with scheduling...
}
```

The next example shows catching the exception and logging the relevant details.

```csharp
try
{
    scheduler.ScheduleJob(jobId);
}
catch (ConcurrencyException ex)
{
    logger.Warn(
        "Job {JobId} cannot be scheduled: {CurrentExecutions} concurrent executions (max {MaxAllowed}).",
        ex.JobId,
        ex.CurrentConcurrentExecutions,
        ex.MaxAllowed);
}
```

## Notes

- The `JobId`, `CurrentConcurrentExecutions`, and `MaxAllowed` properties are mutable. If the exception is caught and then rethrown or stored, the values may be modified by external code. Consider treating the exception as immutable after creation by only setting these properties once.
- The exception does not enforce any relationship between `CurrentConcurrentExecutions` and `MaxAllowed`; it is the responsibility of the throwing code to ensure the values are consistent.
- Thread safety is not guaranteed for instances of this exception. If the same instance is accessed concurrently from multiple threads, external synchronization is required.
- When using object initializers to set properties, be aware that the exception is fully constructed only after all property assignments complete. No validation is performed during construction.
