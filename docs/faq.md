# Frequently Asked Questions (FAQ)

Common questions and troubleshooting tips for dotnet-job-scheduler.

## Installation & Setup

### Q: What are the system requirements?
**A:** 
- .NET 10.0 or later
- Any EF Core-supported database (SQL Server, PostgreSQL, MySQL, SQLite)
- Minimum 256MB RAM, 500MB disk space
- 1+ CPU core (more for higher throughput)

### Q: Can I use it with .NET 8 or .NET 9?
**A:** No, the project requires .NET 10.0 or later. However, you could fork and modify to support earlier versions by changing the target framework in the `.csproj` files.

### Q: Which databases are supported?
**A:** Any database supported by Entity Framework Core:
- SQL Server (2016+)
- PostgreSQL (9.6+)
- MySQL (5.7+)
- SQLite (3.26+)
- Oracle (12.1+)
- MariaDB

### Q: Do I need to install a separate service?
**A:** No, it integrates directly into your .NET application. Add the NuGet package and register services in your dependency injection container.

### Q: Can I use this in a Console Application?
**A:** Yes! You need to set up a `ServiceCollection`, create the scheduler service, then manually call `ExecuteDueJobsAsync()` in a loop or timer.

---

## Job Scheduling

### Q: What's the cron expression format?
**A:** Standard POSIX format with 5 fields: `minute hour day month dayofweek`

Examples:
- `0 9 * * *` = 9 AM daily
- `*/15 * * * *` = Every 15 minutes
- `0 9 * * 1-5` = Weekdays at 9 AM

