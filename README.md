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

## DashboardController REST API

`DashboardController` exposes monitoring data under the `/api/Dashboard` route.
All endpoints use `GET` and return `200 OK` with the response described below.

| Action | Route | Response |
| --- | --- | --- |
| `GetOverview` | `/api/Dashboard/overview` | `DashboardOverview` |
| `GetQueueStatus` | `/api/Dashboard/queue-status` | `QueueStatusResponse` |
| `GetPriorityDistribution` | `/api/Dashboard/priority-distribution` | `PriorityDistributionResponse` |
| `GetPerformanceTimeline` | `/api/Dashboard/performance-timeline?hours={hours}` | A list of `PerformanceTimelinePoint` objects. `hours` is optional and defaults to `24`. |
| `GetSlowestJobs` | `/api/Dashboard/slowest-jobs` | A list of `SlowestJobResponse` objects for the ten slowest jobs. |
| `GetMostFailingJobs` | `/api/Dashboard/most-failing-jobs` | A list of `FailingJobResponse` objects for the ten most frequently failing jobs. |
| `GetHealthReport` | `/api/Dashboard/health-report` | `HealthReportResponse` |

### Response DTO fields

- `DashboardOverview`: `TotalJobs` (`int`), `ActiveJobs` (`int`),
  `RunningExecutions` (`int`), `FailedJobsLast24Hours` (`int`),
  `AverageSuccessRate` (`double`), `TotalExecutions` (`int`),
  `SuccessfulExecutions` (`int`), `AverageExecutionTimeMs` (`long`), and
  `LastUpdatedAt` (`DateTime`).
- `QueueStatusResponse`: `PendingJobs` (`int`), `RunningJobs` (`int`),
  `FailedJobs` (`int`), `CompletedJobs` (`int`), `SuspendedJobs` (`int`),
  `TotalQueued` (`int`), `QueueUtilization` (`double`), and
  `EstimatedTimeToEmpty` (`TimeSpan?`).
- `PriorityDistributionResponse`: `CriticalJobs` (`int`), `HighJobs` (`int`),
  `NormalJobs` (`int`), `LowJobs` (`int`), and `TotalJobs` (`int`).
- `PerformanceTimelinePoint`: `Timestamp` (`DateTime`), `ExecutionCount` (`int`),
  `SuccessCount` (`int`), `FailureCount` (`int`), and
  `AverageExecutionTimeMs` (`long`).
- `SlowestJobResponse`: `JobId` (`Guid`), `JobName` (`string`),
  `AverageExecutionTimeMs` (`long`), `MaxExecutionTimeMs` (`long`), and
  `ExecutionCount` (`int`).
- `FailingJobResponse`: `JobId` (`Guid`), `JobName` (`string`),
  `FailureRate` (`double`), `FailedCount` (`int`), and `SuccessRate` (`double`).
- `HealthReportResponse`: `Timestamp` (`DateTime`), `DatabaseConnected` (`bool`),
  `MemoryUsageMb` (`long`), `ProcessorUtilization` (`double`), `Warnings`
  (`List<HealthWarning>`), and `IsHealthy` (`bool`).
- `HealthWarning`: `Severity` (`string`) and `Message` (`string`).

## HistoryController REST API

`HistoryController` exposes read-only execution history and aggregate statistics
under the `/api/History` route. All endpoints use `GET`.

| Action | Route | Query parameters | Response |
| --- | --- | --- | --- |
| `GetJobHistory` | `/api/History/jobs/{jobId}` | `status` (optional `ExecutionStatus`), `from` and `to` (optional `DateTime` bounds), `pageNumber` (default `1`), `pageSize` (default `20`) | `200 OK` with `PagedResult<ExecutionResponse>`; `404 Not Found` when the job does not exist |
| `GetJobSummary` | `/api/History/jobs/{jobId}/summary` | `from` and `to` (optional `DateTime` bounds) | `200 OK` with `JobExecutionSummary`; `404 Not Found` when the job does not exist |
| `GetSystemHistory` | `/api/History` | `status` (optional `ExecutionStatus`), `from` and `to` (optional `DateTime` bounds), `pageNumber` (default `1`), `pageSize` (default `20`) | `200 OK` with `PagedResult<ExecutionResponse>` |
| `GetSystemSummary` | `/api/History/summary` | `from` and `to` (optional `DateTime` bounds) | `200 OK` with `JobExecutionSummary` |

`jobId` must be a GUID. Date filters are supplied as date-time query-string
values; ISO 8601 values are recommended. `status` accepts an `ExecutionStatus`
value such as `Running`, `Success`, `Failed`, `Cancelled`, `TimedOut`, or
`Skipped`.

### Response shapes

- `PagedResult<ExecutionResponse>` contains `Items` (`IReadOnlyList<ExecutionResponse>`),
  `TotalCount` (`int`), `PageNumber` (`int`, 1-based), `PageSize` (`int`),
  `TotalPages` (`int`), `HasPreviousPage` (`bool`), and `HasNextPage` (`bool`).
- Each `ExecutionResponse` contains `Id` (`Guid`), `JobId` (`Guid`), `Status`
  (`string`), `StartedAt` (`DateTime`), `CompletedAt` (`DateTime?`),
  `DurationMilliseconds` (`long`), `AttemptNumber` (`int`), `ExecutionTimeMs`
  (`long`), `RetryAttempt` (`int`), `ErrorMessage` (`string?`), `ExecutorName`
  (`string`), `IsRetryable` (`bool`), and `CreatedAt` (`DateTime`).
- `JobExecutionSummary` contains `JobId` (`Guid?`; null for a system summary),
  `JobName` (`string?`), `TotalExecutions` (`int`), `SuccessCount` (`int`),
  `FailureCount` (`int`), `TimedOutCount` (`int`), `CancelledCount` (`int`),
  `SuccessRate` (`double`, percentage from 0 to 100), `AverageDurationMs`
  (`long`), `MinDurationMs` (`long`), `MaxDurationMs` (`long`),
  `LastExecutedAt` (`DateTime?`), and `LastStatus` (`ExecutionStatus?`).

### Curl example

```bash
curl --get "http://localhost:5000/api/History/jobs/12345678-1234-1234-1234-123456789012" \
  --data-urlencode "status=Success" \
  --data-urlencode "from=2025-01-01T00:00:00Z" \
  --data-urlencode "to=2025-01-31T23:59:59Z" \
  --data-urlencode "pageNumber=1" \
  --data-urlencode "pageSize=20"
```

## PipelinesController REST API

The `PipelinesController` exposes endpoints for managing job pipelines under the `/api/Pipelines` route.

| Action | Route | Description |
| --- | --- | --- |
| `CreatePipeline` | `POST /api/Pipelines` | Creates a new pipeline from an ordered list of job IDs. |
| `GetPipeline` | `GET /api/Pipelines/{id:guid}` | Returns a specific pipeline by ID with all steps and job details. |
| `ListPipelines` | `GET /api/Pipelines` | Returns all pipelines ordered by creation date (newest first). |
| `DeletePipeline` | `DELETE /api/Pipelines/{id:guid}` | Deletes a pipeline and removes its inter-step dependency edges. |
| `GetPipelineStatus` | `GET /api/Pipelines/{id:guid}/status` | Returns the current execution status of each step in the pipeline. |

