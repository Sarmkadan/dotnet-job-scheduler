# Scheduler Constants

`SchedulerConstants` defines the core timeouts, limits, and configuration defaults used by the job scheduler. References below are usages found under `src`, excluding each constant's declaration.

| Name | Value | Meaning | References in `src` |
| --- | ---: | --- | --- |
| `DefaultMaxConcurrentJobs` | `10` | Default maximum number of concurrent job executions | `JobScheduler.Core/Configuration/DependencyInjectionExtensions.cs:217`<br>`JobScheduler.Core/Services/ConcurrencyManager.cs:38, 49` |
| `DefaultExecutionTimeoutSeconds` | `300` | Default job execution timeout in seconds | `JobScheduler.Core/Configuration/DependencyInjectionExtensions.cs:220`<br>`JobScheduler.Core/Domain/Entities/Job.cs:59`<br>`JobScheduler.Core/Domain/Models/CreateJobRequest.cs:55` |
| `DefaultMaxRetries` | `3` | Default maximum retry attempts for failed jobs | `JobScheduler.Core/Configuration/DependencyInjectionExtensions.cs:223`<br>`JobScheduler.Core/Domain/Entities/Job.cs:55`<br>`JobScheduler.Core/Domain/Entities/RetryPolicy.cs:22`<br>`JobScheduler.Core/Domain/Models/CreateJobRequest.cs:49` |
| `DefaultRetryBackoffSeconds` | `5` | Default initial backoff delay for retries in seconds | `JobScheduler.Core/Configuration/DependencyInjectionExtensions.cs:226`<br>`JobScheduler.Core/Domain/Entities/Job.cs:57`<br>`JobScheduler.Core/Domain/Entities/RetryPolicy.cs:24`<br>`JobScheduler.Core/Domain/Models/CreateJobRequest.cs:52` |
| `DefaultMaxRetryBackoffSeconds` | `300` | Default maximum backoff delay for retries in seconds | `JobScheduler.Core/Domain/Entities/Job.cs:179`<br>`JobScheduler.Core/Domain/Entities/RetryPolicy.cs:26` |
| `RetryBackoffMultiplier` | `2.0` | Backoff multiplier for exponential backoff strategy | `JobScheduler.Core/Domain/Entities/Job.cs:181`<br>`JobScheduler.Core/Domain/Entities/RetryPolicy.cs:30` |
| `DefaultHeartbeatIntervalMs` | `5000` | Default heartbeat interval in milliseconds | No references found |
| `MaxJobNameLength` | `256` | Maximum job name length in characters | `JobScheduler.Core/Domain/Entities/Job.cs:96`<br>`JobScheduler.Core/Utilities/ValidationUtility.cs:30-31` |
| `MaxCronExpressionLength` | `100` | Maximum cron expression length in characters | `JobScheduler.Core/Domain/Entities/Job.cs:99`<br>`JobScheduler.Core/Utilities/ValidationUtility.cs:48-49` |
| `MaxJobsPerPriority` | `20` | Maximum concurrent jobs per priority level | `JobScheduler.Core/Domain/Entities/Job.cs:111` |
| `QueuePollIntervalMs` | `1000` | Queue poll interval in milliseconds for checking scheduled jobs | `JobScheduler.Core/Configuration/DependencyInjectionExtensions.cs:229` |
| `CleanupIntervalMs` | `300000` | Cleanup interval for orphaned executions in milliseconds | `JobScheduler.Core/Configuration/DependencyInjectionExtensions.cs:235` |
| `ExecutionHistoryRetentionDays` | `30` | Maximum execution history retention in days | No references found |
