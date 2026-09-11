# JobPriorityExtensions Documentation

## JobPriority Enum

### Definition
Defines priority levels for job execution in the scheduler queue. Higher values indicate higher priority and execute first.

### Values
| Member | Value | Summary |
|--------|-------|---------|
| `Low` | 0 | Lowest priority - executes last |
| `Normal` | 1 | Normal priority - default for most jobs |
| `High` | 2 | High priority - executes before normal priority jobs |
| `Critical` | 3 | Critical priority - executes before all other jobs |

### Ordering Semantics
- The `JobPriority` enum is ordered by integer value, where a higher value represents a higher priority.
- Jobs with higher priority values are executed before jobs with lower priority values in the scheduler queue.
- The default priority for most jobs is `Normal` (value 1).
- `Critical` (value 3) is the highest priority and will execute before all other jobs.
- `Low` (value 0) is the lowest priority and will execute after all other jobs.

## JobPriorityExtensions Class

### ToDisplayName Method
#### Description
Gets a human-readable display name of the job priority.

#### Parameters
- `priority`: The job priority.

#### Return Value
A display name string corresponding to the priority value.

#### Example Usage
```csharp
using JobScheduler.Core.Constants;

// Get display name for a priority
JobPriority priority = JobPriority.High;
string displayName = priority.ToDisplayName(); // Returns "High"

// Using in a switch or conditional
if (priority.ToDisplayName() == "Critical")
{
    // Handle critical priority jobs
}
```

## Examples

### Setting Job Priority
```csharp
using JobScheduler.Core.Constants;
using JobScheduler.Core.Models;

// Create a job with critical priority
var job = new Job
{
    Name = "Database Backup",
    Priority = JobPriority.Critical, // Highest priority
    // Other job properties...
};

// Create a job with low priority
var lowPriorityJob = new Job
{
    Name = "Log Cleanup",
    Priority = JobPriority.Low, // Lowest priority
    // Other job properties...
};
```

### Checking Priority in Processing Logic
```csharp
using JobScheduler.Core.Constants;

public void ProcessJob(Job job)
{
    // Process critical jobs immediately
    if (job.Priority >= JobPriority.High)
    {
        ExecuteImmediately(job);
    }
    else
    {
        EnqueueForLaterProcessing(job);
    }
}

// Using ToDisplayName for logging or UI
public string GetJobDescription(Job job)
{
    return $"Job: {job.Name} | Priority: {job.Priority.ToDisplayName()}";
}
```

### Priority Comparison
```csharp
using JobScheduler.Core.Constants;

public bool IsHigherPriority(JobPriority first, JobPriority second)
{
    return first > second; // Higher value means higher priority
}

// Example usage:
if (IsHigherPriority(JobPriority.High, JobPriority.Normal))
{
    // High priority jobs execute before normal priority jobs
}
```