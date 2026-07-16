// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

# dotnet-job-scheduler

A .NET job scheduling library for background tasks, cron schedules, retries, pipelines and distributed execution, built on EF Core.

## Architecture

The scheduler is a single package (`JobScheduler.Core`): a polling `BackgroundService` picks up due jobs, `JobExecutorService` dispatches them to your `IJobHandler` implementations with timeout/retry/concurrency handling, and everything persists through EF Core repositories. Multi-node deployments coordinate via database-backed job locks and optional leader election.

## ExecutionStatisticsService

The `ExecutionStatisticsService` provides detailed statistics and performance insights for job executions. It includes methods to retrieve execution metrics, performance analysis, trends, and anomaly reports.

### Usage

```csharp
using JobScheduler.Core.Services;
using JobScheduler.Core.Data.Repositories;
using JobScheduler.Core.Domain.Models;
using Microsoft.Extensions.Logging;

// Create ExecutionStatisticsService with required dependencies
var executionRepository = new ExecutionRepository(dbContext);
var jobRepository = new JobRepository(dbContext);
var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<ExecutionStatisticsService>();
var statsService = new ExecutionStatisticsService(executionRepository, jobRepository, logger);

// Get job execution stats
var jobId = Guid.Parse("your-job-id");
var stats = await statsService.GetJobExecutionStatsAsync(jobId);
Console.WriteLine($"Execution count: {stats?.TotalExecutions}, Success rate: {stats?.SuccessRate}%");

// Performance analysis
var analysis = await statsService.GetJobPerformanceAnalysisAsync(jobId);
Console.WriteLine($"Median execution time: {analysis?.MedianExecutionTimeMs}ms");

// Performance trend
var trend = await statsService.GetPerformanceTrendAsync(jobId);
Console.WriteLine($"Trend points: {trend.Count}");

// Detect anomalies
var anomalies = await statsService.DetectExecutionAnomaliesAsync(jobId);
Console.WriteLine($"Anomalies detected: {anomalies.Count}");
```

Full breakdown - components, data flow, design decisions with trade-offs, extension points and known limitations - in [docs/architecture.md](docs/architecture.md).

## JobExecution
... rest of file content ...
