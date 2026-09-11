# ExecutionStatus Extensions

Documentation for the `ExecutionStatusExtensions` class and the `ExecutionStatus` enum.

## `ExecutionStatus`

Represents the result status of one job execution attempt.

| Member | Value | Meaning |
| --- | ---: | --- |
| `Running` | 0 | The execution is currently in progress. |
| `Success` | 1 | The execution completed successfully. |
| `Failed` | 2 | The execution failed with an error. |
| `Cancelled` | 3 | The execution was cancelled before completion. |
| `TimedOut` | 4 | The execution timed out. |
| `Skipped` | 5 | The execution was skipped because of concurrency control. |

## `ExecutionStatusExtensions`

Extension methods for <see cref="ExecutionStatus"/>.

### `IsTerminal`

Determines whether the specified execution status represents a completed execution.

```csharp
public static bool IsTerminal(this ExecutionStatus status)
```

**Parameters**

- `status`: The execution status to evaluate.

**Returns**

- <c>true</c> if the status is `Success`, `Failed`, `Cancelled`, `TimedOut`, or `Skipped`; otherwise, <c>false</c>.

**Remarks**

A terminal status indicates that the execution has finished and will not transition to another status. This is useful for determining when to stop polling for execution updates or when to consider a job execution complete for reporting purposes.

**Example**

```csharp
using JobScheduler.Core.Constants;

// Check if an execution has finished
ExecutionStatus status = GetExecutionStatus(jobId);
if (status.IsTerminal())
{
    // Execution is complete, handle result
    HandleCompletedExecution(jobId, status);
}
else
{
    // Execution is still in progress, continue waiting
    WaitForExecution(jobId);
}
```