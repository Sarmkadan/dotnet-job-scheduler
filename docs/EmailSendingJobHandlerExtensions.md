# EmailSendingJobHandlerExtensions

Provides extension methods for creating, querying, and validating email-sending jobs within the job scheduler. This static class centralizes the configuration and lifecycle management of email dispatch jobs, supporting both single and batch creation, daily recurrence, and lookup operations against the active job store.

## API

### CreateEmailSendingJobAsync

```csharp
public static async Task<Job> CreateEmailSendingJobAsync(
    this IJobScheduler scheduler,
    EmailJobConfiguration configuration,
    CancellationToken cancellationToken = default)
```

Creates and schedules a single email-sending job based on the supplied configuration. The job is persisted immediately and becomes eligible for execution according to its trigger schedule.

**Parameters**
- `scheduler` — The scheduler instance this method extends.
- `configuration` — An `EmailJobConfiguration` object specifying recipients, subject, body template, SMTP settings, and scheduling trigger.
- `cancellationToken` — A token that can cancel the asynchronous operation.

**Returns**
The newly created `Job` entity with its assigned identifier and initial state.

**Exceptions**
- `ArgumentNullException` — `configuration` is `null`.
- `InvalidOperationException` — the scheduler has been disposed or is not accepting new jobs.
- `EmailConfigurationException` — the SMTP settings in the configuration fail pre-validation.

---

### `CreateEmailSendingJobsBatchAsync`

```csharp
public static async Task<IReadOnlyList<Job>> CreateEmailSendingJobsBatchAsync(
    this IJobScheduler scheduler,
    IEnumerable<EmailJobConfiguration> configurations,
    CancellationToken cancellationToken = default)
```

Creates multiple email-sending jobs in a single atomic batch. All jobs are validated before any are persisted; if one configuration is invalid, none are created.

- **Parameters**
  - `scheduler` — the target scheduler.
  - `configurations` — a collection of `EmailJobConfiguration` objects.
  - `cancellationToken` — a cancellation token.

- **Returns**
  A read-only list of the created `Job` entities in the same order as the input configurations.

- **Exceptions**
  - `ArgumentNullException` — `configurations` is null.
  - `ArgumentException` — `configurations` is empty.
  - `InvalidOperationException` — the scheduler is disposed or not accepting jobs.
  - `EmailJobConfigurationException` — any configuration fails pre-validation.

---

### `GetActiveEmailSendingJobsAsync`

```csharp
public static async Task<IReadOnlyList<Job>> GetActiveEmailSendingJobsAsync(
    this IJobScheduler scheduler,
    CancellationToken cancellationToken = default)
```

Retrieves all email-sending jobs that are currently in an active state (e.g., `Scheduled`, `Running`, or `Retrying`). Jobs that are completed, failed permanently, or deleted are excluded.

- **Parameters**
  - `scheduler` — the scheduler to query.
  - `cancellationToken` — optional cancellation token.

- **Returns**
  A read-only list of active email-sending `Job` instances. Returns an empty list if no active jobs exist.

- **Exceptions**
  - `InvalidOperationException` — the scheduler is disposed.

---

### `FindEmailSendingJobsByNameAsync`

```csharp
public static async Task<IReadOnlyList<Job>> FindEmailSendingJobsByNameAsync(
    this IJobScheduler scheduler,
    string jobName,
    CancellationToken cancellationToken = default)
```

Searches for email-sending jobs whose name matches the given string. The match is case-insensitive and may return multiple jobs if names are reused across different scopes.

- **Parameters**
  - `scheduler` — the scheduler to search.
  - `jobName` — the name to match against.
  - `cancellationToken` — optional cancellation token.

- **Returns**
  A read-only list of matching `Job` entities. Returns an empty list if no matches are found.

- **Exceptions**
  - `ArgumentNullException` — `jobName` is null.
  - `ArgumentException` — `jobName` is empty or whitespace.
  - `InvalidOperationException` — the scheduler is disposed.

---

### `ValidateEmailJobConfiguration`

```csharp
public static bool ValidateEmailJobConfiguration(
    EmailJobConfiguration config,
    out IReadOnlyList<string> validationErrors)
```

Validates an email job configuration without creating a job. Checks SMTP connectivity, recipient address format, template syntax, and scheduling rule validity.

- **Parameters**
  - `config` — the configuration to validate.
  - `validationErrors` — on return, contains a list of human-readable error messages if validation fails; otherwise an empty list.

- **Returns**
  `true` if the configuration passes all validation checks; `false` otherwise.

- **Exceptions**
  - `ArgumentNullException` — `config` is null.

---

### `GetNextExecutionTime`

```csharp
public static string GetNextExecutionTime(
    EmailJobConfiguration config)
```

Computes the next execution time for a given email job configuration based on its scheduling trigger and returns it as an ISO 8601 string in UTC.

- **Parameters**
  - `config` — the email job configuration.

- **Returns**
  A string representation of the next UTC execution time, or `"Never"` if the trigger is disabled or has no future occurrences.

- **Exceptions**
  - `ArgumentNullException` — `config` is null.
  - `InvalidOperationException` — the trigger expression is malformed and cannot be evaluated.

