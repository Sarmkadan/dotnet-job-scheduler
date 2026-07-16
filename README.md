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

## ExecutionRepository

The `ExecutionRepository` provides data access methods for job executions, enabling efficient querying and analysis of execution history, status tracking, and performance monitoring. It serves as the primary repository for all job execution data, supporting filtering by job, status, date ranges, and execution statistics.

### Usage

```csharp
using JobScheduler.Core.Data.Repositories;
using JobScheduler.Core.Domain.Models;
using Microsoft.EntityFrameworkCore;

// Setup (typically via dependency injection)
var options = new DbContextOptionsBuilder<JobSchedulerContext>()
    .UseSqlite("Data Source=jobscheduler.db")
    .Options;
var dbContext = new JobSchedulerContext(options);
var executionRepository = new ExecutionRepository(dbContext);

// Get the latest execution for a specific job
var latestExecution = await executionRepository.GetLatestExecutionAsync(Guid.Parse("job-id-123"));
Console.WriteLine($"Latest execution status: {latestExecution?.Status}");

// Get all executions for a specific job
var jobExecutions = await executionRepository.GetExecutionsByJobAsync(Guid.Parse("job-id-123"));
Console.WriteLine($"Total executions: {jobExecutions.Count()}");

// Get executions by status (e.g., failed executions that need retry)
var failedExecutions = await executionRepository.GetExecutionsByStatusAsync("Failed");
Console.WriteLine($"Failed executions requiring attention: {failedExecutions.Count()}");

// Get executions by both job and status
var failedJobExecutions = await executionRepository.GetExecutionsByJobAndStatusAsync(
    Guid.Parse("job-id-123"),
    "Failed"
);
Console.WriteLine($"Failed executions for job: {failedJobExecutions.Count()}");

// Get currently running execution count (for concurrency monitoring)
var runningCount = await executionRepository.GetCurrentlyRunningCountAsync();
Console.WriteLine($"Currently running executions: {runningCount}");

// Get concurrent running count for a specific job
var concurrentCount = await executionRepository.GetConcurrentRunningCountAsync(Guid.Parse("job-id-123"));
Console.WriteLine($"Concurrent executions for job: {concurrentCount}");

// Get all currently running executions
var runningExecutions = await executionRepository.GetRunningExecutionsAsync();
foreach (var execution in runningExecutions)
{
    Console.WriteLine($"Running: {execution.JobId} - {execution.StartedAt}");
}

// Get failed executions that require retry (based on retry policy)
var retryCandidates = await executionRepository.GetFailedExecutionsRequiringRetryAsync();
Console.WriteLine($"Executions ready for retry: {retryCandidates.Count()}");

// Get executions within a specific date range
var dateRangeExecutions = await executionRepository.GetExecutionsByDateRangeAsync(
    DateTime.UtcNow.AddDays(-7),
    DateTime.UtcNow
);
Console.WriteLine($"Executions in last 7 days: {dateRangeExecutions.Count()}");

// Get average execution time for performance monitoring
var avgExecutionTime = await executionRepository.GetAverageExecutionTimeAsync();
Console.WriteLine($"Average execution time: {avgExecutionTime:F2} seconds");

// Get all executions for a job by job ID (alternative method)
var executionsByJobId = await executionRepository.GetByJobIdAsync(Guid.Parse("job-id-123"));
Console.WriteLine($"All executions via GetByJobIdAsync: {executionsByJobId.Count}");
```

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

## ConcurrencyManager

The `ConcurrencyManager` service manages concurrent job execution limits and ensures concurrency constraints are respected across the scheduler. It prevents system overload by enforcing both global and job-specific concurrency limits, tracking running executions in memory while synchronizing with database state for multi-node deployments.

### Usage

