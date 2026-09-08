// ... existing content ...

## HelloWorldJobHandlerExtensions

`HelloWorldJobHandlerExtensions` offers a collection of extension methods for `JobSchedulerService` that make it easy to work with the built‑in `HelloWorldJobHandler`.  
With these helpers you can create single or batched hello‑world jobs, retrieve active jobs, search by name pattern, validate a job's configuration, format its next execution time, and create recurring jobs using a simple interval.

**Usage example**

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Services;

public class HelloWorldDemo
{
    public static async Task Main()
    {
        // Assume an already configured JobSchedulerService instance
        var scheduler = new JobSchedulerService(/* dependencies */);

        // 1️⃣ Create a single hello‑world job
        var job = await scheduler.CreateHelloWorldJobAsync(
            name: "DemoHello",
            cronExpression: "*/5 * * * *", // every 5 minutes
            priority: JobPriority.Normal,
            isActive: true,
            maxRetries: 2,
            timeoutSeconds: 30,
            createdBy: "demo");

        // 2️⃣ Create a batch of jobs
        var batch = await scheduler.CreateHelloWorldJobsBatchAsync(
            baseName: "BatchJob",
            count: 3,
            startIndex: 1,
            cronExpression: "0 * * * *", // hourly
            createdBy: "demo");

        // 3️⃣ Retrieve all active hello‑world jobs
        IReadOnlyList<Job> activeJobs = await scheduler.GetActiveHelloWorldJobsAsync();

        // 4️⃣ Find jobs whose name contains "Demo"
        IReadOnlyList<Job> found = await scheduler.FindHelloWorldJobsByNameAsync("Demo");

        // 5️⃣ Validate a job configuration
        bool isValid = job.ValidateHelloWorldJobConfiguration();

        // 6️⃣ Get a human‑readable next execution time
        string next = job.GetNextExecutionTime();
        Console.WriteLine($"Next execution: {next}");

        // 7️⃣ Create a recurring hello‑world job that runs every 10 minutes
        var recurring = await scheduler.CreateRecurringHelloWorldJobAsync(
            name: "RecurringHello",
            intervalMinutes: 10,
            createdBy: "demo");

        Console.WriteLine("Demo completed.");
    }
}

## EmailSendingJobHandlerExtensions

`EmailSendingJobHandlerExtensions` provides a set of extension methods for `JobSchedulerService` to simplify the creation and management of email sending jobs. These extensions enable you to create single or batched email sending jobs, retrieve active jobs, find jobs by name pattern, validate job configurations, and get the next execution time in a human-readable format.

**Usage example**

```csharp
using System;
using System.Threading.Tasks;
using JobScheduler.Core.Services;

public class EmailSendingDemo
{
    public static async Task Main()
    {
        // Assume an already configured JobSchedulerService instance
        var scheduler = new JobSchedulerService(/* dependencies */);

        // Create a single email sending job
        var emailJob = await scheduler.CreateEmailSendingJobAsync(
            name: "DemoEmail",
            emailConfig: "{\"to\":\"example@example.com\",\"subject\":\"Hello\"}",
            cronExpression: "*/5 * * * *", // every 5 minutes
            priority: JobPriority.Normal,
            isActive: true,
            maxRetries: 2,
            timeoutSeconds: 30,
            createdBy: "demo");

        // Create a batch of email sending jobs
        var emailBatch = await scheduler.CreateEmailSendingJobsBatchAsync(
            baseName: "BatchEmail",
            emailConfig: "{\"to\":\"example@example.com\",\"subject\":\"Hello\"}",
            cronExpression: "0 * * * *", // hourly
            count: 3,
            startIndex: 1,
            createdBy: "demo");

        // Retrieve all active email sending jobs
        var activeEmailJobs = await scheduler.GetActiveEmailSendingJobsAsync();

        // Find email sending jobs by name pattern
        var foundEmailJobs = await scheduler.FindEmailSendingJobsByNameAsync("Demo");

        // Validate an email job configuration
        bool isValidEmailJob = emailJob.ValidateEmailJobConfiguration();

        // Get the next execution time for an email job
        string nextExecutionTime = emailJob.GetNextExecutionTime();
        Console.WriteLine($"Next execution: {nextExecutionTime}");

        Console.WriteLine("Demo completed.");
    }
}

These extensions streamline common email sending job scenarios while keeping the core scheduler logic untouched.

## JobValidationExceptionTests

The `JobValidationExceptionTests` class verifies the behavior of the `JobValidationException` exception, including constructor overloads and property mutability.

**Usage example**

```csharp
using JobScheduler.Core.Exceptions;