### Request Models

#### CreatePipelineRequest
- `Name` (string, required): Human-readable name for the pipeline (max 256 chars).
- `Description` (string, optional): Optional description of the pipeline's purpose.
- `Steps` (List<PipelineStepRequest>, required): Ordered list of job IDs that form the pipeline.

#### PipelineStepRequest
- `JobId` (Guid, required): The job to execute at this step.
- `StopOnFailure` (bool, optional): When true the pipeline stops if this step fails. Defaults to true.

### Response Models

#### PipelineResponse
- `Id` (Guid): The pipeline identifier.
- `Name` (string): Human-readable name for the pipeline.
- `Description` (string): Description of the pipeline's purpose.
- `IsActive` (bool): Whether the pipeline is active.
- `CreatedAt` (DateTime): The date and time the pipeline was created.
- `CreatedBy` (string?): The user who created the pipeline.
- `Steps` (List<PipelineStepResponse>): The steps in the pipeline.

#### PipelineStepResponse
- `StepId` (Guid): The step identifier.
- `JobId` (Guid): The job identifier for this step.
- `JobName` (string?): The name of the job (if available).
- `StepOrder` (int): The order of the step in the pipeline (0-based).
- `StopOnFailure` (bool): Whether the pipeline stops if this step fails.

#### PipelineStatusResponse
- `PipelineId` (Guid): The pipeline identifier.
- `PipelineName` (string): The name of the pipeline.
- `StepStatuses` (List<PipelineStepStatus>): The status of each step in the pipeline.

#### PipelineStepStatus
- `StepOrder` (int): The order of the step in the pipeline.
- `JobId` (Guid): The job identifier for this step.
- `JobName` (string?): The name of the job (if available).
- `Status` (string): The current status of the step (e.g., "Pending", "Running", "Success", "Failed").
- `LastExecutedAt` (DateTime?): The date and time the step was last executed.
- `IsReady` (bool): Whether the step is ready to run (i.e., all previous steps have succeeded).

### Curl Example

Create a pipeline with two steps:

```bash
curl -X POST "http://localhost:5000/api/Pipelines" \
  -H "Content-Type: application/json" \
  -d '{
    "Name": "Data Processing Pipeline",
    "Description": "Processes incoming data and generates a report",
    "Steps": [
      {
        "JobId": "11111111-1111-1111-1111-111111111111",
        "StopOnFailure": true
      },
      {
        "JobId": "22222222-2222-2222-2222-222222222222",
        "StopOnFailure": true
      }
    ]
  }'
```

Note: Replace the JobId values with actual job IDs from your system.

## JobPipelineService

The `JobPipelineService` manages job pipelines — ordered chains of jobs where each step is triggered only after the previous step succeeds. Pipeline step ordering is enforced through `JobDependency` edges in the dependency graph.

## AuditLogger

The `AuditLogger` service provides comprehensive audit logging capabilities for compliance and security purposes. It tracks all important scheduler operations and API calls, storing them in memory with automatic trimming to maintain performance.

### Key Shapes

#### AuditLogEntry
Represents a single audit log entry with the following properties:
- `EventId`: Unique identifier for the audit log entry
- `EventType`: Type of the audited event (e.g., API_CALL, JOB_CREATED)
- `Timestamp`: When the audited event occurred
- `UserId`: Identifier of the user who performed the action
- `EntityId`: Identifier of the entity associated with the audited event
- `EntityType`: Type of the entity associated with the audited event
- `Details`: Additional details about the audited event
- `Severity`: Severity level of the audited event (Debug, Info, Warning, Error, Critical)

#### ApiCallAudit
Represents API call audit data:
- `Method`: HTTP method of the API call
- `Path`: Path/endpoint of the API call
- `StatusCode`: HTTP status code returned by the API call
- `ExecutionTimeMs`: Execution time of the API call in milliseconds
- `UserId`: Identifier of the user who made the API call
- `Timestamp`: When the API call was made

#### AuditStatistics
Provides aggregated audit log statistics:
- `TotalLogs`: Total number of audit logs
- `LogsByEventType`: Count of audit logs grouped by event type
- `LogsBySeverity`: Count of audit logs grouped by severity level
- `OldestLog`: Timestamp of the oldest audit log entry
- `NewestLog`: Timestamp of the newest audit log entry

#### AuditSeverity
Enum defining severity levels:
- `Debug` = 0
- `Info` = 1
- `Warning` = 2
- `Error` = 3
- `Critical` = 4

### Public Methods

- `LogApiCallAsync(ApiCallAudit audit)`: Logs API call with method, endpoint, and status
- `LogJobCreationAsync(Guid jobId, string jobName, string? createdBy)`: Logs job creation event
- `LogJobModificationAsync(Guid jobId, string jobName, string field, object? oldValue, object? newValue, string? modifiedBy)`: Logs job modification event
- `LogJobDeletionAsync(Guid jobId, string jobName, string? deletedBy)`: Logs job deletion event
- `LogSecurityEventAsync(string eventType, string? userId, string message, int severity = 2)`: Logs security-related event
- `LogExecutionEventAsync(Guid jobId, Guid executionId, string eventType, string status, long executionTimeMs)`: Logs execution event (start, complete, fail)
- `GetAuditLogs(DateTime? from = null, DateTime? to = null, string? userId = null, string? eventType = null)`: Retrieves audit logs with optional filtering
- `ClearOldLogsAsync(int daysOld = 90)`: Clears audit logs older than specified days (retention policy)
- `GetStatistics()`: Gets audit log statistics

### Usage Example

```csharp
using System;
using System.Threading.Tasks;
using JobScheduler.Core.Services;

// Assume dependencies are resolved via DI or manually instantiated
var loggerFactory = LoggerFactory.Create(builder => 
    builder.AddConsole()
           .SetMinimumLevel(LogLevel.Information));
var auditLogger = new AuditLogger(loggerFactory.CreateLogger<AuditLogger>());

// Log an API call
await auditLogger.LogApiCallAsync(new ApiCallAudit
{
    Method = "GET",
    Path = "/api/jobs/123",
    StatusCode = 200,
    ExecutionTimeMs = 45,
    UserId = "user-123",
    Timestamp = DateTime.UtcNow
});

// Log job creation
await auditLogger.LogJobCreationAsync(
    jobId: Guid.NewGuid(),
    jobName: "DailyReportJob",
    createdBy: "system");

// Log a security event
await auditLogger.LogSecurityEventAsync(
    eventType: "UNAUTHORIZED_ACCESS",
    userId: "unknown-user",
    message: "Attempted access to restricted endpoint",
    severity: 2); // Warning level

// Retrieve audit logs from the last 24 hours
var recentLogs = auditLogger.GetAuditLogs(
    from: DateTime.UtcNow.AddHours(-24));

// Get audit statistics
var stats = auditLogger.GetStatistics();
Console.WriteLine($"Total audit logs: {stats.TotalLogs}");
```

### Key Features

