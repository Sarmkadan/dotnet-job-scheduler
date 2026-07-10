# JobNotFoundException

Represents an exception that is thrown when a scheduled job cannot be located by its unique identifier. This exception carries the `JobId` that caused the lookup failure, enabling callers to inspect or log the missing identifier without parsing error messages.

## API

### Properties

- **`public Guid JobId`**  
  Gets the unique identifier of the job that was not found. This value is set at construction and remains immutable for the lifetime of the exception instance.

### Constructors

- **`public JobNotFoundException()`**  
  Initializes a new instance of the `JobNotFoundException` class with default error information. The `JobId` property is set to `Guid.Empty`. Use this overload when the missing job identifier is not available.

- **`public JobNotFoundException(Guid jobId)`**  
  Initializes a new instance of the `JobNotFoundException` class with a specified job identifier.  
  *Parameters*:  
  `jobId` — The `Guid` of the job that could not be found.  
  *Remarks*: The `JobId` property is set to the provided value.

- **`public JobNotFoundException(Guid jobId, string message)`**  
  Initializes a new instance of the `JobNotFoundException` class with a specified job identifier and a descriptive error message.  
  *Parameters*:  
  `jobId` — The `Guid` of the job that could not be found.  
  `message` — The error message that explains the reason for the exception.  
  *Remarks*: The `JobId` property is set to the provided value; the message is forwarded to the base exception class.

## Usage

### Example 1: Throwing when a job lookup fails

```csharp
public async Task CancelJobAsync(Guid jobId, IJobStore store, CancellationToken ct)
{
    var job = await store.GetJobAsync(jobId, ct);
    if (job is null)
    {
        throw new JobNotFoundException(jobId, $"No job exists with id '{jobId}'.");
    }

    job.Cancel();
    await store.SaveAsync(job, ct);
}
```

### Example 2: Catching and extracting the missing identifier

```csharp
try
{
    scheduler.Reschedule(jobId, newCronExpression);
}
catch (JobNotFoundException ex)
{
    logger.LogWarning(ex, "Attempted to reschedule a non-existent job. JobId: {MissingJobId}", ex.JobId);
    // Optionally clean up stale references or notify an administrator.
}
```

## Notes

- The `JobId` property is read-only after construction. It is safe to access from any thread once the exception object is fully instantiated.
- When the parameterless constructor is used, `JobId` defaults to `Guid.Empty`. Callers should not assume that a non-empty `Guid` guarantees the exception originated from a real lookup failure; always validate the context in which the exception was caught.
- This exception type does not implement custom serialization beyond what the base `Exception` class provides. Deserialized instances in cross-process or remoting scenarios will restore the `JobId` only if the underlying serialization mechanism supports it.
- No additional thread-safety guarantees are provided beyond those of the base `Exception` class. Instances are effectively immutable after construction, making them safe for concurrent read access.
