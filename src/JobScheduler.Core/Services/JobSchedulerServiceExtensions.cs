#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JobScheduler.Core.Constants;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Domain.Models;

namespace JobScheduler.Core.Services;

/// <summary>
/// Extension methods for <see cref="JobSchedulerService"/> providing additional utility functionality
/// for job scheduling operations.
/// </summary>
public static class JobSchedulerServiceExtensions
{
    /// <summary>
    /// Gets jobs filtered by multiple criteria including name, status, and priority.
    /// </summary>
    /// <param name="service">The job scheduler service instance.</param>
    /// <param name="nameFilter">Optional job name filter (case-insensitive partial match).</param>
    /// <param name="statusFilter">Optional job status filter.</param>
    /// <param name="priorityFilter">Optional job priority filter.</param>
    /// <param name="isActiveOnly">If true, returns only active jobs.</param>
    /// <returns>Filtered collection of jobs.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="service"/> is <c>null</c>.</exception>
    public static async Task<IEnumerable<Job>> GetFilteredJobsAsync(
        this JobSchedulerService service,
        string? nameFilter = null,
        JobStatus? statusFilter = null,
        JobPriority? priorityFilter = null,
        bool isActiveOnly = false)
    {
        ArgumentNullException.ThrowIfNull(service);

        var jobs = await service.GetJobsAsync(statusFilter);

        if (isActiveOnly)
        {
            jobs = jobs.Where(j => j.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(nameFilter))
        {
            var filter = nameFilter.Trim();
            jobs = jobs.Where(j => j.Name.Contains(filter, StringComparison.OrdinalIgnoreCase));
        }

        if (priorityFilter.HasValue)
        {
            jobs = jobs.Where(j => j.Priority == priorityFilter.Value);
        }

        return jobs;
    }

    /// <summary>
    /// Creates a new job from a request object.
    /// </summary>
    /// <param name="service">The job scheduler service instance.</param>
    /// <param name="request">The job creation request.</param>
    /// <param name="createdBy">Optional creator identifier.</param>
    /// <returns>The created job.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="service"/> or <paramref name="request"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="request"/>.Name is null, empty, or whitespace.
    /// </exception>
    public static async Task<Job> CreateJobAsync(
        this JobSchedulerService service,
        CreateJobRequest request,
        string? createdBy = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(request);

        ArgumentException.ThrowIfNullOrEmpty(request.Name, nameof(request));
        ArgumentException.ThrowIfNullOrEmpty(request.CronExpression, nameof(request));
        ArgumentException.ThrowIfNullOrEmpty(request.HandlerType, nameof(request));

        var job = new Job
        {
            Name = request.Name,
            Description = request.Description ?? string.Empty,
            CronExpression = request.CronExpression,
            TimeZoneId = request.TimeZoneId,
            HandlerType = request.HandlerType,
            HandlerParameters = request.HandlerParameters ?? string.Empty,
            Priority = request.Priority,
            MaxRetries = request.MaxRetries,
            RetryBackoffSeconds = request.RetryBackoffSeconds,
            ExecutionTimeoutSeconds = request.ExecutionTimeoutSeconds,
            MaxConcurrentExecutions = request.MaxConcurrentExecutions,
            IsActive = request.IsActive
        };

        return await service.CreateJobAsync(job, createdBy);
    }

    /// <summary>
    /// Executes all due jobs with a timeout to prevent indefinite execution.
    /// </summary>
    /// <param name="service">The job scheduler service instance.</param>
    /// <param name="timeoutSeconds">Maximum execution time in seconds.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of execution records.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="service"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="timeoutSeconds"/> is less than or equal to zero.
    /// </exception>
    public static async Task<IEnumerable<JobExecution>> ExecuteDueJobsWithTimeoutAsync(
        this JobSchedulerService service,
        int timeoutSeconds = 30,
        System.Threading.CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeoutSeconds, 0, nameof(timeoutSeconds));

        using var timeoutCts = System.Threading.CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        return await service.ExecuteDueJobsAsync(timeoutCts.Token);
    }

    /// <summary>
    /// Gets execution statistics for all jobs in a single call.
    /// </summary>
    /// <param name="service">The job scheduler service instance.</param>
    /// <returns>Dictionary mapping job IDs to their statistics.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="service"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if a job's execution statistics are unexpectedly null.
    /// </exception>
    public static async Task<Dictionary<Guid, ExecutionStatistics>> GetAllJobExecutionStatisticsAsync(
        this JobSchedulerService service)
    {
        ArgumentNullException.ThrowIfNull(service);

        var jobs = await service.GetJobsAsync(null);
        var statsDict = new Dictionary<Guid, ExecutionStatistics>();

        foreach (var job in jobs)
        {
            var stats = await service.GetJobDetailsAsync(job.Id);
            statsDict[job.Id] = stats.ExecutionStatistics ?? throw new InvalidOperationException(
                $"Execution statistics for job {job.Id} ({job.Name}) are unexpectedly null.");
        }

        return statsDict;
    }

