# JobNotFoundExceptionExtensions

The `JobNotFoundExceptionExtensions` class provides a suite of static extension methods designed to simplify the configuration and inspection of `JobNotFoundException` instances within the `dotnet-job-scheduler` framework. By providing a fluent interface for exception modification and safe metadata retrieval, these extensions enable developers to write more concise and robust error-handling logic when interacting with missing job scenarios.

## API

### IsForJob
Determines whether the provided exception instance pertains to the specified Job ID.
- **Parameters:** `JobNotFoundException ex` (the exception), `Guid jobId` (the ID to check).
- **Returns:** `bool` - `true` if the exception is associated with the provided ID; otherwise, `false`.

### WithMessage
Configures the exception with a custom error message.
- **Parameters:** `JobNotFoundException ex` (the exception), `string message` (the error message to apply).
- **Returns:** `JobNotFoundException` - The modified exception instance, enabling fluent method chaining.

### WithJobId
Associates a specific Job ID with the exception instance.
- **Parameters:** `JobNotFoundException ex` (the exception), `Guid jobId` (the Job ID to associate).
- **Returns:** `JobNotFoundException` - The modified exception instance, enabling fluent method chaining.

### TryGetJobId
Safely attempts to retrieve the Job ID associated with the exception.
- **Parameters:** `JobNotFoundException ex` (the exception), `out Guid jobId` (the output parameter where the Job ID will be stored if found).
- **Returns:** `bool` - `true` if a Job ID was successfully retrieved; otherwise, `false`.

## Usage

### Fluent Exception Configuration
```csharp
public void HandleJobRequest(Guid jobId)
{
    // ...
    throw new JobNotFoundException()
        .WithJobId(jobId)
        .WithMessage($"No job found with the ID: {jobId}");
}
```

### Safe Metadata Retrieval in Catch Blocks
```csharp
try
{
    // ... logic that might throw JobNotFoundException
}
catch (JobNotFoundException ex)
{
    if (ex.TryGetJobId(out Guid jobId))
    {
        _logger.LogWarning("Operation failed: Job {JobId} not found.", jobId);
    }
    else
    {
        _logger.LogError(ex, "Operation failed: Job not found, ID unavailable.");
    }
}
```

## Notes

- **Thread-Safety:** These extension methods are static and do not modify internal state in a way that introduces thread-safety concerns; they operate directly on the instance provided. They are safe to use in multi-threaded contexts.
- **Null Reference Exceptions:** As with standard C# extension methods, calling any of these methods on a `null` `JobNotFoundException` instance will result in a `NullReferenceException` before the method body is executed.
- **Fluent Interface:** The `WithMessage` and `WithJobId` methods are designed to facilitate fluent API usage. They return the original exception instance, allowing them to be chained immediately upon the instantiation of the exception.
