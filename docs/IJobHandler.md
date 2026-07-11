# IJobHandler

The `IJobHandler` interface provides a contract for executing and managing scheduled jobs, tracking execution statistics, and validating job readiness. It is designed to be implemented by job executors that need to report progress, handle failures, and expose runtime metrics.

## API

### `JobExecutorService`

The service responsible for executing jobs. This is typically injected into implementations of `IJobHandler`.

### `ExecuteJobAsync()`

Executes the associated job asynchronously.

- **Parameters**: None.
- **Return value**: A `Task<JobExecution>` representing the asynchronous operation. The `JobExecution` contains details about the execution outcome, including success, failure, or timeout.
- **Exceptions**: May throw if job execution cannot be initiated (e.g., job is invalid or service is unavailable).

### `ExecuteJobAsync(CancellationToken cancellationToken)`

Executes the associated job asynchronously with a cancellation token.

- **Parameters**:
  - `cancellationToken`: A `CancellationToken` to monitor for cancellation requests.
- **Return value**: A `Task<JobExecution>` representing the asynchronous operation. The `JobExecution` contains details about the execution outcome.
- **Exceptions**: May throw if job execution cannot be initiated or if cancellation is requested prematurely.

### `ValidateJobForExecutionAsync()`

Determines whether the job is ready to be executed.

- **Parameters**: None.
- **Return value**: A `Task<(bool CanExecute, string? Reason)>` tuple where:
  - `CanExecute`: `true` if the job can be executed; otherwise, `false`.
  - `Reason`: Optional reason why the job cannot be executed (e.g., dependencies not met, cooldown period active).
- **Exceptions**: May throw if validation logic fails due to system errors.

### `GetExecutionStatisticsAsync()`

Retrieves aggregated statistics about past executions of the job.

- **Parameters**: None.
- **Return value**: A `Task<ExecutionStatistics>` containing counts of total, successful, failed, timed-out, and skipped executions, along with average duration and success rate.
- **Exceptions**: May throw if statistics cannot be retrieved (e.g., data store unavailable).

### `JobId`

Gets the unique identifier of the job.

- **Type**: `Guid`
- **Access**: Read-only property.

### `TotalExecutions`

Gets the total number of times the job has been executed.

- **Type**: `int`
- **Access**: Read-only property.

### `SuccessfulExecutions`

Gets the number of executions that completed successfully.

- **Type**: `int`
- **Access**: Read-only property.

### `FailedExecutions`

Gets the number of executions that failed.

- **Type**: `int`
- **Access**: Read-only property.

### `TimedOutExecutions`

Gets the number of executions that timed out.

- **Type**: `int`
- **Access**: Read-only property.

### `SkippedExecutions`

Gets the number of executions that were skipped (e.g., due to validation failure).

- **Type**: `int`
- **Access**: Read-only property.

### `AverageDurationMs`

Gets the average duration of job executions in milliseconds.

- **Type**: `long`
- **Access**: Read-only property.

### `SuccessRate`

Gets the success rate of job executions as a value between 0.0 and 1.0.

- **Type**: `double`
- **Access**: Read-only property.

## Usage

### Example 1: Executing and Monitoring a Job