    /// <summary>
    /// Gets all active jobs filtered by handler type.
    /// </summary>
    /// <typeparam name="THandler">The job handler type to filter by</typeparam>
    /// <param name="service">The job scheduler service</param>
    /// <returns>Read-only list of active jobs for the specified handler type</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="service"/> is <c>null</c>.</exception>
    public static async Task<IReadOnlyList<Job>> GetActiveJobsByHandlerAsync<THandler>(this JobSchedulerService service)
        where THandler : IJobHandler
    {
        ArgumentNullException.ThrowIfNull(service);

        var jobs = await service.GetJobsAsync(null);
        var handlerTypeName = typeof(THandler).FullName!;

        return jobs.Where(j => string.Equals(j.HandlerType, handlerTypeName, StringComparison.Ordinal))
                  .Where(j => j.IsActive)
                  .ToList()
                  .AsReadOnly();
    }

    /// <summary>
    /// Finds jobs by name pattern filtered by handler type.
    /// </summary>
    /// <typeparam name="THandler">The job handler type to filter by</typeparam>
    /// <param name="service">The job scheduler service</param>
    /// <param name="namePattern">Name pattern to match (supports * and ?)</param>
    /// <returns>Read-only list of matching jobs for the specified handler type</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="service"/> or <paramref name="namePattern"/> is <c>null</c>.</exception>
    public static async Task<IReadOnlyList<Job>> FindJobsByNameAsync<THandler>(this JobSchedulerService service, string namePattern)
        where THandler : IJobHandler
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentException.ThrowIfNullOrEmpty(namePattern);

        var allJobs = await service.GetJobsAsync(null);
        var handlerTypeName = typeof(THandler).FullName!;

        return allJobs.Where(j => string.Equals(j.HandlerType, handlerTypeName, StringComparison.Ordinal))
                     .Where(j => namePattern == "*" || j.Name.Contains(namePattern, StringComparison.OrdinalIgnoreCase))
                     .ToList()
                     .AsReadOnly();
    }

    /// <summary>
    /// Creates a job that runs every N minutes.
    /// </summary>
    /// <param name="service">The job scheduler service</param>
    /// <param name="name">The name of the job</param>
    /// <param name="intervalMinutes">Interval in minutes between executions</param>
    /// <param name="handlerType">The job handler type</param>
    /// <param name="priority">Job priority</param>
    /// <param name="isActive">Whether the job is active</param>
    /// <param name="maxRetries">Maximum retry attempts</param>
    /// <param name="timeoutSeconds">Execution timeout in seconds</param>
    /// <param name="createdBy">Who created the job</param>
    /// <returns>The created job</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="service"/>, <paramref name="name"/>, or <paramref name="handlerType"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="intervalMinutes"/> is less than 1.</exception>
    public static async Task<Job> CreateRecurringJobByIntervalAsync(
        this JobSchedulerService service,
        string name,
        int intervalMinutes,
        Type handlerType,
        JobPriority priority = JobPriority.Normal,
        bool isActive = true,
        int maxRetries = 0,
        int timeoutSeconds = 30,
        string? createdBy = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(handlerType);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentOutOfRangeException.ThrowIfLessThan(intervalMinutes, 1);

        var cronExpression = intervalMinutes == 1
            ? "*/1 * * * *"
            : $"*/{intervalMinutes} * * * *";

        var job = new Job
        {
            Name = name,
            Description = $"Recurring job: {name}",
            CronExpression = cronExpression,
            HandlerType = handlerType.FullName!,
            Priority = priority,
            IsActive = isActive,
            MaxRetries = maxRetries,
            ExecutionTimeoutSeconds = timeoutSeconds
        };

        return await service.CreateJobAsync(job, createdBy ?? "system");
    }

    /// <summary>
    /// Gets the next execution time for a job in a human-readable format.
    /// </summary>
    /// <param name="job">The job</param>
    /// <returns>Formatted next execution time or "Not scheduled"</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="job"/> is <c>null</c>.</exception>
    public static string GetNextExecutionTime(this Job job)
    {
        ArgumentNullException.ThrowIfNull(job);
        return job.NextExecutionAt?.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture) ??
               "Not scheduled";
    }

    /// <summary>
    /// Validates job configuration.
    /// </summary>
    /// <param name="job">The job to validate</param>
    /// <returns>True if valid; false otherwise</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="job"/> is <c>null</c>.</exception>
    public static bool ValidateJobConfiguration(this Job job)
    {
        ArgumentNullException.ThrowIfNull(job);

        return job.Name is { Length: > 0 } &&
               job.HandlerType is { Length: > 0 } &&
               job.CronExpression is { Length: > 0 } &&
               job.ExecutionTimeoutSeconds > 0 &&
               job.MaxRetries >= 0 &&
               (job.IsActive || job.NextExecutionAt.HasValue);
    }
}