// Creating an exception with only a message
var ex1 = new JobValidationException("Job validation failed");
// ex1.Message == "Job validation failed"
// ex1.PropertyName == null

// Creating an exception with message and property name
var ex2 = new JobValidationException("Invalid priority", "Priority");
// ex2.Message == "Invalid priority"
// ex2.PropertyName == "Priority"

// Creating an exception with message and inner exception
var inner = new InvalidOperationException("Inner failure");
var ex3 = new JobValidationException("Validation error occurred", inner);
// ex3.Message == "Validation error occurred"
// ex3.InnerException == inner

// Modifying the PropertyName after construction
ex1.PropertyName = "JobId";
// ex1.PropertyName == "JobId"
```
```
```

## JobSchedulerExceptionExtensionsTests

The `JobSchedulerExceptionExtensionsTests` class verifies the behavior of the extension methods for `JobSchedulerException`,
including formatting details, checking specific error codes, and generating a summary dictionary.

**Usage example**

```csharp
using JobScheduler.Core.Exceptions;

// Create an exception with an error code
var exception = new JobSchedulerException("Something went wrong", "JOB-123");

// Get formatted details (includes error code)
string details = exception.FormatDetails();
// details == "Something went wrong (Error Code: JOB-123)"

// Check if the exception matches a specific error code
bool isMatch = exception.IsSpecificError("JOB-123");
// isMatch == true

// Get a summary of the exception as a dictionary
var summary = exception.GetSummary();
// summary contains Type, Message, and ErrorCode
```

## JobSchedulerExceptionTests

The `JobSchedulerExceptionTests` class verifies the behavior of the `JobSchedulerException` exception,
including constructor overloads, property behavior, and validation of null arguments.

**Usage example**

```csharp
using JobScheduler.Core.Exceptions;

// Creating an exception with only a message
var ex1 = new JobSchedulerException("Job scheduler failed");
// ex1.Message == "Job scheduler failed"
// ex1.ErrorCode == null

// Creating an exception with message and error code
var ex2 = new JobSchedulerException("Job scheduler failed", "SCH-001");
// ex2.Message == "Job scheduler failed"
// ex2.ErrorCode == "SCH-001"

// Creating an exception with message and inner exception
var inner = new InvalidOperationException("Inner failure");
var ex3 = new JobSchedulerException("Job scheduler failed", inner);
// ex3.Message == "Job scheduler failed"
// ex3.InnerException == inner
```
```

## ConcurrencyExceptionTests

The `ConcurrencyExceptionTests` class verifies the behavior of the `ConcurrencyException` exception,
which is thrown when job execution exceeds concurrency limits. It tests constructor behavior,
message formatting, property mutability, and inheritance from `JobSchedulerException`.

**Usage example**

```csharp
using JobScheduler.Core.Exceptions;

// Creating a concurrency exception with job ID and counts
var jobId = Guid.Parse("12345678-1234-1234-1234-123456789012");
var ex = new ConcurrencyException(jobId, 5, 3);
// ex.Message == "Job 12345678-1234-1234-1234-123456789012 cannot execute: current concurrent executions (5) exceed maximum allowed (3)."
// ex.JobId == jobId
// ex.CurrentConcurrentExecutions == 5
// ex.MaxAllowed == 3
// ex.ErrorCode == "CONCURRENCY_LIMIT_EXCEEDED"

// Properties can be modified after construction
ex.JobId = Guid.NewGuid();
ex.CurrentConcurrentExecutions = 99;
ex.MaxAllowed = 100;
```

## ExecutionExceptionTests

