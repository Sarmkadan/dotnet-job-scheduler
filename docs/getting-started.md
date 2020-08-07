# Getting Started with dotnet-job-scheduler

This guide will help you set up and run your first scheduled job in 15 minutes.

## Installation

### Step 1: Install NuGet Package

```bash
dotnet add package DotNet.JobScheduler.Core
```

Or add to your `.csproj`:

```xml
<ItemGroup>
    <PackageReference Include="DotNet.JobScheduler.Core" Version="1.2.0" />
</ItemGroup>
```

### Step 2: Create Database

The scheduler stores job definitions and execution history in a database. Choose your database:

#### Option A: SQLite (Development)

```csharp
var connectionString = "Data Source=scheduler.db";
```

#### Option B: SQL Server

```csharp
var connectionString = "Server=localhost;Database=JobScheduler;Integrated Security=true;";
```

#### Option C: PostgreSQL

```csharp
var connectionString = "Host=localhost;Database=job_scheduler;Username=postgres;Password=password;";
```

## Basic Setup

### 1. Create Your Job Handler

Jobs are executed by handler classes that implement `IJobHandler`:

```csharp
using JobScheduler.Core.Domain.Entities;

public class HelloWorldHandler : IJobHandler
{
    private readonly ILogger<HelloWorldHandler> _logger;

    public HelloWorldHandler(ILogger<HelloWorldHandler> logger)
    {
        _logger = logger;
    }

    public async Task<string> ExecuteAsync(Job job, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Hello World job executed at {Time}", DateTime.UtcNow);
        await Task.Delay(100, cancellationToken);  // Simulate work
        return "Hello World execution completed";
    }
}
```

### 2. Configure Services

In your `Program.cs`:

```csharp
using JobScheduler.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplicationBuilder.CreateBuilder(args);

// Add job scheduler
builder.Services.AddJobScheduler(options =>
{
    options.ConnectionString = "Data Source=scheduler.db";
    options.MaxConcurrentJobs = 5;
    options.DefaultTimeoutSeconds = 300;
    options.DefaultMaxRetries = 2;
});

// Register job handlers
builder.Services.AddScoped<HelloWorldHandler>();

// Add background service to execute jobs
builder.Services.AddHostedService<JobSchedulerBackgroundService>();

var app = builder.Build();

// Map API controllers
app.MapControllers();

await app.RunAsync();
```

### 3. Create Background Service

```csharp
using JobScheduler.Core.Services;

public class JobSchedulerBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<JobSchedulerBackgroundService> _logger;

    public JobSchedulerBackgroundService(IServiceProvider serviceProvider, 
        ILogger<JobSchedulerBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Job Scheduler background service starting");

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            using var scope = _serviceProvider.CreateScope();
            var scheduler = scope.ServiceProvider.GetRequiredService<JobSchedulerService>();

            try
            {
                var executions = await scheduler.ExecuteDueJobsAsync();
                if (executions.Any())
                {
                    _logger.LogInformation("Executed {Count} jobs", executions.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing due jobs");
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Job Scheduler background service stopping");
        await base.StopAsync(cancellationToken);
    }
}
```

## Creating Your First Job

### Option 1: Programmatic Creation

```csharp
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Constants;

var schedulerService = app.Services.GetRequiredService<JobSchedulerService>();

var job = new Job
{
    Name = "HelloWorld",
    Description = "First scheduled job",
    CronExpression = "* * * * *",  // Every minute
    HandlerType = typeof(HelloWorldHandler).FullName!,
    Priority = JobPriority.Normal,
    IsActive = true,
    MaxRetries = 1,
    ExecutionTimeoutSeconds = 60
};

var created = await schedulerService.CreateJobAsync(job, "system");
Console.WriteLine($"Job created with ID: {created.Id}");
```

### Option 2: API Call

```bash
curl -X POST http://localhost:5000/api/jobs \
  -H "Content-Type: application/json" \
  -d '{
    "name": "HelloWorld",
    "description": "First scheduled job",
    "cronExpression": "* * * * *",
    "handlerType": "YourNamespace.HelloWorldHandler, YourAssembly",
    "priority": 1,
    "isActive": true,
    "maxRetries": 1,
    "executionTimeoutSeconds": 60
  }'
```

## Monitoring Jobs

### Check Job Status

