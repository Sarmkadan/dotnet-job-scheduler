# Job

The `Job` class represents the core data model for a scheduled task within the `dotnet-job-scheduler` ecosystem. It encapsulates all configuration details required to define execution logic, including scheduling intervals via CRON expressions, retry policies, concurrency limits, and runtime state tracking. This entity serves as the primary contract between the scheduler engine and the underlying storage mechanism, ensuring that job definitions and their historical execution metrics are persisted and accessible for management and monitoring purposes.

## API

The following members expose the configuration and state of a `Job` instance. As this type functions primarily as a data transfer object (DTO) or entity model, these members are properties that do not accept parameters, return values in the traditional method sense, or throw exceptions during standard access.

### `public Guid Id`
Gets or sets the unique identifier for the job. This value is immutable once assigned and is used to reference the job across the system, particularly in logs, execution records, and API endpoints.

### `public string Name`
Gets or sets the human-readable name of the job. This should be unique within the context of the application to facilitate identification in dashboards and logging outputs.

### `public string Description`
Gets or sets a detailed description of the job's purpose and behavior. This field is informational and does not influence scheduler logic.

### `public string CronExpression`
Gets or sets the CRON expression string that defines the schedule for job execution. The format must adhere to the standard supported by the scheduler's time parsing engine (typically seconds, minutes, hours, day, month, day-of-week). Invalid expressions may cause scheduling failures at runtime when the job is activated.

### `public string? TimeZoneId`
Gets or sets the IANA time zone identifier (e.g., "America/New_York") used to evaluate the `CronExpression`. If `null`, the scheduler defaults to the server's local time or UTC, depending on global configuration.

### `public JobPriority Priority`
Gets or sets the execution priority level. This enum value influences the order in which pending jobs are dequeued and executed when system resources are constrained or multiple jobs are triggered simultaneously.

### `public JobStatus Status`
Gets or sets the current lifecycle state of the job (e.g., `Active`, `Paused`, `Completed`, `Failed`). The scheduler uses this property to determine whether the job is eligible for triggering.

### `public string HandlerType`
Gets or sets the fully qualified type name of the class responsible for executing the job logic. The scheduler uses reflection or an IoC container to resolve and instantiate this type during execution.

### `public string? HandlerParameters`
Gets or sets an optional JSON-formatted string containing configuration parameters passed to the `HandlerType` at runtime. The structure of this string is defined by the specific handler implementation.

### `public bool IsActive`
Gets or sets a flag indicating whether the job is currently enabled. While similar to `Status`, this boolean often serves as a quick filter for active scheduling loops, whereas `Status` provides granular state information.

### `public int MaxConcurrentExecutions`
Gets or sets the maximum number of instances of this specific job allowed to run simultaneously. If this limit is reached, new triggers are either queued or skipped based on the scheduler's overflow policy.

### `public int MaxRetries`
Gets or sets the maximum number of retry attempts permitted if the job execution fails. A value of `0` indicates no retries should be attempted.

### `public int RetryBackoffSeconds`
Gets or sets the delay in seconds between retry attempts. This value is typically used in an exponential or linear backoff strategy to prevent immediate resource contention upon failure.

### `public int ExecutionTimeoutSeconds`
Gets or sets the maximum duration allowed for a single execution of the job. If the handler exceeds this timeframe, the execution is typically cancelled or marked as timed out.

### `public DateTime CreatedAt`
Gets or sets the timestamp indicating when the job record was initially created in the system.

### `public DateTime? UpdatedAt`
Gets or sets the timestamp of the last modification made to the job configuration. This value is `null` if the job has never been updated since creation.

### `public DateTime? LastExecutedAt`
Gets or sets the timestamp when the job last started or completed execution. This is `null` if the job has never been run.

### `public DateTime? NextExecutionAt`
Gets or sets the calculated timestamp for the next scheduled execution based on the `CronExpression` and `TimeZoneId`. This value is updated dynamically by the scheduler.

