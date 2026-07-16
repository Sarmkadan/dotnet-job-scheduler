# Architecture

How dotnet-job-scheduler is actually put together: the moving parts, the data flow,
the design decisions and their trade-offs, and where the sharp edges are. Everything
below is grounded in the code under `src/JobScheduler.Core/` - if it's not in the
code, it's not in this document.

## Big Picture

The whole scheduler ships as a single project, `JobScheduler.Core` (net10.0). It is
both a library (consumed via `AddJobScheduler()` on any `IServiceCollection`) and a
runnable host (`Program.cs` wires up a generic host with a background scheduling
loop against SQLite).

```
┌───────────────────────────────────────────────────────┐
│  Hosting                                              │
│  Program / SchedulerHostedService (poll loop)         │
│  Controllers (Jobs, Executions, Pipelines, History,   │
│  Metrics, Dashboard, Health) + Middleware             │
└───────────────────────┬───────────────────────────────┘
                        │
┌───────────────────────▼───────────────────────────────┐
│  Services (business logic)                            │
│  JobSchedulerService ── orchestrator                  │
│  JobExecutorService ── runs one job (handler dispatch)│
│  CronExpressionService, RetryService,                 │
│  ConcurrencyManager, ScheduleService                  │
│  JobPipelineService, JobDependencyService,            │
│  JobHistoryService, ExecutionStatisticsService        │
│  DistributedJobLockService, LeaderElectionService     │
│  CacheService, AuditLogger, PerformanceMonitor        │
│  Webhook/Slack notification, EventPublisher           │
└───────────────────────┬───────────────────────────────┘
                        │
┌───────────────────────▼───────────────────────────────┐
│  Data access                                          │
│  IRepository<T> / IJobRepository / IExecutionRepo     │
│  JobSchedulerContext (EF Core, SQLite provider)       │
└───────────────────────────────────────────────────────┘
```

There is no separate "domain project" or plugin system - the layering is by
namespace (`Domain`, `Services`, `Data`, `Controllers`, `Middleware`), not by
assembly. That was deliberate: one NuGet package, one dependency for the consumer.
The cost is that nothing physically stops a controller from touching the DbContext;
discipline is enforced by review, not by the compiler.

## Domain Model

All entities live in `Domain/Entities` and use **Guid primary keys**
(`Guid.NewGuid()` client-side). Guids were chosen over identity ints so that
entities can be created and wired together (job → executions → pipeline steps)
before anything hits the database, and so ids stay stable across environments.
Trade-off: worse index locality than sequential ints; acceptable at the volumes
this scheduler targets.

- **Job** - the scheduled unit: name, cron expression, `HandlerType` (assembly-
  qualified type name of the handler), timeout, `MaxConcurrentExecutions`,
  priority, retry settings, `NextExecutionTime` / `LastExecutionTime`, soft-delete
  metadata.
- **JobExecution** - one attempt: start/complete timestamps, duration, attempt
  number, status, output, error message + stack trace, `IsRetryable`,
  `NextRetryAt`, executor name/instance, memory/CPU usage fields. Owns the
  `ShouldRetry(maxRetries)` decision helper.
- **RetryPolicy** - backoff calculation (exponential / linear / fixed) with max
  cap. Pure value logic, no persistence of its own.
- **JobPipeline** - ordered chain of jobs with per-step failure behaviour.
- **JobDependency** - edges of the job dependency graph.
- **JobScheduleHistory** - audit trail of schedule changes.
- **ExecutionMetrics** - aggregated counters (successes, failures, timeouts,
  average duration, success rate). Computed object, not a table of its own.

Enums (`JobStatus`, `ExecutionStatus`, `JobPriority`) and shared defaults live in
`Constants/`. Request/response DTOs used by controllers live in `Domain/Models` -
entities are never serialized straight out of the API.

## The Scheduling Loop

`SchedulerHostedService` (a `BackgroundService` in `Program.cs`) is the heartbeat:

```
every ~5s:
  create DI scope
  JobSchedulerService.ExecuteDueJobsAsync(ct)
      → IJobRepository.GetDueJobsAsync()          (NextExecutionTime <= now)
      → per job: ConcurrencyManager.CanExecuteAsync(job)
      → JobExecutorService.ExecuteJobAsync(job, ...)
      → CronExpressionService → compute next NextExecutionTime
  JobSchedulerService.ProcessRetriesAsync()
      → failed executions whose NextRetryAt has arrived
```

A fresh scope per tick keeps the `DbContext` short-lived - the standard EF Core
pattern for long-running loops (a context that lives for hours accumulates tracked
entities and stale state).

**Polling, not timers-per-job.** One poll query every few seconds is simpler and
more robust than maintaining an in-memory timer per job (which needs rehydration
on restart and careful drift handling). The trade-off is scheduling granularity:
a job never fires more precisely than the poll interval
(`JobSchedulerOptions.QueuePollIntervalMs`).

