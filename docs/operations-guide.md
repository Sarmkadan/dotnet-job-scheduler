# Operations Guide

This guide covers production configuration topics that go beyond basic job registration: retry policies, concurrency limits, the dashboard, distributed leader election, and the Prometheus metrics endpoint.

---

## Table of Contents

1. [Retry Policies](#retry-policies)
2. [Concurrency Limits](#concurrency-limits)
3. [Dashboard Configuration](#dashboard-configuration)
4. [Distributed Leader Election](#distributed-leader-election)
5. [Prometheus Metrics Endpoint](#prometheus-metrics-endpoint)
6. [Recommended Settings for High-Throughput Environments](#recommended-settings)
7. [Configuration Reference](#configuration-reference)

---

## Retry Policies

Each job carries its own retry configuration. When a job execution fails the scheduler checks whether the attempt count is below `MaxRetries`. If so, it schedules a retry after a delay computed by exponential back-off:

```
delay = RetryBackoffSeconds × 2^(attemptNumber - 1)
```

The delay is capped at `ExecutionTimeoutSeconds` to prevent unreasonably long waits.

### Per-job retry settings

```csharp
var job = new Job
{
    Name            = "send-invoice",
    CronExpression  = "0 8 * * 1-5",   // Weekdays 08:00
    HandlerType     = "InvoiceHandler",

    MaxRetries             = 5,     // Up to 5 retries after the first failure
    RetryBackoffSeconds    = 10,    // First retry after 10 s, then 20 s, 40 s …
    ExecutionTimeoutSeconds = 120,  // Kill the job if it runs longer than 2 min
};
```

### Global retry defaults

Override the defaults at registration time so every new job inherits sensible values:

```csharp
builder.Services.AddJobScheduler(options =>
{
    options.DefaultMaxRetries          = 3;
    options.DefaultRetryBackoffSeconds = 5;
    options.DefaultTimeoutSeconds      = 300;
});
```

### Retry budget

Use `RetryService.IsRetryBudgetExceededAsync` to guard against a runaway job consuming all available slots with rapid retries:

```csharp
var exceeded = await retryService.IsRetryBudgetExceededAsync(
    jobId,
    retryBudgetCount: 10,  // max retries inside the window
    timeWindowMinutes: 5   // sliding window
);
if (exceeded)
    logger.LogWarning("Retry budget exceeded for job {JobId}", jobId);
```

---

## Concurrency Limits

### Global limit

Controls the maximum number of executions running across **all** jobs simultaneously:

```csharp
builder.Services.AddJobScheduler(options =>
{
    options.MaxConcurrentJobs = 20;  // default: 10
});
```

### Per-job limit

Prevents a single job from spawning many parallel copies:

```csharp
var job = new Job
{
    Name                    = "data-export",
    CronExpression          = "*/5 * * * *",   // Every 5 min
    HandlerType             = "DataExportHandler",
    MaxConcurrentExecutions = 1,  // Only one copy at a time (default)
};
```

Set `MaxConcurrentExecutions = 2` (or higher) for jobs that benefit from parallelism, such as independent partition processing.

### Priority aging (anti-starvation)

Under sustained high-priority load, low-priority jobs automatically age up in effective priority at a rate of **one level per 5 minutes overdue**. This ensures low-priority jobs are eventually dequeued even when the system is saturated. The implementation is transparent and requires no additional configuration.

---

## Dashboard Configuration

### Enabling the dashboard

The dashboard controllers are registered automatically by ASP.NET Core's controller discovery. Ensure you call `MapControllers()` in your application pipeline:

```csharp
app.UseJobSchedulerMiddleware();
app.MapControllers();
```

Key endpoints:

| Endpoint | Description |
|----------|-------------|
| `GET /api/dashboard/overview` | System-wide stats (total jobs, success rate, etc.) |
| `GET /api/dashboard/queue-status` | Pending / running / failed counts |
| `GET /api/dashboard/priority-distribution` | Jobs per priority tier |
| `GET /api/dashboard/performance-timeline?hours=24` | Hourly execution timeline |
| `GET /api/dashboard/slowest-jobs` | Top 10 slowest jobs by average duration |
| `GET /api/dashboard/most-failing-jobs` | Top 10 jobs by failure rate |
| `GET /api/dashboard/health-report` | System health and warnings |

### Securing the dashboard

Protect dashboard (and job management) endpoints using ASP.NET Core authorization policies:

```csharp
builder.Services.AddAuthentication().AddJwtBearer();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SchedulerAdmin", policy =>
        policy.RequireRole("scheduler-admin"));
});
```

```csharp
// Protect all /api/dashboard routes:
app.MapControllers()
   .RequireAuthorization("SchedulerAdmin");
```

Or use `[Authorize]` attribute per controller for finer-grained control.

### API key authentication

Enable the built-in API key guard for simpler deployments:

```json
// appsettings.json
{
  "Security": {
    "EnableApiKeyAuth": true,
    "ApiKeys": [
      { "Key": "your-secret-key", "Name": "ops-team", "Active": true }
    ]
  }
}
```

```csharp
builder.Services.Configure<SecuritySettings>(
    builder.Configuration.GetSection("Security"));
```

---

## Distributed Leader Election

In a horizontally-scaled deployment every running instance of the scheduler would independently fire due jobs, causing duplicate executions.  Enable leader election so only the **elected leader** fires jobs at each tick:

```csharp
builder.Services.AddJobScheduler(options =>
{
    options.EnableLeaderElection            = true;  // opt-in
    options.LeaderElectionInstanceId        = Environment.MachineName; // or pod name in Kubernetes
    options.LeaderElectionLeaseDurationSeconds = 30;
});
```

The implementation uses a single row in the `SchedulerLeaderLocks` database table — no Redis or etcd dependency required.

### How it works

1. On each scheduling tick the `SchedulerHostedService` calls `ILeaderElectionService.TryAcquireLeadershipAsync()`.
2. If the current instance wins (or renews) the lease it proceeds to execute due jobs.
3. If another instance holds a valid lease the current instance skips execution for that tick.
4. If the lease holder crashes, the lease expires after `LeaderElectionLeaseDurationSeconds` and the next instance to call `TryAcquireLeadershipAsync` takes over.
5. On graceful shutdown `ReleaseLeadershipAsync()` expires the lease immediately so failover happens without waiting for the timeout.

### Integrating with your hosted service

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    var leaderElection = _serviceProvider
        .GetService<ILeaderElectionService>(); // null when feature is disabled

    while (!stoppingToken.IsCancellationRequested)
    {
        if (leaderElection is null || await leaderElection.TryAcquireLeadershipAsync(stoppingToken))
        {
            using var scope = _serviceProvider.CreateScope();
            var scheduler = scope.ServiceProvider.GetRequiredService<JobSchedulerService>();
            await scheduler.ExecuteDueJobsAsync(stoppingToken);
        }

        await Task.Delay(5_000, stoppingToken);
    }

    if (leaderElection is not null)
        await leaderElection.ReleaseLeadershipAsync();
}
```

---

## Prometheus Metrics Endpoint

A `/metrics` endpoint following the OpenMetrics text format is available out of the box.  Add it to your Prometheus `scrape_configs`:

```yaml
# prometheus.yml
scrape_configs:
  - job_name: job_scheduler
    static_configs:
      - targets: ['scheduler-host:8080']
    metrics_path: /metrics
```

### Available metrics

| Metric | Type | Labels | Description |
|--------|------|--------|-------------|
| `job_scheduler_jobs_total` | gauge | `state` (all, active, suspended, failed) | Registered job counts |
| `job_scheduler_executions_total` | counter | `outcome` (total, success, failure) | Cumulative execution counts |
| `job_scheduler_running_executions` | gauge | — | Currently running executions |
| `job_scheduler_queue_depth` | gauge | `priority` (critical, high, normal, low) | Jobs due for execution per priority tier |
| `job_scheduler_scheduler_lag_seconds` | gauge | — | Average seconds overdue for pending jobs |
| `job_scheduler_execution_duration_ms` | gauge | — | Average execution duration (ms) |
| `job_scheduler_success_rate_percent` | gauge | — | Overall success rate (0–100) |
| `job_scheduler_memory_bytes` | gauge | — | Process memory usage (bytes) |

### Example Grafana alert

```yaml
# Alert if more than 100 jobs are overdue for > 5 min
- alert: SchedulerLagHigh
  expr: job_scheduler_scheduler_lag_seconds > 300
  for: 5m
  labels:
    severity: warning
  annotations:
    summary: "Scheduler lag is high ({{ $value }}s)"
```

---

## Recommended Settings

### High-throughput environment (> 1 000 jobs/min)

```csharp
builder.Services.AddJobScheduler(options =>
{
    options.MaxConcurrentJobs          = 50;
    options.DefaultTimeoutSeconds      = 60;    // Tight timeout to free slots quickly
    options.DefaultMaxRetries          = 2;     // Fewer retries to reduce queue pressure
    options.DefaultRetryBackoffSeconds = 3;
    options.QueuePollIntervalMs        = 1_000; // Poll every second
    options.EnableLeaderElection       = true;  // Mandatory for multi-node
    options.LeaderElectionLeaseDurationSeconds = 15; // Fast failover
});
```

### Low-latency / SLA-sensitive environment

```csharp
builder.Services.AddJobScheduler(options =>
{
    options.MaxConcurrentJobs          = 10;
    options.DefaultTimeoutSeconds      = 30;
    options.DefaultMaxRetries          = 5;
    options.DefaultRetryBackoffSeconds = 5;
    options.QueuePollIntervalMs        = 500;
});
```

### Development / single-node

```csharp
builder.Services.AddJobScheduler(options =>
{
    options.ConnectionString           = "Data Source=scheduler-dev.db";
    options.MaxConcurrentJobs          = 5;
    options.EnableLeaderElection       = false;
    options.QueuePollIntervalMs        = 2_000;
});
```

---

## Configuration Reference

### `JobSchedulerOptions`

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ConnectionString` | `string?` | SQLite `scheduler.db` | Database connection string |
| `MaxConcurrentJobs` | `int` | `10` | Global execution concurrency limit |
| `DefaultTimeoutSeconds` | `int` | `300` | Default per-job execution timeout |
| `DefaultMaxRetries` | `int` | `3` | Default max retry attempts |
| `DefaultRetryBackoffSeconds` | `int` | `5` | Initial backoff for exponential retry |
| `QueuePollIntervalMs` | `int` | `1000` | Milliseconds between scheduler ticks |
| `EnableCleanup` | `bool` | `true` | Auto-clean orphaned executions |
| `CleanupIntervalMs` | `int` | `300000` | Cleanup interval (5 min) |
| `EnableLeaderElection` | `bool` | `false` | Enable distributed leader election |
| `LeaderElectionInstanceId` | `string?` | `MachineName` | Unique node identifier |
| `LeaderElectionLeaseDurationSeconds` | `int` | `30` | Leader lease expiry in seconds |

### `Job` entity retry / concurrency fields

| Property | Default | Description |
|----------|---------|-------------|
| `MaxRetries` | `3` | Max retry attempts after failure |
| `RetryBackoffSeconds` | `5` | Initial backoff (doubles each attempt) |
| `ExecutionTimeoutSeconds` | `300` | Kill threshold in seconds |
| `MaxConcurrentExecutions` | `1` | Max parallel copies of this job |
| `Priority` | `Normal` | `Low`, `Normal`, `High`, `Critical` |