### `public int TotalExecutions`
Gets or sets the cumulative count of how many times the job has been triggered, regardless of success or failure.

### `public int SuccessfulExecutions`
Gets or sets the cumulative count of executions that completed without error. This metric is useful for calculating reliability and success rates.

## Usage

### Example 1: Defining a New Recurring Job
The following example demonstrates instantiating a `Job` object configured for a daily cleanup task with specific retry logic and concurrency constraints.

```csharp
using System;
using DotNetJobScheduler;

public class JobConfiguration
{
    public static Job CreateDailyCleanupJob()
    {
        return new Job
        {
            Id = Guid.NewGuid(),
            Name = "DailyDatabaseCleanup",
            Description = "Removes temporary records older than 30 days.",
            CronExpression = "0 0 2 * * *", // Every day at 2:00 AM
            TimeZoneId = "UTC",
            Priority = JobPriority.High,
            Status = JobStatus.Active,
            HandlerType = "MyApp.Jobs.DatabaseCleanupHandler",
            HandlerParameters = "{\"retentionDays\": 30, \"batchSize\": 1000}",
            IsActive = true,
            MaxConcurrentExecutions = 1,
            MaxRetries = 3,
            RetryBackoffSeconds = 60,
            ExecutionTimeoutSeconds = 1800, // 30 minutes
            CreatedAt = DateTime.UtcNow,
            TotalExecutions = 0,
            SuccessfulExecutions = 0
        };
    }
}
```

### Example 2: Updating Job State After Execution
This example illustrates how a scheduler service might update the statistical and state properties of a `Job` instance after an execution cycle completes.

```csharp
using System;
using DotNetJobScheduler;

public class SchedulerService
{
    public void RecordExecutionResult(Job job, bool isSuccess, DateTime startTime)
    {
        // Update execution counters
        job.TotalExecutions++;
        if (isSuccess)
        {
            job.SuccessfulExecutions++;
            job.Status = JobStatus.Active; // Ensure it remains active
        }
        else
        {
            // Logic to handle failure status would go here
            // depending on retry count vs MaxRetries
        }

        // Update timestamps
        job.LastExecutedAt = startTime;
        job.UpdatedAt = DateTime.UtcNow;
        
        // Note: NextExecutionAt is typically recalculated by the scheduler engine
        // based on the CronExpression, not manually set here.
    }
}
```

## Notes

*   **Thread Safety**: The `Job` class is designed as a data container and is not inherently thread-safe. In a multi-threaded scheduler environment where multiple workers might read or update properties like `TotalExecutions`, `Status`, or `LastExecutedAt` concurrently, external synchronization (e.g., locks) or atomic operations provided by the underlying storage repository must be used to prevent race conditions.
*   **CRON Validation**: The `CronExpression` property accepts a raw string. No validation is performed automatically by the property setter. Invalid expressions will only result in exceptions when the scheduler attempts to calculate the `NextExecutionAt` time.
*   **Nullability**: Properties such as `TimeZoneId`, `HandlerParameters`, `UpdatedAt`, `LastExecutedAt`, and `NextExecutionAt` are nullable. Consumers must handle `null` values gracefully, particularly when calculating schedules or serializing the object to JSON.
*   **State Consistency**: There is a logical relationship between `IsActive`, `Status`, and `MaxRetries`. Setting `IsActive` to `false` should ideally correspond to a `Status` of `Paused` or `Disabled`. Similarly, if retry logic is exhausted, the `Status` should be updated to reflect a failure state, even if `IsActive` remains `true`. Maintaining this consistency is the responsibility of the application logic managing the `Job` entity.
*   **Time Zone Sensitivity**: Changing the `TimeZoneId` on an active job will immediately alter the calculation for `NextExecutionAt`. Care should be taken when modifying this property on running jobs to avoid unintended execution skips or duplicate triggers during the transition.