- **Pipeline Creation**: Creates pipelines from ordered job steps with automatic dependency registration
- **Step Ordering**: Ensures steps execute in sequence through dependency graph management
- **StopOnFailure Semantics**: Individual steps can be configured to halt pipeline execution on failure
- **Status Tracking**: Provides real-time status of each pipeline step based on job execution history
- **Validation**: Automatically validates pipeline dependency graphs to prevent cycles
- **Extension Methods**: Additional convenience methods via `JobPipelineServiceExtensions`

### Core Responsibilities

1. **Creating Pipelines**: The `CreatePipelineAsync` method validates job existence, creates pipeline entities, registers sequential dependencies, and validates the resulting dependency graph
2. **Retrieving Pipelines**: Methods like `GetPipelineAsync` and `GetAllPipelinesAsync` fetch pipelines with their associated steps and job details
3. **Managing Pipelines**: `DeletePipelineAsync` removes pipelines and cleans up their dependency edges
4. **Status Monitoring**: `GetPipelineStatusAsync` examines latest execution records for each step to determine current pipeline state
5. **Dependency Management**: Works with `IJobDependencyService` to maintain proper step ordering through graph edges

### StopOnFailure Behavior

Each pipeline step can be configured with `StopOnFailure = true` (default) or `false`:
- When `true`: Pipeline execution stops if this step fails, preventing subsequent steps from running
- When `false`: Pipeline continues to next step even if this step fails (though the step itself is marked as failed)

### Pipeline Status Response

The service returns a `PipelineStatusResponse` containing:
- Pipeline identification and name
- List of `PipelineStepStatus` objects showing:
  - Step order and associated job details
  - Current execution status (NotStarted, Pending, Running, Success, Failed, etc.)
  - Last execution timestamp
  - Readiness status (whether previous steps succeeded)

### Extension Methods

`JobPipelineServiceExtensions` provides additional functionality:
- `ExistsAsync`: Check if a pipeline exists by ID
- `GetPipelineByNameAsync`: Find pipeline by name (case-insensitive)
- `GetActivePipelinesAsync`: Retrieve only active pipelines
- `GetPipelineStatusWithStatsAsync`: Get status with execution statistics (success/failure counts, average durations)
- `GetDbContext`: Access underlying DbContext for advanced queries

### Usage Example

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Services;

// Assume dependencies are resolved via DI or manually instantiated
var context = new JobSchedulerContext(/* options */);
var dependencyService = new JobDependencyService(context);
var logger = /* logger instance */;
var pipelineService = new JobPipelineService(context, dependencyService, logger);

// Define pipeline steps (must have at least 2 steps)
var steps = new List<CreatePipelineRequest.PipelineStepRequest>
{
    new() { JobId = Guid.Parse("11111111-1111-1111-1111-111111111111"), StopOnFailure = true },
    new() { JobId = Guid.Parse("22222222-2222-2222-2222-222222222222"), StopOnFailure = true },
    new() { JobId = Guid.Parse("33333333-3333-3333-3333-333333333333"), StopOnFailure = false }
};

var request = new CreatePipelineRequest
{
    Name = "Data Processing Pipeline",
    Description = "Ingest, process, and archive daily data",
    Steps = steps
};

// Create the pipeline
var pipeline = await pipelineService.CreatePipelineAsync(request, createdBy: "system");

// Check pipeline status
var status = await pipelineService.GetPipelineStatusAsync(pipeline.Id);
if (status != null)
{
    Console.WriteLine($"Pipeline '{status.PipelineName}' status:");
    foreach (var stepStatus in status.StepStatuses)
    {
        Console.WriteLine($"  Step {stepStatus.StepOrder}: {stepStatus.JobName} - {stepStatus.Status} " +
                         $"(Ready: {stepStatus.IsReady})");
    }
}

// Get extended status with statistics
var stats = await pipelineService.GetPipelineStatusWithStatsAsync(pipeline.Id);
if (stats != null)
{
    Console.WriteLine($"Pipeline execution statistics:");
    foreach (var stepStat in stats.ExecutionStats)
    {
        Console.WriteLine($"  Step {stepStat.StepOrder}: {stepStat.SuccessCount} successes, " +
                         $"{stepStat.FailureCount} failures, avg duration: {stepStat.AverageDuration}");
    }
}
```

## ExecutionsController REST API

The `ExecutionsController` provides access to job execution history, logs, and detailed execution metrics under the `/api/Executions` route.

| Action | Route | Description |
| --- | --- | --- |
| `GetJobExecutions` | `GET /api/Executions/job/{jobId}` | Retrieves paginated execution history for a specific job. |
| `GetExecution` | `GET /api/Executions/{id}` | Retrieves a single execution by ID with complete details. |
| `GetJobStatistics` | `GET /api/Executions/job/{jobId}/stats` | Gets execution statistics for a specific job including success rates and performance metrics. |
| `GetRecentFailures` | `GET /api/Executions/recent-failures` | Retrieves recent failed executions across all jobs for quick failure tracking. |
| `GetJobPerformance` | `GET /api/Executions/job/{jobId}/performance` | Retrieves execution performance analysis including slowest and fastest runs. |
| `CleanupOldExecutions` | `DELETE /api/Executions/cleanup` | Clears old execution records based on retention policy. |

### Request Parameters

#### GetJobExecutions
- `jobId` (Guid, required): The unique identifier of the job.
- `pageNumber` (int, optional, default=1): The page number for pagination.
- `pageSize` (int, optional, default=20): The number of items per page.

#### GetExecution
- `id` (Guid, required): The unique identifier of the execution.

#### GetJobStatistics
- `jobId` (Guid, required): The unique identifier of the job.

#### GetRecentFailures
- `days` (int, optional, default=7): Number of days to look back for failures.
- `limit` (int, optional, default=50): Maximum number of failures to return.

#### GetJobPerformance
- `jobId` (Guid, required): The unique identifier of the job.

#### CleanupOldExecutions
- `olderThanDays` (int, optional, default=90): Delete executions older than this many days.

### Response Types

#### GetJobExecutions
- Returns `200 OK` with `PaginatedResponse<ExecutionResponse>` containing:
  - `Data` (List<ExecutionResponse>): List of execution responses
  - `TotalCount` (int): Total number of executions
  - `PageNumber` (int): Current page number
  - `PageSize` (int): Page size
- Returns `404 Not Found` if job not found
- Returns `500 Internal Server Error` on failure

#### GetExecution
- Returns `200 OK` with `ExecutionDetailsResponse` containing:
  - `Id` (Guid): Execution ID
  - `JobId` (Guid): Job ID
  - `JobName` (string): Name of the job
  - `Status` (string): Execution status
  - `StartedAt` (DateTime?): Start timestamp
  - `CompletedAt` (DateTime?): Completion timestamp
  - `ExecutionTimeMs` (long): Execution time in milliseconds
  - `ErrorMessage` (string?): Error message if failed
  - `RetryAttempt` (int): Current retry attempt
  - `MaxRetries` (int): Maximum retry attempts allowed
  - `Output` (string?): Execution output/logs
- Returns `404 Not Found` if execution not found
- Returns `500 Internal Server Error` on failure

#### GetJobStatistics
- Returns `200 OK` with `ExecutionStatsResponse` containing:
  - `JobId` (Guid): Job ID
  - `TotalExecutions` (int): Total number of executions
  - `SuccessfulExecutions` (int): Number of successful executions
  - `FailedExecutions` (int): Number of failed executions
  - `SuccessRate` (double): Success rate percentage
  - `AverageExecutionTimeMs` (long): Average execution time
  - `MinExecutionTimeMs` (long): Minimum execution time
  - `MaxExecutionTimeMs` (long): Maximum execution time
  - `LastExecutionAt` (DateTime?): Timestamp of last execution
- Returns `404 Not Found` if job not found
- Returns `500 Internal Server Error` on failure

#### GetRecentFailures
- Returns `200 OK` with `List<ExecutionResponse>` containing:
  - `Id` (Guid): Execution ID
  - `JobId` (Guid): Job ID
  - `Status` (string): Execution status
  - `StartedAt` (DateTime?): Start timestamp
  - `CompletedAt` (DateTime?): Completion timestamp
  - `ExecutionTimeMs` (long): Execution time in milliseconds
  - `ErrorMessage` (string?): Error message if failed
  - `RetryAttempt` (int): Retry attempt count
- Returns `500 Internal Server Error` on failure

#### GetJobPerformance
- Returns `200 OK` with `PerformanceAnalysisResponse` containing:
  - `JobId` (Guid): Job ID
  - `AverageExecutionTimeMs` (long): Average execution time
  - `MedianExecutionTimeMs` (long): Median execution time
  - `P95ExecutionTimeMs` (long): 95th percentile execution time
  - `P99ExecutionTimeMs` (long): 99th percentile execution time
  - `SlowestExecutionTimeMs` (long): Slowest execution time
  - `FastestExecutionTimeMs` (long): Fastest execution time
  - `SlowestExecutionAt` (DateTime?): Timestamp of slowest execution
  - `FastestExecutionAt` (DateTime?): Timestamp of fastest execution
- Returns `404 Not Found` if job not found
- Returns `500 Internal Server Error` on failure

#### CleanupOldExecutions
- Returns `200 OK` with `CleanupResponse` containing:
  - `DeletedCount` (int): Number of records deleted
  - `CutoffDate` (DateTime): Date used as cutoff for deletion
  - `Message` (string): Status message
- Returns `500 Internal Server Error` on failure

### Curl Examples

Get paginated executions for a job:

```bash
curl --get "http://localhost:5000/api/Executions/job/12345678-1234-1234-1234-123456789012" \
  --data-urlencode "pageNumber=1" \
  --data-urlencode "pageSize=20"
