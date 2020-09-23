# JobSchedulerException
The `JobSchedulerException` class represents an exception that occurs during the execution of a job scheduler. It provides a way to handle and propagate errors that may arise during the scheduling and execution of jobs, allowing for more robust and reliable job scheduling systems.

## API
The `JobSchedulerException` class has the following public members:
* `ErrorCode`: a property of type `string?` that represents the error code associated with the exception.
* `JobSchedulerException(string message)`: a constructor that initializes a new instance of the `JobSchedulerException` class with the specified error message.
* `JobSchedulerException(string message, string errorCode)`: a constructor that initializes a new instance of the `JobSchedulerException` class with the specified error message and error code.
* `JobSchedulerException(string message, Exception innerException)`: a constructor that initializes a new instance of the `JobSchedulerException` class with the specified error message and inner exception.

## Usage
Here are two examples of using the `JobSchedulerException` class:
```csharp
// Example 1: Throwing a JobSchedulerException with an error message
try
{
    // Code that may throw an exception
    throw new JobSchedulerException("Failed to schedule job");
}
catch (JobSchedulerException ex)
{
    Console.WriteLine($"Error scheduling job: {ex.Message}");
}

// Example 2: Throwing a JobSchedulerException with an error message and inner exception
try
{
    // Code that may throw an exception
    throw new Exception("Inner exception");
}
catch (Exception ex)
{
    throw new JobSchedulerException("Failed to schedule job", ex);
}
```

## Notes
The `JobSchedulerException` class is designed to be used in a job scheduling system to handle and propagate errors that may arise during the scheduling and execution of jobs. It is recommended to use the `ErrorCode` property to provide additional information about the error that occurred. The class is thread-safe, as it only contains immutable state and does not have any static members. However, it is still important to handle the exception properly to avoid losing error information. Additionally, when throwing a `JobSchedulerException`, it is recommended to provide a descriptive error message and, if applicable, an inner exception to provide more context about the error that occurred.
