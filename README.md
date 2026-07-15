// ... (rest of README.md content)

## JobSchedulerException

The `JobSchedulerException` is a base exception class for all job scheduler-related errors. It provides a way to handle errors that occur during job scheduling and execution. The exception includes an optional `ErrorCode` property to provide additional information about the error.

Example usage:
```csharp
try
{
    // Attempt to execute a job
    await jobExecutorService.ExecuteJobAsync(job);
}
catch (JobSchedulerException ex)
{
    Console.WriteLine($"Job scheduler error: {ex.Message}");
    if (ex.ErrorCode != null)
    {
        Console.WriteLine($"Error code: {ex.ErrorCode}");
    }
}

// Alternatively, you can throw a JobSchedulerException with an error code
throw new JobSchedulerException("Job scheduling failed", "SCHEDULING_ERROR");

// Or throw a JobSchedulerException with an inner exception
try
{
    // Attempt to execute a job
    await jobExecutorService.ExecuteJobAsync(job);
}
catch (Exception ex)
{
    throw new JobSchedulerException("Job execution failed", ex);
}
```