### Executing one job

`JobExecutorService.RunAsync` is the execution engine:

1. Creates a `JobExecution` row up front (so a crash mid-run leaves evidence).
2. Resolves the handler: `Job.HandlerType` → `Type.GetType` / assembly scan →
   must implement `IJobHandler` → instantiated via `ActivatorUtilities` so handler
   constructors can take DI dependencies. A pre-built handler instance can also be
   passed in explicitly (used by benchmarks and custom hosts).
3. Runs `handler.ExecuteAsync(job, ct)` under a linked `CancellationTokenSource`
   with the job's timeout.
4. Marks the execution completed/failed, captures output or error + stack trace,
   and lets `RetryService` decide about retry scheduling.

```csharp
public interface IJobHandler
{
    Task<string> ExecuteAsync(Job job, CancellationToken cancellationToken);
}
```

This string-typed handler resolution ("stringly-typed dispatch") is the main
extension point and the main foot-gun: a typo in `HandlerType` is a runtime
`ExecutionException`, not a compile error. It was chosen anyway because handlers
are stored *in the database per job*, so they must survive serialization - a
generic `AddJobHandler<T>()` registry would be nicer to use but couldn't express
"this row in the Jobs table runs that class" without the same string mapping
underneath.

### Retry

`RetryService.ShouldRetryAsync(job, execution)` gates on `IsRetryable`, attempt
count vs `MaxRetries`, and a **retry budget** (`IsRetryBudgetExceededAsync`: max N
retries per job per time window) so a hard-down dependency doesn't generate an
unbounded retry storm. Backoff comes from `RetryPolicy` (exponential with cap by
default). Retries are persisted as `NextRetryAt` on the execution row and picked
up by the same poll loop - no separate queue infrastructure to operate.

### Concurrency

`ConcurrencyManager` enforces two limits before a job is allowed to run: the
global `MaxConcurrentJobs` and the per-job `Job.MaxConcurrentExecutions`. The
authoritative counts come from the database
(`IExecutionRepository.GetConcurrentRunningCountAsync()` /
`GetCurrentlyRunningCountAsync(jobId)`), which means the limits hold across
multiple scheduler instances sharing one database, not just within one process.
The in-memory counters (`ConcurrentDictionary` + interlocked global count) are a
fast-path cache on top; the DB is the source of truth. Violation raises
`ConcurrencyException`.

## Multi-Instance Coordination

Two mechanisms, both database-backed on purpose - the scheduler already requires
a database, and requiring Redis/ZooKeeper just for coordination would double the
operational footprint for what is usually a 2-3 node deployment:

- **DistributedJobLockService** (`IDistributedJobLockService`) - per-job lease
  locks in a `DistributedJobLock` table: `TryAcquireLockAsync` /
  `RenewLockAsync` / `ReleaseLockAsync` / `CleanExpiredLocksAsync`. Guarantees a
  specific job runs on at most one node at a time.
- **DatabaseLeaderElectionService** (`ILeaderElectionService`) - opt-in via
  `JobSchedulerOptions.EnableLeaderElection`; one node holds a renewable lease
  (default 30 s) and acts as the scheduler, the rest idle as warm standbys.
  Instance identity defaults to `Environment.MachineName`.

The known trade-off of DB-based leases: failover latency is bounded by lease
duration, and a node frozen longer than its lease (GC pause, VM migration) can
briefly overlap with the new leader. Handlers should still be idempotent.

## Data Access

Classic repository pattern over EF Core:

- `IRepository<T>` / `Repository<T>` - generic CRUD.
- `IJobRepository` / `IExecutionRepository` - the queries the scheduler actually
  needs (due jobs, running counts, recent failures, cleanup by cutoff date).

Repositories exist here for one concrete reason: the scheduling services are unit
tested against mocked `IJobRepository`/`IExecutionRepository` (see
`tests/dotnet-job-scheduler.Tests`), and the due-job / running-count queries have
a single authoritative implementation instead of being scattered as ad-hoc LINQ
across services.

`JobSchedulerContext` configures entities, relations and indexes.
`InitializeDatabaseAsync()` applies migrations at startup.

**Provider note:** `AddJobScheduler()` currently calls `UseSqlite(...)`
unconditionally. EF Core keeps the model provider-portable, but switching to
PostgreSQL/SQL Server today means registering your own `DbContext` instead of
relying on the built-in registration. Making the provider pluggable through
`JobSchedulerOptions` is the obvious next step.

## API Surface & Middleware

Controllers (`Controllers/`) expose jobs CRUD + trigger (`JobsController`),
executions (`ExecutionsController`), pipelines (`PipelinesController`), history
(`HistoryController`), metrics (`MetricsController`), a dashboard aggregate
(`DashboardController`) and health checks (`HealthController`). All inherit
`BaseController` for shared response shaping. They speak DTOs from
`Domain/Models`, never raw entities.