```

Get a specific execution by ID:

```bash
curl --get "http://localhost:5000/api/Executions/11111111-1111-1111-1111-111111111111"
```

Get job statistics:

```bash
curl --get "http://localhost:5000/api/Executions/job/12345678-1234-1234-1234-123456789012/stats"
```

Get recent failures (last 7 days, limit 50):

```bash
curl --get "http://localhost:5000/api/Executions/recent-failures"
```

Get recent failures with custom parameters:

```bash
curl --get "http://localhost:5000/api/Executions/recent-failures" \
  --data-urlencode "days=30" \
  --data-urlencode "limit=100"
```

Get job performance analysis:

```bash
curl --get "http://localhost:5000/api/Executions/job/12345678-1234-1234-1234-123456789012/performance"
```

Cleanup old executions (older than 90 days):

```bash
curl -X DELETE "http://localhost:5000/api/Executions/cleanup"
```

Cleanup with custom retention period:

```bash
curl -X DELETE "http://localhost:5000/api/Executions/cleanup" \
  --data-urlencode "olderThanDays=30"
```

## JobsController REST API

The `JobsController` manages scheduled jobs under the `/api/Jobs` route.

## Leader election

The job scheduler supports distributed leader election to ensure only one scheduler node fires jobs at each scheduled interval in a multi-instance deployment.

### How it works

The leader election is backed by a database table `SchedulerLeaderLock` (represented by the `SchedulerLeaderLock` entity). 
This table contains a single row (identified by `LockName = 'scheduler-leader'`) that stores:
- `LeaderInstanceId`: the instance ID of the current leader.
- `LeaseExpiresAt`: the UTC time when the current lease expires.
- `AcquiredAt`: the UTC time when the lock was acquired by the current leader.

### Lease and renewal

The `DatabaseLeaderElectionService` implements the `ILeaderElectionService` interface and provides the following behavior:

- When `TryAcquireLeadershipAsync` is called:
  1. If no lock row exists, it creates one with the current instance as the leader and sets the lease to expire in `_leaseDuration` seconds (default 30).
  2. If the lock row exists and the current instance is already the leader, it renews the lease by updating `LeaseExpiresAt`.
  3. If the lock row exists and is held by another instance but the lease has not expired, it returns false (not the leader).
  4. If the lock row exists and the lease has expired, the current instance takes over by updating the row with its instance ID and a new lease expiration.

- The service can be called periodically (e.g., every 15 seconds) to renew the lease. If the service fails to renew the lease within the lease duration, another instance may take over.

- The `ReleaseLeadershipAsync` method expires the lease immediately (by setting `LeaseExpiresAt` to a time in the past) so that another node can take over.

### Configuration

The `DatabaseLeaderElectionService` is configured via its constructor:

- `context`: The `JobSchedulerContext` (EF Core) used to access the lock table.
- `instanceId` (optional): A unique identifier for this scheduler node. Defaults to the machine name.
- `leaseDurationSeconds` (optional): The lease duration in seconds. Defaults to 30.
- `logger` (optional): An `ILogger<DatabaseLeaderElectionService>` for logging.

### Usage example

Here's an example of how to use the leader election service in a background service:

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using JobScheduler.Core.Data;
using JobScheduler.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class LeaderElectionBackgroundService : BackgroundService
{
    private readonly ILeaderElectionService _leaderElectionService;
    private readonly ILogger<LeaderElectionBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(15);

    public LeaderElectionBackgroundService(
        JobSchedulerContext context,
        ILogger<LeaderElectionBackgroundService> logger,
        string? instanceId = null,
        int leaseDurationSeconds = 30)
    {
        _leaderElectionService = new DatabaseLeaderElectionService(
            context,
            instanceId,
            leaseDurationSeconds,
            logger: logger); // Note: we are passing the same logger for the election service, but you can use a different one.

        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Leader election background service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var isLeader = await _leaderElectionService.TryAcquireLeadershipAsync(stoppingToken);
                if (isLeader)
                {
                    _logger.LogInformation("This instance is the leader. Performing leader tasks...");
                    // Perform leader-only tasks here (e.g., schedule jobs, run pending tasks).
                }
                else
                {
                    _logger.LogDebug("This instance is not the leader.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in leader election loop.");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        // Ensure we release leadership on shutdown.
        await _leaderElectionService.ReleaseLeadershipAsync(stoppingToken);
        _logger.LogInformation("Leader election background service stopping.");
    }
}
```

Then, register the service in your host:

```csharp
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        // Add the JobSchedulerContext (replace with your actual connection string)
        services.AddDbContext<JobSchedulerContext>(options =>
            options.UseSqlServer(hostContext.Configuration.GetConnectionString("JobSchedulerDb")));

        // Add the leader election background service
        services.AddHostedService<LeaderElectionBackgroundService>();
    })
    .Build();

await host.RunAsync();
```

