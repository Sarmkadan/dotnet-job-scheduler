# Scheduler Enums

The enums in `src/JobScheduler.Core/Constants/` describe execution outcomes, job state, queue priority, and misfire handling. Their integer values are part of their current definitions and are listed explicitly below.

## `ExecutionStatus`

Represents the result status of one job execution attempt.

| Member | Value | Meaning |
| --- | ---: | --- |
| `Running` | 0 | The execution is currently in progress. |
| `Success` | 1 | The execution completed successfully. |
| `Failed` | 2 | The execution failed with an error. |
| `Cancelled` | 3 | The execution was cancelled before completion. |
| `TimedOut` | 4 | The execution timed out. |
| `Skipped` | 5 | The execution was skipped because of concurrency control. |

## `JobStatus`

Represents the status of a scheduled job in the system.

| Member | Value | Meaning |
| --- | ---: | --- |
| `Pending` | 0 | The job has been created but has not yet been scheduled. |
| `Scheduled` | 1 | The job is scheduled and waiting for execution. |
| `Running` | 2 | The job is currently executing. |
| `Completed` | 3 | The job execution completed successfully. |
| `Failed` | 4 | The job failed and is awaiting a retry. |
| `Suspended` | 5 | The job was suspended by a user or by the system. |
| `Cancelled` | 6 | The job has been cancelled. |
| `FailedPermanently` | 7 | The job failed permanently after all retries were exhausted. |

### Lifecycle in `JobSchedulerService`

`JobSchedulerService.CreateJobAsync` assigns `Scheduled` when it creates and schedules a job. Due and misfired jobs are then passed to `JobExecutorService`; `JobSchedulerService` itself does not directly assign `Running`, `Completed`, or `Failed`, so those execution transitions are not asserted here. The service exposes these additional status changes:

- `SuspendJobAsync` assigns `Suspended`.
- `ResumeJobAsync` assigns `Scheduled` and calculates a new next execution time.
- `ProcessRetriesAsync` assigns `FailedPermanently` when the retry service determines that a failed execution should not be retried.

`Pending` and `Cancelled` are enum states, but `JobSchedulerService` does not directly assign either one.

## `JobPriority`

Defines execution priority in the scheduler queue. Higher numeric values have higher priority and execute first.

| Member | Value | Meaning |
| --- | ---: | --- |
| `Low` | 0 | Lowest priority; executes last. |
| `Normal` | 1 | Normal priority and the default for most jobs. |
| `High` | 2 | High priority; executes before normal-priority jobs. |
| `Critical` | 3 | Critical priority; executes before all other jobs. |

## `MisfirePolicy`

Defines how to handle a job that was scheduled to run while the scheduler was not running.

| Member | Value | Meaning |
| --- | ---: | --- |
| `FireOnceNow` | 0 | Run the job once immediately after the scheduler restarts. If many jobs misfired, this can cause a thundering-herd problem. |
| `SkipToNext` | 1 | Skip the missed execution and calculate the next execution from the cron expression. This is the safest default for recurring jobs. |
| `FireAll` | 2 | Run all missed executions immediately after the scheduler restarts. A large number of missed executions can cause performance problems. |
