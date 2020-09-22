# ExecutionException
The `ExecutionException` class represents an exception that occurs during the execution of a job in the `dotnet-job-scheduler` project. It provides information about the execution that failed, including the execution ID, job ID, and attempt number. This exception can be used to handle and log execution failures, and to provide insights into the execution process.

## API
* `public Guid ExecutionId`: Gets the ID of the execution that failed.
* `public Guid JobId`: Gets the ID of the job that was being executed.
* `public int AttemptNumber`: Gets the number of the attempt that failed.
* `public ExecutionException()`: Initializes a new instance of the `ExecutionException` class.
* `public ExecutionException(ExecutionException other)`: Initializes a new instance of the `ExecutionException` class, copying the properties from the specified `ExecutionException` instance.
* `public ExecutionException(Exception innerException)`: Initializes a new instance of the `ExecutionException` class with the specified inner exception.

## Usage
The following examples demonstrate how to use the `ExecutionException` class:
```csharp
try
{
    // Execute a job
    var executionId = ExecuteJob(jobId);
}
catch (ExecutionException ex)
{
    // Log the execution failure
    LogExecutionFailure(ex.ExecutionId, ex.JobId, ex.AttemptNumber);
}
```

```csharp
// Create a new ExecutionException instance
var ex = new ExecutionException(new InvalidOperationException("Execution failed"));

// Throw the exception
throw ex;
```

## Notes
When using the `ExecutionException` class, note that it is designed to provide information about the execution that failed. The `ExecutionId`, `JobId`, and `AttemptNumber` properties can be used to identify the specific execution and job that failed. The class is thread-safe, as it only provides read-only access to its properties. However, when creating a new instance of the `ExecutionException` class, be aware that the `ExecutionException(Exception innerException)` constructor will wrap the specified inner exception, which may affect the stack trace and debugging experience. Additionally, the `ExecutionException(ExecutionException other)` constructor will copy the properties from the specified `ExecutionException` instance, which may be useful when creating a new exception instance based on an existing one.