Note: The leader election service is designed to be lightweight and can be used in any background service that requires leader election.

## Extension methods

### StringExtensions
| Method | Purpose |
|--------|---------|
| ToSha256 | Computes SHA256 hash of the string for secure hashing |
| Truncate | Truncates string to specified length with optional ellipsis |
| ToSlug | Converts string to slug format (lowercase with hyphens) |
| JsonEscape | Safely encodes string for JSON to prevent injection |
| IsValidGuid | Determines if string is a valid GUID |
| IsValidEmail | Determines if string is a valid email address |
| Repeat | Repeats a string N times |
| Mask | Masks sensitive parts of string (e.g., API keys, passwords) |
| ToList | Converts delimited string to list |
| IsAlphanumericWithUnderscore | Checks if string contains only alphanumeric characters and underscores |

### DateTimeExtensions
| Method | Purpose |
|--------|---------|
| IsInThePast | Checks if a DateTime is in the past |
| IsInTheFuture | Checks if a DateTime is in the future |
| TimeUntil | Gets the time remaining until a DateTime |
| TimeSince | Gets the time elapsed since a DateTime |
| IsSameDay | Checks if two DateTime values are on the same day (UTC) |
| RoundToNearestMinute | Rounds a DateTime to the nearest minute |
| RoundToNearestHour | Rounds a DateTime to the nearest hour |
| StartOfDay | Gets the start of the day (midnight) |
| EndOfDay | Gets the end of the day (23:59:59) |
| StartOfWeek | Gets the start of the week (Monday) |
| StartOfMonth | Gets the start of the month |
| EndOfMonth | Gets the end of the month |

### CollectionExtensions
| Method | Purpose |
|--------|---------|
| Batch | Batches enumerable into chunks of specified size |
| SafeGetAt | Safely gets item at index or returns default if index is out of bounds |
| IsEmpty | Checks if collection is empty without throwing |
| HasItems | Checks if collection is not empty |
| ForEachWhere | Applies action to each item if condition is met |
| Random | Gets specified number of random items from collection |
| Chunk | Groups items into fixed-size groups maintaining order |
| DistinctBy | Groups items into chunks, preserving first occurrence of each key |
| SafeCast | Safely casts collection without throwing on type mismatch |
| TakeWhile | Returns items from start until predicate becomes false |
| ToPage | Converts list to page based on page number and size |

### HttpContextExtensions
| Method | Purpose |
|--------|---------|
| GetUserId | Gets the authenticated user ID from claims |
| GetClaimValue | Gets a specific claim value |
| HasClaim | Checks if user has a specific claim value |
| GetClientIpAddress | Gets the client IP address (handles proxy headers) |
| SetCacheControl | Sets a response header for caching control |
| SetNoCache | Sets a response header to prevent caching |
| SetSecurityHeaders | Sets security headers to prevent common web attacks |
| GetCorrelationId | Gets the request correlation ID (for tracing) |
| GetQueryParameter | Gets a query parameter with type conversion |
| AcceptsJson | Checks if the request accepts JSON response |
| IsHttps | Checks if the request is HTTPS |
| GetRequestScheme | Gets the request scheme (http or https) |
| GetFullRequestUrl | Gets the full request URL |

## Enums and policies

### JobStatus

Represents the status of a scheduled job in the system.

| Member | Value | Description |
|--------|-------|-------------|
| Pending | 0 | Job has been created but not yet scheduled |
| Scheduled | 1 | Job is currently scheduled and waiting for execution |
| Running | 2 | Job is currently executing |
| Completed | 3 | Job execution completed successfully |
| Failed | 4 | Job failed and is awaiting retry |
| Suspended | 5 | Job was suspended by user or system |
| Cancelled | 6 | Job has been cancelled |
| FailedPermanently | 7 | Job failed permanently after all retries exhausted |

**Extension methods:**
- `IsFinal()`: Returns `true` if the status is `Completed`, `Cancelled`, or `FailedPermanently`; otherwise `false`.

### ExecutionStatus

Represents the result status of a single job execution attempt.

| Member | Value | Description |
|--------|-------|-------------|
| Running | 0 | Execution is currently in progress |
| Success | 1 | Execution completed successfully |
| Failed | 2 | Execution failed with an error |
| Cancelled | 3 | Execution was cancelled before completion |
| TimedOut | 4 | Execution timed out |
| Skipped | 5 | Execution was skipped due to concurrency control |

**Extension methods:**
- `IsTerminal()`: Returns `true` if the status is `Success`, `Failed`, `Cancelled`, `TimedOut`, or `Skipped`; otherwise `false`.

### JobPriority

Defines priority levels for job execution in the scheduler queue. Higher values indicate higher priority and execute first.

| Member | Value | Description |
|--------|-------|-------------|
| Low | 0 | Lowest priority - executes last |
| Normal | 1 | Normal priority - default for most jobs |
| High | 2 | High priority - executes before normal priority jobs |
| Critical | 3 | Critical priority - executes before all other jobs |

### MisfirePolicy

Defines the policy for handling misfired jobs (jobs that were scheduled to run but the scheduler was not running at the scheduled time).

| Member | Value | Description |
|--------|-------|-------------|
| FireOnceNow | 0 | Fire the job once immediately when the scheduler restarts after a misfire. This can cause a "thundering herd" problem if many jobs have missed their execution windows. |
| SkipToNext | 1 | Skip the missed execution and schedule the next execution based on the cron expression. This is the safest default for recurring jobs. |
| FireAll | 2 | Fire all missed executions immediately when the scheduler restarts. This can cause performance issues if many executions were missed. |

## Examples

The repository includes various example files demonstrating different aspects of the job scheduler:

- **01-BasicConsoleApp.cs** - Basic Console Application example showing integration into a simple console app
- **02-AspNetCoreIntegration.cs** - ASP.NET Core Integration example with background service for job execution
- **03-RetryAndErrorHandling.cs** - Demonstrates retry strategies and error handling patterns including exponential backoff
- **04-MetricsAndMonitoring.cs** - Shows how to collect, analyze, and report on job execution metrics
- **05-ConcurrencyAndPriority.cs** - Demonstrates concurrency control and job priority execution
- **06-RealWorldScenario.cs** - Real-world business scenario: e-commerce system with daily reporting, inventory sync, and customer notifications
- **07-DataExportAndReporting.cs** - Demonstrates exporting job execution data and generating reports
- **08-MultiDatabaseSupport.cs** - Shows how to work with multiple database connections
- **AdvancedUsage.cs** - Demonstrates custom configuration, priority settings, retry policies, and error handling
- **BasicUsage.cs** - Absolute minimum setup required to use the dotnet-job-scheduler library
- **DailySalesReportJobHandlerJsonExtensions.cs** - JSON serialization extensions for DailySalesReportJobHandler
- **DataExportJobHandlerJsonExtensions.cs** - JSON serialization extensions for DataExportJobHandler
- **IntegrationExample.cs** - Shows integration into ASP.NET Core using HostedService
- **HelloWorldJobHandlerExtensions.cs** - Extension methods for creating and managing hello world jobs
- **EmailSendingJobHandlerExtensions.cs** - Extension methods for creating and managing email sending jobs
- **HelloWorldJobHandlerJsonExtensions.cs** - JSON serialization extensions for HelloWorldJobHandler
- **v2-basic-usage/Program.cs** - Demonstrates basic usage of dotnet-job-scheduler v2.0 features

