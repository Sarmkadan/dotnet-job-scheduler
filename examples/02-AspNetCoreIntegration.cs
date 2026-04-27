#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using JobScheduler.Core.Configuration;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Constants;
using JobScheduler.Core.Services;

/// <summary>
/// Example 2: ASP.NET Core Integration
///
/// Demonstrates how to integrate dotnet-job-scheduler into an ASP.NET Core
/// application with background service for job execution.
/// </summary>

public sealed class EmailSendingJobHandler : IJobHandler
{
    private readonly ILogger<EmailSendingJobHandler> _logger;

    public EmailSendingJobHandler(ILogger<EmailSendingJobHandler> logger)
    {
        _logger = logger;
    }

    public async Task<string> ExecuteAsync(Job job, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sending emails...");

        // Simulate email sending
        var emailCount = 5;
        for (int i = 0; i < emailCount; i++)
        {
            await Task.Delay(50, cancellationToken); // Simulate email send
            _logger.LogInformation("Email {Number} sent", i + 1);
        }

        return $"Successfully sent {emailCount} emails";
    }
}

public sealed class DataCleanupJobHandler : IJobHandler
{
    private readonly ILogger<DataCleanupJobHandler> _logger;

    public DataCleanupJobHandler(ILogger<DataCleanupJobHandler> logger)
    {
        _logger = logger;
    }

    public async Task<string> ExecuteAsync(Job job, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting data cleanup...");

        // Simulate cleanup tasks
        await Task.Delay(100, cancellationToken);
        var recordsDeleted = 1250;

        _logger.LogInformation("Cleanup completed. Deleted {Count} old records", recordsDeleted);
        return $"Deleted {recordsDeleted} old records";
    }
}

public sealed class JobSchedulerBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<JobSchedulerBackgroundService> _logger;

    /// Executes scheduled jobs at regular intervals
    public JobSchedulerBackgroundService(IServiceProvider serviceProvider,
        ILogger<JobSchedulerBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Job Scheduler background service started");

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

        try
        {
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
        finally
        {
            timer.Dispose();
        }

        _logger.LogInformation("Job Scheduler background service stopped");
    }
}

public sealed class AspNetCoreJobSchedulerExample
{
    /// Sets up and runs the ASP.NET Core example
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Information);
            logging.AddConsole();
        });

        builder.Services.AddJobScheduler(options =>
        {
            options.ConnectionString = "Data Source=scheduler.db";
            options.MaxConcurrentJobs = 10;
            options.DefaultTimeoutSeconds = 300;
            options.DefaultMaxRetries = 2;
        });

        builder.Services.AddScoped<EmailSendingJobHandler>();
        builder.Services.AddScoped<DataCleanupJobHandler>();

        builder.Services.AddHostedService<JobSchedulerBackgroundService>();

        var app = builder.Build();

        app.MapGet("/", () => "Job Scheduler is running");

        app.MapGet("/api/jobs/list", async (JobSchedulerService scheduler) =>
        {
            var jobs = await scheduler.GetActiveJobsAsync();
            return Results.Ok(jobs);
        });

        app.MapPost("/api/jobs/create-email-job", async (JobSchedulerService scheduler) =>
        {
            var job = new Job
            {
                Name = $"EmailJob_{Guid.NewGuid().ToString().Substring(0, 8)}",
                Description = "Send emails daily",
                CronExpression = "0 9 * * *",
                HandlerType = typeof(EmailSendingJobHandler).FullName!,
                Priority = JobPriority.High,
                IsActive = true,
                MaxRetries = 3,
                ExecutionTimeoutSeconds = 600
            };

            var created = await scheduler.CreateJobAsync(job, "api");
            return Results.Created($"/api/jobs/{created.Id}", created);
        });

        app.MapPost("/api/jobs/create-cleanup-job", async (JobSchedulerService scheduler) =>
        {
            var job = new Job
            {
                Name = $"CleanupJob_{Guid.NewGuid().ToString().Substring(0, 8)}",
                Description = "Clean old data weekly",
                CronExpression = "0 2 * * 0",
                HandlerType = typeof(DataCleanupJobHandler).FullName!,
                Priority = JobPriority.Normal,
                IsActive = true,
                MaxRetries = 1,
                ExecutionTimeoutSeconds = 1800
            };

            var created = await scheduler.CreateJobAsync(job, "api");
            return Results.Created($"/api/jobs/{created.Id}", created);
        });

        app.MapGet("/api/jobs/stats", async (JobSchedulerService scheduler) =>
        {
            var stats = await scheduler.GetSchedulerStatisticsAsync();
            return Results.Ok(new
            {
                stats.TotalJobs,
                stats.ActiveJobs,
                stats.RunningExecutions,
                stats.AverageSuccessRate,
                stats.ExecutionsLast24Hours
            });
        });

        await app.RunAsync();
    }
}

/// Usage in Startup:
///
/// var builder = WebApplication.CreateBuilder(args);
/// builder.Services.AddJobScheduler(options => {...});
/// builder.Services.AddScoped<EmailSendingJobHandler>();
/// builder.Services.AddHostedService<JobSchedulerBackgroundService>();
/// var app = builder.Build();
/// app.MapControllers();
/// await app.RunAsync();
