# LongRunningJobHandler

The `LongRunningJobHandler` class provides a robust mechanism for executing time-intensive tasks within the `dotnet-job-scheduler` framework, ensuring that long-duration operations are handled asynchronously and reliably. This handler is designed to manage tasks that require substantial processing time, minimizing the risk of timeouts and resource exhaustion in the scheduler.

## API

### `LongRunningJobHandler`
*   **`public LongRunningJobHandler()`**
    *   **Purpose:** Initializes a new instance of the `LongRunningJobHandler` class.
    *   **Parameters:** None.
*   **`public async Task<string> ExecuteAsync()`**
    *   **Purpose:** Executes the assigned long-running job asynchronously.
    *   **Returns:** A `Task<string>` that resolves to a status message indicating the completion result of the job.
    *   **Throws:** Throws `InvalidOperationException` if the job is improperly configured, or `TaskCanceledException` if the execution is aborted.

### `CriticalJobHandler`
*   **`public async Task<string> ExecuteAsync()`**
    *   **Purpose:** Executes a critical-priority job asynchronously, ensuring high resource availability.
    *   **Returns:** A `Task<string>` that resolves to the result status of the critical operation.
    *   **Throws:** Throws `SystemException` if the underlying critical resource is unavailable.

### `QuickTaskJobHandler`
*   **`public async Task<string> ExecuteAsync()`**
    *   **Purpose:** Executes a short, high-frequency task asynchronously.
    *   **Returns:** A `Task<string>` that resolves to the completion status.
    *   **Throws:** Throws `ArgumentException` if the quick task parameters are invalid.

### `Main`
*   **`public static async Task Main()`**
    *   **Purpose:** Provides the entry point for the job scheduler application.
    *   **Parameters:** None.
    *   **Returns:** An `async Task`.

## Usage

### Example 1: Basic Handler Execution
```csharp
var handler = new LongRunningJobHandler();
string result = await handler.ExecuteAsync();
Console.WriteLine($"Job result: {result}");
```

### Example 2: Handling a Critical Job
```csharp
var criticalHandler = new CriticalJobHandler();
try 
{
    string status = await criticalHandler.ExecuteAsync();
    // Proceed with success logic
}
catch (SystemException ex)
{
    // Log failure for critical infrastructure
    Console.WriteLine($"Critical job failed: {ex.Message}");
}
```

## Notes

*   **Thread Safety:** The `ExecuteAsync` methods are designed to be thread-safe; however, the internal state of the job logic itself must manage concurrent access to shared resources if required.
*   **Async/Await:** All `ExecuteAsync` methods follow standard TPL patterns; ensure that calling code properly awaits the returned `Task` to avoid unhandled exceptions or premature termination of the process.
*   **Exception Handling:** Implement robust `try-catch` blocks around `ExecuteAsync` calls, as underlying job failures will propagate exceptions back to the caller.
*   **Lifecycle:** The `Main` method is intended for application startup only; do not invoke it programmatically during job execution.