```csharp
var job = await schedulerService.GetJobByIdAsync(1);
Console.WriteLine($"Name: {job.Name}");
Console.WriteLine($"Status: {job.Status}");
Console.WriteLine($"Next Execution: {job.NextExecutionTime}");
```

### View Execution History

```csharp
var repository = app.Services.GetRequiredService<IExecutionRepository>();

var executions = await repository
    .GetByJobIdAsync(1)
    .OrderByDescending(e => e.ExecutedAt)
    .Take(10)
    .ToListAsync();

foreach (var execution in executions)
{
    Console.WriteLine($"[{execution.ExecutedAt:yyyy-MM-dd HH:mm:ss}] {execution.Status}");
    if (execution.Duration.HasValue)
        Console.WriteLine($"  Duration: {execution.Duration.Value.TotalSeconds:F2}s");
    if (!string.IsNullOrEmpty(execution.ErrorMessage))
        Console.WriteLine($"  Error: {execution.ErrorMessage}");
}
```

### Get Statistics

```csharp
var stats = await schedulerService.GetSchedulerStatisticsAsync();
Console.WriteLine($"Total Jobs: {stats.TotalJobs}");
Console.WriteLine($"Active Jobs: {stats.ActiveJobs}");
Console.WriteLine($"Running Now: {stats.RunningExecutions}");
Console.WriteLine($"Success Rate: {stats.AverageSuccessRate:F1}%");
Console.WriteLine($"Executions (24h): {stats.ExecutionsLast24Hours}");
```

## Common Cron Expressions

| Schedule | Expression |
|----------|------------|
| Every minute | `* * * * *` |
| Every 5 minutes | `*/5 * * * *` |
| Every hour | `0 * * * *` |
| Every day at 9 AM | `0 9 * * *` |
| Every weekday at 9 AM | `0 9 * * 1-5` |
| Every Monday | `0 0 * * 1` |
| First day of month | `0 0 1 * *` |
| New Year's Day | `0 0 1 1 *` |

## Troubleshooting

### Database Connection Error

**Error**: `Could not connect to database`

**Solution**:
1. Verify connection string in configuration
2. Ensure database server is running
3. Check username/password credentials
4. Verify network connectivity

### Handler Type Not Found

**Error**: `Could not find handler type`

**Solution**:
1. Ensure handler class implements `IJobHandler`
2. Use fully qualified type name: `Namespace.ClassName, AssemblyName`
3. Register handler in DI: `services.AddScoped<YourHandler>()`

### Jobs Not Executing

**Error**: Job created but never runs

**Solutions**:
1. Check job is active: `IsActive = true`
2. Verify cron expression is valid
3. Ensure background service is running
4. Check job status: Should be `Scheduled`
5. Review application logs for errors

### Execution Timeout

**Error**: Job execution timeout

**Solutions**:
1. Increase `ExecutionTimeoutSeconds`
2. Optimize job handler code
3. Reduce `MaxConcurrentJobs` if resource-constrained
4. Check if job handler is blocking

## Next Steps

- Read the [Architecture Guide](architecture.md) to understand design
- Review [API Reference](api-reference.md) for all available methods
- Check [Deployment Guide](deployment.md) for production setup
- Browse [examples/](../examples/) directory for complete applications

## Getting Help

- Check [FAQ](faq.md) for common questions
- Review application logs for error messages
- Check GitHub Issues for known problems
- Email: rutova2@gmail.com

## Performance Tips

1. **Adjust Poll Interval**: `QueuePollIntervalMs` controls how often jobs are checked
   - Lower = More responsive but higher CPU usage
   - Default 5000ms is suitable for most applications

2. **Tune Concurrency**: `MaxConcurrentJobs` should match system capacity
   - Higher = More parallelism but more resource usage
   - Default 10 is good for small/medium applications

3. **Monitor Execution Time**: Long-running jobs impact queue responsiveness
   - Consider breaking large jobs into smaller tasks
   - Or increase poll interval for batch-heavy workloads

4. **Database Performance**:
   - Use connection pooling (EF Core default)
   - Index frequently queried columns
   - Archive old execution history regularly

## What's Next?

- Create more complex job handlers
- Set up monitoring and alerting
- Configure webhook notifications
- Integrate with your business workflows
- Deploy to production (see [Deployment Guide](deployment.md))
