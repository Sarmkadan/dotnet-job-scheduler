// ... (rest of README.md content)

## ExecutionException

Thrown when a job execution fails or encounters an error. This exception provides information about the failed execution, including the execution ID, job ID, and attempt number.

Example usage:
```csharp
try
{
    // Attempt to execute a job
    await jobExecutorService.ExecuteJobAsync(job);
}
catch (ExecutionException ex)
{
    Console.WriteLine($"Execution failed: {ex.Message}");
    Console.WriteLine($"Execution ID: {ex.ExecutionId}");
    Console.WriteLine($"Job ID: {ex.JobId}");
    Console.WriteLine($"Attempt number: {ex.AttemptNumber}");
}
```