## Utilities

The `src/JobScheduler.Core/Utilities` namespace contains utility classes that provide common functionality used throughout the job scheduler.

### CryptoUtility

Provides cryptographic operations for securing sensitive data and generating secure tokens.

- `GenerateSecureRandomString(int length = 32)` - Generates a cryptographically secure random string
- `GenerateTimestampedToken()` - Generates a GUID-based token with timestamp embedded
- `ComputeSha256(string input)` - Computes SHA-256 hash of input string
- `ComputeHmacSha256(string message, string secret)` - Computes HMAC-SHA256 for message authentication
- `VerifyHmacSha256(string message, string signature, string secret)` - Verifies HMAC-SHA256 signature
- `EncryptAes256(string plaintext, string key)` - Encrypts string using AES-256-CBC
- `DecryptAes256(string ciphertext, string key, string iv)` - Decrypts AES-256-CBC ciphertext
- `CompareStringsSecurely(string a, string b)` - Performs constant-time string comparison
- `ComputeFileHash(Stream stream)` - Generates cryptographic hash for file integrity verification
- `GeneratePasswordHash(string password, int iterations = 10000)` - Generates a password hash using PBKDF2
- `VerifyPasswordHash(string password, string hash, string salt, int iterations = 10000)` - Verifies password against stored hash

**Usage example:**
```csharp
// Generate a secure random token for job authentication
string token = CryptoUtility.GenerateSecureRandomString();

// Hash sensitive data for storage
string hashedData = CryptoUtility.ComputeSha256("sensitive-job-data");

// Verify webhook signature
bool isValid = CryptoUtility.VerifyHmacSha256(payload, signature, secret);
```

### TimeUtility

Provides consistent time and date operations with proper timezone handling.

- `GetUtcNow()` - Gets current UTC time
- `FromUnixTimestamp(long unixTimestamp)` - Converts Unix timestamp to DateTime
- `ToUnixTimestamp(DateTime dateTime)` - Converts DateTime to Unix timestamp
- `ToIso8601(DateTime dateTime)` - Converts DateTime to ISO 8601 string format
- `ParseIso8601(string? isoString)` - Parses ISO 8601 string to DateTime
- `RoundDown(DateTime dateTime, TimeSpan interval)` - Rounds time down to nearest interval
- `RoundUp(DateTime dateTime, TimeSpan interval)` - Rounds time up to nearest interval
- `GetAge(DateTime birthDate, DateTime? referenceDate = null)` - Gets age in specified unit
- `IsBetweenTimes(DateTime time, TimeSpan startTime, TimeSpan endTime)` - Checks if a time falls within a time range
- `IsBetweenDates(DateTime date, DateTime startDate, DateTime endDate)` - Checks if a date falls within a date range
- `GetBusinessDaysBetween(DateTime startDate, DateTime endDate)` - Gets the number of business days between two dates
- `GetNextBusinessDay(DateTime date)` - Gets the next business day
- `GetPreviousBusinessDay(DateTime date)` - Gets the previous business day
- `GetStartOfWeek(DateTime date)` - Gets the start of the week (Monday)
- `GetEndOfWeek(DateTime date)` - Gets the end of the week (Sunday)
- `GetStartOfMonth(DateTime date)` - Gets the start of the month
- `GetEndOfMonth(DateTime date)` - Gets the end of the month
- `FormatDuration(TimeSpan duration)` - Formats duration in human-readable format
- `IsLeapYear(int year)` - Checks if a year is a leap year

**Usage example:**
```csharp
// Convert Unix timestamp to DateTime
DateTime time = TimeUtility.FromUnixTimestamp(1640995200);

// Format a duration for display
string durationText = TimeUtility.FormatDuration(TimeSpan.FromSeconds(90)); // "1 minute 30 seconds"

// Check if current time is within business hours
bool isWorkHours = TimeUtility.IsBetweenTimes(DateTime.UtcNow, new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0));
```

### ParseUtility

Provides safe parsing and type conversion operations with consistent error handling.

- `ParseInt(string? value, int defaultValue = 0)` - Safely parses integer from string
- `ParseLong(string? value, long defaultValue = 0)` - Safely parses long from string
- `ParseDouble(string? value, double defaultValue = 0.0)` - Safely parses double from string
- `ParseBool(string? value, bool defaultValue = false)` - Safely parses boolean from string
- `ParseDateTime(string? value, DateTime? defaultValue = null)` - Safely parses DateTime from string
- `ParseGuid(string? value)` - Safely parses Guid from string
- `ParseEnum<T>(string? value, T defaultValue)` - Safely parses enum from string
- `ParseJson<T>(string? json)` - Safely parses JSON string into typed object
- `ParseTimeSpan(string? value, TimeSpan? defaultValue = null)` - Safely parses TimeSpan from string
- `ParsePriority(string? priority)` - Parses priority string to numeric priority level (1-4)
- `FormatFileSize(long bytes)` - Formats bytes as human-readable file size
- `FormatDuration(long milliseconds)` - Formats milliseconds as human-readable duration
- `FormatPercentage(double value, int decimals = 2)` - Formats percentage value
- `ParseCsvLine(string line)` - Parses CSV line handling quoted fields with commas
- `EscapeCsvField(string field)` - Escapes CSV field for safe writing

**Usage example:**
```csharp
// Safely parse configuration values
int retryCount = ParseUtility.ParseInt(config["MaxRetries"], 3);
bool isEnabled = ParseUtility.ParseBool(config["IsEnabled"], false);
DateTime startTime = ParseUtility.ParseDateTime(config["StartTime"]);

// Parse CSV data
List<string> columns = ParseUtility.ParseCsvLine("value1,\"value, with comma\",value3");

// Format file size for display
string sizeText = ParseUtility.FormatFileSize(1024 * 1024 * 5); // "5.00 MB"
```

### ValidationUtility

Provides centralized validation logic for job scheduler operations.

- `ValidateJobName(string? name)` - Validates job name format and constraints
- `ValidateCronExpression(string? expression)` - Validates cron expression format
- `ValidateHandlerType(string? handlerType)` - Validates handler type format
- `ValidateJobConfiguration(Job job)` - Validates job configuration parameters
- `ValidateJsonParameters(string? jsonParams)` - Validates JSON parameter string format
- `ValidatePagination(int pageNumber, int pageSize)` - Validates page number and size parameters
- `ValidateRetryStrategy(string? strategy)` - Validates retry backoff strategy type

**Usage example:**
```csharp
// Validate job name before creation
ValidationResult nameResult = ValidationUtility.ValidateJobName("my-job_123");
if (!nameResult.IsValid)
{
    Console.WriteLine($"Invalid job name: {nameResult.Message}");
}

// Validate cron expression
ValidationResult cronResult = ValidationUtility.ValidateCronExpression("0 0 * * *");
if (cronResult.IsValid)
{
    // Proceed with job creation
}

// Validate job configuration
ValidationResult configResult = ValidationUtility.ValidateJobConfiguration(job);
if (!configResult.IsValid)
{
    throw new JobValidationException(configResult.Message);
}
```

