# JobExtensions

This document describes the public extension methods for the `Job` entity in the `JobScheduler.Core.Domain.Entities` namespace.

## Methods

### IsActiveAndDueForExecution

```csharp
public static bool IsActiveAndDueForExecution(this Job job)
```

Determines whether the job is active and due for execution at the current UTC time.

**Parameters**
- `job`: The job to evaluate.

**Returns**
- `true` if the job is active and its `NextExecutionAt` is set and less than or equal to `DateTime.UtcNow`; otherwise `false`.

**Exceptions**
- `ArgumentNullException`: Thrown when `job` is `null`.

**Usage Example**
```csharp
var job = GetJobFromRepository();
if (job.IsActiveAndDueForExecution())
{
    // Execute the job
    ExecuteJob(job);
}
```

### IsMisfired

```csharp
public static bool IsMisfired(this Job job, int toleranceSeconds = 60)
```

Determines whether the job has missed its scheduled execution time (misfired).
A job is considered misfired if its `NextExecutionAt` is in the past by more than the tolerance.

**Parameters**
- `job`: The job to evaluate.
- `toleranceSeconds`: The number of seconds after the scheduled time that is still considered on-time. Defaults to 60 seconds.

**Returns**
- `true` if the job is misfired; otherwise `false`.

**Exceptions**
- `ArgumentNullException`: Thrown when `job` is `null`.

**Usage Example**
```csharp
var job = GetJobFromRepository();
// Check if job is misfired with default tolerance (60 seconds)
if (job.IsMisfired())
{
    // Handle misfired job
    HandleMisfiredJob(job);
}

// Check if job is misfired with custom tolerance (30 seconds)
if (job.IsMisfired(30))
{
    // Handle misfired job with stricter tolerance
    HandleMisfiredJobStrict(job);
}
```

### GetSuccessRate

```csharp
public static double GetSuccessRate(this Job job)
```

Calculates the success rate of the job as a value between 0 and 1.

**Parameters**
- `job`: The job whose success rate is calculated.

**Returns**
- The success rate as a `double`. If the job has never been executed, `0.0` is returned.

**Exceptions**
- `ArgumentNullException`: Thrown when `job` is `null`.

**Usage Example**
```csharp
var job = GetJobFromRepository();
double successRate = job.GetSuccessRate();

// Display success rate as percentage
Console.WriteLine($"Job Success Rate: {successRate:P0}");

// Check if job has poor success rate
if (successRate < 0.8) // Less than 80%
{
    // Investigate job failures
    InvestigateJobFailures(job);
}
```

### GetTimeZoneInfo

```csharp
public static TimeZoneInfo GetTimeZoneInfo(this Job job)
```

Retrieves the `TimeZoneInfo` associated with the job.

**Parameters**
- `job`: The job whose time zone is retrieved.

**Returns**
- The `TimeZoneInfo` corresponding to `job.TimeZoneId`; if `job.TimeZoneId` is `null`, empty, or whitespace, `TimeZoneInfo.Utc` is returned.

**Exceptions**
- `ArgumentNullException`: Thrown when `job` is `null`.
- `TimeZoneNotFoundException`: Thrown when `job.TimeZoneId` is not `null` or whitespace but does not correspond to a valid system time zone ID.

**Usage Example**
```csharp
var job = GetJobFromRepository();
TimeZoneInfo timeZone = job.GetTimeZoneInfo();

// Schedule next execution in job's time zone
DateTime nextUtc = TimeZoneInfo.ConvertTimeToUtc(nextLocal, timeZone);

// Display job time in local time
DateTime localTime = TimeZoneInfo.ConvertTimeFromUtc(job.NextExecutionAt.Value, timeZone);
Console.WriteLine($"Next execution: {localTime} ({timeZone.DisplayName})");
```

### GetSummary

```csharp
public static string GetSummary(this Job job)
```

Generates a concise summary string for the job, including its name, status, priority, and execution statistics.

**Parameters**
- `job`: The job to summarize.

**Returns**
- A formatted string containing key job information.

**Exceptions**
- `ArgumentNullException`: Thrown when `job` is `null`.

**Usage Example**
```csharp
var job = GetJobFromRepository();
string summary = job.GetSummary();

// Log job summary
_logger.LogInformation("Job status: {Summary}", summary);

// Display in UI
jobSummaryLabel.Text = summary;

// Example output:
// "Job 'Backup Database' [123] - Status: Running, Priority: High, Executions: 42 (Success: 38) (90%)"
```

## See Also

- [Job Entity Documentation](Job.md)