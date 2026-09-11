# JobStatusExtensions Documentation

## JobStatus Enum

The `JobStatus` enum represents the status of a scheduled job in the system.

| Value | Description |
|-------|-------------|
| `Pending` (0) | Job has been created but not yet scheduled |
| `Scheduled` (1) | Job is currently scheduled and waiting for execution |
| `Running` (2) | Job is currently executing |
| `Completed` (3) | Job execution completed successfully |
| `Failed` (4) | Job failed and is awaiting retry |
| `Suspended` (5) | Job was suspended by user or system |
| `Cancelled` (6) | Job has been cancelled |
| `FailedPermanently` (7) | Job failed permanently after all retries exhausted |

## Extension Methods

### IsFinal

Determines whether the specified job status is a final status (i.e., no further transitions are expected).

#### Signature
```csharp
public static bool IsFinal(this JobStatus status)
```

#### Returns
- `true` if the status is `Completed`, `Cancelled`, or `FailedPermanently`
- `false` otherwise

#### Allowed Transitions
This method does not define transitions; it only evaluates whether a given status is considered final in the job lifecycle. Final statuses are terminal states where no further job processing occurs.

#### Examples
```csharp
// Example 1: Checking a completed job
JobStatus status = JobStatus.Completed;
bool isFinal = status.IsFinal(); // returns true

// Example 2: Checking a running job
status = JobStatus.Running;
isFinal = status.IsFinal(); // returns false

// Example 3: Checking a permanently failed job
status = JobStatus.FailedPermanently;
isFinal = status.IsFinal(); // returns true

// Example 4: Checking a suspended job
status = JobStatus.Suspended;
isFinal = status.IsFinal(); // returns false
```