### JobHelper

Provides helper methods for job operations and data handling.

- `SerializeParameters(object? parameters)` - Serializes job handler parameters to JSON
- `DeserializeParameters<T>(string? jsonParameters)` - Deserializes job handler parameters from JSON
- `GetJobStatusDescription(Job job)` - Gets a human-readable status description for a job
- `IsValidHandlerType(string? handlerType)` - Validates handler type format
- `GetExecutionFrequencyDescription(string cronExpression)` - Gets execution frequency description based on cron expression
- `CalculateReliabilityScore(Job job)` - Calculates the job reliability score (0-100) based on execution history
- `GetRecommendedAction(Job job)` - Gets recommended action based on job's current state
- `FormatDuration(long milliseconds)` - Formats duration in milliseconds to human-readable format
- `IsConcerning(Job job)` - Determines if a job should be marked as having concerning behavior

**Usage example:**
```csharp
// Get human-readable job status
string statusDesc = JobHelper.GetJobStatusDescription(job);
Console.WriteLine($"Job status: {statusDesc}");

// Get reliability score for monitoring
int reliabilityScore = JobHelper.CalculateReliabilityScore(job);
if (reliabilityScore < 50)
{
    Console.WriteLine($"Job reliability is low: {reliabilityScore}%");
}

// Get recommended action based on job state
string recommendation = JobHelper.GetRecommendedAction(job);
Console.WriteLine($"Recommendation: {recommendation}");

// Serialize job parameters for storage
string jsonParams = JobHelper.SerializeParameters(new { ReportType = "Summary", Format = "PDF" });
```

## Benchmarks

The `benchmarks/dotnet-job-scheduler.Benchmarks` project contains performance benchmarks for core scheduler components using BenchmarkDotNet. To run the benchmarks:

```bash
dotnet run -c Release
```

### Benchmark Classes

- **CacheServiceBenchmarks** - Measures in-memory caching operations for cron expressions, job metadata, performance metrics, and distributed lock leases
- **ConcurrencyManagerBenchmarks** - Measures global and per-job concurrency limit enforcement, execution slot acquisition/release
- **CronExpressionBenchmarks** - Measures cron expression parsing, schedule evaluation, and next execution time calculation throughput
- **CsvProcessingBenchmarks** - Measures CSV parsing and escaping used by export formatters and audit log serializers
- **JobExecutorServiceBenchmarks** - Measures actual job execution handling including timeout, error handling, retry logic, and metrics collection
- **JobManagementBenchmarks** - Measures job management operations: slug generation, JSON escaping, truncation, and credential masking
- **JobPipelineServiceBenchmarks** - Measures job pipeline operations: creation, validation, execution flow control, dependency resolution, and status tracking
- **JobSchedulerServiceBenchmarks** - Measures core scheduler operations: job creation/validation, schedule evaluation, and bulk job processing
- **RetryServiceBenchmarks** - Measures retry logic operations: delay calculation (exponential/linear/fixed backoff), policy validation, and attempt tracking
- **StringProcessingBenchmarks** - Measures string manipulation: slug generation, JSON escaping, truncation, and credential masking
- **JobDependencyBenchmarks** - Measures job dependency graph operations: adding/removing dependencies, topological ordering, and cycle detection
- **JobPipelineBenchmarks** - Measures job pipeline execution and status tracking performance
- **Exception Benchmarks** (JobSchedulerExceptionBenchmarks, CyclicDependencyExceptionBenchmarks) - Measures exception creation and ToString() performance

## Running tests

To run the tests, use the `dotnet test` command.

### Test projects

1. **JobScheduler.Core.Tests** (located in `tests/JobScheduler.Core.Tests`)
   - Contains unit tests for the core job scheduler functionality.
   - Run all tests: `dotnet test tests/JobScheduler.Core.Tests/JobScheduler.Core.Tests.csproj`
   - Run tests for a specific class: `dotnet test tests/JobScheduler.Core.Tests/JobScheduler.Core.Tests.csproj --filter "FullyQualifiedName~<ClassName>"`

2. **dotnet-job-scheduler.Tests** (located in `tests/dotnet-job-scheduler.Tests`)
   - Contains integration and end-to-end tests for the job scheduler application.
   - Run all tests: `dotnet test tests/dotnet-job-scheduler.Tests/dotnet-job-scheduler.Tests.csproj`
   - Run tests for a specific class: `dotnet test tests/dotnet-job-scheduler.Tests/dotnet-job-scheduler.Tests.csproj --filter "FullyQualifiedName~<ClassName>"`

Note: The third test project `src/JobScheduler.Core.Tests` does not exist in this repository.

## Performance monitoring

The job scheduler includes built-in performance monitoring capabilities through the `PerformanceMonitor` and `ExecutionStatisticsService` services. These services collect, analyze, and report on job execution metrics to help identify bottlenecks, track performance trends, and detect anomalies.

### PerformanceMonitor

The `PerformanceMonitor` service (`src/JobScheduler.Core/Services/PerformanceMonitor.cs`) provides real-time monitoring of scheduler performance metrics including execution times, throughput, and resource usage.

#### Key features

- **Execution time tracking**: Records execution time for individual jobs with success/failure status
- **Metrics aggregation**: Computes average, minimum, and maximum execution times
- **Throughput measurement**: Calculates executions per minute for the scheduler
- **Success rate calculation**: Tracks percentage of successful executions
- **Percentile analysis**: Supports percentile calculations (P95, P99) for SLA monitoring
- **Resource monitoring**: Provides CPU utilization and memory usage measurements
- **Timeline data**: Generates hourly aggregated performance data for dashboard visualization
- **Metrics summary**: Provides comprehensive statistics summary including total executions, success rates, and resource usage

#### Core classes

- `PerformanceMonitor`: Main service for recording and retrieving performance metrics
- `PerformanceMetric`: Individual performance metric record containing job ID, name, execution time, success status, and timestamp
- `MetricsSummary`: Summary statistics including total executions, successful/failed executions, average/min/max execution times, and memory usage
- `PerformanceTimelinePoint`: Hourly aggregated data point showing execution counts, success/failure counts, and average execution time

#### Usage example

```csharp
using JobScheduler.Core.Services;
using Microsoft.Extensions.Logging;

// Assume logger is available from dependency injection
var performanceMonitor = new PerformanceMonitor(logger);

// Record a job execution
performanceMonitor.RecordExecutionTime(
    jobId: Guid.NewGuid(),
    jobName: "ReportGenerationJob",
    elapsedMs: 1542,
    success: true);

// Get average execution time for a specific job
var avgTime = performanceMonitor.GetAverageExecutionTime(jobId);

// Get overall scheduler throughput (executions per minute)
var throughput = performanceMonitor.GetThroughputPerMinute();

// Get success rate percentage
var successRate = performanceMonitor.GetSuccessRate();

// Get P95 execution time for SLA monitoring
var p95Time = performanceMonitor.GetPercentileExecutionTime(jobId, 95);

// Get comprehensive metrics summary
var summary = performanceMonitor.GetSummary();
Console.WriteLine($"Total executions: {summary.TotalExecutions}");
Console.WriteLine($"Average execution time: {summary.AverageExecutionTimeMs}ms");
Console.WriteLine($"Success rate: {summary.SuccessRate:F2}%");
```

