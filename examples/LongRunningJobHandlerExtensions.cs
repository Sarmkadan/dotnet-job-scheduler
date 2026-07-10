using System.Globalization;
using JobScheduler.Core.Domain.Entities;

namespace JobScheduler.Examples;

/// <summary>
/// Extension methods for <see cref="LongRunningJobHandler"/> that provide additional functionality
/// for working with long-running jobs in the scheduler.
/// </summary>
public static class LongRunningJobHandlerExtensions
{
    /// <summary>
    /// Creates a new job configuration for a long-running operation with default settings.
    /// </summary>
    /// <param name="handler">The job handler instance.</param>
    /// <param name="jobName">The name of the job.</param>
    /// <param name="description">The job description.</param>
    /// <param name="cronExpression">The cron expression for scheduling.</param>
    /// <returns>A configured <see cref="Job"/> ready for registration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when handler, jobName, or cronExpression is null.</exception>
    /// <exception cref="ArgumentException">Thrown when jobName or cronExpression is empty.</exception>
    public static Job CreateLongRunningJobConfiguration(
        this LongRunningJobHandler handler,
        string jobName,
        string description,
        string cronExpression)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentException.ThrowIfNullOrEmpty(jobName);
        ArgumentException.ThrowIfNullOrEmpty(description);
        ArgumentException.ThrowIfNullOrEmpty(cronExpression);

        return new Job
        {
            Name = jobName,
            Description = description,
            CronExpression = cronExpression,
            HandlerType = typeof(LongRunningJobHandler).FullName!,
            Priority = JobPriority.Normal,
            IsActive = true,
            MaxRetries = 3,
            MaxConcurrentExecutions = 1,
            ExecutionTimeoutSeconds = 300 // 5 minutes
        };
    }

    /// <summary>
    /// Creates a new job configuration for a long-running operation with custom settings.
    /// </summary>
    /// <param name="handler">The job handler instance.</param>
    /// <param name="jobName">The name of the job.</param>
    /// <param name="description">The job description.</param>
    /// <param name="cronExpression">The cron expression for scheduling.</param>
    /// <param name="priority">The job priority level.</param>
    /// <param name="maxRetries">Maximum retry attempts on failure.</param>
    /// <param name="executionTimeoutSeconds">Maximum execution time in seconds.</param>
    /// <returns>A configured <see cref="Job"/> ready for registration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when handler, jobName, or cronExpression is null.</exception>
    /// <exception cref="ArgumentException">Thrown when jobName or cronExpression is empty.</exception>
    public static Job CreateLongRunningJobConfiguration(
        this LongRunningJobHandler handler,
        string jobName,
        string description,
        string cronExpression,
        JobPriority priority,
        int maxRetries = 3,
        int executionTimeoutSeconds = 300)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentException.ThrowIfNullOrEmpty(jobName);
        ArgumentException.ThrowIfNullOrEmpty(description);
        ArgumentException.ThrowIfNullOrEmpty(cronExpression);

        return new Job
        {
            Name = jobName,
            Description = description,
            CronExpression = cronExpression,
            HandlerType = typeof(LongRunningJobHandler).FullName!,
            Priority = priority,
            IsActive = true,
            MaxRetries = maxRetries,
            MaxConcurrentExecutions = 1,
            ExecutionTimeoutSeconds = executionTimeoutSeconds
        };
    }

    /// <summary>
    /// Creates a batch of long-running jobs with sequential execution constraints.
    /// </summary>
    /// <param name="handler">The job handler instance.</param>
    /// <param name="jobConfigurations">Collection of job configurations to create.</param>
    /// <returns>Read-only list of created jobs.</returns>
    /// <exception cref="ArgumentNullException">Thrown when handler or jobConfigurations is null.</exception>
    public static IReadOnlyList<Job> CreateLongRunningJobBatch(
        this LongRunningJobHandler handler,
        IEnumerable<Job> jobConfigurations)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(jobConfigurations);

        var jobs = jobConfigurations.ToList();
        if (jobs.Count == 0)
        {
            return Array.Empty<Job>();
        }

        foreach (var job in jobs)
        {
            job.HandlerType = typeof(LongRunningJobHandler).FullName!;
            job.MaxConcurrentExecutions = 1; // Ensure sequential execution
            job.IsActive = true;
        }

        return jobs.AsReadOnly();
    }

    /// <summary>
    /// Validates a long-running job configuration for common issues.
    /// </summary>
    /// <param name="handler">The job handler instance.</param>
    /// <param name="job">The job to validate.</param>
    /// <returns>True if the job configuration is valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when handler or job is null.</exception>
    public static bool ValidateLongRunningJobConfiguration(
        this LongRunningJobHandler handler,
        Job job)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(job);

        if (string.IsNullOrWhiteSpace(job.Name))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(job.HandlerType))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(job.CronExpression))
        {
            return false;
        }

        if (job.MaxConcurrentExecutions < 1)
        {
            return false;
        }

        if (job.ExecutionTimeoutSeconds <= 0)
        {
            return false;
        }

        return true;
    }
}