```csharp
using JobScheduler.Core.Services;
using JobScheduler.Core.Data.Repositories;
using JobScheduler.Core.Domain.Entities;
using Microsoft.Extensions.Logging;

// Setup dependencies (typically via dependency injection)
var executionRepository = new ExecutionRepository(dbContext);
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<ConcurrencyManager>();

// Create ConcurrencyManager with default global limit (100 concurrent jobs)
var concurrencyManager = new ConcurrencyManager(executionRepository, logger: logger);

// OR create with custom global limit (e.g., 50 concurrent jobs)
var customConcurrencyManager = new ConcurrencyManager(executionRepository, maxGlobalConcurrency: 50, logger: logger);

// Check if a job can execute (returns true/false)
var job = new Job { Id = Guid.NewGuid(), MaxConcurrentExecutions = 3 };
bool canExecute = await concurrencyManager.CanExecuteAsync(job);
Console.WriteLine($"Can job execute: {canExecute}");

// Ensure a job can execute (throws ConcurrencyException if limits exceeded)
try
{
    await concurrencyManager.EnsureCanExecuteAsync(job);
    Console.WriteLine("Job can execute - concurrency check passed");
}
catch (ConcurrencyException ex)
{
    Console.WriteLine($"Concurrency limit exceeded: {ex.Message}");
}

// When a job execution starts, increment concurrency count
concurrencyManager.IncrementConcurrencyCount(job.Id);
Console.WriteLine($"Job concurrency count: {concurrencyManager.GetJobConcurrencyCount(job.Id)}");
Console.WriteLine($"Global concurrency count: {concurrencyManager.GetGlobalConcurrencyCount()}");

// When a job execution completes, decrement concurrency count
concurrencyManager.DecrementConcurrencyCount(job.Id);

// Synchronize with database state (call on startup or periodically)
await concurrencyManager.SynchronizeWithDatabaseAsync();

// Get detailed concurrency statistics
await concurrencyManager.SynchronizeWithDatabaseAsync();
var stats = concurrencyManager.GetConcurrencyStats();
Console.WriteLine($"Global running: {stats["GlobalRunning"]}/{stats["GlobalLimit"]}");
Console.WriteLine($"Jobs with executions: {stats["JobsWithExecutions"]}");
Console.WriteLine($"Total cached jobs: {stats["TotalCachedJobs"]}");
```

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

## Repository

The `Repository<T>` class is a generic base repository implementation that provides standard CRUD operations for all entity types in the job scheduler. It serves as the foundation for data access, offering common query and modification methods that work with any entity type inheriting from the base class. The repository pattern abstracts Entity Framework Core operations, making it easier to maintain consistent data access patterns across the application.

### Usage

