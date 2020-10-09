# ExecutionResponse

Represents the outcome of a single job execution, capturing identifiers, timing, retry information, and any error details. It is used by the scheduler to report execution results to callers and to persist execution history.

## API

### Id
- **Type:** `Guid`
- **Purpose:** Unique identifier for this execution record.
- **Remarks:** Set when the execution is created; should not be empty.

### JobId
- **Type:** `Guid`
- **Purpose:** Identifier of the job that this execution belongs to.
- **Remarks:** Links the execution to its job definition.

### Status
- **Type:** `string`
- **Purpose:** Current state of the execution (e.g., `"Running"`, `"Completed"`, `"Failed"`).
- **Remarks:** The exact string values are defined by the scheduler; consumers should treat it as opaque unless they know the allowed set.

### StartedAt
- **Type:** `DateTime`
- **Purpose:** Timestamp when the execution began.
- **Remarks:** Always set; represents UTC time.

### CompletedAt
- **Type:** `DateTime?`
- **Purpose:** Timestamp when the execution finished, if it has completed.
- **Remarks:** `null` while the execution is still in progress.

### DurationMilliseconds
- **Type:** `long`
- **Purpose:** Total elapsed time of the execution in milliseconds.
- **Remarks:** May be zero or unset for incomplete executions; otherwise reflects `CompletedAt - StartedAt`.

### AttemptNumber
- **Type:** `int`
- **Purpose:** Sequential number of this execution attempt for the job.
- **Remarks:** Starts at `1` for the first try and increments with each retry.

### ExecutionTimeMs
- **Type:** `long`
- **Purpose:** Amount of CPU time consumed by the execution, in milliseconds.
- **Remarks:** Provided by the executor; may be approximate.

### RetryAttempt
- **Type:** `int`
- **Purpose:** Number of retry attempts that have been performed for this execution.
- **Remarks:** Distinct from `AttemptNumber`; counts only retries after an initial failure.

### ErrorMessage
- **Type:** `string?`
- **Purpose:** Descriptive message if the execution ended with an error.
- **Remarks:** `null` when the execution succeeded or is still running.

### ExecutorName
- **Type:** `string`
- **Purpose:** Name of the executor component that carried out the execution.
- **Remarks:** Useful for diagnostics and logging.

### IsRetryable
- **Type:** `bool`
- **Purpose:** Indicates whether the execution can be retried according to the job’s retry policy.
- **Remarks:** Derived from the job configuration and the current error state.

### CreatedAt
- **Type:** `DateTime`
- **Purpose:** Timestamp when the execution record was first created in the system.
- **Remarks:** Set at record inception; typically close to `StartedAt` but may precede it if the record is created before execution begins.

### FromExecution(Execution execution)
- **Type:** `static ExecutionResponse`
- **Purpose:** Factory method that maps an internal `Execution` domain object to an `ExecutionResponse` DTO.
- **Parameters:**
  - `execution`: The source `Execution` instance; must not be `null`.
- **Return:** A new `ExecutionResponse` populated with values from `execution`.
- **Exceptions:**
  - `ArgumentNullException` if `execution` is `null`.
  - `InvalidOperationException` if required fields on `execution` are missing or invalid.

### GetStatusText()
- **Type:** `string`
- **Purpose:** Returns a human‑readable description of the `Status` value.
- **Parameters:** None.
- **Return:** A string suitable for display in logs or UIs; returns the raw `Status` if no mapping exists.
- **Exceptions:** None.

## Usage

```csharp
// Example 1: Creating an ExecutionResponse from an internal Execution object.
Execution internalExec = jobScheduler.GetLastExecution(jobId);
ExecutionResponse response = ExecutionResponse.FromExecution(internalExec);

Console.WriteLine($"Execution {response.Id} for job {response.JobId} finished with status: {response.GetStatusText()}");
```

```csharp
// Example 2: Inspecting retryability and error details.
if (response.IsRetryable && response.RetryAttempt < job.MaxRetries)
{
    // Schedule a retry.
    jobScheduler.RetryJob(response.JobId);
}
else if (!string.IsNullOrWhiteSpace(response.ErrorMessage))
{
    // Log the failure.
    logger.Error($"Execution {response.Id} failed: {response.ErrorMessage}");
}
```

## Notes

- `CompletedAt` is nullable; consumers must check for `null` before using it to calculate durations.
- `DurationMilliseconds` may lag behind the actual wall‑clock time if the executor updates are not synchronized; treat it as an advisory metric.
- The type exposes its members as public fields/properties with no immutability guarantees; concurrent modification of the same instance from multiple threads is not thread‑safe. Instances should be considered immutable after construction via `FromExecution`, or external synchronization must be applied if mutation is required.
- `Status` values are arbitrary strings; relying on specific values couples the consumer to the scheduler’s internal state machine. Use `GetStatusText` for display purposes and avoid parsing the string for logic when possible.
- `ErrorMessage` may contain sensitive information; exercise caution when logging or exposing it to end‑users.