For detailed info, see [README.md](../README.md#cron-expression-format).

### Q: Can I use non-standard cron formats?
**A:** No, only standard POSIX cron (5 fields) is supported. If you need seconds precision, you can schedule more frequently (every minute) and check time in your handler.

### Q: How do I schedule a job to run every 30 seconds?
**A:** You can't directly with cron. Two options:
1. Schedule to run every minute and check elapsed time in your handler
2. Use `*/30 * * * *` which is "every 30 minutes" (not seconds)

### Q: What's the minimum interval between jobs?
**A:** 1 minute, due to POSIX cron format. For sub-minute intervals, implement a loop in your job handler.

### Q: Can I schedule a job with timezone support?
**A:** Yes! When creating a job, you can specify a `TimeZone` field:
```csharp
job.TimeZone = "Eastern Standard Time"; // Windows timezone
```

### Q: What happens if the server's clock changes?
**A:** The scheduler recalculates next execution times based on the new clock. No jobs are skipped or duplicated (thanks to database constraints).

---

## Job Execution

### Q: How do I create a custom job handler?
**A:** Implement `IJobHandler` interface:
```csharp
public class MyHandler : IJobHandler
{
    public async Task<string> ExecuteAsync(Job job, CancellationToken token)
    {
        // Your code here
        return "Success message";
    }
}
```

Register it: `services.AddScoped<MyHandler>();`

### Q: Can I pass custom parameters to my job handler?
**A:** Not directly via the job definition. Options:
1. Store parameters in a config file
2. Use the `Job.Description` field for simple parameters
3. Query a database for job-specific configuration
4. Use environment variables

### Q: How do I handle job dependencies (Job A before Job B)?
**A:** The scheduler doesn't support direct dependencies. Implement workflow logic:

1. **Option 1**: Have Job A set a flag when complete, Job B checks the flag
2. **Option 2**: Job A calls Job B's handler directly at the end
3. **Option 3**: Use an external workflow engine (Azure Logic Apps, n8n, etc.)

### Q: Can I manually trigger a job outside its schedule?
**A:** Yes! Call:
```csharp
var execution = await schedulerService.ExecuteJobNowAsync(jobId);
```

Or via API:
```bash
POST /api/jobs/{jobId}/execute-now
```

### Q: What happens if a job takes longer than the timeout?
**A:** The job is forcefully cancelled via `CancellationToken`. The execution is marked as `Timeout` status, and if retries remain, it's queued for retry.

### Q: Can I interrupt a running job?
**A:** Yes, through the cancellation token passed to your handler. Respond to `cancellationToken.ThrowIfCancellationRequested()` or check `IsCancellationRequested`.

### Q: How do I log job execution details?
**A:** Inject `ILogger<T>`:
```csharp
public class MyHandler : IJobHandler
{
    private readonly ILogger<MyHandler> _logger;

    public MyHandler(ILogger<MyHandler> logger) => _logger = logger;

    public async Task<string> ExecuteAsync(Job job, CancellationToken token)
    {
        _logger.LogInformation("Job {Name} started", job.Name);
        // ... execute ...
        _logger.LogInformation("Job {Name} completed", job.Name);
        return "Done";
    }
}
```

---

## Concurrency & Performance

### Q: What does `MaxConcurrentJobs` mean?
**A:** Maximum number of jobs executing simultaneously across the entire system. If limit is reached, additional jobs wait in queue.

Set based on your system resources:
- Small app: 5-10
- Medium app: 10-20
- Large app: 30-50+

### Q: Can I limit concurrency per job?
**A:** Yes! Set `Job.MaxConcurrentExecutions`:
- `1` = Only one instance of this job can run at a time
- `5` = Up to 5 instances can run simultaneously (even if global limit is 10)

### Q: How do I prevent duplicate executions?
**A:** The database prevents this:
- Primary key on `(JobId, ExecutedAt)` ensures uniqueness
- Distributed locking ensures only one instance acquires execution slot
- Status transitions are atomic

### Q: Will multiple scheduler instances cause duplicate executions?
**A:** No! Database constraints and locking prevent this. Each instance can safely run alongside others.

### Q: How do I monitor performance?
**A:** Use the statistics endpoint:
```csharp
var stats = await schedulerService.GetSchedulerStatisticsAsync();
Console.WriteLine($"Success rate: {stats.AverageSuccessRate}%");
Console.WriteLine($"Avg execution: {stats.AverageExecutionTimeMs}ms");
```

Or check the `/api/dashboard/metrics` REST endpoint.

---

## Retry & Error Handling

### Q: How do retry attempts work?
**A:** On failure:
1. Check if `RetryAttempt < MaxRetries`
2. If yes: Calculate backoff delay and schedule retry
3. If no: Mark as `FailedPermanently`

### Q: What retry strategies are available?
**A:** Three backoff strategies:
- **Exponential** (default): delay * 2^attempt
- **Linear**: delay * attempt
- **Fixed**: always same delay

Configure via `Job.RetryBackoffType`.

### Q: Can I disable retries?
**A:** Yes, set `MaxRetries = 0`. Job will not retry on failure.

### Q: How do I handle transient errors differently?
**A:** In your handler, catch transient errors and throw a custom exception. Update your retry configuration based on error type.

### Q: What happens to failed jobs?
**A:** They're marked as `Failed` (if retries available) or `FailedPermanently` (if retries exhausted). They appear in the execution history with error message and stack trace.

### Q: How long are failed jobs kept?
**A:** Configurable via `ExecutionHistoryRetentionDays` (default 30). Old records are automatically cleaned up.

---

## Database & Storage

### Q: How much storage do I need?
**A:** Depends on job volume:
- 100 jobs, 1 execution/day: ~100KB/month
- 1000 jobs, 5 executions/day: ~5MB/month
- 10000 jobs, 10 executions/day: ~50MB/month

Adjust `ExecutionHistoryRetentionDays` to manage storage.

### Q: Can I migrate between databases?
**A:** Yes! EF Core migrations support this:

1. Generate migration script for source
2. Adapt script for target database syntax
3. Apply to new database
4. Update connection string

### Q: How do I backup my jobs?
**A:** Jobs are stored in the database. Use standard database backup tools:
- SQL Server: `BACKUP DATABASE`
- PostgreSQL: `pg_dump`
- SQLite: Copy `.db` file

### Q: What if the database grows too large?
**A:** Enable and configure cleanup:
```csharp
options.EnableCleanup = true;
options.CleanupIntervalMs = 300000;  // 5 minutes
options.ExecutionHistoryRetentionDays = 30;  // Keep 30 days
```

Or manually:
```sql
DELETE FROM JobExecutions 
WHERE ExecutedAt < DATEADD(DAY, -30, GETUTCDATE())
```

### Q: Can I export job history?
**A:** Yes! Query the database and export:
```csharp
var executions = dbContext.JobExecutions
    .Where(e => e.JobId == jobId)
    .ToList();

// Export as CSV, JSON, Excel, etc.
```

Or use the `/api/jobs/{jobId}/executions` API endpoint with pagination.

---

## Monitoring & Debugging

### Q: How do I check if the scheduler is running?
**A:** Call the health endpoint:
```bash
curl http://localhost:5000/api/health
```

Or check the service status (Windows/Linux).

### Q: Why are jobs not executing?
**A:** Check in order:
1. Is the scheduler service running?
2. Is the job's `IsActive` flag true?
3. Is the job's `Status` = `Scheduled`?
4. Is `NextExecutionTime` <= now?
5. Are concurrency limits exceeded? Check `Status` = `Pending` in database
6. Review application logs for exceptions

### Q: How do I enable debug logging?
**A:** Set log level to `Debug`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  }
}
```

Or programmatically:
```csharp
builder.Logging.SetMinimumLevel(LogLevel.Debug);
```

### Q: Where are logs stored?
**A:** By default, console output. Configure Serilog for file logging:
```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.File("logs/scheduler-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
```

### Q: How do I monitor database queries?
**A:** Enable EF Core logging:
```csharp
optionsBuilder.LogTo(Console.WriteLine, LogLevel.Information);
```

### Q: Can I get performance metrics?
**A:** Yes! Use:
```csharp
var metrics = await statisticsService.GetPerformanceAsync(jobId: null);
Console.WriteLine($"Avg duration: {metrics.AverageDurationMs}ms");
```

Or REST API: `GET /api/dashboard/metrics`

---

## Integration & Webhooks

### Q: Can I integrate with external systems?
**A:** Yes! Multiple ways:
1. **Webhooks**: Configure URL to post job results
2. **Message Queue**: Have handler publish to Azure Service Bus, RabbitMQ, etc.
3. **REST API**: Call external services from handler
4. **Events**: Subscribe to job completion events

### Q: How do webhooks work?
**A:** After job completes, the scheduler POSTs execution details:
```json
{
  "jobId": 1,
  "jobName": "DailyReport",
  "status": "Completed",
  "executedAt": "2026-05-04T09:00:00Z",
  "duration": "00:05:32",
  "result": "Report sent"
}
```

Configure via:
```csharp
var webhook = new JobWebhook
{
    JobId = 1,
    WebhookUrl = "https://myapp.example.com/notifications",
    EventType = "Completed"
};
```

### Q: Can I send Slack notifications?
**A:** Yes! The `SlackNotificationService` integrates with Slack:
```csharp
var service = scope.ServiceProvider.GetRequiredService<SlackNotificationService>();
await service.SendAsync(jobId, "#alerts", "Job completed!");
```

### Q: How do I handle failures from external calls?
**A:** Implement error handling in your handler:
```csharp
public async Task<string> ExecuteAsync(Job job, CancellationToken token)
{
    try
    {
        var result = await _externalApi.CallAsync(token);
        return "Success";
    }
    catch (HttpRequestException ex)
    {
        // Transient error - will retry
        throw;
    }
}
```

---

## Scalability & Multi-Instance

### Q: Can I run multiple scheduler instances?
**A:** Yes! They coordinate automatically via the database. Each instance can safely execute different jobs simultaneously.

### Q: How do multiple instances coordinate?
**A:** Through database locks and constraints:
- Row-level locking prevents duplicate execution
- Status field synchronization
- Distributed lease algorithm (future version)

### Q: Which instance executes a job?
**A:** Whichever instance polls the database first. No specific assignment.

### Q: How do I ensure specific jobs run on specific instances?
**A:** (Future feature) For now, partition jobs by instance:
- Instance 1 handles jobs with even IDs
- Instance 2 handles jobs with odd IDs

Or implement job affinity in a custom scheduler.

### Q: What happens if one instance crashes?
**A:** Its jobs are picked up by other instances. No jobs are lost. Status updates might be delayed if database connection is lost.

---

## Advanced Usage

### Q: How do I implement custom retry logic?
**A:** Create a custom retry service:
```csharp
public class CustomRetryService : RetryService
{
    public override TimeSpan CalculateBackoff(Job job, int attempt, BackoffStrategy strategy)
    {
        // Custom logic
        return base.CalculateBackoff(job, attempt, strategy);
    }
}
```

### Q: Can I store job results?
**A:** Yes! Your handler returns a string result:
```csharp
return "Processed 1000 records, 50 errors"; // Stored in JobExecution.Result
```

Query it:
```csharp
var execution = await repository.GetByIdAsync(executionId);
var result = execution.Result; // "Processed 1000 records..."
```

### Q: How do I implement job templating?
**A:** Store template parameters in `Job.Description` or external config:
```csharp
job.Description = JsonConvert.SerializeObject(new { 
    email = "admin@example.com",
    reportFormat = "pdf"
});

// In handler
var config = JsonConvert.DeserializeObject<JobConfig>(job.Description);
```

### Q: Can I pause the entire scheduler?
**A:** Yes! Set all jobs to `IsActive = false`:
```csharp
var jobs = await jobRepository.GetAllAsync();
foreach (var job in jobs)
{
    job.IsActive = false;
}
await context.SaveChangesAsync();
```

Or suspend jobs individually.

### Q: How do I implement custom job handlers dynamically?
**A:** Use reflection to load handlers:
```csharp
var type = Type.GetType(job.HandlerType);
var handler = ActivatorUtilities.CreateInstance(provider, type);
var result = await ((IJobHandler)handler).ExecuteAsync(job, token);
```

---

## Common Issues & Solutions

### Q: "Database connection timeout"
**A:**
- Check connection string
- Verify database is running and accessible
- Check firewall rules
- Verify username/password
- Increase timeout in connection string: `Connection Timeout=30`

### Q: "Handler type not found"
**A:**
- Verify fully qualified name: `Namespace.ClassName, AssemblyName`
- Ensure handler implements `IJobHandler`
- Confirm handler is registered in DI
- Check for typos in type name

### Q: "Jobs accumulating in queue"
**A:**
- Increase `MaxConcurrentJobs`
- Reduce execution timeout if jobs are hanging
- Optimize job handler code
- Check database performance
- Monitor server resources (CPU, memory, disk I/O)

### Q: "High memory usage"
**A:**
- Reduce `MaxConcurrentJobs`
- Lower `ExecutionHistoryRetentionDays`
- Enable cleanup and reduce `CleanupIntervalMs`
- Check job handlers for memory leaks
- Monitor with profiler

### Q: "Slow query performance"
**A:**
- Add indices on frequently queried columns
- Analyze query execution plans
- Reduce page size for large result sets
- Archive old execution data
- Monitor with database profiler

---

## Getting Help

Still have questions? Check:
- [README.md](../README.md) - Overview and quick start
- [Getting Started Guide](getting-started.md) - Step-by-step setup
- [Architecture Guide](architecture.md) - Design and internals
- [API Reference](api-reference.md) - All endpoints and methods
- [Deployment Guide](deployment.md) - Production setup
- [GitHub Issues](https://github.com/Sarmkadan/dotnet-job-scheduler/issues) - Known issues
- Email: rutova2@gmail.com

Happy scheduling! 🚀