The `ExecutionExceptionTests` class verifies the behavior of the `ExecutionException` exception,
including constructor overloads for setting execution ID, job ID, attempt number, and inner exception,
as well as property mutability and inheritance from `JobSchedulerException`.

**Usage example**

```csharp
using JobScheduler.Core.Exceptions;

// Creating an exception with execution ID and job ID (defaults attempt number to 0)
var executionId = Guid.Parse("11111111-1111-1111-1111-111111111111");
var jobId = Guid.Parse("22222222-2222-2222-2222-222222222222");
var ex1 = new ExecutionException("Job failed", executionId, jobId);
// ex1.ExecutionId == executionId
// ex1.JobId == jobId
// ex1.AttemptNumber == 0

// Creating an exception with attempt number specified
var ex2 = new ExecutionException("Job failed on retry", executionId, jobId, 3);
// ex2.AttemptNumber == 3

// Creating an exception with inner exception
var inner = new InvalidOperationException("Database connection failed");
var ex3 = new ExecutionException("Job execution failed", executionId, jobId, inner);
// ex3.InnerException == inner

// Properties can be modified after construction
ex1.ExecutionId = Guid.NewGuid();
ex1.JobId = Guid.NewGuid();
ex1.AttemptNumber = 5;
```
```

## JobNotFoundExceptionTests

The `JobNotFoundExceptionTests` class verifies the behavior of the `JobNotFoundException` exception, including constructor overloads for GUID and string job identifiers, property mutability, and inheritance from `JobSchedulerException`.

**Usage example**



## JobNotFoundExceptionTests

The `JobNotFoundExceptionTests` class verifies the behavior of the `JobNotFoundException` exception, including constructor overloads for GUID and string job identifiers, property mutability, and inheritance from `JobSchedulerException`.

**Usage example**

```csharp
using JobScheduler.Core.Exceptions;
using System;

// Creating an exception with a job GUID
var jobId = Guid.NewGuid();
var ex1 = new JobNotFoundException(jobId);
// ex1.JobId == jobId
// ex1.Message == $"Job with ID '{jobId}' not found.";

// Creating an exception with a job name
var jobName = "ImportantJob";
var ex2 = new JobNotFoundException(jobName);
// ex2.Message == $"Job with name '{jobName}' not found.";

// Creating an exception with an inner exception
var inner = new InvalidOperationException("Inner failure");
var ex3 = new JobNotFoundException(jobId, inner);
// ex3.JobId == jobId
// ex3.Message == $"Job with ID '{jobId}' not found.";
// ex3.InnerException == inner;

// Modifying the JobId after construction
ex1.JobId = Guid.NewGuid();
// ex1.JobId now equals the new GUID
```

## CSV export

`CsvExportFormatter` provides CSV serialization for jobs, executions, and execution
statistics, as well as parsing for job CSV data:

- `ExportJobsToCsv(IEnumerable<Job>)` writes one row per job, including scheduling,
  retry, execution, and success-rate information.
- `ExportExecutionsToCsv(IEnumerable<JobExecution>)` writes execution identifiers,
  status, timestamps, duration, error and retry details, and output.
- `ExportStatisticsToCsv(Dictionary<Guid, (int Total, int Successful, long AvgTime)>)`
  writes aggregate totals, successful execution counts, calculated success rates, and
  average execution times for each job.
- `ParseJobsCsv(string)` skips the header and converts valid data rows from the job
  export format into `JobCsvRow` instances. Rows with fewer than 14 fields are ignored.

The header written by `ExportJobsToCsv` contains these columns, in order:

| CSV column | `JobCsvRow` property |
| --- | --- |
| `ID` | `Id` |
| `Name` | `Name` |
| `Description` | `Description` |
| `CronExpression` | `CronExpression` |
| `Priority` | `Priority` |
| `Status` | `Status` |
| `Active` | `IsActive` |
| `HandlerType` | `HandlerType` |
| `MaxRetries` | `MaxRetries` |
| `ExecutionTimeout` | `ExecutionTimeoutSeconds` |
| `NextExecution` | `NextExecution` |
| `LastExecution` | `LastExecution` |
| `TotalExecutions` | `TotalExecutions` |
| `SuccessRate` | `SuccessRate` |

Export jobs to a file with `File.WriteAllText`:

```csharp
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Formatters;

