#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using JobScheduler.Core.Constants;
using JobScheduler.Core.Domain.Entities;

namespace JobScheduler.Core.Data.Repositories;

/// <summary>
/// Repository for job entity operations and queries.
/// Provides job-specific data access methods.
/// </summary>
public sealed class JobRepository : Repository<Job>, IJobRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JobRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public JobRepository(JobSchedulerContext context) : base(context)
    {
        ArgumentNullException.ThrowIfNull(context);
    }

    /// <summary>
    /// Retrieves a job by its name.
    /// </summary>
    /// <param name="name">The name of the job to retrieve.</param>
    /// <returns>The job with the specified name, or null if not found.</returns>
    public async Task<Job?> GetByNameAsync(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        return await _dbSet
            .FirstOrDefaultAsync(j => j.Name == name);
    }

    /// <summary>
    /// Retrieves all active jobs that are not cancelled, ordered by priority.
    /// </summary>
    /// <returns>A collection of active jobs.</returns>
    public async Task<IEnumerable<Job>> GetActiveJobsAsync()
    {
        return await _dbSet
            .Where(j => j.IsActive && j.Status != JobStatus.Cancelled)
            .OrderByPriority()
            .ToListAsync();
    }

    /// <summary>
    /// Retrieves all jobs with the specified status, ordered by priority.
    /// </summary>
    /// <param name="status">The job status to filter by.</param>
    /// <returns>A collection of jobs with the specified status.</returns>
    public async Task<IEnumerable<Job>> GetJobsByStatusAsync(JobStatus status)
    {
        return await _dbSet
            .Where(j => j.Status == status)
            .OrderByPriority()
            .ToListAsync();
    }

    /// <summary>
    /// Retrieves all active jobs with the specified priority, ordered by next execution time.
    /// </summary>
    /// <param name="priority">The job priority to filter by.</param>
    /// <returns>A collection of active jobs with the specified priority.</returns>
    public async Task<IEnumerable<Job>> GetJobsByPriorityAsync(JobPriority priority)
    {
        return await _dbSet
            .Where(j => j.Priority == priority && j.IsActive)
            .OrderBy(j => j.NextExecutionAt)
            .ToListAsync();
    }

    /// <summary>
    /// Retrieves all active jobs that are scheduled for execution (next execution time is in the past and not suspended or cancelled).
    /// </summary>
    /// <returns>A collection of jobs scheduled for execution, ordered by effective priority and next execution time.</returns>
    public async Task<IEnumerable<Job>> GetScheduledJobsForExecutionAsync()
    {
        var now = DateTime.UtcNow;
        var jobs = await _dbSet
            .Where(j => j.IsActive &&
                        j.Status != JobStatus.Suspended &&
                        j.Status != JobStatus.Cancelled &&
                        j.NextExecutionAt <= now)
            .ToListAsync();

        // Sort by effective priority (which includes an aging bonus so that
        // long-waiting low-priority jobs are not starved by high-priority load).
        return jobs
            .OrderByDescending(j => j.CalculateEffectivePriority(now))
            .ThenBy(j => j.NextExecutionAt);
    }

    /// <summary>
    /// Retrieves all active jobs that have misfired (next execution time is more than 60 seconds in the past and not suspended or cancelled).
    /// </summary>
    /// <returns>A collection of misfired jobs, ordered by effective priority and next execution time.</returns>
    public async Task<IEnumerable<Job>> GetMisfiredJobsAsync()
    {
        var now = DateTime.UtcNow;
        var jobs = await _dbSet
            .Where(j => j.IsActive &&
                        j.Status != JobStatus.Suspended &&
                        j.Status != JobStatus.Cancelled &&
                        j.NextExecutionAt.HasValue &&
                        j.NextExecutionAt < now.AddSeconds(-60))
            .ToListAsync();

        return jobs
            .OrderByDescending(j => j.CalculateEffectivePriority(now))
            .ThenBy(j => j.NextExecutionAt);
    }

    /// <summary>
    /// Retrieves all jobs that have failed (status Failed or FailedPermanently), ordered by update time descending.
    /// </summary>
    /// <returns>A collection of failed jobs.</returns>
    public async Task<IEnumerable<Job>> GetFailedJobsAsync()
    {
        return await _dbSet
            .Where(j => j.Status == JobStatus.Failed || j.Status == JobStatus.FailedPermanently)
            .OrderByDescending(j => j.UpdatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Retrieves all running jobs that have been executing longer than the specified threshold.
    /// </summary>
    /// <param name="thresholdSeconds">The threshold in seconds to consider a job as long-running.</param>
    /// <returns>A collection of long-running jobs, ordered by last execution time descending.</returns>
    public async Task<IEnumerable<Job>> GetLongRunningJobsAsync(int thresholdSeconds)
    {
        var now = DateTime.UtcNow;
        var candidates = await _dbSet
            .Where(j => j.Status == JobStatus.Running && j.LastExecutedAt.HasValue)
            .ToListAsync();

        return candidates
            .Where(j => (now - j.LastExecutedAt!.Value).TotalSeconds > thresholdSeconds)
            .OrderByDescending(j => j.LastExecutedAt);
    }

    /// <summary>
    /// Retrieves all active jobs that have not been executed within the specified number of minutes.
    /// </summary>
    /// <param name="minutesThreshold">The number of minutes to check for lack of recent execution.</param>
    /// <returns>A collection of jobs without recent execution, ordered by last execution time ascending.</returns>
    public async Task<IEnumerable<Job>> GetJobsWithoutRecentExecutionAsync(int minutesThreshold)
    {
        var threshold = DateTime.UtcNow.AddMinutes(-minutesThreshold);
        return await _dbSet
            .Where(j => j.IsActive && (!j.LastExecutedAt.HasValue || j.LastExecutedAt < threshold))
            .OrderBy(j => j.LastExecutedAt)
            .ToListAsync();
    }
}

/// <summary>
/// Extension methods for job query ordering.
/// </summary>
internal static class JobQueryExtensions
{
    /// <summary>
    /// Orders the job query by priority in descending order.
    /// </summary>
    /// <param name="query">The job query to order.</param>
    /// <returns>An ordered queryable of jobs.</returns>
    internal static IOrderedQueryable<Job> OrderByPriority(this IQueryable<Job> query)
    {
        return query.OrderByDescending(j => j.Priority);
    }
}