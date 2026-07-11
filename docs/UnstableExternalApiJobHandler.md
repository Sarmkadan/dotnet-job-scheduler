# UnstableExternalApiJobHandler

`UnstableExternalApiJobHandler` is a specialized job handler designed to execute operations against external APIs that may experience intermittent connectivity issues, high latency, or unreliable availability. It incorporates robust retry logic and error-handling strategies to ensure that jobs involving external services can recover from transient failures without requiring manual intervention, thereby enhancing the reliability of background processing tasks.

## API

*   **`UnstableExternalApiJobHandler()`**
    Initializes a new instance of the `UnstableExternalApiJobHandler` class.

*   **`Task<string> ExecuteAsync()`**
    Executes the primary operation defined for the unstable external API. Returns a string representing the result or status of the operation. Throws an `HttpRequestException` if the external service remains unreachable after all configured retry attempts.

*   **`DatabaseQueryJobHandler`**
    A property providing access to a `DatabaseQueryJobHandler` instance, used for managing database-related operations within the job execution context.

*   **`Task<string> DatabaseQueryJobHandler.ExecuteAsync()`**
    Executes a database query operation. Returns a string indicating the result of the query. Throws a `DatabaseException` if the query fails due to connectivity or syntax errors.

*   **`GracefulFailureJobHandler`**
    A property providing access to a `GracefulFailureJobHandler` instance, designed to handle scenarios where a job operation fails but should not terminate the overall process.

*   **`GracefulFailureJobHandler.ExecuteAsync()`**
    Executes an operation that is designed to fail gracefully. Returns a string summarizing the outcome of the operation, even in failure scenarios. Does not throw exceptions related to expected failures.

*   **`static async Task Main(string[] args)`**
    The entry point for the job handler application, responsible for initializing the necessary dependencies and starting the job execution loop.

## Usage

```csharp
// Example 1: Basic usage within a service
var handler = new UnstableExternalApiJobHandler();
try 
{
    string result = await handler.ExecuteAsync();
    Console.WriteLine($"Operation completed: {result}");
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"Operation failed after retries: {ex.Message}");
}
```

```csharp
// Example 2: Utilizing associated handlers for complex workflows
var handler = new UnstableExternalApiJobHandler();
// Execute database-related work
string dbResult = await handler.DatabaseQueryJobHandler.ExecuteAsync();

// Execute an operation with graceful failure handling
string failureResult = await handler.GracefulFailureJobHandler.ExecuteAsync();
```

## Notes

*   **Thread-Safety**: The `UnstableExternalApiJobHandler` and its associated handlers are designed to be thread-safe when instantiated as singletons or registered with appropriate lifetimes in a dependency injection container. Internal state management for retries is handled per execution to prevent race conditions.
*   **Edge Cases**: Ensure that external API timeouts are configured appropriately, as excessive wait times can saturate the thread pool during periods of high service latency.
*   **Dependency Management**: Ensure that `DatabaseQueryJobHandler` and `GracefulFailureJobHandler` are correctly configured before calling their respective `ExecuteAsync` methods to avoid `NullReferenceException`.
