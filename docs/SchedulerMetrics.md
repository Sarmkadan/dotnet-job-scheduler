# Scheduler Metrics

`SchedulerMetrics` provides the tracing and metric instrumentation defined in `src/JobScheduler.Core/Metrics/SchedulerMetrics.cs`.

## Instrumentation names

| Constant | Value | Purpose |
|---|---|---|
| `SchedulerMetrics.ActivitySourceName` | `JobScheduler` | Names the `ActivitySource` used to create traces and spans. |
| `SchedulerMetrics.MeterName` | `JobScheduler` | Names the `Meter` that owns all scheduler metric instruments. |

## Metric instruments

The name, unit, and description values below are reproduced exactly as coded.

| Property | Instrument type | Name | Unit | Description |
|---|---|---|---|---|
| `ExecutionsStarted` | `Counter<int>` | `jobscheduler.executions.started` | `executions` | `Number of job execution attempts started` |
| `ExecutionsSucceeded` | `Counter<int>` | `jobscheduler.executions.succeeded` | `executions` | `Number of successful job executions` |
| `ExecutionsFailed` | `Counter<int>` | `jobscheduler.executions.failed` | `executions` | `Number of failed job executions` |
| `ExecutionDuration` | `Histogram<double>` | `jobscheduler.execution.duration` | `ms` | `Duration of job executions in milliseconds` |
| `DueJobBacklog` | `ObservableGauge<int>` | `jobscheduler.due_job_backlog` | `jobs` | `Number of jobs currently due for execution (waiting in queue)` |

`DueJobBacklog` observes `GetDueJobBacklogCount()`. The current implementation returns `0` as a placeholder.

## OpenTelemetry registration

Register the scheduler meter with the application's OpenTelemetry metrics pipeline by passing `SchedulerMetrics.MeterName` to `AddMeter`:

```csharp
using JobScheduler.Core.Metrics;
using OpenTelemetry.Metrics;

builder.Services
    .AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddMeter(SchedulerMetrics.MeterName));
```

All instruments in the table are emitted by that meter, so one `AddMeter` call subscribes the metrics pipeline to all of them. Configure an OpenTelemetry metric exporter separately in the application so the collected measurements are exported.

For tracing, the corresponding activity source can be registered independently with `SchedulerMetrics.ActivitySourceName`:

```csharp
builder.Services
    .AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource(SchedulerMetrics.ActivitySourceName));
```
