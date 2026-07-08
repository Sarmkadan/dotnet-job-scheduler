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
    public JobRepository(JobSchedulerContext context) : base(context) { }

    public async Task<Job?> GetByNameAsync(string name)
    {
        return await _dbSet
            .FirstOrDefaultAsync(j => j.Name == name);
    }

    public async Task<IEnumerable<Job>> GetActiveJobsAsync()
    {
        return await _dbSet
            .Where(j => j.IsActive && j.Status != JobStatus.Cancelled)
            .OrderByPriority()
            .ToListAsync();
    }

    public async Task<IEnumerable<Job>> GetJobsByStatusAsync(JobStatus status)
    {
        return await _dbSet
            .Where(j => j.Status == status)
            .OrderByPriority()
            .ToListAsync();
    }

    public async Task<IEnumerable<Job>> GetJobsByPriorityAsync(JobPriority priority)
    {
        return await _dbSet
            .Where(j => j.Priority == priority && j.IsActive)
            .OrderBy(j => j.NextExecutionAt)
            .ToListAsync();
    }

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

    public async Task<IEnumerable<Job>> GetFailedJobsAsync()
    {
        return await _dbSet
            .Where(j => j.Status == JobStatus.Failed || j.Status == JobStatus.FailedPermanently)
            .OrderByDescending(j => j.UpdatedAt)
            .ToListAsync();
    }

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
    internal static IOrderedQueryable<Job> OrderByPriority(this IQueryable<Job> query)
    {
        return query.OrderByDescending(j => j.Priority);
    }
}
