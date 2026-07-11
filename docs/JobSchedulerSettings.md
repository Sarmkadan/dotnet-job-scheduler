# JobSchedulerSettings

`JobSchedulerSettings` is a configuration class that defines all tunable parameters for a job scheduler instance. It is used to control database connectivity, concurrency limits, timeouts, retry behavior, queue polling, cleanup policies, naming constraints, and optional webhook, Slack, and email notification channels. All properties have sensible defaults except where noted; the class is designed to be populated via dependency injection or manual instantiation before being passed to the scheduler constructor.

## API

- **`ConnectionString`** (`string?`)  
  The database connection string used by the scheduler to persist job metadata. If `null`, the scheduler may fall back to a default or throw at initialization depending on the provider implementation.

- **`MaxConcurrentJobs`** (`int`)  
  Maximum number of jobs that can execute simultaneously. A value of `0` or less is typically treated as unlimited, but the scheduler may cap this value internally.

- **`DefaultTimeoutSeconds`** (`int`)  
  Default execution timeout in seconds for jobs that do not specify their own timeout. If a job exceeds this duration it is considered failed and may be retried. Must be greater than zero.

- **`DefaultMaxRetries`** (`int`)  
  Number of automatic retry attempts for a failed job before it is permanently marked as failed. A value of `0` means no retries.

- **`DefaultRetryBackoffSeconds`** (`int`)  
  Base delay in seconds between retry attempts. The actual delay may incorporate jitter or exponential backoff depending on the scheduler implementation.

- **`QueuePollIntervalMs`** (`int`)  
  Interval in milliseconds at which the scheduler polls the job queue for new work. Lower values reduce latency but increase database load.

- **`EnableCleanup`** (`bool`)  
  When `true`, the scheduler runs a background cleanup routine that removes old or completed job records according to `CleanupIntervalMs`.

- **`CleanupIntervalMs`** (`int`)  
  Interval in milliseconds between cleanup cycles. Only relevant when `EnableCleanup` is `true`.

- **`MaxJobNameLength`** (`int`)  
  Maximum allowed length (in characters) for a job name. Names exceeding this limit are truncated or rejected at registration time.

- **`MaxCronExpressionLength`** (`int`)  
  Maximum allowed length (in characters) for a cron expression. Expressions longer than this value are rejected.

- **`EnableWebhooks`** (`bool`)  
  Enables or disables the webhook notification channel. When `true`, the scheduler sends HTTP callbacks to the URL specified in `SlackWebhookUrl` (if set) or to a generic webhook endpoint.

- **`EnableSlack`** (`bool`)  
  Enables or disables Slack notifications. When `true`, the scheduler posts messages to the Slack channel configured via `SlackWebhookUrl`.

- **`EnableEmail`** (`bool`)  
  Enables or disables email notifications. When `true`, the scheduler sends emails using the SMTP settings defined below.

- **`SlackWebhookUrl`** (`string?`)  
  The Slack incoming webhook URL. Required if `EnableSlack` is `true`; ignored otherwise.

- **`SmtpServer`** (`string?`)  
  Hostname or IP address of the SMTP server. Required if `EnableEmail` is `true`.

- **`SmtpPort`** (`int`)  
  Port number for the SMTP server. Common values are `25`, `587`, or `465`.

- **`SmtpUsername`** (`string?`)  
  Username for SMTP authentication. May be `null` if the server does not require authentication.

- **`SmtpPassword`** (`string?`)  
  Password for SMTP authentication. Stored in plain text; consider using secure configuration sources.

- **`SmtpFromEmail`** (`string?`)  
  The sender email address used in outgoing notifications. Required if `EnableEmail` is `true`.

- **`AlertEmails`** (`List<string>`)  
  A list of recipient email addresses for job failure alerts. Can be empty; if non-empty and `EnableEmail` is `true`, each address receives a notification on job failure.

## Usage

### Example 1: Basic configuration with retry and Slack notifications

```csharp
var settings = new JobSchedulerSettings
{
    ConnectionString = "Server=localhost;Database=Jobs;Trusted_Connection=True;",
    MaxConcurrentJobs = 5,
    DefaultTimeoutSeconds = 300,
    DefaultMaxRetries = 3,
    DefaultRetryBackoffSeconds = 10,
    QueuePollIntervalMs = 1000,
    EnableCleanup = true,
    CleanupIntervalMs = 3600000,
    MaxJobNameLength = 128,
    MaxCronExpressionLength = 200,
    EnableSlack = true,
    SlackWebhookUrl = "https://hooks.slack.com/services/T00/B00/xxxx",
    EnableEmail = false,
    AlertEmails = new List<string>()
};

var scheduler = new JobScheduler(settings);
```

### Example 2: Minimal configuration with email alerts

```csharp
var settings = new JobSchedulerSettings
{
    ConnectionString = "Server=prod-db;Database=JobScheduler;User Id=admin;Password=secret;",
    MaxConcurrentJobs = 10,
    DefaultTimeoutSeconds = 60,
    DefaultMaxRetries = 2,
    DefaultRetryBackoffSeconds = 30,
    QueuePollIntervalMs = 500,
    EnableCleanup = false,
    EnableEmail = true,
    SmtpServer = "smtp.example.com",
    SmtpPort = 587,
    SmtpUsername = "alerts@example.com",
    SmtpPassword = "smtp-pass",
    SmtpFromEmail = "scheduler@example.com",
    AlertEmails = new List<string> { "ops@example.com", "dev@example.com" }
};

var scheduler = new JobScheduler(settings);
```

## Notes

- **Thread safety**: `JobSchedulerSettings` is a plain data container with no synchronization. It is safe to read from multiple threads after initialization, but concurrent writes are not safe. The scheduler typically reads these values once at startup and does not modify them afterwards.
- **Null vs. empty strings**: Properties like `ConnectionString`, `SlackWebhookUrl`, `SmtpServer`, `SmtpUsername`, `SmtpPassword`, and `SmtpFromEmail` are nullable. Passing `null` when the corresponding feature is enabled (e.g., `EnableSlack = true` with `SlackWebhookUrl = null`) will cause the scheduler to throw an `InvalidOperationException` at initialization.
- **`AlertEmails`**: This list is never null (it is initialized as an empty list by the default constructor). Adding or removing items after the scheduler has started has no effect because the scheduler copies the list internally.
- **Validation**: The scheduler may enforce minimum/maximum values for numeric properties (e.g., `QueuePollIntervalMs` must be ≥ 100, `DefaultTimeoutSeconds` must be ≥ 1). Exceeding these bounds can result in an `ArgumentOutOfRangeException` during scheduler construction.
- **Cleanup behavior**: When `EnableCleanup` is `false`, the `CleanupIntervalMs` property is ignored. When `true`, a value of `0` or negative for `CleanupIntervalMs` may be treated as the default (e.g., 1 hour) or cause an exception.
- **Slack vs. Webhooks**: If both `EnableSlack` and `EnableWebhooks` are `true`, the scheduler sends notifications to both channels. The `SlackWebhookUrl` is used for Slack; a separate webhook URL (not shown in this class) may be configured elsewhere.
- **Email delivery**: The scheduler does not validate SMTP credentials or server reachability at configuration time. Failures during email sending are logged but do not affect job execution.
