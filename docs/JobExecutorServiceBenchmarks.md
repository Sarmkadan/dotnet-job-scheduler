# JobExecutorServiceBenchmarks
The `JobExecutorServiceBenchmarks` type is designed to provide a set of benchmarks for testing the performance and functionality of the `JobExecutorService` in various scenarios, including successful and failing job executions, concurrency limits, priority handling, and metrics collection. These benchmarks enable developers to evaluate the service's behavior under different conditions and identify potential bottlenecks or areas for improvement.

## API
The `JobExecutorServiceBenchmarks` type exposes the following public members:
* `Setup`: Sets up the benchmarking environment.
* `ExecuteJob_Successful`: Tests the successful execution of a job.
* `ExecuteJob_Failing`: Tests the execution of a failing job.
* `ExecuteJob_Timeout`: Tests the execution of a job with a timeout.
* `ExecuteJob_WithConcurrencyLimit`: Tests the execution of a job with a concurrency limit.
* `ExecuteJob_WithPriority`: Tests the execution of a job with priority handling.
* `ExecuteJob_WithMetricsCollection`: Tests the execution of a job with metrics collection.
* `MockJobHandler`: A mock job handler for testing purposes.
* `ExecuteAsync`: Executes a job asynchronously, with multiple overloads for different scenarios.
* `MockSlowHandler`: A mock slow handler for testing purposes.

## Usage
Here are two examples of using the `JobExecutorServiceBenchmarks` type:
```csharp
// Example 1: Testing successful job execution
var benchmarks = new JobExecutorServiceBenchmarks();
benchmarks.Setup();
await benchmarks.ExecuteJob_Successful();

// Example 2: Testing job execution with concurrency limit
var benchmarks = new JobExecutorServiceBenchmarks();
benchmarks.Setup();
await benchmarks.ExecuteJob_WithConcurrencyLimit();
```

## Notes
When using the `JobExecutorServiceBenchmarks` type, note the following:
* The `Setup` method must be called before executing any benchmarks.
* The `ExecuteAsync` methods may throw exceptions if the job execution fails or times out.
* The `MockJobHandler` and `MockSlowHandler` are intended for testing purposes only and should not be used in production code.
* The benchmarks are designed to be thread-safe, but concurrent execution of multiple benchmarks may affect the results.
* The `JobExecutorServiceBenchmarks` type does not handle errors or exceptions that may occur during job execution; it is the responsibility of the caller to handle such errors.
