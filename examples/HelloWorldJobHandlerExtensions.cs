#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Services;

/// <summary>
/// Extension methods for <see cref="HelloWorldJobHandler"/> that provide convenient
/// ways to create, configure, and manage hello world jobs.
/// </summary>
public static class HelloWorldJobHandlerExtensions
{
    /// <summary>
    /// Creates a new hello world job with the specified configuration.
    /// </summary>
    /// <param name="scheduler">The job scheduler service</param>
    /// <param name="name">The name of the job</param>
    /// <param name="cronExpression">Cron expression for scheduling</param>
    /// <param name="priority">Job priority</param>
    /// <param name="isActive">Whether the job is active</param>
    /// <param name="maxRetries">Maximum retry attempts</param>
    /// <param name="timeoutSeconds">Execution timeout in seconds</param>
    /// <param name="createdBy">Who created the job</param>
    /// <returns>The created job</returns>
    /// <exception cref="ArgumentNullException">Thrown when scheduler or name is null</exception>
    /// <exception cref="ArgumentException">Thrown when name is empty or cronExpression is invalid</exception>
    public static async Task<Job> CreateHelloWorldJobAsync(
        this JobSchedulerService scheduler,
        string name,
        string cronExpression = "* * * * *",
        JobPriority priority = JobPriority.Normal,
        bool isActive = true,
        int maxRetries = 0,
        int timeoutSeconds = 30,
        string? createdBy = null)
    {
        ArgumentNullException.ThrowIfNull(scheduler);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(cronExpression);

        var job = new Job
        {
            Name = name,
            Description = $"Hello World job: {name}",
            CronExpression = cronExpression,
            HandlerType = typeof(HelloWorldJobHandler).FullName!,
            Priority = priority,
            IsActive = isActive,
            MaxRetries = maxRetries,
            ExecutionTimeoutSeconds = timeoutSeconds
        };

        return await scheduler.CreateJobAsync(job, createdBy ?? "system");
    }

    /// <summary>
    /// Creates a batch of hello world jobs with sequential numbering.
    /// </summary>
    /// <param name="scheduler">The job scheduler service</param>
    /// <param name="baseName">Base name for the jobs (e.g., "HelloWorld")</param>
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
    public static async Task<IReadOnlyList<Job>> CreateHelloWorldJobsBatchAsync(
        this JobSchedulerService scheduler,
        string baseName,
        string cronExpression = "* * * * *",
        int count = 5,
        int startIndex = 1,
        JobPriority priority = JobPriority.Normal,
        bool isActive = true,
        int maxRetries = 0,
        int timeoutSeconds = 30,
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
            var description = $"Hello World batch job #{jobNumber}: {baseName}";

            tasks.Add(scheduler.CreateHelloWorldJobAsync(
                jobName,
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
    /// Gets all active hello world jobs from the scheduler.
    /// </summary>
    /// <param name="scheduler">The job scheduler service</param>
    /// <returns>Read-only list of active hello world jobs</returns>
    /// <exception cref="ArgumentNullException">Thrown when scheduler is null</exception>
    public static async Task<IReadOnlyList<Job>> GetActiveHelloWorldJobsAsync(this JobSchedulerService scheduler)
    {
        ArgumentNullException.ThrowIfNull(scheduler);

        var jobs = await scheduler.GetActiveJobsAsync();
        var helloWorldJobs = new List<Job>();

        foreach (var job in jobs)
        {
            if (string.Equals(job.HandlerType, typeof(HelloWorldJobHandler).FullName,
                StringComparison.Ordinal))
            {
                helloWorldJobs.Add(job);
            }
        }

        return helloWorldJobs.AsReadOnly();
    }

    /// <summary>
    /// Finds hello world jobs by name pattern.
    /// </summary>
    /// <param name="scheduler">The job scheduler service</param>
    /// <param name="namePattern">Name pattern to match (supports * and ?)</param>
    /// <returns>Read-only list of matching jobs</returns>
    /// <exception cref="ArgumentNullException">Thrown when scheduler or namePattern is null</exception>
    public static async Task<IReadOnlyList<Job>> FindHelloWorldJobsByNameAsync(
        this JobSchedulerService scheduler,
        string namePattern)
    {
        ArgumentNullException.ThrowIfNull(scheduler);
        ArgumentException.ThrowIfNullOrEmpty(namePattern);

        var allJobs = await scheduler.GetActiveJobsAsync();
        var matchingJobs = new List<Job>();

        foreach (var job in allJobs)
        {
            if (string.Equals(job.HandlerType, typeof(HelloWorldJobHandler).FullName,
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
    /// Validates hello world job configuration.
    /// </summary>
    /// <param name="job">The job to validate</param>
    /// <returns>True if valid; false otherwise</returns>
    /// <exception cref="ArgumentNullException">Thrown when job is null</exception>
    public static bool ValidateHelloWorldJobConfiguration(this Job job)
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
    /// Gets the next execution time for a hello world job in a human-readable format.
    /// </summary>
    /// <param name="job">The hello world job</param>
    /// <returns>Formatted next execution time or "Not scheduled"</returns>
    /// <exception cref="ArgumentNullException">Thrown when job is null</exception>
    public static string GetNextExecutionTime(this Job job)
    {
        ArgumentNullException.ThrowIfNull(job);

        return job.NextExecution?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ??
               "Not scheduled";
    }

    /// <summary>
    /// Creates a hello world job that runs every N minutes.
    /// </summary>
    /// <param name="scheduler">The job scheduler service</param>
    /// <param name="name">The name of the job</param>
    /// <param name="intervalMinutes">Interval in minutes between executions</param>
    /// <param name="priority">Job priority</param>
    /// <param name="isActive">Whether the job is active</param>
    /// <param name="maxRetries">Maximum retry attempts</param>
    /// <param name="timeoutSeconds">Execution timeout in seconds</param>
    /// <param name="createdBy">Who created the job</param>
    /// <returns>The created job</returns>
    /// <exception cref="ArgumentNullException">Thrown when scheduler or name is null</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when intervalMinutes is less than 1</exception>
    public static async Task<Job> CreateRecurringHelloWorldJobAsync(
        this JobSchedulerService scheduler,
        string name,
        int intervalMinutes,
        JobPriority priority = JobPriority.Normal,
        bool isActive = true,
        int maxRetries = 0,
        int timeoutSeconds = 30,
        string? createdBy = null)
    {
        ArgumentNullException.ThrowIfNull(scheduler);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentOutOfRangeException.ThrowIfLessThan(intervalMinutes, 1);

        var cronExpression = intervalMinutes == 1
            ? "*/1 * * * *"
            : $"*/{intervalMinutes} * * * *";

        return await scheduler.CreateHelloWorldJobAsync(
            name,
            cronExpression,
            priority,
            isActive,
            maxRetries,
            timeoutSeconds,
            createdBy);
    }
}