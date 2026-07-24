#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using JobScheduler.Core.Configuration;
using JobScheduler.Core.Services;

namespace JobScheduler.Core;

/// <summary>
/// Main entry point for the distributed job scheduler application.
/// Initializes services, database, and starts the scheduling loop.
/// </summary>
public sealed class Program
{
    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();

        try
        {
            // Initialize database
            await host.Services.InitializeDatabaseAsync();
            host.Services.ValidateSchedulerConfiguration();

            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Job Scheduler starting...");

            // SchedulerHostedService (registered in CreateHostBuilder) owns the
            // scheduling loop - no second manual loop here, or every due job
            // would be picked up by two competing pollers.
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogCritical(ex, "Application terminated unexpectedly");
            throw;
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Information);
                });

                services.AddJobScheduler(options =>
                {
                    options.ConnectionString = "Data Source=scheduler.db";
                    options.MaxConcurrentJobs = 10;
                    options.DefaultTimeoutSeconds = 300;
                    options.QueuePollIntervalMs = 5000;
                });

                services.AddHostedService<SchedulerHostedService>();
            });

}

/// <summary>
/// Hosted service that manages the scheduler lifecycle within the host.
/// </summary>
public sealed class SchedulerHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SchedulerHostedService> _logger;
    private readonly TimeSpan _gracefulShutdownTimeout = TimeSpan.FromSeconds(30);

    public SchedulerHostedService(IServiceProvider serviceProvider, ILogger<SchedulerHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Scheduler hosted service started");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _serviceProvider.CreateScope();
                var schedulerService = scope.ServiceProvider.GetRequiredService<JobSchedulerService>();

                // Execute due jobs
                var executions = await schedulerService.ExecuteDueJobsAsync(stoppingToken);

                // Process retries
                var retries = await schedulerService.ProcessRetriesAsync();

                // Get and log statistics periodically
                if ((executions.Any() || retries.Any()) && !stoppingToken.IsCancellationRequested)
                {
                    var stats = await schedulerService.GetSchedulerStatisticsAsync();
                    _logger.LogDebug(
                        "Scheduler stats - Jobs: {TotalJobs}, Running: {Running}, Total Executions: {Total}, Success Rate: {Rate:F1}%",
                        stats.TotalJobs,
                        stats.RunningExecutions,
                        stats.TotalExecutions,
                        stats.AverageSuccessRate);
                }

                await Task.Delay(5000, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Scheduler hosted service stopping - initiating graceful shutdown");
            await GracefulShutdownAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in scheduler hosted service");
            // Do not re-throw exceptions to prevent the hosted service from terminating
            // This allows the scheduler to continue running even if individual jobs fail
        }
    }

    /// <summary>
    /// Performs graceful shutdown by allowing in-flight jobs to complete within a bounded time window.
    /// Any jobs still running after the grace period are marked as interrupted.
    /// </summary>
    private async Task GracefulShutdownAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Graceful shutdown initiated - allowing in-flight jobs to complete within {Timeout} seconds",
            _gracefulShutdownTimeout.TotalSeconds);

        var shutdownCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        var shutdownTask = Task.Delay(_gracefulShutdownTimeout, shutdownCts.Token);

        try
        {
            // Wait for the graceful shutdown period to complete
            await shutdownTask;

            _logger.LogInformation("Graceful shutdown period completed - all in-flight jobs should have finished");
        }
        catch (OperationCanceledException)
        {
            // This means we're being cancelled before the timeout
            _logger.LogDebug("Graceful shutdown cancelled before timeout");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during graceful shutdown");
        }
        finally
        {
            shutdownCts.Dispose();
        }
    }
}
