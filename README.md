# dotnet-job-scheduler

A production-grade, distributed job scheduler for .NET with support for cron expressions, priority queues, automatic retries, concurrency control, and comprehensive job execution metrics.

## Features

- **CRON Scheduling**: Full POSIX cron expression support for flexible job scheduling
- **Priority Queues**: Execute jobs based on priority levels (Low, Normal, High, Critical)
- **Automatic Retries**: Configurable retry policies with exponential, linear, or fixed backoff strategies
- **Concurrency Control**: Global and per-job concurrency limits to prevent system overload
- **Job Execution Metrics**: Detailed tracking of execution history, success rates, and performance
- **Flexible Job Handlers**: Support for custom job handler implementations
- **Execution History**: Complete audit trail of all job executions with error details
- **Status Management**: Job lifecycle management (Scheduled, Running, Failed, Suspended, etc.)
- **Database Agnostic**: Entity Framework Core abstraction supports any EF Core database
- **Dependency Injection**: Built-in Microsoft DI integration for easy integration

## Architecture

### Core Components

- **Domain Models**: Rich domain entities representing jobs, executions, and schedules
- **Repository Pattern**: Data access abstraction with Entity Framework Core
- **Service Layer**: Business logic for scheduling, execution, and retry handling
- **Configuration**: Dependency injection setup and configuration management

### Key Services

1. **JobSchedulerService**: Central orchestrator for job management
2. **JobExecutorService**: Handles actual job execution with timeout and error management
3. **CronExpressionService**: Parses and evaluates cron expressions
4. **RetryService**: Manages retry logic and backoff strategies
5. **ConcurrencyManager**: Enforces concurrency limits and prevents overload

## Installation

### Prerequisites

- .NET 10.0 or later
- SQLite or other EF Core-supported database

### Package Installation

```bash
dotnet add package DotNet.JobScheduler.Core
```

## Getting Started

### 1. Configure Services

```csharp
var services = new ServiceCollection();

services.AddJobScheduler(options =>
{
    options.ConnectionString = "Data Source=scheduler.db";
    options.MaxConcurrentJobs = 10;
    options.DefaultTimeoutSeconds = 300;
    options.DefaultMaxRetries = 3;
});

var provider = services.BuildServiceProvider();
```

### 2. Initialize Database

```csharp
await provider.InitializeDatabaseAsync();
provider.ValidateSchedulerConfiguration();
```

### 3. Create and Schedule a Job

```csharp
var schedulerService = provider.GetRequiredService<JobSchedulerService>();

var job = new Job
{
    Name = "DailyReport",
    Description = "Generates daily report",
    CronExpression = "0 9 * * *", // 9 AM daily
    HandlerType = "MyApp.Jobs.ReportHandler, MyApp",
    Priority = JobPriority.High,
    MaxRetries = 3,
    ExecutionTimeoutSeconds = 600
};

var createdJob = await schedulerService.CreateJobAsync(job, "admin");
```

### 4. Execute Due Jobs

```csharp
var executions = await schedulerService.ExecuteDueJobsAsync();
```

## Cron Expression Format

Uses standard POSIX cron format: `minute hour day month dayofweek`

### Examples

| Expression | Description |
|-----------|-------------|
| `0 0 * * *` | Daily at midnight |
| `0 * * * *` | Every hour |
| `* * * * *` | Every minute |
| `0 9 * * 1-5` | Weekdays at 9 AM |
| `30 2 * * 0` | Sunday at 2:30 AM |
| `0 0 1 * *` | First of month at midnight |

## Job Retry Strategies

### Exponential Backoff (Default)

Delay increases exponentially: `initial_delay * (2 ^ (attempt - 1))`

```csharp
job.MaxRetries = 5;
job.RetryBackoffSeconds = 5;
// Attempts at: 5s, 10s, 20s, 40s, 80s
```

### Linear Backoff

Delay increases linearly: `initial_delay * attempt`

### Fixed Backoff

Same delay between all retries

## Job Execution Lifecycle

```
Pending → Scheduled → Running → Completed
                         ↓
                      Failed → Retrying → Completed
                         ↓
                   FailedPermanently
```

## Status Codes

- **Pending**: Job created but not yet scheduled
- **Scheduled**: Waiting for next execution time
- **Running**: Currently executing
- **Completed**: Execution finished successfully
- **Failed**: Execution failed, eligible for retry
- **FailedPermanently**: Max retries exceeded
- **Suspended**: Paused by user
- **Cancelled**: Manually cancelled

## Configuration Options

```csharp
public class JobSchedulerOptions
{
    public string? ConnectionString { get; set; }
    public int MaxConcurrentJobs { get; set; } = 10;
    public int DefaultTimeoutSeconds { get; set; } = 300;
    public int DefaultMaxRetries { get; set; } = 3;
    public int DefaultRetryBackoffSeconds { get; set; } = 5;
    public int QueuePollIntervalMs { get; set; } = 5000;
    public bool EnableCleanup { get; set; } = true;
    public int CleanupIntervalMs { get; set; } = 300000;
}
```

## Advanced Usage

### Custom Job Handlers

Implement `IJobHandler` interface:

```csharp
public interface IJobHandler
{
    Task<string> ExecuteAsync(Job job, CancellationToken cancellationToken);
}
```

### Monitoring and Metrics

```csharp
var stats = await schedulerService.GetSchedulerStatisticsAsync();
Console.WriteLine($"Running: {stats.RunningExecutions}/{stats.TotalJobs}");
Console.WriteLine($"Success Rate: {stats.AverageSuccessRate:F1}%");
```

### Job Suspension and Resumption

```csharp
// Suspend
await schedulerService.SuspendJobAsync(jobId, "Investigating issue");

// Resume
await schedulerService.ResumeJobAsync(jobId);
```

## Testing

Run the test suite:

```bash
dotnet test tests/JobScheduler.Core.Tests/
```

## Performance Considerations

- Adjust `MaxConcurrentJobs` based on system resources
- Set appropriate `ExecutionTimeoutSeconds` to prevent resource exhaustion
- Monitor execution times and adjust `QueuePollIntervalMs` accordingly
- Enable cleanup to prevent database from growing indefinitely

## Troubleshooting

### Jobs Not Executing

1. Check job status is `Scheduled` or `Running`
2. Verify cron expression is valid
3. Ensure next execution time is in the past
4. Check concurrency limits aren't exceeded

### High Memory Usage

- Reduce `DefaultMaxRetries`
- Decrease `QueuePollIntervalMs`
- Enable cleanup and reduce `CleanupIntervalMs`

### Timeouts Occurring

- Increase `ExecutionTimeoutSeconds`
- Reduce `MaxConcurrentJobs` if under resource pressure
- Optimize job handler implementation

## License

MIT License - Copyright (c) 2026 Vladyslav Zaiets

See LICENSE file for details.

## Contributing

Contributions are welcome! Please follow these guidelines:

1. Fork the repository
2. Create a feature branch
3. Submit a pull request with clear description

## Support

For issues and questions:
- Check existing issues
- Review documentation
- Submit new issues with reproduction steps

## Contact

- Website: https://sarmkadan.com
- Email: rutova2@gmail.com