var jobs = new List<Job>
{
    new()
    {
        Name = "Daily report",
        Description = "Generates the daily report",
        CronExpression = "0 8 * * *",
        HandlerType = "DailyReportHandler"
    }
};

var csv = CsvExportFormatter.ExportJobsToCsv(jobs);
File.WriteAllText("jobs.csv", csv);
```

## EventPublisher

`EventPublisher` is the in-memory implementation of `IEventPublisher` in
`src/JobScheduler.Core/Events`. It provides type-safe, asynchronous pub/sub for
scheduler lifecycle events. Published events are queued on a bounded channel and
subscriber handlers run in the background; handler failures are logged without
preventing the other handlers from running.

The main APIs are:

- `PublishAsync<TEvent>(TEvent eventData)` queues an `ISchedulerEvent` for all
  subscribers registered for that exact event type. The returned task represents
  queueing the event, not completion of every subscriber.
- `Subscribe<TEvent>(Func<TEvent, Task> handler)` registers an asynchronous handler
  and returns an `IDisposable` subscription token. Dispose the token to stop receiving
  that event type.
- `Unsubscribe<TEvent>(object subscriptionToken)` explicitly removes the subscription
  represented by a token returned from `Subscribe<TEvent>`.
- `WaitForEventAsync<TEvent>(TimeSpan timeout)` completes when the next event of the
  requested type is published. This is useful for tests and coordination. In the
  current implementation, the `timeout` argument is accepted but is not enforced, so
  callers that require a deadline should apply their own cancellation or timeout.
- `GetActiveEventTypes()` returns the fully qualified names of event types that
  currently have subscribers.
- `GetSubscriberCount<TEvent>()` returns the number of handlers registered for an
  event type.
- `ClearSubscriptions<TEvent>()` removes every handler registered for one event type.
  `ClearAllSubscriptions()` is also available to remove all event subscriptions.

`PublishAsync`, `Subscribe`, `Unsubscribe`, and `WaitForEventAsync` are declared by
`IEventPublisher`. The inspection and clearing APIs are exposed by the concrete
`EventPublisher` class.

All events implement `ISchedulerEvent`, which supplies `EventId`, `JobId`, optional
`ExecutionId`, `OccurredAtUtc`, and the string `EventType` discriminator.
`IEventPublisher.cs` declares `SchedulerEventBase` and these concrete event types:

- `JobCreatedEvent`
- `JobExecutionStartedEvent`
- `JobExecutionCompletedEvent`
- `JobExecutionFailedEvent`
- `JobExecutionExhaustedEvent`
- `JobExecutionTimedOutEvent`
- `JobExecutionInterruptedEvent`
- `JobSuspendedEvent`
- `JobResumedEvent`
- `JobDeletedEvent`
- `SchedulerErrorEvent`

Subscribe and publish an event as follows:

```csharp
using JobScheduler.Core.Events;
using Microsoft.Extensions.Logging.Abstractions;

using var publisher = new EventPublisher(NullLogger<EventPublisher>.Instance);

using var subscription = publisher.Subscribe<JobCreatedEvent>(createdEvent =>
{
    Console.WriteLine($"Created job: {createdEvent.JobName} ({createdEvent.JobId})");
    return Task.CompletedTask;
});

await publisher.PublishAsync(new JobCreatedEvent
{
    JobId = Guid.NewGuid(),
    JobName = "Daily report",
    CreatedBy = "scheduler"
});

