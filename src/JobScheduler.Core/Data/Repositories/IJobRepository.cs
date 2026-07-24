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
    Task<Job?> GetByNameAsync(string name);
    Task<IEnumerable<Job>> GetActiveJobsAsync();
    Task<IEnumerable<Job>> GetJobsByStatusAsync(JobStatus status);
    Task<IEnumerable<Job>> GetJobsByPriorityAsync(JobPriority priority);
    Task<IEnumerable<Job>> GetScheduledJobsForExecutionAsync();
    Task<IEnumerable<Job>> GetMisfiredJobsAsync();
    Task<IEnumerable<Job>> GetFailedJobsAsync();
    Task<IEnumerable<Job>> GetLongRunningJobsAsync(int thresholdSeconds);
    Task<IEnumerable<Job>> GetJobsWithoutRecentExecutionAsync(int minutesThreshold);
}