```csharp
using JobScheduler.Core.Data.Repositories;
using JobScheduler.Core.Domain.Models;
using Microsoft.EntityFrameworkCore;

// Setup (typically via dependency injection)
var options = new DbContextOptionsBuilder<JobSchedulerContext>()
    .UseSqlite("Data Source=jobscheduler.db")
    .Options;
var dbContext = new JobSchedulerContext(options);

// Create a generic repository for a specific entity type
var jobRepository = new Repository<Job>(dbContext);

// Get a single entity by ID
var jobId = Guid.Parse("a1b2c3d4-5678-90ef-ghij-klmnopqrstuv");
var job = await jobRepository.GetByIdAsync(jobId);
Console.WriteLine(job?.Name);

// Get all entities of a specific type
var allJobs = await jobRepository.GetAllAsync();
Console.WriteLine($"Total jobs: {allJobs.Count()}");

// Find entities matching a predicate
var activeJobs = await jobRepository.FindAsync(j => j.Status == "Active");
Console.WriteLine($"Active jobs: {activeJobs.Count()}");

// Get the first entity matching a predicate
var firstActiveJob = await jobRepository.FirstOrDefaultAsync(j => j.Status == "Active");
Console.WriteLine(firstActiveJob?.Name);

// Count entities matching a predicate
var activeJobCount = await jobRepository.CountAsync(j => j.Status == "Active");
Console.WriteLine($"Active job count: {activeJobCount}");

// Add a new entity
var newJob = new Job
{
    Id = Guid.NewGuid(),
    Name = "DataProcessor",
    CronExpression = "0 * * * *",
    Status = "Active",
    Priority = 1,
    MaxConcurrentExecutions = 3,
    TimeoutSeconds = 300,
    CreatedAt = DateTime.UtcNow
};
await jobRepository.AddAsync(newJob);

// Add multiple entities at once
var additionalJobs = new List<Job>
{
    new Job { Id = Guid.NewGuid(), Name = "ReportGenerator", Status = "Active", Priority = 2 },
    new Job { Id = Guid.NewGuid(), Name = "CleanupService", Status = "Active", Priority = 3 }
};
await jobRepository.AddRangeAsync(additionalJobs);

// Update an existing entity
if (job != null)
{
    job.Status = "Paused";
    job.UpdatedAt = DateTime.UtcNow;
    jobRepository.Update(job);
}

// Update multiple entities
var jobsToUpdate = await jobRepository.FindAsync(j => j.Status == "Active");
foreach (var j in jobsToUpdate)
{
    j.Priority += 1;
}
jobRepository.UpdateRange(jobsToUpdate);

// Remove an entity
if (job != null)
{
    jobRepository.Remove(job);
}

// Remove multiple entities
var jobsToRemove = await jobRepository.FindAsync(j => j.Status == "Inactive");
jobRepository.RemoveRange(jobsToRemove);

// Check if any entities match a predicate
var hasActiveJobs = await jobRepository.AnyAsync(j => j.Status == "Active");
Console.WriteLine($"Has active jobs: {hasActiveJobs}");

// Save all pending changes to the database
await jobRepository.SaveChangesAsync();
```

## JobRepository

The `JobRepository` provides specialized data access methods for job entities, extending the base `Repository<Job>` with job-specific queries. It handles job retrieval by name, status, priority, and execution state, supporting common scheduler operations like finding active jobs, failed jobs, long-running jobs, and jobs due for execution. The repository integrates with EF Core for efficient database queries and includes ordering extensions to prioritize jobs appropriately.

### Usage

```csharp
using JobScheduler.Core.Data.Repositories;
using JobScheduler.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

// Setup (typically via dependency injection)
var options = new DbContextOptionsBuilder<JobSchedulerContext>()
    .UseSqlite("Data Source=jobscheduler.db")
    .Options;
var dbContext = new JobSchedulerContext(options);
var jobRepository = new JobRepository(dbContext);

// Get a job by name
var jobByName = await jobRepository.GetByNameAsync("DataProcessor");
Console.WriteLine(jobByName?.Name);

// Get all active jobs (ordered by priority)
var activeJobs = await jobRepository.GetActiveJobsAsync();
Console.WriteLine($"Active jobs: {activeJobs.Count()}");

// Get jobs by specific status
var pausedJobs = await jobRepository.GetJobsByStatusAsync(JobStatus.Paused);
Console.WriteLine($"Paused jobs: {pausedJobs.Count()}");

// Get jobs by priority level
var highPriorityJobs = await jobRepository.GetJobsByPriorityAsync(JobPriority.High);
Console.WriteLine($"High priority jobs: {highPriorityJobs.Count()}");

// Get jobs scheduled for immediate execution (due jobs)
var jobsForExecution = await jobRepository.GetScheduledJobsForExecutionAsync();
Console.WriteLine($"Jobs ready to execute: {jobsForExecution.Count()}");

// Get failed jobs that need attention
var failedJobs = await jobRepository.GetFailedJobsAsync();
Console.WriteLine($"Failed jobs requiring attention: {failedJobs.Count()}");

// Get long-running jobs (executing longer than threshold)
var longRunningJobs = await jobRepository.GetLongRunningJobsAsync(300); // 5 minutes
Console.WriteLine($"Long-running jobs (>5 min): {longRunningJobs.Count()}");

// Get jobs that haven't executed recently (stale jobs)
var staleJobs = await jobRepository.GetJobsWithoutRecentExecutionAsync(60); // 60 minutes
Console.WriteLine($"Stale jobs (>60 min since last execution): {staleJobs.Count()}");
```