// Disposing subscription (automatically at the end of this scope) unsubscribes it.
```

## Health endpoints

The HealthController provides the following endpoints:

- `GET /api/health/live` - Liveness probe. Returns 200 if the service is running.
- `GET /api/health/ready` - Readiness probe. Returns 200 if the service is ready to handle requests (database connected), 503 otherwise.
- `GET /api/health/status` - Detailed health status. Returns 200 with a HealthStatusResponse object containing:
    - Timestamp: Current UTC timestamp
    - Version: Application version (hardcoded to "1.1.0")
    - Status: Overall status ("OK" or "Degraded")
    - Database: Database status (Available, LastChecked, ErrorMessage)
    - Jobs: Job statistics (TotalCount, ActiveCount)
    - Executions: Execution statistics (TotalCount, SuccessRate)
    - Memory: Memory usage (UsageMb, Threshold)
- `GET /api/health/diagnostics` - Detailed diagnostics. Returns 200 with a DiagnosticsResponse object containing:
    - Timestamp: Current UTC timestamp
    - MachineName: Machine name
    - ProcessorCount: Number of processors
    - RuntimeVersion: .NET runtime version
    - Memory: Memory diagnostics (TotalMemoryMb, ManagedHeapSizeMb, Gen0Collections, Gen1Collections, Gen2Collections)
    - SystemStatistics: System statistics (TotalJobs, ActiveJobs, TotalExecutions, AverageSuccessRate, AverageExecutionTimeMs)
    - RecentErrors: List of recent error log entries (Message, Count, LastOccurred)

Example Kubernetes liveness and readiness probe configuration:

```yaml
livenessProbe:
  httpGet:
    path: /api/health/live
    port: 80
  initialDelaySeconds: 30
  periodSeconds: 10
readinessProbe:
  httpGet:
    path: /api/health/ready
    port: 80
  initialDelaySeconds: 5
  periodSeconds: 10
```

## Distributed locking

`DistributedJobLockService` in `src/JobScheduler.Core/Services` provides
database-backed, per-job locking for scheduler deployments with multiple running
instances. Every instance must use the same scheduler database and a unique,
stable instance ID. A unique row for each job and EF Core write concurrency ensure
that only one instance can acquire a valid lock; after a lock expires, another
instance can take it over.

The service exposes these operations:

- `TryAcquireLockAsync(jobId, holderInstanceId, lockDuration)` creates a lock when
  none exists, renews it when the same instance already owns it, or takes over an
  expired lock. It returns `false` when another instance owns a non-expired lock or
  wins a concurrent acquisition race. The holder ID must not be blank and the
  duration must be positive.
- `ReleaseLockAsync(jobId, holderInstanceId)` removes the lock only when it belongs
  to the calling instance. A missing lock or a different holder is a no-op.
- `IsLockedAsync(jobId)` returns `true` only when the job has a non-expired lock.
- `RenewLockAsync(jobId, holderInstanceId, lockDuration)` extends a non-expired lock
  owned by the calling instance and returns whether renewal succeeded.
- `GetActiveLocksAsync()` returns all non-expired locks, ordered by acquisition time.
- `CleanExpiredLocksAsync()` removes expired rows and returns the number removed.
  Run it periodically to prevent old lock records from accumulating.

`DistributedJobLock` stores the persisted lock state:

| Field | Meaning |
| --- | --- |
| `Id` | Unique lock-row identifier, initialized to a new GUID. |
| `JobId` | Identifier of the job protected by the lock. |
| `HolderInstanceId` | Identifier of the scheduler instance that owns the lock. |
| `AcquiredAt` | UTC time at which the current holder acquired the lock. |
| `ExpiresAt` | UTC time at which the lock becomes eligible for takeover. |

The entity's `IsExpired(DateTime? utcNow = null)` helper tests `ExpiresAt` against
the supplied UTC time, or against `DateTime.UtcNow` when no time is supplied.

The following example represents the same job being considered by two scheduler
instances. In an application, each instance should create its own scoped
`JobSchedulerContext`; both contexts must connect to the same database.

```csharp
using JobScheduler.Core.Data;
using JobScheduler.Core.Services;
using Microsoft.EntityFrameworkCore;

var jobId = Guid.Parse("12345678-1234-1234-1234-123456789012");
var lockDuration = TimeSpan.FromMinutes(2);

var options = new DbContextOptionsBuilder<JobSchedulerContext>()
    .UseSqlServer(sharedConnectionString)
    .Options;

