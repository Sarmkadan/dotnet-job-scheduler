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

## ScheduleService

The `ScheduleService` provides schedule analysis, next execution time calculation, and schedule distribution analysis for jobs. It helps with schedule visualization, capacity planning, and load balancing by offering methods to query upcoming executions, analyze cron expressions, and inspect schedule patterns across your job collection.

### Usage

```csharp
using JobScheduler.Core.Services;
using JobScheduler.Core.Data.Repositories;
using JobScheduler.Core.Domain.Entities;
using Microsoft.Extensions.Logging;

// Setup dependencies (typically via DI)
var jobRepository = new JobRepository(dbContext);
var cronService = new CronExpressionService();
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<ScheduleService>();
var scheduleService = new ScheduleService(jobRepository, cronService, logger);

// Get upcoming execution times for a job
var jobId = Guid.Parse("your-job-id");
var upcomingTimes = await scheduleService.GetUpcomingExecutionTimesAsync(jobId, 5);
foreach (var time in upcomingTimes)
{
    Console.WriteLine($"Next execution: {time:yyyy-MM-dd HH:mm:ss}");
}

// Get human-readable cron expression description
var cronDescription = await scheduleService.GetCronExpressionDescriptionAsync("0 9 * * 1-5");
Console.WriteLine(cronDescription); // "At 9:00 AM, Monday through Friday"

// Calculate execution frequency per day
var frequency = await scheduleService.GetExecutionFrequencyPerDayAsync("*/15 * * * *");
Console.WriteLine($"Runs {frequency} times per day");

// Estimate execution count for capacity planning
var estimatedExecutions = await scheduleService.EstimateExecutionCountAsync(jobId, 7);
Console.WriteLine($"Expected executions in 7 days: {estimatedExecutions}");

// Get next scheduled jobs (immediate workload)
var nextJobs = await scheduleService.GetNextScheduledJobsAsync(10);
Console.WriteLine($"Next {nextJobs.Count} jobs to execute:");
foreach (var job in nextJobs)
{
    Console.WriteLine($"- {job.Name} at {job.NextExecutionAt}");
}

// Analyze schedule distribution by hour (load balancing)
var distribution = await scheduleService.GetScheduleDistributionByHourAsync();
Console.WriteLine("Schedule distribution by hour:");
for (int hour = 0; hour < 24; hour++)
{
    Console.WriteLine($"Hour {hour:00}: {distribution[hour]} jobs");
}

// Get jobs that will execute in the next N minutes
var imminentJobs = await scheduleService.GetJobsExecutingInNextMinutesAsync(5);
Console.WriteLine($"Jobs executing in next 5 minutes: {imminentJobs.Count}");
```

## DistributedJobLock

The `DistributedJobLock` class represents a database-backed distributed lock entry for a single job. It ensures that only one scheduler node runs a given job at a time in multi-instance deployments, preventing duplicate job executions across multiple nodes.

### Usage

```csharp
using JobScheduler.Core.Services;
using Microsoft.Extensions.Logging;

// Create a new distributed job lock
var jobLock = new DistributedJobLock
{
    JobId = Guid.Parse("your-job-id"),
    HolderInstanceId = "scheduler-node-01",
    ExpiresAt = DateTime.UtcNow.AddMinutes(5)
};

// Check if lock is expired
bool isExpired = jobLock.IsExpired();
Console.WriteLine($"Lock expired: {isExpired}");

// Get lock details
Console.WriteLine($"Lock ID: {jobLock.Id}");
Console.WriteLine($"Job ID: {jobLock.JobId}");
Console.WriteLine($"Holder Instance: {jobLock.HolderInstanceId}");
Console.WriteLine($"Acquired At: {jobLock.AcquiredAt}");
Console.WriteLine($"Expires At: {jobLock.ExpiresAt}");
```

### Properties

- **Id**: Gets or sets the primary key (auto-generated Guid)
- **JobId**: Gets or sets the ID of the locked job
- **HolderInstanceId**: Gets or sets the identifier of the node that acquired the lock
- **AcquiredAt**: Gets or sets the UTC timestamp when the lock was acquired
- **ExpiresAt**: Gets or sets the UTC timestamp after which the lock expires automatically
- **IsExpired(DateTime? utcNow)**: Returns true when the lock has passed its expiry time

## JobExecution
... rest of file content ...
