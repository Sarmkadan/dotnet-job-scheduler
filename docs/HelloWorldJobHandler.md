# HelloWorldJobHandler

The `HelloWorldJobHandler` and `CounterJobHandler` classes provide diagnostic job implementations within the `dotnet-job-scheduler` framework. These handlers are designed to demonstrate the basic execution lifecycle for scheduled tasks, facilitating integration testing and baseline performance monitoring.

## API

### HelloWorldJobHandler
- `public HelloWorldJobHandler()`
  - Purpose: Initializes a new instance of the `HelloWorldJobHandler` class.

- `public async Task<string> ExecuteAsync()`
  - Purpose: Executes the core logic for the Hello World job.
  - Return value: Returns a `string` representing the successful execution result.
  - Exceptions: May throw exceptions if the task execution encounters an internal error.

### CounterJobHandler
- `public CounterJobHandler()`
  - Purpose: Initializes a new instance of the `CounterJobHandler` class.

- `public async Task<string> ExecuteAsync()`
  - Purpose: Executes the counter job logic.
  - Return value: Returns a `string` indicating the status or result of the counter operation.
  - Exceptions: May throw exceptions if the task execution encounters an internal error.

### Main
- `public static async Task Main(string[] args)`
  - Purpose: The entry point for the job scheduler application, allowing for the execution of scheduled jobs.
  - Parameters: `string[] args` — The command-line arguments provided at application startup.
  - Return value: Returns a `Task` representing the asynchronous operation.

## Usage

```csharp
// Example 1: Registering the HelloWorldJobHandler in a scheduler
var scheduler = new JobScheduler();
scheduler.RegisterJob<HelloWorldJobHandler>();
await scheduler.RunAsync();
```

```csharp
// Example 2: Manually executing the CounterJobHandler
var handler = new CounterJobHandler();
string result = await handler.ExecuteAsync();
Console.WriteLine($"Execution result: {result}");
```

## Notes

- **Thread Safety**: `HelloWorldJobHandler` and `CounterJobHandler` are designed to be stateless or thread-safe if their implementation does not rely on shared, mutable instance variables. Users should ensure that any external state accessed within `ExecuteAsync` is managed according to standard thread-safety practices.
- **Edge Cases**: In scenarios where the underlying scheduler is under heavy load, `ExecuteAsync` calls might experience execution delays. It is critical to ensure that `Main` properly awaits all asynchronous operations to prevent premature application termination.