await using var nodeAContext = new JobSchedulerContext(options);
await using var nodeBContext = new JobSchedulerContext(options);

var nodeALocks = new DistributedJobLockService(nodeAContext);
var nodeBLocks = new DistributedJobLockService(nodeBContext);

if (await nodeALocks.TryAcquireLockAsync(jobId, "scheduler-node-a", lockDuration))
{
    try
    {
        // A competing instance receives false while node A's lock is valid.
        bool nodeBAcquired = await nodeBLocks.TryAcquireLockAsync(
            jobId,
            "scheduler-node-b",
            lockDuration);

        // Renew before expiry when the protected work can run for longer.
        bool renewed = await nodeALocks.RenewLockAsync(
            jobId,
            "scheduler-node-a",
            lockDuration);

        // Execute the job only while this instance owns a valid lock.
        if (renewed)
        {
            await ExecuteJobAsync(jobId);
        }
    }
    finally
    {
        await nodeALocks.ReleaseLockAsync(jobId, "scheduler-node-a");
    }
}

bool currentlyLocked = await nodeBLocks.IsLockedAsync(jobId);
IReadOnlyList<DistributedJobLock> activeLocks =
    await nodeBLocks.GetActiveLocksAsync();
int expiredLocksRemoved = await nodeBLocks.CleanExpiredLocksAsync();
```

## JobDependencyService

`JobDependencyService` manages prerequisite relationships between jobs while
preserving a directed acyclic graph (DAG). It can be constructed with a
`JobSchedulerContext` (and an optional `ILogger<JobDependencyService>`) or
resolved as `IJobDependencyService` after registering the scheduler services.

- `AddDependencyAsync(jobId, dependsOnJobId, createdBy)` records that `jobId`
  must run after `dependsOnJobId`. Both jobs must already exist. Adding the same
  relationship again is harmless, while self-dependencies are rejected with a
  `JobValidationException`.
- `RemoveDependencyAsync(jobId, dependsOnJobId)` removes that relationship. It
  is a no-op when the relationship does not exist.
- `GetTopologicalOrderAsync()` returns all jobs in execution order, with every
  prerequisite before the jobs that depend on it. If persisted data contains a
  cycle, only the jobs that can be ordered are returned and a warning is logged.
- `ValidateGraphAsync()` checks the complete persisted graph and returns a
  `DependencyGraphValidationResult`. Its `IsValid` property indicates whether
  the graph is a DAG, `CycleNodes` contains the job IDs in a detected cycle, and
  `Message` provides a human-readable summary.

`AddDependencyAsync` throws `CyclicDependencyException` when the proposed
relationship would create a cycle. The exception derives from
`JobSchedulerException`, has the error code `CYCLIC_DEPENDENCY_DETECTED`, and
exposes the attempted dependent and prerequisite IDs through `JobId` and
`DependsOnJobId`.

The following example creates two jobs and makes the report job wait for the
import job:

```csharp
using JobScheduler.Core.Data;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Exceptions;
using JobScheduler.Core.Services;

// Assume context is a configured JobSchedulerContext for the application.
var importJob = new Job
{
    Name = "Import orders",
    CronExpression = "0 * * * *",
    HandlerType = "ImportOrdersJobHandler"
};
var reportJob = new Job
{
    Name = "Build sales report",
    CronExpression = "5 * * * *",
    HandlerType = "BuildSalesReportJobHandler"
};

context.Jobs.AddRange(importJob, reportJob);
await context.SaveChangesAsync();

IJobDependencyService dependencies = new JobDependencyService(context);

// reportJob is the dependent; importJob is its prerequisite.
await dependencies.AddDependencyAsync(
    reportJob.Id,
    importJob.Id,
    createdBy: "sales-pipeline");

DependencyGraphValidationResult validation =
    await dependencies.ValidateGraphAsync();

if (!validation.IsValid)
{
    Console.WriteLine(validation.Message);
}

IReadOnlyList<Job> executionOrder =
    await dependencies.GetTopologicalOrderAsync();
// importJob appears before reportJob in executionOrder.