## JobSchedulerSettings

The `JobSchedulerSettings` class encapsulates all configuration settings for the job scheduler. It provides a centralized, type-safe way to configure database connections, concurrency limits, timeouts, retries, queue polling, cleanup operations, and job naming constraints. This settings class is used throughout the scheduler for consistent configuration management and can be configured via dependency injection or configuration files.

### Usage

```csharp
using JobScheduler.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;

// Create settings instance with default values
var settings = new JobSchedulerSettings
{
    ConnectionString = "Data Source=jobscheduler.db",
    MaxConcurrentJobs = 50,
    DefaultTimeoutSeconds = 600,
    DefaultMaxRetries = 5,
    DefaultRetryBackoffSeconds = 15,
    QueuePollIntervalMs = 2000,
    EnableCleanup = true,
    CleanupIntervalMs = 7200000, // 2 hours
    MaxJobNameLength = 100,
    MaxCronExpressionLength = 100
};

// OR use with dependency injection in ASP.NET Core
var services = new ServiceCollection();

services.Configure<JobSchedulerSettings>(options =>
{
    options.ConnectionString = "Server=localhost;Database=JobScheduler;User Id=sa;Password=your_password;";
    options.MaxConcurrentJobs = 100;
    options.DefaultTimeoutSeconds = 300;
    options.DefaultMaxRetries = 3;
    options.DefaultRetryBackoffSeconds = 10;
    options.QueuePollIntervalMs = 1000;
    options.EnableCleanup = true;
    options.CleanupIntervalMs = 3600000; // 1 hour
    options.MaxJobNameLength = 255;
    options.MaxCronExpressionLength = 255;
});

// Build service provider and access settings
var serviceProvider = services.BuildServiceProvider();
var configuredSettings = serviceProvider.GetRequiredService<IOptions<JobSchedulerSettings>>().Value;

Console.WriteLine($"Connection String: {configuredSettings.ConnectionString}");
Console.WriteLine($"Max Concurrent Jobs: {configuredSettings.MaxConcurrentJobs}");
Console.WriteLine($"Default Timeout: {configuredSettings.DefaultTimeoutSeconds}s");
```

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ConnectionString` | `string?` | `null` | Database connection string (SQLite, SQL Server, PostgreSQL, etc.) |
| `MaxConcurrentJobs` | `int` | `10` | Maximum concurrent job executions allowed globally |
| `DefaultTimeoutSeconds` | `int` | `300` | Default job execution timeout in seconds |
| `DefaultMaxRetries` | `int` | `3` | Default maximum retry attempts for failed jobs |
| `DefaultRetryBackoffSeconds` | `int` | `5` | Default retry backoff interval in seconds |
| `QueuePollIntervalMs` | `int` | `5000` | Poll interval for checking due jobs in milliseconds |
| `EnableCleanup` | `bool` | `true` | Enable automatic cleanup of orphaned executions |
| `CleanupIntervalMs` | `int` | `300000` | Cleanup interval in milliseconds (5 minutes) |
| `MaxJobNameLength` | `int` | `255` | Maximum allowed length for job names |
| `MaxCronExpressionLength` | `int` | `255` | Maximum allowed length for cron expressions |

## DependencyInjectionExtensions

The `DependencyInjectionExtensions` class provides extension methods for registering job scheduler services with the .NET dependency injection container. It centralizes service registration, configuration, and validation, ensuring consistent setup across applications. The extension methods support both minimal configuration and advanced features like caching, monitoring, leader election, and distributed job locking.

### Usage

```csharp
using JobScheduler.Core.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Configure services in your application startup
var services = new ServiceCollection();

