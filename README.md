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

## CronExpressionService

The `CronExpressionService` provides parsing, validation, and evaluation of cron expressions for job scheduling. It uses the NCrontab library to parse POSIX-compliant cron expressions and offers methods for validating expressions, calculating next execution times (including timezone-aware calculations), and generating human-readable descriptions of cron schedules.

### Usage

```csharp
using JobScheduler.Core.Services;
using Microsoft.Extensions.Logging;

// Create the service (typically via dependency injection)
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<CronExpressionService>();
var cronService = new CronExpressionService(logger);

// Validate a cron expression
bool isValid = cronService.IsValidCronExpression("0 9 * * 1-5");
Console.WriteLine($"Is valid: {isValid}"); // true

// Parse a cron expression (throws on invalid expression)
var schedule = cronService.ParseCronExpression("*/15 * * * *");
Console.WriteLine("Expression parsed successfully");

// Get the next execution time from now
var nextExecution = cronService.GetNextExecutionTime("0 9 * * 1-5");
Console.WriteLine($"Next execution: {nextExecution:yyyy-MM-dd HH:mm:ss}");

// Get the next execution time in a specific timezone
var nextInZone = cronService.GetNextExecutionTimeInZone(
    "0 9 * * 1-5",
    "America/New_York"
);
Console.WriteLine($"Next execution in New York timezone: {nextInZone:yyyy-MM-dd HH:mm:ss} UTC");

// Get multiple upcoming execution times
var nextFive = cronService.GetNextExecutionTimes("0 9 * * 1-5", 5);
Console.WriteLine("Next 5 execution times:");
foreach (var time in nextFive)
{
    Console.WriteLine($"- {time:yyyy-MM-dd HH:mm:ss}");
}

// Check if a specific time matches the cron expression
bool shouldExecute = cronService.ShouldExecuteAt(
    "0 9 * * 1-5",
    DateTime.Parse("2024-06-10 09:00:00") // Monday
);
Console.WriteLine($"Should execute at 9:00 AM on Monday: {shouldExecute}"); // true

// Get a human-readable description of the cron expression
string description = cronService.GetCronDescription("0 9 * * 1-5");
Console.WriteLine(description); // "At 09:00, Monday through Friday"
```

## JobPipelineService

The `JobPipelineService` manages job pipelines — ordered chains of jobs where each step is triggered only after the previous step succeeds. It handles pipeline creation, retrieval, status monitoring, and cleanup, automatically establishing sequential dependency edges between pipeline steps to enforce execution order.

### Usage

```csharp
using JobScheduler.Core.Services;
using JobScheduler.Core.Data;
using JobScheduler.Core.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

// Setup dependencies (typically via DI)
var dbContext = new JobSchedulerContext(options);
var dependencyService = new JobDependencyService(dbContext, logger);
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<JobPipelineService>();
var pipelineService = new JobPipelineService(dbContext, dependencyService, logger);

// Create a new pipeline with 3 jobs
var pipeline = await pipelineService.CreatePipelineAsync(new CreatePipelineRequest
{
    Name = "DataProcessingPipeline",
    Description = "ETL pipeline for customer data processing",
    Steps = new List<PipelineStepRequest>
    {
        new() { JobId = Guid.Parse("a1b2c3d4-5678-90ef-ghij-klmnopqrstuv"), StopOnFailure = true },
        new() { JobId = Guid.Parse("b2c3d4e5-6789-01fg-hijk-lmnopqrstuvw"), StopOnFailure = false },
        new() { JobId = Guid.Parse("c3d4e5f6-7890-12gh-ijk-lmnopqrstuvwx"), StopOnFailure = true }
    }
}, "system");

Console.WriteLine($"Pipeline created: {pipeline.Name} with {pipeline.Steps.Count} steps");

// Get a specific pipeline
var retrievedPipeline = await pipelineService.GetPipelineAsync(pipeline.Id);
Console.WriteLine($"Retrieved pipeline: {retrievedPipeline?.Name}");

// Get all pipelines
var allPipelines = await pipelineService.GetAllPipelinesAsync();
Console.WriteLine($"Total pipelines: {allPipelines.Count}");

// Get pipeline status
var status = await pipelineService.GetPipelineStatusAsync(pipeline.Id);
if (status != null)
{
    Console.WriteLine($"Pipeline status for {status.PipelineName}:");
    foreach (var step in status.StepStatuses)
    {
        Console.WriteLine($"  Step {step.StepOrder}: {step.JobName} - {step.Status} (Ready: {step.IsReady})");
    }
}

// Map pipeline to response model
var response = JobPipelineService.MapToResponse(pipeline);
Console.WriteLine($"Pipeline response: {response.Name} with {response.Steps.Count} steps");

// Delete pipeline when no longer needed
var deleted = await pipelineService.DeletePipelineAsync(pipeline.Id);
Console.WriteLine($"Pipeline deleted: {deleted}");
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

## AuditLogger

The `AuditLogger` service provides comprehensive auditing capabilities for job scheduler operations, tracking API calls, job lifecycle events, security incidents, and execution activities. It maintains a complete audit trail with timestamps, user information, severity levels, and detailed event descriptions, enabling compliance tracking, debugging, and security monitoring.

### Usage

```csharp
using JobScheduler.Core.Services;
using JobScheduler.Core.Domain.Models;
using Microsoft.Extensions.Logging;