try
{
    // Reversing an existing edge would close a cycle and throw.
    await dependencies.AddDependencyAsync(importJob.Id, reportJob.Id);
}
catch (CyclicDependencyException ex)
{
    Console.WriteLine($"Rejected dependency {ex.JobId} -> {ex.DependsOnJobId}");
}

// Remove the relationship when the report no longer requires the import.
await dependencies.RemoveDependencyAsync(reportJob.Id, importJob.Id);
```

## ConcurrencyManager

`ConcurrencyManager` enforces global and per-job execution limits using the
current execution state from an `IExecutionRepository`. It also maintains local
in-memory counters that callers can update as executions start and finish.

Construct it with an execution repository to use
`SchedulerConstants.DefaultMaxConcurrentJobs` as the global limit and no
logger. The other constructor accepts an explicit `maxGlobalConcurrency` and
an optional `ILogger<ConcurrencyManager>`; its global-limit argument also
defaults to `SchedulerConstants.DefaultMaxConcurrentJobs`.

- `CanExecuteAsync(job)` returns `false` when the repository's global running
  count has reached the configured global limit, when a job marked
  `DisallowConcurrentExecution` already has a running instance, or when the
  job's `MaxConcurrentExecutions` limit has been reached. Otherwise it returns
  `true`.
- `EnsureCanExecuteAsync(job)` performs the same check and throws a
  `ConcurrencyException` when execution is not allowed.
- `IncrementConcurrencyCount(jobId)` and
  `DecrementConcurrencyCount(jobId)` update the local per-job and global
  counters. Counts are prevented from going below zero.
- `GetJobConcurrencyCount(jobId)` returns the cached count for one job, or zero
  if the job has no cached count. `GetGlobalConcurrencyCount()` returns the
  local global count.
- `SynchronizeWithDatabaseAsync()` rebuilds the per-job cache from running
  executions in the repository and replaces the local global count with the
  repository's authoritative concurrent-running count. Call it during startup
  and periodically when other scheduler nodes can start executions.
- `GetConcurrencyStats()` returns a dictionary containing `GlobalRunning`,
  `GlobalLimit`, `JobsWithExecutions`, and `TotalCachedJobs`.

The admission methods query persisted execution state; incrementing and
decrementing the local counters does not itself create or update an execution
record in the repository.

**Usage example**

```csharp
using System.Collections.Generic;
using JobScheduler.Core.Data.Repositories;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Exceptions;
using JobScheduler.Core.Services;

// Assume executionRepository is the application's configured repository.
IExecutionRepository executionRepository = /* resolve from dependency injection */;

// Uses SchedulerConstants.DefaultMaxConcurrentJobs as the global limit.
var concurrency = new ConcurrencyManager(executionRepository);

// An explicit limit and optional ILogger<ConcurrencyManager> can also be supplied:
// var concurrency = new ConcurrencyManager(executionRepository, 20, logger);

var job = new Job
{
    Name = "Import orders",
    CronExpression = "*/5 * * * *",
    HandlerType = "ImportOrdersJobHandler",
    MaxConcurrentExecutions = 2
};

await concurrency.SynchronizeWithDatabaseAsync();

if (await concurrency.CanExecuteAsync(job))
{
    bool incremented = false;
    try
    {
        // Rechecks the limits and throws ConcurrencyException if capacity was lost.
        await concurrency.EnsureCanExecuteAsync(job);
        concurrency.IncrementConcurrencyCount(job.Id);
        incremented = true;

        // Run the job and persist its execution state here.
    }
    catch (ConcurrencyException ex)
    {
        Console.WriteLine(ex.Message);
    }
    finally
    {
        if (incremented)
            concurrency.DecrementConcurrencyCount(job.Id);
    }
}