// Basic configuration with SQLite
services.AddJobScheduler(options =>
{
    options.ConnectionString = "Data Source=jobscheduler.db";
    options.MaxConcurrentJobs = 50;
    options.DefaultTimeoutSeconds = 60;
    options.DefaultMaxRetries = 3;
    options.DefaultRetryBackoffSeconds = 10;
    options.QueuePollIntervalMs = 1000;
});

// Advanced configuration with leader election for multi-node deployments
services.AddJobScheduler(options =>
{
    options.ConnectionString = "Data Source=jobscheduler.db";
    options.MaxConcurrentJobs = 100;
    options.EnableLeaderElection = true;
    options.LeaderElectionInstanceId = "scheduler-node-01";
    options.LeaderElectionLeaseDurationSeconds = 60;
});

// Build service provider
var serviceProvider = services.BuildServiceProvider();

// Initialize database and apply migrations
await serviceProvider.InitializeDatabaseAsync();

// Validate that all required services are properly registered
serviceProvider.ValidateSchedulerConfiguration();

// Use middleware in your ASP.NET Core pipeline
var appBuilder = new ApplicationBuilder(null);
appBuilder.UseJobSchedulerMiddleware();

// Access configuration values
var optionsSnapshot = serviceProvider.GetRequiredService<IOptions<JobSchedulerOptions>>();
var connectionString = optionsSnapshot.Value.ConnectionString;
var maxConcurrentJobs = optionsSnapshot.Value.MaxConcurrentJobs;
```

### Configuration Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ConnectionString` | `string?` | `null` | Database connection string (SQLite, SQL Server, etc.) |
| `MaxConcurrentJobs` | `int` | `10` | Maximum concurrent job executions allowed globally |
| `DefaultTimeoutSeconds` | `int` | `300` | Default job execution timeout in seconds |
| `DefaultMaxRetries` | `int` | `3` | Default maximum retry attempts for failed jobs |
| `DefaultRetryBackoffSeconds` | `int` | `10` | Default retry backoff interval in seconds |
| `QueuePollIntervalMs` | `int` | `1000` | Poll interval for checking due jobs in milliseconds |
| `EnableCleanup` | `bool` | `true` | Enable automatic cleanup of orphaned executions |
| `CleanupIntervalMs` | `int` | `3600000` | Cleanup interval in milliseconds |
| `EnableLeaderElection` | `bool` | `false` | Enable distributed leader election for multi-node deployments |
| `LeaderElectionInstanceId` | `string?` | `null` | Unique identifier for this scheduler instance |
| `LeaderElectionLeaseDurationSeconds` | `int` | `30` | Leadership lease duration in seconds |

### Services Registered

The `AddJobScheduler` method registers the following services:

**Phase 1 (Core):**
- `JobSchedulerContext` (DbContext)
- `IJobRepository` / `JobRepository`
- `IExecutionRepository` / `ExecutionRepository`
- `CronExpressionService` (singleton)
- `ConcurrencyManager` (scoped)
- `RetryService` (scoped)
- `JobExecutorService` (scoped)
- `JobSchedulerService` (scoped)

**Phase 2 (Features):**
- `CacheService` (scoped)
- `PerformanceMonitor` (singleton)
- `ExecutionStatisticsService` (scoped)
- `AuditLogger` (scoped)
- `IEventPublisher` / `EventPublisher` (singleton)
- `WebhookNotificationService` (with HttpClient)
- `SlackNotificationService` (with HttpClient)
- `ExternalApiClient` (with HttpClient)
- `ScheduleService` (scoped)
- `IJobDependencyService` / `JobDependencyService` (scoped)
- `JobHistoryService` (scoped)
- `JobPipelineService` (scoped)
- `IDistributedJobLockService` / `DistributedJobLockService` (scoped)

**Middleware:**
- `GlobalExceptionMiddleware`
- `LoggingMiddleware`
- `RateLimitMiddleware`

## DotnetJobSchedulerOptions

