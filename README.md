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

## CacheService

The `CacheService` provides in-memory caching for frequently accessed scheduler data, reducing database queries and improving response times for hot data. It's essential for performance when dealing with large job sets and supports pattern-based invalidation for related cache entries.

### Usage

```csharp
using JobScheduler.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;

// Setup (typically in DI configuration)
var cache = new MemoryCache(new MemoryCacheOptions());
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<CacheService>();
var cacheService = new CacheService(cache, logger);

// Store a job in cache
var jobId = Guid.NewGuid();
var job = new Job { Id = jobId, Name = "DataProcessor", Status = "Active" };
await cacheService.SetAsync(CacheKeyGenerator.JobKey(jobId), job, TimeSpan.FromMinutes(30));

// Retrieve a job from cache
var cachedJob = await cacheService.GetAsync<Job>(CacheKeyGenerator.JobKey(jobId));
Console.WriteLine(cachedJob?.Name); // "DataProcessor"

// Get or set with factory pattern (cache-aside)
var stats = await cacheService.GetOrSetAsync(
    CacheKeyGenerator.JobStatsKey(jobId),
    async () => await _jobStatsRepository.GetJobStatsAsync(jobId),
    TimeSpan.FromMinutes(5)
);

// Remove specific cache entry
await cacheService.RemoveAsync(CacheKeyGenerator.JobKey(jobId));

// Invalidate all cache entries related to a job (e.g., after job update)
await cacheService.InvalidatePatternAsync($"job:{jobId}:");

// Clear entire cache (e.g., during scheduler shutdown)
await cacheService.ClearAllAsync();

// Get cache statistics for monitoring
var stats = cacheService.GetStatistics();
Console.WriteLine($"Total cached keys: {stats.TotalKeys}, Timestamp: {stats.Timestamp}");
```

### Cache Key Generator

Use `CacheKeyGenerator` static methods to ensure consistent key naming throughout the application:

- `JobKey(Guid jobId)` - Cache a specific job by ID
- `JobKey(string jobName)` - Cache a job by name
- `JobExecutionsKey(Guid jobId, int pageNumber)` - Cache job execution history
- `JobStatsKey(Guid jobId)` - Cache job statistics
- `AllJobsKey()` - Cache list of all jobs
- `JobsByStatusKey(string status)` - Cache jobs by status
- `SystemStatsKey()` - Cache system-wide statistics
- `QueueStatusKey()` - Cache queue status
- `SchedulerConfigKey()` - Cache scheduler configuration
- `ExecutionKey(Guid executionId)` - Cache a specific execution

## JobExecution
... rest of file content ...
