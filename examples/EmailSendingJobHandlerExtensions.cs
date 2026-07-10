#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Services;

/// <summary>
/// Extension methods for <see cref="EmailSendingJobHandler"/> that provide convenient
/// ways to create, configure, and manage email sending jobs.
/// </summary>
public static class EmailSendingJobHandlerExtensions
{
    /// <summary>
    /// Creates a new email sending job with the specified configuration.
    /// </summary>
    /// <param name="scheduler">The job scheduler service</param>
    /// <param name="name">The name of the job</param>
    /// <param name="emailConfig">Email configuration as JSON string</param>
    /// <param name="cronExpression">Cron expression for scheduling</param>
    /// <param name="priority">Job priority</param>
    /// <param name="isActive">Whether the job is active</param>
    /// <param name="maxRetries">Maximum retry attempts</param>
    /// <param name="timeoutSeconds">Execution timeout in seconds</param>
    /// <param name="createdBy">Who created the job</param>
    /// <returns>The created job</returns>
    /// <exception cref="ArgumentNullException">Thrown when scheduler or name is null</exception>
    /// <exception cref="ArgumentException">Thrown when name is empty or cronExpression is invalid</exception>
    public static async Task<Job> CreateEmailSendingJobAsync(
        this JobSchedulerService scheduler,
        string name,
        string emailConfig,
        string cronExpression,
        JobPriority priority = JobPriority.Normal,
        bool isActive = true,
        int maxRetries = 3,
        int timeoutSeconds = 300,
        string? createdBy = null)
    {
        ArgumentNullException.ThrowIfNull(scheduler);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(emailConfig);
        ArgumentException.ThrowIfNullOrEmpty(cronExpression);

        var job = new Job
        {
            Name = name,
            Description = $"Email sending job: {name}",
            CronExpression = cronExpression,
            HandlerType = typeof(EmailSendingJobHandler).FullName!,
            Priority = priority,
            IsActive = isActive,
            MaxRetries = maxRetries,
            ExecutionTimeoutSeconds = timeoutSeconds,
            Payload = emailConfig
        };

        return await scheduler.CreateJobAsync(job, createdBy ?? "system");
    }

    /// <summary>
    /// Creates a batch of email sending jobs with sequential numbering.
    /// </summary>
    /// <param name="scheduler">The job scheduler service</param>
    /// <param name="baseName">Base name for the jobs (e.g., "DailyNewsletter")</param>
    /// <param name="emailConfig">Email configuration as JSON string</param>
    /// <param name="cronExpression">Cron expression for scheduling</param>
    /// <param name="count">Number of jobs to create</param>
    /// <param name="startIndex">Starting index for job names</param>
    /// <param name="priority">Job priority</param>
    /// <param name="isActive">Whether the jobs are active</param>
    /// <param name="maxRetries">Maximum retry attempts</param>
    /// <param name="timeoutSeconds">Execution timeout in seconds</param>
    /// <param name="createdBy">Who created the jobs</param>
    /// <returns>List of created jobs</returns>
    /// <exception cref="ArgumentNullException">Thrown when scheduler or baseName is null</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when count is less than 1</exception>
    public static async Task<IReadOnlyList<Job>> CreateEmailSendingJobsBatchAsync(
        this JobSchedulerService scheduler,
        string baseName,
        string emailConfig,
        string cronExpression,
        int count,
        int startIndex = 1,
        JobPriority priority = JobPriority.Normal,
        bool isActive = true,
        int maxRetries = 3,
        int timeoutSeconds = 300,
        string? createdBy = null)
    {
        ArgumentNullException.ThrowIfNull(scheduler);
        ArgumentException.ThrowIfNullOrEmpty(baseName);
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);

        var jobs = new List<Job>(count);
        var tasks = new List<Task<Job>>(count);

        for (int i = 0; i < count; i++)
        {
            int jobNumber = startIndex + i;
            var jobName = $"{baseName}_{jobNumber:D4}";
            var description = $"Email batch job #{jobNumber}: {baseName}";

            tasks.Add(scheduler.CreateEmailSendingJobAsync(
                jobName,
                emailConfig,
                cronExpression,
                priority,
                isActive,
                maxRetries,
                timeoutSeconds,
                createdBy));
        }