The `DotnetJobSchedulerOptions` class provides configuration settings for the Dotnet Job Scheduler. It allows you to customize database connections, concurrency limits, timeouts, retries, queue polling, cleanup operations, and other scheduler behaviors through strongly-typed properties.

### Usage

```csharp
using JobScheduler.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;

// Create options instance with required connection string
var options = new DotnetJobSchedulerOptions
{
    ConnectionString = "Data Source=jobscheduler.db",
    MaxConcurrentJobs = 50,
    DefaultTimeoutSeconds = 600,
    DefaultMaxRetries = 5,
    DefaultRetryBackoffSeconds = 15,
    QueuePollIntervalMs = 2000,
    EnableCleanup = true,
    CleanupIntervalMs = 7200000 // 2 hours
};

// OR configure via dependency injection in ASP.NET Core
var services = new ServiceCollection();

services.Configure<DotnetJobSchedulerOptions>(options =>
{
    options.ConnectionString = "Server=localhost;Database=JobScheduler;User Id=sa;Password=your_password;";
    options.MaxConcurrentJobs = 100;
    options.DefaultTimeoutSeconds = 300;
    options.DefaultMaxRetries = 3;
    options.DefaultRetryBackoffSeconds = 10;
    options.QueuePollIntervalMs = 1000;
    options.EnableCleanup = true;
    options.CleanupIntervalMs = 3600000; // 1 hour
});

// Build service provider and access configured options
var serviceProvider = services.BuildServiceProvider();
var configuredOptions = serviceProvider.GetRequiredService<IOptions<DotnetJobSchedulerOptions>>().Value;

Console.WriteLine($"Connection String: {configuredOptions.ConnectionString}");
Console.WriteLine($"Max Concurrent Jobs: {configuredOptions.MaxConcurrentJobs}");
Console.WriteLine($"Default Timeout: {configuredOptions.DefaultTimeoutSeconds}s");
Console.WriteLine($"Default Max Retries: {configuredOptions.DefaultMaxRetries}");
Console.WriteLine($"Default Retry Backoff: {configuredOptions.DefaultRetryBackoffSeconds}s");
Console.WriteLine($"Queue Poll Interval: {configuredOptions.QueuePollIntervalMs}ms");
Console.WriteLine($"Enable Cleanup: {configuredOptions.EnableCleanup}");
Console.WriteLine($"Cleanup Interval: {configuredOptions.CleanupIntervalMs}ms");
```

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `ConnectionString` | `string` | Database connection string (SQLite, SQL Server, PostgreSQL, etc.) |
| `MaxConcurrentJobs` | `int` | Maximum concurrent job executions allowed globally |
| `DefaultTimeoutSeconds` | `int` | Default job execution timeout in seconds |
| `DefaultMaxRetries` | `int` | Default maximum retry attempts for failed jobs |
| `DefaultRetryBackoffSeconds` | `int` | Default retry backoff interval in seconds |
| `QueuePollIntervalMs` | `int` | Poll interval for checking due jobs in milliseconds |
| `EnableCleanup` | `bool` | Enable automatic cleanup of completed jobs |
| `CleanupIntervalMs` | `int` | Cleanup interval in milliseconds |

## JobSchedulerContext

The `JobSchedulerContext` is the Entity Framework Core database context for the job scheduler. It serves as the primary data access layer, providing `DbSet<T>` collections for all scheduler entities and managing database connections, migrations, and transactions. The context is designed to work with dependency injection and supports both SQLite and SQL Server backends through EF Core's provider model.

### Usage

