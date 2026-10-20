#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using JobScheduler.Core.Constants;
using JobScheduler.Core.Data.Repositories;
using JobScheduler.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace JobScheduler.Core.Configuration;

/// <summary>
/// Extension methods for registering the job scheduler health check.
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>
    /// Registers <see cref="JobSchedulerHealthCheck"/> with the health-check system.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="name">The health check name. Defaults to "job-scheduler".</param>
    /// <param name="failureStatus">The <see cref="HealthStatus"/> reported when the check fails. Defaults to <see cref="HealthStatus.Unhealthy"/>.</param>
    /// <param name="tags">Optional tags for the health check.</param>
    /// <param name="pollIntervalMs">The scheduler's polling interval in milliseconds. Used to compute the overdue threshold.</param>
    /// <returns>The <see cref="IHealthChecksBuilder"/> so that additional calls can be chained.</returns>
    public static IHealthChecksBuilder AddJobSchedulerHealthCheck(
        this IHealthChecksBuilder builder,
        string name = "job-scheduler",
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null,
        int pollIntervalMs = SchedulerConstants.QueuePollIntervalMs)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.Add(new HealthCheckRegistration(
            name,
            sp =>
            {
                using var scope = sp.CreateScope();
                var jobRepo = scope.ServiceProvider.GetRequiredService<IJobRepository>();
                var execRepo = scope.ServiceProvider.GetRequiredService<IExecutionRepository>();
                var logger = sp.GetService<ILogger<JobSchedulerHealthCheck>>();
                return new JobSchedulerHealthCheck(jobRepo, execRepo, pollIntervalMs, logger);
            },
            failureStatus,
            tags));
    }

    /// <summary>
    /// Registers <see cref="ExecutionTracker"/> and <see cref="JobSchedulerHealthCheck"/>
    /// as a single convenience call on the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="pollIntervalMs">The scheduler's polling interval in milliseconds.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddJobSchedulerHealthCheck(
        this IServiceCollection services,
        int pollIntervalMs = SchedulerConstants.QueuePollIntervalMs)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<ExecutionTracker>();

        services.AddHealthChecks()
            .AddJobSchedulerHealthCheck(pollIntervalMs: pollIntervalMs);

        return services;
    }
}