        var createdJobs = await Task.WhenAll(tasks);
        return createdJobs;
    }

    /// <summary>
    /// Gets all active email sending jobs from the scheduler.
    /// </summary>
    /// <param name="scheduler">The job scheduler service</param>
    /// <returns>Read-only list of active email jobs</returns>
    /// <exception cref="ArgumentNullException">Thrown when scheduler is null</exception>
    public static async Task<IReadOnlyList<Job>> GetActiveEmailSendingJobsAsync(this JobSchedulerService scheduler)
    {
        ArgumentNullException.ThrowIfNull(scheduler);

        var jobs = await scheduler.GetActiveJobsAsync();
        var emailJobs = new List<Job>();

        foreach (var job in jobs)
        {
            if (string.Equals(job.HandlerType, typeof(EmailSendingJobHandler).FullName,
                StringComparison.Ordinal))
            {
                emailJobs.Add(job);
            }
        }

        return emailJobs.AsReadOnly();
    }

    /// <summary>
    /// Finds email sending jobs by name pattern.
    /// </summary>
    /// <param name="scheduler">The job scheduler service</param>
    /// <param name="namePattern">Name pattern to match (supports * and ?)</param>
    /// <returns>Read-only list of matching jobs</returns>
    /// <exception cref="ArgumentNullException">Thrown when scheduler or namePattern is null</exception>
    public static async Task<IReadOnlyList<Job>> FindEmailSendingJobsByNameAsync(
        this JobSchedulerService scheduler,
        string namePattern)
    {
        ArgumentNullException.ThrowIfNull(scheduler);
        ArgumentException.ThrowIfNullOrEmpty(namePattern);

        var allJobs = await scheduler.GetActiveJobsAsync();
        var matchingJobs = new List<Job>();

        foreach (var job in allJobs)
        {
            if (string.Equals(job.HandlerType, typeof(EmailSendingJobHandler).FullName,
                StringComparison.Ordinal) &&
                (namePattern == "*" ||
                 job.Name.Contains(namePattern, StringComparison.OrdinalIgnoreCase)))
            {
                matchingJobs.Add(job);
            }
        }

        return matchingJobs.AsReadOnly();
    }

    /// <summary>
    /// Validates email sending job configuration.
    /// </summary>
    /// <param name="job">The job to validate</param>
    /// <returns>True if valid; false otherwise</returns>
    /// <exception cref="ArgumentNullException">Thrown when job is null</exception>
    public static bool ValidateEmailJobConfiguration(this Job job)
    {
        ArgumentNullException.ThrowIfNull(job);

        return !string.IsNullOrWhiteSpace(job.Name) &&
               !string.IsNullOrWhiteSpace(job.HandlerType) &&
               !string.IsNullOrWhiteSpace(job.CronExpression) &&
               job.ExecutionTimeoutSeconds > 0 &&
               job.MaxRetries >= 0 &&
               (job.IsActive || job.NextExecution.HasValue);
    }

    /// <summary>
    /// Gets the next execution time for an email job in a human-readable format.
    /// </summary>
    /// <param name="job">The email job</param>
    /// <returns>Formatted next execution time or "Not scheduled"</returns>
    /// <exception cref="ArgumentNullException">Thrown when job is null</exception>
    public static string GetNextExecutionTime(this Job job)
    {
        ArgumentNullException.ThrowIfNull(job);

        return job.NextExecution?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ??
               "Not scheduled";
    }

    /// <summary>
    /// Creates a daily email sending job at a specific time.
    /// </summary>
    /// <param name="scheduler">The job scheduler service</param>
    /// <param name="name">The name of the job</param>
    /// <param name="emailConfig">Email configuration as JSON string</param>
    /// <param name="hour">Hour of day (0-23)</param>
    /// <param name="minute">Minute of hour (0-59)</param>
    /// <param name="priority">Job priority</param>
    /// <param name="isActive">Whether the job is active</param>
    /// <param name="maxRetries">Maximum retry attempts</param>
    /// <param name="timeoutSeconds">Execution timeout in seconds</param>
    /// <param name="createdBy">Who created the job</param>
    /// <returns>The created job</returns>
    /// <exception cref="ArgumentNullException">Thrown when scheduler or name is null</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when hour or minute is out of range</exception>
    public static async Task<Job> CreateDailyEmailSendingJobAsync(
        this JobSchedulerService scheduler,
        string name,
        string emailConfig,
        int hour,
        int minute = 0,
        JobPriority priority = JobPriority.Normal,
        bool isActive = true,
        int maxRetries = 3,
        int timeoutSeconds = 300,
        string? createdBy = null)
    {
        ArgumentNullException.ThrowIfNull(scheduler);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentOutOfRangeException.ThrowIfLessThan(hour, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(hour, 23);
        ArgumentOutOfRangeException.ThrowIfLessThan(minute, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(minute, 59);

        var cronExpression = $"0 {minute} {hour} * * *";
        return await scheduler.CreateEmailSendingJobAsync(
            name,
            emailConfig,
            cronExpression,
            priority,
            isActive,
            maxRetries,
            timeoutSeconds,
            createdBy);
    }
}