int jobCount = concurrency.GetJobConcurrencyCount(job.Id);
int globalCount = concurrency.GetGlobalConcurrencyCount();
Dictionary<string, int> stats = concurrency.GetConcurrencyStats();
```

## RetryService

`RetryService` evaluates failed executions, calculates retry timing, creates the
next execution record, and reports retry activity from an `IExecutionRepository`.

- `ShouldRetryAsync(job, execution)` returns `false` when either argument is
  `null`, when `execution.AttemptNumber` is greater than `job.MaxRetries`, or
  when `execution.IsRetryable` is `false`. Otherwise it returns `true`.
- `CalculateNextRetryTime(job, failedExecution)` adds the delay from
  `CalculateBackoffDelay` to `failedExecution.CompletedAt`. If `CompletedAt` is
  `null`, it uses the current UTC time. A `null` argument causes an
  `ArgumentNullException`.
- `CalculateBackoffDelay(job, attemptNumber)` calculates exponential backoff as
  `RetryBackoffSeconds * 2^(attemptNumber - 1)`. Attempts 0 and 1 both use the
  base delay. A base delay of zero or less becomes one second, and the result is
  clamped from one second through `max(1, ExecutionTimeoutSeconds)`. A `null`
  job or negative attempt number causes an exception.
- `CreateRetryExecution(job, failedExecution)` creates a new running execution
  with a new ID, the job ID, the current UTC start time, the failed execution's
  executor name, `IsRetryable` set to `true`, and its attempt number incremented
  by one. It throws `ArgumentNullException` if either argument is `null`.
- `IsRetryBudgetExceededAsync(jobId, retryBudgetCount, timeWindowMinutes)`
  queries executions between the current UTC time minus the window and the
  current UTC time. It returns `true` only when the number of failed executions
  for the specified job is greater than the budget. The defaults are five
  failures in five minutes.
- `GetRetryStatisticsAsync(jobId)` reports total executions, failed executions,
  total retries (the sum of `AttemptNumber - 1` for failures), average retries
  per failure, the latest failure completion time, and the percentage of
  executions started within the last hour that failed. Counts and rates that
  have no matching failures or recent executions are zero; the last failure
  time is `null` when there are no failures.
- `CalculateRetryDelay(attemptNumber, strategy, baseDelaySeconds)` returns a
  `TimeSpan` using a zero-based attempt index. `Exponential` uses
  `baseDelaySeconds * 2^attemptNumber`, `Linear` uses
  `baseDelaySeconds * (attemptNumber + 1)`, and `Fixed` always uses the base
  delay. An unrecognized enum value also uses the base delay. The default base
  delay is five seconds.

`JobRetryBackoffStrategy` has these values:

- `Exponential`: doubles the delay for each successive attempt.
- `Linear`: adds one base-delay unit for each successive attempt.
- `Fixed`: keeps the delay equal to the base delay.

**Usage example**

```csharp
using System;
using JobScheduler.Core.Data.Repositories;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Services;

// Assume both repositories are configured application services.
IJobRepository jobRepository = /* resolve from dependency injection */;
IExecutionRepository executionRepository = /* resolve from dependency injection */;
var retries = new RetryService(jobRepository, executionRepository);

var job = new Job
{
    MaxRetries = 3,
    RetryBackoffSeconds = 5,
    ExecutionTimeoutSeconds = 60
};

var failedExecution = new JobExecution
{
    JobId = job.Id,
    Status = JobScheduler.Core.Constants.ExecutionStatus.Failed,
    CompletedAt = DateTime.UtcNow,
    AttemptNumber = 1,
    ExecutorName = "worker-1",
    IsRetryable = true
};

if (await retries.ShouldRetryAsync(job, failedExecution) &&
    !await retries.IsRetryBudgetExceededAsync(job.Id))
{
    DateTime retryAt = retries.CalculateNextRetryTime(job, failedExecution);
    JobExecution nextExecution = retries.CreateRetryExecution(job, failedExecution);
    Console.WriteLine($"Attempt {nextExecution.AttemptNumber} at {retryAt:O}");
}

TimeSpan preview = retries.CalculateRetryDelay(
    attemptNumber: 2,
    strategy: JobRetryBackoffStrategy.Exponential,
    baseDelaySeconds: 5); // 20 seconds

RetryStatistics statistics = await retries.GetRetryStatisticsAsync(job.Id);
Console.WriteLine($"Failure rate: {statistics.RecentFailureRate:F1}%");
```
