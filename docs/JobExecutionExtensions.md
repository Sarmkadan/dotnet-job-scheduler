# JobExecutionExtensions

This document describes the public extension methods for the `JobExecution` entity in the `JobScheduler.Core.Domain.Entities` namespace.

## Methods

### IsOverdue

```csharp
public static bool IsOverdue(this JobExecution execution, double maxAllowedDurationMinutes = 60)
```

Determines whether the job execution is overdue based on its start time and duration.

**Parameters**
- `execution`: The job execution to evaluate.
- `maxAllowedDurationMinutes`: The maximum allowed duration in minutes before considering the job overdue.

**Returns**
- `true` if the job is overdue; otherwise, `false`.

**Exceptions**
- `ArgumentNullException`: Thrown if `execution` is `null`.
- `ArgumentOutOfRangeException`: Thrown if `maxAllowedDurationMinutes` is non-positive.

**Usage Example**
```csharp
var execution = GetJobExecutionFromRepository();
// Check if job execution is overdue with default threshold (60 minutes)
if (execution.IsOverdue())
{
    // Handle overdue job execution
    HandleOverdueExecution(execution);
}

// Check if job execution is overdue with custom threshold (30 minutes)
if (execution.IsOverdue(30))
{
    // Handle overdue job execution with stricter threshold
    HandleOverdueExecutionStrict(execution);
}
```

### ShouldRetry

```csharp
public static bool ShouldRetry(this JobExecution execution, int maxRetryAttempts = 3)
```

Determines whether the job execution should be retried based on retry policy and attempt count.

**Parameters**
- `execution`: The job execution to evaluate.
- `maxRetryAttempts`: The maximum allowed retry attempts.

**Returns**
- `true` if the job should be retried; otherwise, `false`.

**Exceptions**
- `ArgumentNullException`: Thrown if `execution` is `null`.
- `ArgumentOutOfRangeException`: Thrown if `maxRetryAttempts` is non-positive.

**Usage Example**
```csharp
var execution = GetJobExecutionFromRepository();
// Check if job execution should be retried with default max attempts (3)
if (execution.ShouldRetry())
{
    // Retry the job execution
    RetryJobExecution(execution);
}

// Check if job execution should be retried with custom max attempts (5)
if (execution.ShouldRetry(5))
{
    // Retry the job execution with higher retry limit
    RetryJobExecutionWithLimit(execution);
}
```

### GetExecutionRate

```csharp
public static double GetExecutionRate(this JobExecution execution)
```

Calculates the execution rate in executions per second for completed jobs.

**Parameters**
- `execution`: The job execution to evaluate.

**Returns**
- The execution rate in executions per second, or `0` if the job hasn't completed.

**Exceptions**
- `ArgumentNullException`: Thrown if `execution` is `null`.

**Usage Example**
```csharp
var execution = GetJobExecutionFromRepository();
double rate = execution.GetExecutionRate();

// Display execution rate
Console.WriteLine($"Execution Rate: {rate:F2} executions/second");

// Check if job execution rate is too low
if (rate < 0.1) // Less than 0.1 executions per second
{
    // Investigate slow job execution
    InvestigateSlowExecution(execution);
}
```

## See Also

- [JobExecution Entity Documentation](JobExecution.md)