// Setup dependencies (typically via dependency injection)
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<AuditLogger>();
var auditLogger = new AuditLogger(logger);

// Log an API call
await auditLogger.LogApiCallAsync(
    eventType: "GET /api/jobs",
    userId: "admin@company.com",
    entityId: Guid.Parse("job-id-123"),
    entityType: "Job",
    details: "Retrieved job details for job ID job-id-123",
    severity: AuditSeverity.Information,
    method: "GET",
    path: "/api/jobs"
);

// Log job creation
await auditLogger.LogJobCreationAsync(
    userId: "admin@company.com",
    jobId: Guid.Parse("new-job-id-456"),
    jobName: "DataProcessor",
    details: "Created new job 'DataProcessor' with daily schedule",
    severity: AuditSeverity.Information
);

// Log job modification
await auditLogger.LogJobModificationAsync(
    userId: "admin@company.com",
    jobId: Guid.Parse("job-id-123"),
    jobName: "DataProcessor",
    oldStatus: "Active",
    newStatus: "Paused",
    details: "Job status changed from Active to Paused",
    severity: AuditSeverity.Warning
);

// Log job deletion
await auditLogger.LogJobDeletionAsync(
    userId: "admin@company.com",
    jobId: Guid.Parse("job-id-123"),
    jobName: "DataProcessor",
    details: "Deleted job 'DataProcessor' and all associated executions",
    severity: AuditSeverity.High
);

// Log security event
await auditLogger.LogSecurityEventAsync(
    userId: "hacker@external.com",
    eventType: "FailedAuthentication",
    details: "Multiple failed login attempts from IP 192.168.1.100",
    severity: AuditSeverity.Critical
);

// Log job execution event
await auditLogger.LogExecutionEventAsync(
    jobId: Guid.Parse("job-id-123"),
    jobName: "DataProcessor",
    executionId: Guid.Parse("exec-id-789"),
    status: "Completed",
    details: "Job executed successfully in 1.2 seconds",
    severity: AuditSeverity.Information
);

// Retrieve audit logs
var auditLogs = await auditLogger.GetAuditLogs(
    startDate: DateTime.UtcNow.AddDays(-7),
    endDate: DateTime.UtcNow,
    severity: AuditSeverity.High
);
Console.WriteLine($"Found {auditLogs.Count} high severity events in last 7 days");

// Get audit statistics
var statistics = auditLogger.GetStatistics(
    startDate: DateTime.UtcNow.AddDays(-30),
    endDate: DateTime.UtcNow
);
Console.WriteLine($"Total events: {statistics.TotalEvents}");
Console.WriteLine($"Critical events: {statistics.CriticalEvents}");
Console.WriteLine($"Average severity: {statistics.AverageSeverity}");

// Clear old logs (older than 90 days)
int clearedCount = await auditLogger.ClearOldLogsAsync(TimeSpan.FromDays(90));
Console.WriteLine($"Cleared {clearedCount} old audit log entries");
```

### Properties

- **EventId**: Gets the unique identifier for the audit event
- **EventType**: Gets the type/category of the audit event
- **Timestamp**: Gets the UTC timestamp when the event occurred
- **UserId**: Gets the user identifier associated with the event
- **EntityId**: Gets the ID of the entity involved in the event
- **EntityType**: Gets the type of entity involved in the event
- **Details**: Gets the detailed description of the event
- **Severity**: Gets the severity level of the event
- **Method**: Gets the HTTP method for API call events
- **Path**: Gets the HTTP path for API call events

## JobExecution
... rest of file content ...
