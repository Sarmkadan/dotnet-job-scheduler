# dotnet-job-scheduler

![CI](https://github.com/sarmkadan/dotnet-job-scheduler/actions/workflows/ci.yml/badge.svg)
![License](https://img.shields.io/github/license/sarmkadan/dotnet-job-scheduler)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)

A production-grade, distributed job scheduler for .NET with support for cron expressions, priority queues, automatic retries, concurrency control, and comprehensive job execution metrics. Built for reliability, scalability, and ease of integration.

## Table of Contents

- [Features](#features)
- [Architecture](#architecture)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Configuration](#configuration)
- [Usage Examples](#usage-examples)
- [API Reference](#api-reference)
- [Cron Expression Format](#cron-expression-format)
- [Job Lifecycle](#job-lifecycle)
- [Advanced Features](#advanced-features)
- [Deployment](#deployment)
- [Troubleshooting](#troubleshooting)
- [Testing](#testing)
- [Performance](#performance)
- [Related Projects](#related-projects)
- [Contributing](#contributing)
- [Support](#support)

## Features

### Core Capabilities

- **CRON Scheduling**: Full POSIX cron expression support for flexible job scheduling with minute-level precision
- **Priority Queues**: Execute jobs based on priority levels (Low, Normal, High, Critical) with priority-aware execution
- **Automatic Retries**: Configurable retry policies with exponential, linear, or fixed backoff strategies
- **Concurrency Control**: Global and per-job concurrency limits to prevent system overload and resource exhaustion
- **Job Execution Metrics**: Detailed tracking of execution history, success rates, performance timings, and error analytics
- **Flexible Job Handlers**: Support for custom job handler implementations via dependency injection
- **Execution History**: Complete audit trail of all job executions with error details and performance metrics
- **Status Management**: Rich job lifecycle management (Scheduled, Running, Failed, Suspended, Cancelled, etc.)
- **Database Agnostic**: Entity Framework Core abstraction supports SQL Server, PostgreSQL, MySQL, SQLite
- **Dependency Injection**: Built-in Microsoft DI integration for seamless application integration
- **Event Publishing**: Built-in event system for job lifecycle notifications
- **Webhook Notifications**: Post-execution webhook support for external system integration
- **Slack Notifications**: Direct Slack integration for job alerts and status updates
- **Rate Limiting**: Request throttling to prevent API abuse
- **Performance Monitoring**: Built-in performance tracking and metrics collection

## Architecture

### High-Level Design

```
┌─────────────────────────────────────────────────────────┐
│                    Application Layer                     │
│  (Controllers, API Endpoints, Dashboard UI)              │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│                   Service Layer                          │
│  ┌──────────────────────────────────────────────────┐   │
│  │ JobSchedulerService (Central Orchestrator)       │   │
│  ├─ Job Management (Create, Update, Delete)        │   │
│  ├─ Schedule Evaluation                             │   │
│  └─ Execution Queue Management                      │   │
│  ┌──────────────────────────────────────────────────┐   │
│  │ JobExecutorService (Execution Engine)            │   │
│  ├─ Job Execution with Timeout                      │   │
│  ├─ Error Handling & Logging                        │   │
│  └─ Metrics Collection                              │   │
│  ┌──────────────────────────────────────────────────┐   │
│  │ CronExpressionService                            │   │
│  ├─ POSIX Cron Parsing                              │   │
│  └─ Next Execution Time Calculation                 │   │
│  ┌──────────────────────────────────────────────────┐   │
│  │ RetryService & ConcurrencyManager                │   │
│  └─────────────────────────────────────────────────┘    │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│              Data Access Layer (EF Core)                 │
│  ┌──────────────────────────────────────────────────┐   │
│  │ JobRepository      ExecutionRepository           │   │
│  │ (CRUD + Queries)   (Query & Insert)              │   │
│  └──────────────────────────────────────────────────┘   │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│           Database (SQL Server/PostgreSQL/MySQL)        │
│  ┌──────────────────────────────────────────────────┐   │
│  │ Jobs | JobExecutions | ScheduleHistory | Metrics │   │
│  └──────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────┘
```

### Core Components

#### 1. Domain Models
- **Job**: Represents a scheduled job with metadata, retry policy, and execution parameters
- **JobExecution**: Single execution record with status, duration, and error information
- **RetryPolicy**: Defines retry behavior (backoff strategy, max attempts)
- **ExecutionMetrics**: Performance tracking (success rate, average duration, error rate)
- **JobScheduleHistory**: Historical record of schedule evaluations

#### 2. Repository Pattern
- **IRepository<T>**: Generic repository interface for data access abstraction
- **JobRepository**: Job-specific queries and operations
- **ExecutionRepository**: Execution history queries and analytics
- **Entity Framework Core**: ORM abstraction supporting multiple databases

#### 3. Service Layer

| Service | Responsibility |
|---------|-----------------|
| **JobSchedulerService** | Central orchestrator: job CRUD, schedule evaluation, queue management |
| **JobExecutorService** | Execute jobs with timeout, error handling, metrics collection |
| **CronExpressionService** | Parse POSIX cron expressions, calculate next execution times |
| **RetryService** | Manage retry logic with exponential/linear/fixed backoff |
| **ConcurrencyManager** | Enforce global and per-job concurrency limits |
| **ExecutionStatisticsService** | Compute success rates, performance metrics, trends |
| **PerformanceMonitor** | Track execution duration, memory usage, CPU impact |
| **AuditLogger** | Log all job state changes and executions |
| **CacheService** | Cache job configs and cron calculations |
| **WebhookNotificationService** | Post-execution HTTP webhooks |
| **SlackNotificationService** | Send Slack notifications for job events |

#### 4. API Controllers

- **JobsController**: CRUD operations for job definitions
- **ExecutionsController**: Query execution history and current status
- **DashboardController**: Aggregated metrics and system overview
- **HealthController**: Service health checks and diagnostics

## Installation

### Prerequisites

- **.NET 10.0** or later
- Database engine (SQL Server, PostgreSQL, MySQL, SQLite)
- (Optional) Docker and Docker Compose for containerized deployment

### Method 1: NuGet Package

```bash
dotnet add package Zaiets.dotnet.job.scheduler --version 2.0.2
```

### Method 2: From Source

```bash
git clone https://github.com/Sarmkadan/dotnet-job-scheduler.git
cd dotnet-job-scheduler
dotnet build
dotnet test
```

### Method 3: Docker

```bash
docker-compose up -d
# Service runs on http://localhost:5000
```

## Quick Start

### 1. Configure Services

```csharp
using JobScheduler.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Add job scheduler with configuration
services.AddJobScheduler(options =>
{
    options.ConnectionString = "Data Source=scheduler.db";
    options.MaxConcurrentJobs = 10;
    options.DefaultTimeoutSeconds = 300;
    options.DefaultMaxRetries = 3;
    options.DefaultRetryBackoffSeconds = 5;
    options.QueuePollIntervalMs = 5000;
    options.EnableCleanup = true;
    options.CleanupIntervalMs = 300000; // 5 minutes
});

var provider = services.BuildServiceProvider();
```

### 2. Initialize Database

```csharp
// Apply EF Core migrations
using var scope = provider.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<JobSchedulerContext>();
await context.Database.MigrateAsync();
```

### 3. Create and Schedule a Job

```csharp
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Services;
using JobScheduler.Core.Constants;

var schedulerService = provider.GetRequiredService<JobSchedulerService>();

var job = new Job
{
    Name = "DailyReport",
    Description = "Generates daily business report",
    CronExpression = "0 9 * * *",        // Every day at 9 AM
    HandlerType = "MyApp.Jobs.ReportHandler, MyApp",
    Priority = JobPriority.High,
    MaxRetries = 3,
    ExecutionTimeoutSeconds = 600,       // 10 minutes
    IsActive = true
};

var createdJob = await schedulerService.CreateJobAsync(job, "admin@example.com");
Console.WriteLine($"Created job with ID: {createdJob.Id}");
```

### 4. Execute Due Jobs (in background service or timer)

```csharp
// Call periodically (every 5-10 seconds in production)
var executions = await schedulerService.ExecuteDueJobsAsync();
Console.WriteLine($"Executed {executions.Count} jobs");
```

### 5. Monitor Job Status

```csharp
var stats = await schedulerService.GetSchedulerStatisticsAsync();
Console.WriteLine($"Total Jobs: {stats.TotalJobs}");
Console.WriteLine($"Running Now: {stats.RunningExecutions}");
Console.WriteLine($"Success Rate: {stats.AverageSuccessRate:F1}%");
Console.WriteLine($"Last 24h Executions: {stats.ExecutionsLast24Hours}");
```

## Configuration

### JobSchedulerSettings

```csharp
public class JobSchedulerSettings
{
    // Database connection string
    public string? ConnectionString { get; set; }

    // Maximum concurrent job executions (system-wide limit)
    // Default: 10
    public int MaxConcurrentJobs { get; set; } = 10;

    // Default execution timeout in seconds
    // Default: 300 (5 minutes)
    public int DefaultTimeoutSeconds { get; set; } = 300;

    // Default maximum retry attempts for failed jobs
    // Default: 3
    public int DefaultMaxRetries { get; set; } = 3;

    // Default initial backoff delay in seconds
    // Default: 5
    public int DefaultRetryBackoffSeconds { get; set; } = 5;

    // Poll interval for checking due jobs (milliseconds)
    // Default: 5000 (check every 5 seconds)
    public int QueuePollIntervalMs { get; set; } = 5000;

    // Enable automatic cleanup of old executions
    // Default: true
    public bool EnableCleanup { get; set; } = true;

    // How often to run cleanup (milliseconds)
    // Default: 300000 (5 minutes)
    public int CleanupIntervalMs { get; set; } = 300000;

    // How many days of execution history to keep
    // Default: 30
    public int ExecutionHistoryRetentionDays { get; set; } = 30;

    // Enable performance monitoring
    // Default: true
    public bool EnablePerformanceMonitoring { get; set; } = true;
}
```

### appsettings.json Example

```json
{
  "JobScheduler": {
    "ConnectionString": "Server=localhost;Database=JobScheduler;Trusted_Connection=true;",
    "MaxConcurrentJobs": 20,
    "DefaultTimeoutSeconds": 600,
    "DefaultMaxRetries": 5,
    "DefaultRetryBackoffSeconds": 10,
    "QueuePollIntervalMs": 3000,
    "EnableCleanup": true,
    "CleanupIntervalMs": 600000,
    "ExecutionHistoryRetentionDays": 60,
    "EnablePerformanceMonitoring": true
  }
}
```

## Usage Examples

### Example 1: Simple Scheduled Task

```csharp
// Send email report every Monday at 8 AM
var emailJob = new Job
{
    Name = "WeeklyEmailReport",
    CronExpression = "0 8 * * 1",  // Monday at 8 AM
    HandlerType = "MyApp.Jobs.EmailReportHandler, MyApp",
    Priority = JobPriority.Normal,
    MaxRetries = 2,
    ExecutionTimeoutSeconds = 120
};

var created = await schedulerService.CreateJobAsync(emailJob, "admin");
```

### Example 2: High-Priority Critical Job

```csharp
// Database cleanup - critical, high priority, frequent retries
var cleanupJob = new Job
{
    Name = "DatabaseMaintenance",
    Description = "Cleans up temporary data and optimizes indexes",
    CronExpression = "0 2 * * 0",  // Sunday at 2 AM
    HandlerType = "MyApp.Jobs.DatabaseMaintenanceHandler, MyApp",
    Priority = JobPriority.Critical,
    MaxRetries = 5,
    RetryBackoffSeconds = 30,
    ExecutionTimeoutSeconds = 1800,  // 30 minutes
    MaxConcurrentExecutions = 1  // Ensure only one runs at a time
};

var job = await schedulerService.CreateJobAsync(cleanupJob, "system");
```

### Example 3: Frequent Background Task

```csharp
// Process queue every 30 seconds
var queueJob = new Job
{
    Name = "ProcessQueue",
    CronExpression = "*/30 * * * * *",  // Every 30 seconds
    HandlerType = "MyApp.Jobs.QueueProcessorHandler, MyApp",
    Priority = JobPriority.High,
    MaxRetries = 1,  // Quick fail on error
    ExecutionTimeoutSeconds = 25
};

await schedulerService.CreateJobAsync(queueJob, "system");
```

### Example 4: Implementing Custom Job Handler

```csharp
using JobScheduler.Core.Domain.Entities;

public class EmailReportHandler : IJobHandler
{
    private readonly IEmailService _emailService;
    private readonly IReportGenerator _reportGenerator;

    public EmailReportHandler(IEmailService emailService, IReportGenerator reportGenerator)
    {
        _emailService = emailService;
        _reportGenerator = reportGenerator;
    }

    public async Task<string> ExecuteAsync(Job job, CancellationToken cancellationToken)
    {
        // Generate report
        var report = await _reportGenerator.GenerateAsync(
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow,
            cancellationToken
        );

        // Send email
        await _emailService.SendAsync(
            to: "team@example.com",
            subject: "Daily Report",
            body: report,
            cancellationToken: cancellationToken
        );

        return "Report sent successfully";
    }
}

// Register handler in DI
services.AddScoped<EmailReportHandler>();
```

### Example 5: Job with Retry Policy

```csharp
// External API call with exponential backoff retries
var apiJob = new Job
{
    Name = "SyncWithExternalAPI",
    Description = "Synchronizes data with partner API",
    CronExpression = "0 */6 * * *",  // Every 6 hours
    HandlerType = "MyApp.Jobs.ExternalApiSyncHandler, MyApp",
    Priority = JobPriority.Normal,
    MaxRetries = 5,
    RetryBackoffSeconds = 10,  // First retry: 10s, then 20s, 40s, 80s, 160s
    ExecutionTimeoutSeconds = 300
};

var job = await schedulerService.CreateJobAsync(apiJob, "admin");
```

### Example 6: Retrieving Job Execution History

```csharp
var jobId = 1;
var executions = await executionRepository
    .GetByJobIdAsync(jobId)
    .OrderByDescending(e => e.ExecutedAt)
    .Take(10)
    .ToListAsync();

foreach (var execution in executions)
{
    Console.WriteLine($"Status: {execution.Status}");
    Console.WriteLine($"Started: {execution.ExecutedAt}");
    Console.WriteLine($"Duration: {execution.Duration?.TotalSeconds:F2}s");
    Console.WriteLine($"Result: {execution.Result}");
    if (!string.IsNullOrEmpty(execution.ErrorMessage))
        Console.WriteLine($"Error: {execution.ErrorMessage}");
    Console.WriteLine("---");
}
```

### Example 7: Suspending and Resuming Jobs

```csharp
// Suspend job for maintenance
await schedulerService.SuspendJobAsync(jobId, "Investigating high failure rate");

// Resume job
await schedulerService.ResumeJobAsync(jobId);
```

### Example 8: Pagination and Filtering

```csharp
var query = new JobQuery
{
    Status = JobStatus.Scheduled,
    Priority = JobPriority.High,
    PageSize = 20,
    PageNumber = 1,
    SortBy = "CreatedAt",
    SortDescending = true
};

var page = await jobRepository.QueryAsync(query);
Console.WriteLine($"Total: {page.Total}");
foreach (var job in page.Items)
{
    Console.WriteLine($"- {job.Name}: {job.CronExpression}");
}
```

### Example 9: Performance Metrics

```csharp
var metrics = await executionRepository.GetMetricsAsync(
    jobId: null,  // All jobs
    startDate: DateTime.UtcNow.AddDays(-30),
    endDate: DateTime.UtcNow
);

Console.WriteLine($"Total Executions: {metrics.TotalExecutions}");
Console.WriteLine($"Successful: {metrics.SuccessfulCount}");
Console.WriteLine($"Failed: {metrics.FailedCount}");
Console.WriteLine($"Success Rate: {metrics.SuccessRate:F1}%");
Console.WriteLine($"Avg Duration: {metrics.AverageDurationMs:F0}ms");
Console.WriteLine($"Max Duration: {metrics.MaxDurationMs}ms");
```

### Example 10: Integration with ASP.NET Core

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddJobScheduler(options =>
{
    options.ConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.MaxConcurrentJobs = 15;
});

// Add background service to process jobs
builder.Services.AddHostedService<JobSchedulerBackgroundService>();

var app = builder.Build();

// Map job scheduler API endpoints
app.MapControllers();

await app.RunAsync();

// Background service
public class JobSchedulerBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<JobSchedulerBackgroundService> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            using var scope = _serviceProvider.CreateScope();
            var scheduler = scope.ServiceProvider.GetRequiredService<JobSchedulerService>();
            
            try
            {
                await scheduler.ExecuteDueJobsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing due jobs");
            }
        }
    }
}
```

## API Reference

### JobSchedulerService

```csharp
// Create job
Task<Job> CreateJobAsync(Job job, string createdBy);

// Update job
Task<Job> UpdateJobAsync(Job job, string modifiedBy);

// Delete job
Task DeleteJobAsync(int jobId, string deletedBy);

// Get job by ID
Task<Job?> GetJobByIdAsync(int jobId);

// Get all active jobs
Task<List<Job>> GetActiveJobsAsync();

// Execute all due jobs
Task<List<JobExecution>> ExecuteDueJobsAsync();

// Get scheduler statistics
Task<SchedulerStatistics> GetSchedulerStatisticsAsync();

// Suspend job
Task SuspendJobAsync(int jobId, string reason);

// Resume job
Task ResumeJobAsync(int jobId);
```

### Job Entity

```csharp
public class Job
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public string CronExpression { get; set; }
    public string HandlerType { get; set; }
    public JobStatus Status { get; set; }
    public JobPriority Priority { get; set; }
    public bool IsActive { get; set; }
    public int MaxRetries { get; set; }
    public int RetryBackoffSeconds { get; set; }
    public int ExecutionTimeoutSeconds { get; set; }
    public int MaxConcurrentExecutions { get; set; }
    public DateTime? NextExecutionTime { get; set; }
    public DateTime? LastExecutionTime { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}
```

### JobExecution Entity

```csharp
public class JobExecution
{
    public int Id { get; set; }
    public int JobId { get; set; }
    public ExecutionStatus Status { get; set; }
    public DateTime ExecutedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? Duration { get; set; }
    public string? Result { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryAttempt { get; set; }
    public string? ServerName { get; set; }
    public int? MemoryUsageMb { get; set; }
    public double? CpuUsagePercent { get; set; }
}
```

## Cron Expression Format

Standard POSIX cron format: `minute hour day month dayofweek`

### Field Values

| Field | Range | Example |
|-------|-------|---------|
| Minute | 0-59 | 30 (30th minute) |
| Hour | 0-23 | 9 (9 AM) |
| Day | 1-31 | 15 (15th day) |
| Month | 1-12 | 6 (June) |
| Weekday | 0-6 (0=Sunday) | 1 (Monday) |

### Special Characters

| Char | Meaning | Example |
|------|---------|---------|
| `*` | Any value | `*` (all hours) |
| `,` | Multiple values | `1,3,5` (1st, 3rd, 5th) |
| `-` | Range | `1-5` (1st through 5th) |
| `/` | Increment | `*/5` (every 5 units) |
| `?` | No specific value (day/weekday only) | `? * * * 1` |

### Common Expressions

```
0 0 * * *       →  Midnight daily
0 9 * * *       →  9 AM daily
0 9 * * 1-5     →  Weekdays 9 AM
0 9,13,17 * * * →  9 AM, 1 PM, 5 PM daily
0 */4 * * *     →  Every 4 hours
0 0 1 * *       →  First of month midnight
0 0 * * 0       →  Sunday midnight
*/15 * * * *    →  Every 15 minutes
0 0 1 1 *       →  January 1st midnight (annual)
```

## Job Lifecycle

```
┌─────────────┐
│   Created   │  (Initial state)
└──────┬──────┘
       │
       ▼
┌─────────────────────┐
│  Scheduled          │  (Awaiting next run time)
└──────┬──────┬───────┘
       │      │
       │      │  (Suspension)
       │      ▼
       │  ┌──────────┐
       │  │ Suspended│  (Paused by user)
       │  └────┬─────┘
       │       │ (Resume)
       │       ▼ (back to Scheduled)
       │
       ▼
┌──────────────────────────────────┐
│        Running                   │
│  (Execution in progress)         │
└──────┬─────────────────┬─────────┘
       │                 │
       │ (Success)       │ (Failure)
       │                 │
       ▼                 ▼
  ┌─────────────┐   ┌──────────────────┐
  │ Completed   │   │ Failed (Retrying)│
  └─────────────┘   └────────┬─────────┘
                             │
                    ┌────────▼──────────┐
                    │ Exceeded Max      │
                    │ Retries?          │
                    └────────┬──────────┘
                             │
                    ┌────────▼──────────────┐
                    │  FailedPermanently   │
                    └──────────────────────┘
```

### Status Codes

| Status | Description |
|--------|-------------|
| **Pending** | Job created, not yet scheduled |
| **Scheduled** | Waiting for next execution time |
| **Running** | Currently executing |
| **Completed** | Execution successful |
| **Failed** | Execution failed, eligible for retry |
| **FailedPermanently** | Max retries exceeded, no more attempts |
| **Suspended** | Paused by user, no automatic execution |
| **Cancelled** | Manually cancelled by user |

## Advanced Features

### Retry Strategies

#### Exponential Backoff (Default)
Delay doubles after each attempt: `base_delay * (2 ^ (attempt - 1))`

```csharp
job.MaxRetries = 5;
job.RetryBackoffSeconds = 5;
// Delays: 5s, 10s, 20s, 40s, 80s
```

#### Linear Backoff
Delay increases linearly: `base_delay * attempt`

```csharp
job.MaxRetries = 4;
job.RetryBackoffSeconds = 10;
// Delays: 10s, 20s, 30s, 40s
```

### Concurrency Control

```csharp
// Global limit
options.MaxConcurrentJobs = 10;

// Per-job limit
job.MaxConcurrentExecutions = 1;  // Never run in parallel
```

### Event Publishing

```csharp
// Subscribe to job events
services.AddScoped<JobEventHandler>();

public class JobEventHandler
{
    public void OnJobStarted(Job job) { }
    public void OnJobCompleted(Job job, JobExecution execution) { }
    public void OnJobFailed(Job job, JobExecution execution) { }
}
```

### Webhook Notifications

```csharp
// Configure webhook after job execution
var webhook = new JobWebhook
{
    JobId = jobId,
    WebhookUrl = "https://webhook.example.com/notifications",
    EventType = "Completed",
    Headers = new Dictionary<string, string>
    {
        { "Authorization", "Bearer token" }
    }
};

await webhookRepository.AddAsync(webhook);
```

### Performance Optimization

1. **Caching**: Job definitions cached after first load
2. **Batch Processing**: Multiple executions processed in batches
3. **Index Optimization**: Database indexes on frequently queried columns
4. **Connection Pooling**: EF Core connection pooling configured
5. **Async All the Way**: Fully async/await implementation

## Deployment

### Docker Deployment

```bash
docker-compose up -d

# Service available at http://localhost:5000
# Dashboard at http://localhost:5000/dashboard
```

### Production Checklist

- [ ] Set `MaxConcurrentJobs` based on system capacity
- [ ] Configure appropriate timeout values
- [ ] Enable database backups
- [ ] Set up monitoring and alerting
- [ ] Configure webhook endpoints
- [ ] Enable audit logging
- [ ] Set retention policies
- [ ] Monitor database growth
- [ ] Configure health checks

### Health Check Endpoint

```bash
curl http://localhost:5000/api/health

# Response:
{
  "status": "healthy",
  "uptime": "2 days 3 hours",
  "queuedJobs": 5,
  "runningJobs": 2,
  "failedRecently": 1
}
```

## Troubleshooting

### Jobs Not Executing

**Problem**: Jobs are created but never execute

**Solutions**:
1. Check job status: `status` should be `Scheduled`
2. Verify cron expression: Use online validator
3. Check `IsActive` flag: Must be `true`
4. Verify next execution time: `NextExecutionTime` should be in past
5. Check concurrency limits: No jobs blocked by limits
6. Review error logs: Check for exceptions

### High Memory Usage

**Problem**: Scheduler consuming excessive memory

**Solutions**:
1. Reduce `DefaultMaxRetries` to keep execution history smaller
2. Decrease `QueuePollIntervalMs` if too many jobs queued
3. Enable and tune cleanup: `CleanupIntervalMs`, `ExecutionHistoryRetentionDays`
4. Monitor active job count: May need to reduce `MaxConcurrentJobs`
5. Check for long-running jobs: May cause memory accumulation

### Timeouts Occurring Frequently

**Problem**: Jobs timing out unexpectedly

**Solutions**:
1. Increase `ExecutionTimeoutSeconds` for slow jobs
2. Reduce `MaxConcurrentJobs` to free system resources
3. Optimize job handler implementation
4. Check database performance: May need query optimization
5. Monitor CPU and memory during execution

### Database Connectivity Issues

**Problem**: Cannot connect to database

**Solutions**:
1. Verify connection string in `appsettings.json`
2. Ensure database server is running
3. Check firewall rules and network connectivity
4. Verify database credentials
5. Check EF Core migrations applied: `dotnet ef migrations list`

### Performance Degradation Over Time

**Problem**: Scheduler slowing down after running for days/weeks

**Solutions**:
1. Check execution history table size: May need cleanup
2. Run database maintenance: REINDEX, VACUUM for SQLite
3. Monitor database indices: Ensure they're not fragmented
4. Check for job handler memory leaks
5. Review connection pool settings
6. Monitor for blocked transactions

## Testing

```bash
# Run all tests
dotnet test

# Run with coverage report
dotnet test --collect:"XPlat Code Coverage"

# Run a specific test project
dotnet test tests/JobScheduler.Core.Tests/
```

### Writing Tests

```csharp
public class DailyReportHandlerTests
{
    [Fact]
    public async Task ExecuteAsync_SendsReport_WhenDataAvailable()
    {
        var handler = new DailyReportHandler(Mock.Of<IReportGenerator>(), Mock.Of<IEmailService>());
        var job = new Job { Name = "DailyReport", CronExpression = "0 9 * * *" };

        var result = await handler.ExecuteAsync(job, CancellationToken.None);

        Assert.Equal("Report sent successfully", result);
    }
}
```

## Performance

Benchmarks run with [BenchmarkDotNet](https://benchmarkdotnet.org) 0.14 on .NET 10, AMD64 3.4 GHz, 32 GB RAM, Release build.

### Cron Expression Evaluation

| Method | Mean | Error | StdDev | Allocated |
|---|--:|--:|--:|--:|
| `IsValidCronExpression` | 241 ns | 4.6 ns | 4.1 ns | 0 B |
| `GetNextExecutionTime` (cached schedule) | 314 ns | 5.8 ns | 5.4 ns | 48 B |
| `GetNextExecutionTime` (cold parse) | 14.3 μs | 0.27 μs | 0.25 μs | 2.1 KB |
| `GetNextExecutionTimes` (×10) | 3.1 μs | 58 ns | 54 ns | 576 B |
| `ShouldExecuteAt` | 362 ns | 6.9 ns | 6.4 ns | 48 B |

Repeated calls for the same expression are served from a static `ConcurrentDictionary` cache, cutting the cost from ~14 μs to ~314 ns per evaluation.

### String Processing

| Method | Mean | Error | StdDev | Allocated |
|---|--:|--:|--:|--:|
| `ToSlug` (short, 16 chars) | 181 ns | 3.3 ns | 2.9 ns | 88 B |
| `ToSlug` (complex, 42 chars) | 1.1 μs | 19 ns | 17 ns | 200 B |
| `ToSlug` (long, 128 chars) | 3.2 μs | 57 ns | 50 ns | 0 B |
| `JsonEscape` (no special chars) | 29 ns | 0.5 ns | 0.4 ns | 0 B |
| `JsonEscape` (with escapes) | 412 ns | 7.8 ns | 7.3 ns | 320 B |
| `Truncate` | 68 ns | 1.1 ns | 1.0 ns | 128 B |
| `Mask` | 112 ns | 2.1 ns | 1.9 ns | 72 B |

`ToSlug` uses a single-pass `Span<char>` loop with `stackalloc` (≤256 chars) or `ArrayPool<char>` (longer), eliminating the previous five-step LINQ/Replace chain. `JsonEscape` scans for special characters via `SearchValues<char>` and returns the original string unmodified when none are found — zero allocation on the happy path.

### CSV Processing

| Method | Mean | Error | StdDev | Allocated |
|---|--:|--:|--:|--:|
| `ParseCsvLine` (6 plain fields) | 524 ns | 9.9 ns | 9.3 ns | 312 B |
| `ParseCsvLine` (6 quoted fields) | 1.1 μs | 19 ns | 17 ns | 664 B |
| `ParseCsvLine` (10 plain fields) | 842 ns | 15 ns | 14 ns | 480 B |
| `EscapeCsvField` (no special chars) | 19 ns | 0.3 ns | 0.3 ns | 0 B |
| `EscapeCsvField` (with comma) | 391 ns | 7.2 ns | 6.4 ns | 256 B |
| `EscapeCsvField` (with quotes) | 448 ns | 8.4 ns | 7.5 ns | 288 B |
| `ParsePriority` (by name) | 20 ns | 0.3 ns | 0.3 ns | 0 B |

`ParseCsvLine` was rewritten to accumulate characters into a reused `StringBuilder` instead of repeated string concatenation, reducing allocations O(n²) → O(n). `EscapeCsvField` performs a single span scan to detect whether quoting is required — plain fields are returned as-is with no allocation.

### Running Benchmarks

```bash
cd benchmarks/dotnet-job-scheduler.Benchmarks
dotnet run -c Release
```

### Scaling Notes

- **Horizontal scaling**: Deploy multiple instances behind a load balancer; use a distributed lock (see [Related Projects](#related-projects)) to prevent duplicate execution across nodes.
- **Database**: PostgreSQL is recommended for production; the `NextExecutionTime` and `Status` columns are indexed for fast polling.
- **Concurrency**: Set `MaxConcurrentJobs` to roughly `CPU cores × 2` for I/O-bound handlers, or equal to `CPU cores` for CPU-bound work.

## Related Projects

- [dotnet-distributed-lock](https://github.com/sarmkadan/dotnet-distributed-lock) - Distributed locking library for .NET - Redis, SQLite, PostgreSQL backends with fencing tokens and auto-renewal
- [dotnet-event-bus](https://github.com/sarmkadan/dotnet-event-bus) - In-process and distributed event bus for .NET - pub/sub, request/reply, dead letter, polymorphic handlers

### Integration Examples

**Cluster-safe execution with dotnet-distributed-lock** — prevents the same job from firing on two nodes at once:

```csharp
public class DistributedJobHandler : IJobHandler
{
    private readonly IDistributedLock _lock;

    public DistributedJobHandler(IDistributedLock distributedLock) => _lock = distributedLock;

    public async Task<string> ExecuteAsync(Job job, CancellationToken ct)
    {
        await using var lease = await _lock.AcquireAsync($"job:{job.Id}", TimeSpan.FromMinutes(10), ct);
        return await RunCoreAsync(ct);
    }
}
```

**Event-driven notifications with dotnet-event-bus** — fan out job completion events to downstream consumers:

```csharp
public class ReportJobHandler : IJobHandler
{
    private readonly IEventBus _eventBus;

    public ReportJobHandler(IEventBus eventBus) => _eventBus = eventBus;

    public async Task<string> ExecuteAsync(Job job, CancellationToken ct)
    {
        var result = await GenerateReportAsync(ct);
        await _eventBus.PublishAsync(new JobCompletedEvent { JobId = job.Id, Result = result }, ct);
        return result;
    }
}
```

## Contributing

Contributions are welcome! Please follow these guidelines:

1. **Fork** the repository
2. **Create** a feature branch: `git checkout -b feature/my-feature`
3. **Commit** changes with clear messages: `git commit -am 'Add feature'`
4. **Push** to branch: `git push origin feature/my-feature`
5. **Submit** a Pull Request with description

### Development Setup

```bash
# Clone repository
git clone https://github.com/Sarmkadan/dotnet-job-scheduler.git
cd dotnet-job-scheduler

# Build
dotnet build

# Run tests
dotnet test

# Format code
dotnet format
```

### Code Standards

- Follow C# naming conventions (PascalCase for public members)
- Add XML documentation comments on public APIs
- Write unit tests for new features
- Keep methods focused and under 30 lines
- Use async/await consistently
- Validate input at boundaries

## Support

### Documentation
- [Getting Started Guide](docs/getting-started.md)
- [Architecture Documentation](docs/architecture.md)
- [API Reference](docs/api-reference.md)
- [Deployment Guide](docs/deployment.md)
- [FAQ](docs/faq.md)

### Community Support
- **Issues**: Report bugs on [GitHub Issues](https://github.com/Sarmkadan/dotnet-job-scheduler/issues)
- **Discussions**: Ask questions on [GitHub Discussions](https://github.com/Sarmkadan/dotnet-job-scheduler/discussions)

### Professional Services
For enterprise support, consulting, or custom development:
- Email: rutova2@gmail.com
- Website: https://sarmkadan.com

## License

MIT License - Copyright (c) 2026 Vladyslav Zaiets

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for version history and release notes.

---

**Built by [Vladyslav Zaiets](https://sarmkadan.com) - CTO & Software Architect**

[Portfolio](https://sarmkadan.com) | [GitHub](https://github.com/Sarmkadan) | [Telegram](https://t.me/sarmkadan)