```csharp
using JobScheduler.Core.Data;
using JobScheduler.Core.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

// Configure DbContextOptions (typically in Startup.cs or Program.cs)
var options = new DbContextOptionsBuilder<JobSchedulerContext>()
    .UseSqlite("Data Source=jobscheduler.db")
    // OR for SQL Server:
    // .UseSqlServer("Server=localhost;Database=JobScheduler;User Id=sa;Password=your_password;")
    .UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole()))
    .Options;

// Create the context
var context = new JobSchedulerContext(options);

// Access DbSet collections for all scheduler entities
var jobs = await context.Jobs.ToListAsync();
var executions = await context.JobExecutions.ToListAsync();
var schedules = await context.JobScheduleHistories.ToListAsync();
var retryPolicies = await context.RetryPolicies.ToListAsync();
var metrics = await context.ExecutionMetrics.ToListAsync();
var dependencies = await context.JobDependencies.ToListAsync();
var leaderLocks = await context.SchedulerLeaderLocks.ToListAsync();
var pipelines = await context.JobPipelines.ToListAsync();
var pipelineSteps = await context.JobPipelineSteps.ToListAsync();
var distributedLocks = await context.DistributedJobLocks.ToListAsync();

// Query jobs with filtering
var activeJobs = await context.Jobs
    .Where(j => j.Status == "Active")
    .OrderBy(j => j.Priority)
    .ToListAsync();

// Add a new job
var newJob = new Job
{
    Id = Guid.NewGuid(),
    Name = "DataProcessor",
    Description = "Processes customer data",
    CronExpression = "0 * * * *",
    Status = "Active",
    Priority = 1,
    MaxConcurrentExecutions = 3,
    TimeoutSeconds = 300,
    CreatedAt = DateTime.UtcNow
};
context.Jobs.Add(newJob);

// Save changes asynchronously
var changes = await context.SaveChangesAsync();
Console.WriteLine($"Saved {changes} changes to database");

// Find a job by ID
var jobId = Guid.Parse("your-job-id-here");
var job = await context.Jobs.FindAsync(jobId);
if (job != null)
{
    Console.WriteLine($"Found job: {job.Name}");
}

// Update a job
if (job != null)
{
    job.Status = "Paused";
    job.UpdatedAt = DateTime.UtcNow;
    await context.SaveChangesAsync();
}

// Remove a job (with related data cleanup)
context.Jobs.Remove(job);
await context.SaveChangesAsync();

// Query with joins (jobs with their executions)
var jobsWithStats = await context.Jobs
    .Include(j => j.Executions)
    .ThenInclude(e => e.Metrics)
    .Where(j => j.Status == "Active")
    .Select(j => new
    {
        JobName = j.Name,
        ExecutionCount = j.Executions.Count,
        SuccessRate = j.Executions.Count(e => e.Status == "Completed") / (double)j.Executions.Count
    })
    .ToListAsync();
```

### Key DbSet Collections

- **Jobs**: The main collection of scheduled jobs with their configuration
- **JobExecutions**: Records of every job execution attempt with status and timing
- **JobScheduleHistories**: Historical tracking of schedule changes and executions
- **RetryPolicies**: Retry configuration for failed job executions
- **ExecutionMetrics**: Performance metrics and statistics for job executions
- **JobDependencies**: Dependency relationships between jobs
- **SchedulerLeaderLocks**: Leader election locks for multi-node deployments
- **JobPipelines**: Pipeline definitions for ordered job execution chains
- **JobPipelineSteps**: Individual steps within job pipelines
- **DistributedJobLocks**: Distributed locks to prevent duplicate executions


### Entity Relationships

The context establishes these key relationships:
- Jobs → Executions (one-to-many)
- Jobs → Dependencies (many-to-many)
- Jobs → Pipelines (via JobPipelineSteps)
- Executions → Metrics (one-to-one)
- SchedulerLeaderLocks → Jobs (one-to-one for leader election)


### Best Practices

- Use dependency injection to inject `JobSchedulerContext` into services
- Always use async methods (`ToListAsync`, `SaveChangesAsync`, etc.)
- Configure appropriate connection strings for your environment
- Use migrations for database schema changes (`dotnet ef migrations add ...`)
- Consider using repository pattern for complex queries to keep business logic separate
- Enable sensitive data logging only in development environments
```

## JobExecution
... rest of file content ...
