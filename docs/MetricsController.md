# MetricsController

The `MetricsController` is an ASP.NET Core controller that exposes scheduler and process metrics as Prometheus-compatible text. Its controller route is `/metrics`.

## Dependencies

The controller receives the following dependencies through its constructor:

- `IJobRepository`: supplies jobs used to calculate job counts, execution totals, queue depth, and scheduler lag.
- `IExecutionRepository`: supplies the currently running executions.
- `PerformanceMonitor`: supplies average execution duration and success rate.
- `ILogger<MetricsController>`: records errors encountered while generating the response.

Each constructor argument is required. Passing `null` causes an `ArgumentNullException`.

## API

### GetMetrics()

```csharp
public async Task<IActionResult> GetMetrics()
```

**Route:** `GET /metrics`  
**Purpose:** Collects scheduler, execution, queue, performance, and process-memory measurements.  
**Parameters:** None.  
**Return Value:** A `Task<IActionResult>` that returns `200 OK` with a plain-text metrics payload. If metric generation fails, it logs the exception and returns `500 Internal Server Error` with `# Error generating metrics`.  
**Throws:** Exceptions raised while collecting metrics are caught by the method and converted to the `500` response.

## Output Format

Successful responses use the content type `text/plain; version=0.0.4; charset=utf-8`. Each metric family has `# HELP` and `# TYPE` lines followed by one or more samples. The response ends with the OpenMetrics `# EOF` marker.

The endpoint produces these metric families:

- `job_scheduler_jobs_total` (`gauge`): registered job counts labeled with `state="all"`, `state="active"`, `state="suspended"`, and `state="failed"`.
- `job_scheduler_executions_total` (`counter`): cumulative job execution counts labeled with `outcome="total"`, `outcome="success"`, and `outcome="failure"`.
- `job_scheduler_running_executions` (`gauge`): number of executions currently in progress.
- `job_scheduler_queue_depth` (`gauge`): jobs currently due for execution, labeled with `priority="critical"`, `priority="high"`, `priority="normal"`, and `priority="low"`.
- `job_scheduler_scheduler_lag_seconds` (`gauge`): average number of seconds that currently due active jobs are overdue; it is `0` when there are no overdue jobs.
- `job_scheduler_execution_duration_ms` (`gauge`): average job execution duration in milliseconds.
- `job_scheduler_success_rate_percent` (`gauge`): overall execution success rate from `0` to `100`.
- `job_scheduler_memory_bytes` (`gauge`): managed process memory reported by `GC.GetTotalMemory(false)`.

Values are formatted using invariant culture. `NaN` and positive or negative infinity are emitted as `0`.

A response has this structure (the numeric values vary with application state):

```text
# HELP job_scheduler_jobs_total Total number of registered jobs
# TYPE job_scheduler_jobs_total gauge
job_scheduler_jobs_total{state="all"} 12
job_scheduler_jobs_total{state="active"} 8
job_scheduler_jobs_total{state="suspended"} 2
job_scheduler_jobs_total{state="failed"} 1
...
# EOF
```

## Usage

### Example: Retrieving metrics with curl

```bash
curl --fail --show-error http://localhost:5000/metrics
```

## Notes

- Job data is requested separately for job metrics, execution totals, and queue metrics, so a response may reflect repository changes that occur between those reads.
- Execution totals are aggregated from counters stored on jobs rather than by scanning the execution table.
- Queue depth includes active jobs whose `NextExecutionAt` is at or before the current UTC time.
