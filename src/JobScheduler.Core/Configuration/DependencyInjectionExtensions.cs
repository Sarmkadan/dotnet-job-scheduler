// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using JobScheduler.Core.Constants;
using JobScheduler.Core.Data;
using JobScheduler.Core.Data.Repositories;
using JobScheduler.Core.Services;

namespace JobScheduler.Core.Configuration;

/// <summary>
/// Extension methods for registering job scheduler services with dependency injection.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Adds all job scheduler services to the service collection.
    /// </summary>
    public static IServiceCollection AddJobScheduler(
        this IServiceCollection services,
        Action<JobSchedulerOptions>? configureOptions = null)
    {
        var options = new JobSchedulerOptions();
        configureOptions?.Invoke(options);

        // Configure database
        services.AddDbContext<JobSchedulerContext>(dbOptions =>
        {
            if (string.IsNullOrWhiteSpace(options.ConnectionString))
            {
                dbOptions.UseSqlite("Data Source=scheduler.db");
            }
            else
            {
                dbOptions.UseSqlite(options.ConnectionString);
            }
        });

        // Register repositories
        services.AddScoped<IJobRepository, JobRepository>();
        services.AddScoped<IExecutionRepository, ExecutionRepository>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // Register services
        services.AddSingleton<CronExpressionService>();
        services.AddSingleton(sp => new ConcurrencyManager(
            sp.GetRequiredService<IExecutionRepository>(),
            options.MaxConcurrentJobs));

        services.AddScoped<RetryService>();
        services.AddScoped<JobExecutorService>();
        services.AddScoped<JobSchedulerService>();

        return services;
    }

    /// <summary>
    /// Applies database migrations and initializes the scheduler.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<JobSchedulerContext>();

        await context.Database.MigrateAsync();
    }

    /// <summary>
    /// Validates that all required services are properly registered.
    /// </summary>
    public static void ValidateSchedulerConfiguration(this IServiceProvider serviceProvider)
    {
        try
        {
            serviceProvider.GetRequiredService<JobSchedulerContext>();
            serviceProvider.GetRequiredService<IJobRepository>();
            serviceProvider.GetRequiredService<IExecutionRepository>();
            serviceProvider.GetRequiredService<CronExpressionService>();
            serviceProvider.GetRequiredService<ConcurrencyManager>();
            serviceProvider.GetRequiredService<RetryService>();
            serviceProvider.GetRequiredService<JobExecutorService>();
            serviceProvider.GetRequiredService<JobSchedulerService>();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("JobScheduler is not properly configured. Ensure AddJobScheduler() was called on the service collection.", ex);
        }
    }
}

/// <summary>
/// Configuration options for the job scheduler.
/// </summary>
public class JobSchedulerOptions
{
    /// <summary>Database connection string (defaults to SQLite in-memory)</summary>
    public string? ConnectionString { get; set; }

    /// <summary>Maximum concurrent job executions allowed globally</summary>
    public int MaxConcurrentJobs { get; set; } = SchedulerConstants.DefaultMaxConcurrentJobs;

    /// <summary>Default job execution timeout in seconds</summary>
    public int DefaultTimeoutSeconds { get; set; } = SchedulerConstants.DefaultExecutionTimeoutSeconds;

    /// <summary>Default maximum retry attempts</summary>
    public int DefaultMaxRetries { get; set; } = SchedulerConstants.DefaultMaxRetries;

    /// <summary>Default retry backoff in seconds</summary>
    public int DefaultRetryBackoffSeconds { get; set; } = SchedulerConstants.DefaultRetryBackoffSeconds;

    /// <summary>Poll interval for checking due jobs in milliseconds</summary>
    public int QueuePollIntervalMs { get; set; } = SchedulerConstants.QueuePollIntervalMs;

    /// <summary>Enable automatic cleanup of orphaned executions</summary>
    public bool EnableCleanup { get; set; } = true;

    /// <summary>Cleanup interval in milliseconds</summary>
    public int CleanupIntervalMs { get; set; } = SchedulerConstants.CleanupIntervalMs;
}