`UseJobSchedulerMiddleware()` installs, in this order:
`GlobalExceptionMiddleware` (maps the exception hierarchy under
`JobSchedulerException` to proper status codes) → `LoggingMiddleware` →
`RateLimitMiddleware`. Order matters and is fixed by the extension method so
consumers can't get it wrong.

The bundled `Program.cs` runs a **generic host without the HTTP pipeline** - the
controllers and middleware only come alive when the package is used inside an
ASP.NET Core app (see `examples/02-AspNetCoreIntegration.cs`). Headless scheduler
by default, API opt-in.

## Cross-Cutting Services

- **CacheService** - `IMemoryCache` wrapper with key conventions and TTLs for
  hot lookups (job by id, statistics).
- **EventPublisher** (`IEventPublisher` / `ISchedulerEvent`) - in-process pub/sub
  for lifecycle events. In-process only, by design: it's for wiring notifications
  and custom reactions inside the host, not a message bus.
- **WebhookNotificationService / SlackNotificationService / ExternalApiClient** -
  outbound HTTP, registered via `AddHttpClient<T>` so `HttpClientFactory` manages
  handler lifetimes (no socket exhaustion from naive `new HttpClient()`).
- **AuditLogger, PerformanceMonitor, ExecutionStatisticsService,
  JobHistoryService** - observability: who changed what, how long executions
  take, success rates, history queries.
- **CsvExportFormatter** (`Formatters/`) - CSV export used by
  reporting/export endpoints.
- **Utilities/** - `TimeUtility`, `ParseUtility`, `ValidationUtility`,
  `CryptoUtility`, `JobHelper`: stateless static helpers.

Cron parsing (`CronExpressionService`) delegates to **NCrontab** rather than a
hand-rolled parser - cron semantics (DOM/DOW interaction, ranges, steps) are a
graveyard of subtle bugs, and the library is battle-tested. The service adds
validation, next/previous-occurrence calculation and caching on top;
`CronExpressionDescriptor` provides the human-readable descriptions.

## Dependency Injection & Lifetimes

Everything is registered in one place,
`Configuration/DependencyInjectionExtensions.AddJobScheduler()`:

| Lifetime | Services | Why |
|---|---|---|
| Singleton | `CronExpressionService`, `PerformanceMonitor`, `IEventPublisher` | Stateless or process-global state (parse cache, counters, subscriptions) |
| Scoped | repositories, `JobSchedulerService`, `JobExecutorService`, `RetryService`, `ConcurrencyManager`, pipeline/history/statistics/audit services, `IDistributedJobLockService`, `ILeaderElectionService` | Everything that touches `JobSchedulerContext` must share its scope |
| HttpClient-typed | webhook/Slack/external API clients | factory-managed handlers |

`ValidateSchedulerConfiguration()` resolves every required service once at
startup (inside a scope) so a broken registration fails at boot with a clear
message instead of surprising the first request.

## Extension Points

1. **`IJobHandler`** - implement it, register the type in DI (or let
   `ActivatorUtilities` construct it), point `Job.HandlerType` at it.
2. **`IEventPublisher`** - subscribe to scheduler events for custom side effects,
   or replace the implementation.
3. **`IRetryPolicy` / `RetryPolicy`** - custom backoff strategies.
4. **Repositories** - `IJobRepository`/`IExecutionRepository` can be re-registered
   with custom implementations after `AddJobScheduler()` (last registration wins).
5. **`ILeaderElectionService` / `IDistributedJobLockService`** - swap the
   database-backed coordination for Redis/Consul-based implementations.

## Known Limitations

- **SQLite is hard-wired** in `AddJobScheduler()` (see Data Access above).
- **Polling granularity** - nothing fires more often than the poll interval;
  sub-second schedules are out of scope.
- **Handler resolution is string-based** - typos surface at run time.
- **Events are in-process** - restarting the host drops subscriptions; there is
  no outbox/durable event log.
- **Rate limiting is per-instance** - `RateLimitMiddleware` keeps counters in
  memory, so limits multiply with node count behind a load balancer.
- **Metrics are pull-only** - `MetricsController`/`PerformanceMonitor` expose
  numbers, but there is no built-in Prometheus/OTel exporter yet.

## Testing

- `tests/dotnet-job-scheduler.Tests` - unit tests per service (scheduler,
  executor, retry, concurrency, locks, leader election, pipelines, history,
  cache) plus `JobSchedulerIntegrationTests` running against a real (SQLite)
  context.
- `tests/JobScheduler.Core.Tests` - focused cron expression tests.
- `benchmarks/` - BenchmarkDotNet suites for the hot paths (cron parsing,
  scheduling, execution, caching, retries, concurrency checks).

Services take their dependencies via constructor and most expose `virtual`
methods, so tests mock at either the repository seam or the service seam.