### ExecutionStatisticsService

The `ExecutionStatisticsService` (`src/JobScheduler.Core/Services/ExecutionStatisticsService.cs`) computes advanced execution statistics and analytics for jobs, providing detailed performance analysis including percentiles, trends, and anomaly detection.

#### Key features

- **Comprehensive job statistics**: Success rates, execution time metrics, and last execution timestamp
- **Performance analysis**: Percentile-based analysis (median, P95, P99) with slowest/fastest execution timestamps
- **Trend analysis**: Daily performance trends showing execution counts, average execution times, and success rates over time
- **Anomaly detection**: Identifies anomalous execution times using standard deviation (executions >2σ from mean)
- **Historical analysis**: Works with historical execution data from the repository

#### Core classes

- `ExecutionStatsResponse`: Basic job statistics including total executions, successful/failed executions, success rate, average/min/max execution times, and last execution timestamp
- `PerformanceAnalysisResponse`: Detailed performance metrics including average, median, P95, P99 execution times, slowest/fastest execution times and timestamps
- `PerformanceTrendPoint`: Daily trend data point showing date, execution count, average execution time, success rate, and maximum execution time
- `ExecutionAnomalyReport`: Anomaly detection result showing execution ID, timestamp, actual/expected execution times, deviation factor, and anomaly type (SlowExecution/FastExecution)

#### Usage example

```csharp
using JobScheduler.Core.Services;
using Microsoft.Extensions.Logging;

// Assume repositories are available from dependency injection
var executionStatisticsService = new ExecutionStatisticsService(
    executionRepository,
    jobRepository,
    logger);

// Get comprehensive statistics for a job
var stats = await executionStatisticsService.GetJobExecutionStatsAsync(jobId);
if (stats != null)
{
    Console.WriteLine($"Job {stats.JobId} statistics:");
    Console.WriteLine($"  Total executions: {stats.TotalExecutions}");
    Console.WriteLine($"  Success rate: {stats.SuccessRate:F2}%");
    Console.WriteLine($"  Average execution time: {stats.AverageExecutionTimeMs}ms");
}

// Get detailed performance analysis including percentiles
var analysis = await executionStatisticsService.GetJobPerformanceAnalysisAsync(jobId);
if (analysis != null)
{
    Console.WriteLine($"Performance analysis for job {analysis.JobId}:");
    Console.WriteLine($"  Average execution time: {analysis.AverageExecutionTimeMs}ms");
    Console.WriteLine($"  Median execution time: {analysis.MedianExecutionTimeMs}ms");
    Console.WriteLine($"  P95 execution time: {analysis.P95ExecutionTimeMs}ms");
    Console.WriteLine($"  P99 execution time: {analysis.P99ExecutionTimeMs}ms");
    Console.WriteLine($"  Slowest execution: {analysis.SlowestExecutionTimeMs}ms at {analysis.SlowestExecutionAt}");
    Console.WriteLine($"  Fastest execution: {analysis.FastestExecutionTimeMs}ms at {analysis.FastestExecutionAt}");
}

// Get performance trend over the last 7 days
var trend = await executionStatisticsService.GetPerformanceTrendAsync(jobId, days: 7);
foreach (var point in trend)
{
    Console.WriteLine($"{point.Date:yyyy-MM-dd}: {point.ExecutionCount} executions, " +
                     $"avg {point.AverageExecutionTimeMs}ms, success rate {point.SuccessRate:F1}%");
}

// Detect execution anomalies
var anomalies = await executionStatisticsService.DetectExecutionAnomaliesAsync(jobId);
foreach (var anomaly in anomalies)
{
    Console.WriteLine($"{anomaly.AnomalyType} on {anomaly.Timestamp:yyyy-MM-dd HH:mm:ss}: " +
                     $"{anomaly.ExecutionTimeMs}ms (expected {anomaly.ExpectedTimeMs}ms, " +
                     $"deviation factor {anomaly.DeviationFactor:F2})");
}
```

### Integration with controllers

Performance monitoring data is exposed through REST API endpoints:

- **DashboardController**: 
  - `GET /api/Dashboard/performance-timeline?hours={hours}` - Returns hourly performance timeline data
  - `GET /api/Dashboard/overview` - Includes average execution time and success rate in overview

- **ExecutionsController**:
  - `GET /api/Executions/job/{jobId}/stats` - Returns execution statistics for a specific job
  - `GET /api/Executions/job/{jobId}/performance` - Returns performance analysis including percentiles

- **HistoryController**:
  - `GET /api/History/jobs/{jobId}/summary` - Returns job execution summary with success rates and average duration

These services work together to provide comprehensive performance monitoring capabilities, enabling operators to track job scheduler health, identify performance bottlenecks, and make data-driven decisions for optimization and capacity planning.
| `CreateJob` | `POST /api/Jobs` | JSON `CreateJobRequest` body | `201 Created`, `400 Bad Request` |
| `GetJob` | `GET /api/Jobs/{id}` | `id` (Guid) | `200 OK`, `404 Not Found` |
| `ListJobs` | `GET /api/Jobs` | Optional query parameters: `status`, `pageNumber` (default `1`), and `pageSize` (default `10`) | `200 OK` |
| `UpdateJob` | `PUT /api/Jobs/{id}` | `id` (Guid) and JSON `CreateJobRequest` body | `200 OK`, `400 Bad Request`, `404 Not Found` |
| `DeleteJob` | `DELETE /api/Jobs/{id}` | `id` (Guid) | `204 No Content`, `404 Not Found` |
| `SuspendJob` | `POST /api/Jobs/{id}/suspend` | `id` (Guid) and optional JSON `SuspendJobRequest` body containing `reason` | `200 OK`, `404 Not Found` |
| `ResumeJob` | `POST /api/Jobs/{id}/resume` | `id` (Guid) | `200 OK`, `404 Not Found` |
| `TriggerJobExecution` | `POST /api/Jobs/{id}/execute` | `id` (Guid) | `200 OK`, `404 Not Found`, `409 Conflict` |
| `GetJobExecutionHistory` | `GET /api/Jobs/{id}/history` | `id` (Guid) and optional `limit` query parameter (default `20`) | `200 OK`, `404 Not Found` |

Create a job with a `CreateJobRequest` body:

```bash
curl -X POST "http://localhost:5000/api/Jobs" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Nightly report",
    "description": "Generate the nightly reporting dataset",
    "cronExpression": "0 2 * * *",
    "timeZoneId": "UTC",
    "handlerType": "JobScheduler.Handlers.NightlyReportHandler",
    "handlerParameters": "{\"reportType\":\"summary\"}",
    "priority": 1,
    "maxConcurrentExecutions": 1,
    "maxRetries": 3,
    "retryBackoffSeconds": 60,
    "executionTimeoutSeconds": 300,
    "isActive": true
  }'
```
