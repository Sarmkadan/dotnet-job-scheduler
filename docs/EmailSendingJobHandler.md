# EmailSendingJobHandler

The `EmailSendingJobHandler` class is responsible for the execution of automated email notification tasks within the `dotnet-job-scheduler` system. It encapsulates the logic required to process email jobs, interfacing with external mail services to dispatch messages reliably. The project also includes `DataCleanupJobHandler` for maintenance tasks and `JobSchedulerBackgroundService` as the entry point for the application.

## API

### EmailSendingJobHandler

*   **`EmailSendingJobHandler()`**: Initializes a new instance of the `EmailSendingJobHandler` class.

*   **`public async Task<string> ExecuteAsync()`**: Executes the email sending job.
    *   **Returns**: A `Task<string>` that completes with a status message or unique identifier for the executed job.

### DataCleanupJobHandler

*   **`DataCleanupJobHandler()`**: Initializes a new instance of the `DataCleanupJobHandler` class.

*   **`public async Task<string> ExecuteAsync()`**: Executes the data cleanup job, typically removing expired records from the system.
    *   **Returns**: A `Task<string>` that completes with a status summary of the cleanup operation.

### JobSchedulerBackgroundService

*   **`public static async Task Main(string[] args)`**: The application entry point for the job scheduler service.
    *   **Parameters**: `string[] args` - Command-line arguments passed to the application.
    *   **Returns**: A `Task` representing the asynchronous operation of the service.

## Usage

### Example 1: Executing an Email Sending Job
```csharp
var emailHandler = new EmailSendingJobHandler();
string result = await emailHandler.ExecuteAsync();
Console.WriteLine($"Email Job Result: {result}");
```

### Example 2: Initiating the Scheduler Service
```csharp
public class Program
{
    public static async Task Main(string[] args)
    {
        await JobSchedulerBackgroundService.Main(args);
    }
}
```

## Notes

*   **Thread Safety**: These handler classes are designed to be stateless regarding their job execution, allowing instances to be used safely within the application's dependency injection container, provided that dependencies injected into their constructors are also thread-safe.
*   **Asynchronous Execution**: All `ExecuteAsync` methods are asynchronous and should be awaited. Blocking on these tasks (e.g., using `.Result` or `.Wait()`) is strongly discouraged, as it may lead to deadlocks within the synchronization context.
*   **Error Handling**: Implementers should wrap calls to `ExecuteAsync` in `try-catch` blocks to handle potential network or service-related exceptions gracefully, as these methods do not explicitly define a set of thrown exceptions.
