#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using JobScheduler.Core.Constants;
using JobScheduler.Core.Data;
using JobScheduler.Core.Data.Repositories;
using JobScheduler.Core.Services;
using JobScheduler.Core.Events;
using JobScheduler.Core.Middleware;

namespace JobScheduler.Core.Configuration;

/// <summary>
/// Extension methods for registering job scheduler services with dependency injection.
/// WHY: Centralized DI registration ensures consistent service configuration and lifetime management.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Adds all job scheduler services to the service collection with full Phase 2 features.
    /// Includes caching, monitoring, webhooks, events, and advanced functionality.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configureOptions">Optional configuration action for <see cref="JobSchedulerOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddJobScheduler(
        this IServiceCollection services,
        Action<JobSchedulerOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new JobSchedulerOptions();
        configureOptions?.Invoke(options);

        // Configure database
        services.AddDbContext<JobSchedulerContext>(dbOptions =>
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(options.ConnectionString);
            dbOptions.UseSqlite(options.ConnectionString);
        });

        // Register repositories
        services.AddScoped<IJobRepository, JobRepository>();
        services.AddScoped<IExecutionRepository, ExecutionRepository>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // Phase 1: Core services
        services.AddSingleton<CronExpressionService>();
        services.AddSingleton(sp => new ConcurrencyManager(
            sp.GetRequiredService<IExecutionRepository>(),
            options.MaxConcurrentJobs));

        services.AddScoped<RetryService>();
        services.AddScoped<JobExecutorService>();
        services.AddScoped<JobSchedulerService>();

        // Phase 2: Caching layer
        services.AddMemoryCache();
        services.AddScoped<CacheService>();

        // Phase 2: Monitoring and statistics
        services.AddSingleton<PerformanceMonitor>();
        services.AddScoped<ExecutionStatisticsService>();
        services.AddScoped<AuditLogger>();

        // Phase 2: Event system
        services.AddSingleton<IEventPublisher, EventPublisher>();

        // Phase 2: Integration services
        services.AddHttpClient<WebhookNotificationService>();
        services.AddHttpClient<SlackNotificationService>();
        services.AddHttpClient<ExternalApiClient>();

        // Phase 2: Schedule service
        services.AddScoped<ScheduleService>();

        // Phase 3: Dependency graph
        services.AddScoped<IJobDependencyService, JobDependencyService>();

        // Phase 3: Job history viewer
        services.AddScoped<JobHistoryService>();

        // Phase 3: Job chain/pipeline support
        services.AddScoped<JobPipelineService>();

        // Phase 3: Distributed job locking
        services.AddScoped<IDistributedJobLockService, DistributedJobLockService>();

        // Leader election (opt-in)
        if (options.EnableLeaderElection)
        {
            services.AddScoped<ILeaderElectionService>(sp => new DatabaseLeaderElectionService(
                sp.GetRequiredService<JobSchedulerContext>(),
                string.IsNullOrWhiteSpace(options.LeaderElectionInstanceId)
                    ? Environment.MachineName
                    : options.LeaderElectionInstanceId,
                options.LeaderElectionLeaseDurationSeconds,
                sp.GetService<ILogger<DatabaseLeaderElectionService>>()));
        }

        // Phase 2: Middleware registration
        services.AddScoped<GlobalExceptionMiddleware>();
        services.AddScoped<LoggingMiddleware>();
        services.AddScoped<RateLimitMiddleware>();

        // Phase 2: Controllers (will be registered automatically by ASP.NET Core)

        return services;
    }

    /// <summary>
    /// Adds middleware to the application pipeline in the correct order.
    /// WHY: Middleware order matters - exception handling must be first, then logging, then rate limiting.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/> to configure.</param>
    /// <returns>The <see cref="IApplicationBuilder"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="app"/> is <see langword="null"/>.</exception>
    public static IApplicationBuilder UseJobSchedulerMiddleware(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseMiddleware<GlobalExceptionMiddleware>();
        app.UseMiddleware<LoggingMiddleware>();
        app.UseMiddleware<RateLimitMiddleware>();
        return app;
    }

    /// <summary>
    /// Applies database migrations and initializes the scheduler.
    /// </summary>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> to resolve services from.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="serviceProvider"/> is <see langword="null"/>.</exception>
    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<JobSchedulerContext>();

        await context.Database.MigrateAsync();
    }

    /// <summary>
    /// Validates that all required services are properly registered.
    /// Checks both Phase 1 core services and Phase 2 feature services.
    /// </summary>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> to validate.</param>
    /// <exception cref="ArgumentNullException"><paramref name="serviceProvider"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when required services are not registered.</exception>
    public static void ValidateSchedulerConfiguration(this IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        try
        {
            // Phase 1 core services
            _ = serviceProvider.GetRequiredService<JobSchedulerContext>();
            _ = serviceProvider.GetRequiredService<IJobRepository>();
            _ = serviceProvider.GetRequiredService<IExecutionRepository>();
            _ = serviceProvider.GetRequiredService<CronExpressionService>();
            _ = serviceProvider.GetRequiredService<ConcurrencyManager>();
            _ = serviceProvider.GetRequiredService<RetryService>();
            _ = serviceProvider.GetRequiredService<JobExecutorService>();
            _ = serviceProvider.GetRequiredService<JobSchedulerService>();

            // Phase 2 services
            _ = serviceProvider.GetRequiredService<CacheService>();
            _ = serviceProvider.GetRequiredService<PerformanceMonitor>();
            _ = serviceProvider.GetRequiredService<ExecutionStatisticsService>();
            _ = serviceProvider.GetRequiredService<IEventPublisher>();
            _ = serviceProvider.GetRequiredService<WebhookNotificationService>();
            _ = serviceProvider.GetRequiredService<SlackNotificationService>();
            _ = serviceProvider.GetRequiredService<ScheduleService>();
            _ = serviceProvider.GetRequiredService<AuditLogger>();
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
public sealed class JobSchedulerOptions
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

    // ---- Leader election (opt-in) ----

    /// <summary>
    /// Enables distributed leader election so that only one scheduler node executes
    /// jobs at a time in multi-instance deployments. Defaults to <c>false</c>.
    /// </summary>
    public bool EnableLeaderElection { get; set; } = false;

    /// <summary>
    /// Unique identifier for this scheduler instance.
    /// Defaults to <c>Environment.MachineName</c> when left null or empty.
    /// </summary>
    public string? LeaderElectionInstanceId { get; set; }

    /// <summary>
    /// How many seconds a leadership lease is valid before it must be renewed.
    /// Defaults to 30 s.
    /// </summary>
    public int LeaderElectionLeaseDurationSeconds { get; set; } = 30;
}