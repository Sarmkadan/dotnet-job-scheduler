# DelayingJobHandler

The `DelayingJobHandler` type encapsulates the logic for executing a job that incorporates an intentional delay, manages concurrency limits, records execution metadata, and persists results to a repository. It is intended for use within the scheduler infrastructure where jobs may need to throttle simultaneous executions and capture timing information for monitoring and debugging.

## API

### `public async Task<string> ExecuteAsync()`
- **Purpose**: Executes the delaying job workflow, which includes waiting for the configured delay, invoking the underlying job logic, and returning a result identifier.
- **Parameters**: None.
- **Return Value**: A `Task<string>` that completes with a string representing the outcome or identifier of the executed job.
- **Exceptions**: May propagate any exception thrown by the underlying job implementation or by concurrency/timeout guards (see specific test methods for detailed conditions).

### `public async Task ExecuteJobAsync_WithValidJob_CreatesExecution()`
- **Purpose**: Verifies that when a valid job is supplied, the handler creates an execution record.
- **Parameters**: None.
- **Return Value**: A `Task` that completes when the verification finishes.
- **Exceptions**: Does not throw under normal conditions; failures indicate a test assertion error.

### `public async Task ExecuteJobAsync_WithNullJob_ThrowsArgumentNullException()`
- **Purpose**: Confirms that passing a null job argument results in an `ArgumentNullException`.
- **Parameters**: None.
- **Return Value**: A `Task` that completes when the assertion is validated.
- **Exceptions**: Throws `ArgumentNullException` if the job is null.

### `public async Task ExecuteJobAsync_WhenConcurrencyExceeded_ThrowsConcurrencyException()`
- **Purpose**: Ensures that exceeding the allowed concurrent executions triggers a `ConcurrencyException`.
- **Parameters**: None.
- **Return Value**: A `Task` that completes when the assertion is validated.
- **Exceptions**: Throws `ConcurrencyException` when the concurrency limit is surpassed.

### `public async Task ExecuteJobAsync_DecrementsCounterOnCompletion()`
- **Purpose**: Validates that the internal concurrency counter is decremented after a job finishes execution.
- **Parameters**: None.
- **Return Value**: A `Task` that completes when the verification finishes.
- **Exceptions**: Does not throw under normal conditions.

### `public async Task ExecuteJobAsync_WithCancellation_SetsRunningStatus()`
- **Purpose**: Checks that when a cancellation token is triggered, the handler correctly updates the job’s running status.
- **Parameters**: None.
- **Return Value**: A `Task` that completes when the assertion is validated.
- **Exceptions**: Does not throw under normal conditions; cancellation is handled internally.

### `public async Task ExecuteJobAsync_RecordsStartedAndCompletedTimes()`
- **Purpose**: Ensures that the handler records both the start and completion timestamps for an execution.
- **Parameters**: None.
- **Return Value**: A `Task` that completes when the verification finishes.
- **Exceptions**: Does not throw under normal conditions.

### `public async Task ExecuteJobAsync_MultipleConcurrentExecutions_HandlesConcurrency()`
- **Purpose**: Tests that the handler correctly manages several concurrent job executions without violating concurrency limits.
- **Parameters**: None.
- **Return Value**: A `Task` that completes when the verification finishes.
- **Exceptions**: Does not throw under normal conditions.

### `public async Task ExecuteJobAsync_WithShortTimeout_HandlesTimeoutScenario()`
- **Purpose**: Verifies that a job execution respecting a short timeout is handled appropriately (e.g., cancelled or faulted).
- **Parameters**: None.
- **Return Value**: A `Task` that completes when the verification finishes.
- **Exceptions**: May throw a `TimeoutException` or operation‑canceled exception depending on timeout configuration.

### `public async Task ExecuteJobAsync_SavesExecutionToRepository()`
- **Purpose**: Confirms that after execution, the handler persists the execution record to the underlying repository.
- **Parameters**: None.
- **Return Value**: A `Task` that completes when the verification finishes.
- **Exceptions**: Does not throw under normal conditions; repository errors would propagate.

### `public async Task ExecuteJobAsync_WithExceptionDuringExecution_RecordsError()`
- **Purpose**: Ensures that any exception thrown during job execution is captured and recorded in the execution record.
- **Parameters**: None.
- **Return Value**: A `Task` that completes when the verification finishes.
- **Exceptions**: Does not throw; the expected behavior is that the exception is recorded rather than bubbling up.

## Usage

```csharp
// Example 1: Basic execution of a delaying job
var handler = new DelayingJobHandler(job, delayMs: 500, maxConcurrency: 3);
string result = await handler.ExecuteAsync();
Console.WriteLine($"Job completed with result: {result}");
```

```csharp
// Example 2: Executing with a cancellation token
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
var handler = new DelayingJobHandler(job, delayMs: 1000, maxConcurrency: 1);
try
{
    await handler.ExecuteAsync(cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Job was cancelled due to timeout.");
}
```

## Notes

- The handler is **not thread‑safe** for concurrent calls to `ExecuteAsync` from multiple threads unless the caller respects the configured `maxConcurrency` limit; exceeding this limit will cause a `ConcurrencyException` as verified by `ExecuteJobAsync_WhenConcurrencyExceeded_ThrowsConcurrencyException`.
- All asynchronous methods are designed to be awaited; failing to do so may result in unobserved exceptions or incomplete execution records.
- The handler relies on an external repository for persisting execution data; repository failures will propagate as exceptions from `ExecuteAsync` and related test methods.
- Cancellation is honored only if a `CancellationToken` is supplied to the execution path; internal delays respect the token and will throw `OperationCanceledException` when triggered.
- The internal concurrency counter is incremented before job execution and decremented upon completion, regardless of whether the job succeeds, fails, or is cancelled. This ensures accurate tracking of active executions.
