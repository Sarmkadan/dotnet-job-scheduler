# JobExecutionSummary

A simple data transfer object that aggregates statistics about the execution history of a scheduled job. It is typically populated by the scheduler after a job run and consumed by monitoring or reporting components to display execution trends.

## API

| Member | Type | Purpose | Remarks |
|--------|------|---------|---------|
| `JobId` | `Guid?` | Identifier of the job to which the summary belongs. Can be `null` if the job has not been persisted yet. | No exceptions are thrown by direct field access. |
| `JobName` | `string?` | Human‑readable name of the job. May be `null` when the name is unknown. | No exceptions are thrown by direct field access. |
| `TotalExecutions` | `int` | Total number of times the job has been executed (successful, failed, timed‑out, or cancelled). | No exceptions are thrown by direct field access. |
| `SuccessCount` | `int` | Number of executions that completed successfully. | No exceptions are thrown by direct field access. |
| `FailureCount` | `int` | Number of executions that ended with an error. | No exceptions are thrown by direct field access. |
| `TimedOutCount` | `int` | Number of executions that were terminated because they exceeded the configured timeout. | No exceptions are thrown by direct field access. |
| `CancelledCount` | `int` | Number of executions that were cancelled by an external request. | No exceptions are thrown by direct field access. |
| `AverageDurationMs` | `long` | Average execution duration in milliseconds across all recorded executions. If `TotalExecutions` is zero, the value is `0`. | No exceptions are thrown by direct field access. |
| `MinDurationMs` | `long` | Shortest observed execution duration in milliseconds. If no executions have occurred, the value is `0`. | No exceptions are thrown by direct field access. |
| `MaxDurationMs` | `long` | Longest observed execution duration in milliseconds. If no executions have occurred, the value is `0`. | No exceptions are thrown by direct field access. |
| `LastExecutedAt` | `DateTime?` | Timestamp of the most recent job execution. `null` indicates that the job has never been run. | No exceptions are thrown by direct field access. |
| `LastStatus` | `ExecutionStatus?` | Status of the most recent execution (e.g., Success, Failed, TimedOut, Cancelled). `null` when there is no execution history. | No exceptions are thrown by direct field access. |

## Usage

### Example 1: Reading summary after a job run

```csharp
using DotnetJobScheduler.Models;

// Assume scheduler has just finished a job and filled the summary.
JobExecutionSummary summary = scheduler.GetLastJobSummary(jobId);

Console.WriteLine($"Job {summary.JobName} (ID: {summary.JobId})");
Console.WriteLine($"Total runs: {summary.TotalExecutions}");
Console.WriteLine($"Success: {summary.SuccessCount}, Failures: {summary.FailureCount}");
Console.WriteLine($"Average duration: {summary.AverageDurationMs} ms");
Console.WriteLine($"Last run: {summary.LastExecutedAt:O} with status {summary.LastStatus}");
```

### Example 2: Aggregating multiple summaries for a report

```csharp
using System.Linq;
using DotnetJobScheduler.Models;

// Collect summaries for all jobs in the last hour.
IEnumerable<JobExecutionSummary> recentSummaries = scheduler.GetSummariesSince(DateTime.UtcNow.AddHours(-1));

var report = recentSummaries.GroupBy(s => s.JobId)
                            .Select(g => new
                            {
                                JobId = g.Key,
                                JobName = g.First().JobName,
                                TotalExecutions = g.Sum(s => s.TotalExecutions),
                                SuccessRate = g.Sum(s => s.SuccessCount) / (double)g.Sum(s => s.TotalExecutions),
                                AvgDurationMs = (long)g.Average(s => s.AverageDurationMs)
                            })
                            .ToList();

foreach (var r in report)
{
    Console.WriteLine($"{r.JobName}: {r.TotalExecutions} runs, {r.SuccessRate:P1} success, avg {r.AvgDurationMs} ms");
}
```

## Notes

- All numeric counters (`TotalExecutions`, `SuccessCount`, etc.) are expected to be non‑negative; negative values indicate a bug in the updating logic.
- When `TotalExecutions` is zero, the duration fields (`AverageDurationMs`, `MinDurationMs`, `MaxDurationMs`) are defined to be zero to avoid division‑by‑zero scenarios.
- The nullable fields (`JobId`, `JobName`, `LastExecutedAt`, `LastStatus`) may be `null` if the job has not yet been persisted or has never been executed; consumers should check for `null` before using these values.
- The type contains only mutable fields and does not provide any internal synchronization. Concurrent reads and writes from multiple threads are not thread‑safe; external locking or concurrent collections must be used if the instance is shared.
- The class does not inherit from any other type and implements no interfaces; it is intended solely as a DTO. No versioning or serialization concerns are addressed in this documentation.