---

### `CreateDailyEmailSendingJobAsync`

```csharp
public static async Task<Job> CreateDailyEmailSendingJobAsync(
    this IJobScheduler scheduler,
    EmailJobConfiguration config,
    TimeSpan timeOfDay,
    CancellationToken cancellationToken = default)
```

Convenience method that creates an email-sending job configured to execute daily at a specific time of day. The method internally adjusts the configuration’s trigger to a daily schedule before persisting.

- **Parameters**
  - `scheduler` — the target scheduler.
  - `config` — the base email configuration (recipients, subject, body, SMTP settings).
  - `timeOfDay` — the time of day (UTC) at which the job should execute each day.
  - `cancellationToken` — optional cancellation token.

- **Returns**
  The newly created `Job` with a daily schedule.

- **Exceptions**
  - `ArgumentNullException` — `config` is null.
  - `ArgumentOutOfRangeException` — `timeOfDay` is negative or exceeds 24 hours.
  - `InvalidOperationException` — the scheduler is disposed or not accepting new jobs.
  - `DuplicateEmailJobException` — a job with the same name and daily schedule already exists.

## Usage

### Example 1: Creating and validating a single email job

```csharp
var scheduler = serviceProvider.GetRequiredService<IJobScheduler>();

var config = new EmailJobConfiguration
{
    JobName = "Weekly Report",
    Recipients = new[] { "team@example.com" },
    Subject = "Weekly Summary",
    BodyTemplate = "<h1>Report</h1><p>Attached.</p>",
    SmtpServer = "smtp.example.com",
    SmtpPort = 587,
    Trigger = new CronTrigger("0 8 * * 1") // Every Monday at 08:00 UTC
};

// Validate before scheduling
if (!EmailSendingJobHandlerExtensions.ValidateEmailJobConfiguration(config, out var errors))
{
    foreach (var error in errors)
    {
        Console.WriteLine($"Validation error: {error}");
    }
    return;
}

var job = await scheduler.CreateEmailSendingJobAsync(config);
Console.WriteLine($"Job created with ID: {job.Id}");
Console.WriteLine($"Next execution: {EmailSendingJobHandlerExtensions.GetNextExecutionTime(config)}");
```

### Example 2: Batch creation and querying active jobs

```csharp
var scheduler = serviceProvider.GetRequiredService<IJobScheduler>();

var configs = new List<EmailJobConfiguration>
{
    new EmailJobConfiguration
    {
        JobName = "Morning Digest",
        Recipients = new[] { "alerts@example.com" },
        Subject = "Morning Digest",
        BodyTemplate = "Good morning!",
        SmtpServer = "smtp.example.com",
        SmtpPort = 587,
        Trigger = new CronTrigger("0 7 * * *")
    },
    new EmailJobConfiguration
    {
        JobName = "Evening Summary",
        Recipients = new[] { "alerts@example.com" },
        Subject = "Evening Summary",
        BodyTemplate = "Good evening!",
        SmtpServer = "smtp.example.com",
        SmtpPort = 587,
        Trigger = new CronTrigger("0 19 * * *")
    }
};

// Batch creation
IReadOnlyList<Job> createdJobs = await scheduler.CreateEmailSendingJobsBatchAsync(configs);
Console.WriteLine($"Created {createdJobs.Count} jobs.");

// Retrieve all active email jobs
IReadOnlyList<Job> activeJobs = await scheduler.GetActiveEmailSendingJobsAsync();
Console.WriteLine($"Active email jobs: {activeJobs.Count}");

// Find a specific job by name
IReadOnlyList<Job> morningJobs = await scheduler.FindEmailSendingJobsByNameAsync("Morning Digest");
foreach (var job in morningJobs)
{
    Console.WriteLine($"Found: {job.Id} — State: {job.State}");
}
```

## Notes

- All `async` methods internally use the scheduler's underlying storage and may be subject to the same concurrency guarantees provided by the `IJobScheduler` implementation. In multi-threaded scenarios, callers should avoid concurrent modifications to the same logical job without external coordination.
- `ValidateEmailJobConfiguration` is a pure, synchronous method and does not interact with the scheduler or its storage. It is safe to call from any thread without side effects.
- `GetNextExecutionTime` relies on the trigger expression being syntactically valid. If the trigger was constructed from user input, validate it with `ValidateEmailJobConfiguration` first to avoid runtime exceptions.
- `CreateEmailSendingJobsBatchAsync` provides atomicity: if any configuration in the batch fails validation, no jobs are created. This prevents partial creation scenarios that would require manual cleanup.
- `FindEmailSendingJobsByNameAsync` performs a case-insensitive search. If job names are not unique within the system, the returned list may contain multiple entries. Use additional filtering on job metadata if uniqueness is required.
- `CreateDailyEmailSendingJobAsync` overwrites any existing trigger on the supplied configuration with a daily schedule. The original trigger value is ignored.
- All methods that accept a `CancellationToken` will throw `OperationCanceledException` if the token is signaled before the operation completes. The state of the scheduler remains consistent in such cases; no partial job creation occurs.
