#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JobScheduler.Core.Constants;
using JobScheduler.Core.Domain.Entities;

namespace JobScheduler.Core.Data.Repositories;

/// <summary>
/// Repository for job-specific queries and operations.
/// Extends base repository with job domain-specific methods.
/// </summary>
public interface IJobRepository : IRepository<Job>
{
    /// <summary>
    /// Retrieves a job by its name.
    /// </summary>
    /// <param name="name">The name of the job to retrieve.</param>
    /// <returns>The job if found; otherwise, null.</returns>
    Task<Job?> GetByNameAsync(string name);

    /// <summary>
    /// Retrieves all active jobs that are not cancelled.
    /// </summary>
    /// <returns>A collection of active jobs.</returns>
    Task<IEnumerable<Job>> GetActiveJobsAsync();

    /// <summary>
    /// Retrieves jobs filtered by their status.
    /// </summary>
    /// <param name="status">The status to filter by.</param>
    /// <returns>A collection of jobs matching the specified status.</returns>
    Task<IEnumerable<Job>> GetJobsByStatusAsync(JobStatus status);

    /// <summary>
    /// Retrieves active jobs filtered by their priority.
    /// </summary>
    /// <param name="priority">The priority level to filter by.</param>
    /// <returns>A collection of jobs matching the specified priority.</returns>
    Task<IEnumerable<Job>> GetJobsByPriorityAsync(JobPriority priority);

    /// <summary>
    /// Retrieves jobs that are scheduled for execution based on their next execution time.
    /// </summary>
    /// <returns>A collection of jobs scheduled for execution.</returns>
    Task<IEnumerable<Job>> GetScheduledJobsForExecutionAsync();

    /// <summary>
    /// Retrieves jobs that have missed their scheduled execution time by more than 60 seconds.
    /// </summary>
    /// <returns>A collection of misfired jobs.</returns>
    Task<IEnumerable<Job>> GetMisfiredJobsAsync();

    /// <summary>
    /// Retrieves jobs that have failed or failed permanently.
    /// </summary>
    /// <returns>A collection of failed jobs.</returns>
    Task<IEnumerable<Job>> GetFailedJobsAsync();

    /// <summary>
    /// Retrieves jobs that are currently running and have exceeded the specified duration threshold.
    /// </summary>
    /// <param name="thresholdSeconds">The duration threshold in seconds.</param>
    /// <returns>A collection of long-running jobs.</returns>
    Task<IEnumerable<Job>> GetLongRunningJobsAsync(int thresholdSeconds);

    /// <summary>
    /// Retrieves active jobs that have not been executed within the specified time threshold.
    /// </summary>
    /// <param name="minutesThreshold">The time threshold in minutes.</param>
    /// <returns>A collection of jobs without recent execution.</returns>
    Task<IEnumerable<Job>> GetJobsWithoutRecentExecutionAsync(int minutesThreshold);
}
