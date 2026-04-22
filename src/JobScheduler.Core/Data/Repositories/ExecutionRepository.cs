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
/// Repository for job execution tracking and queries.
/// Manages execution history and provides execution-specific queries.
/// </summary>
public sealed class ExecutionRepository : Repository<JobExecution>, IExecutionRepository
{
    public ExecutionRepository(JobSchedulerContext context) : base(context) { }

    public async Task<JobExecution?> GetLatestExecutionAsync(Guid jobId)
    {
        return await _dbSet
            .Where(e => e.JobId == jobId)
            .OrderByDescending(e => e.StartedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<JobExecution>> GetExecutionsByJobAsync(Guid jobId)
    {
        return await _dbSet
            .Where(e => e.JobId == jobId)
            .OrderByDescending(e => e.StartedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<JobExecution>> GetExecutionsByStatusAsync(ExecutionStatus status)
    {
        return await _dbSet
            .Where(e => e.Status == status)
            .OrderByDescending(e => e.StartedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<JobExecution>> GetExecutionsByJobAndStatusAsync(Guid jobId, ExecutionStatus status)
    {
        return await _dbSet
            .Where(e => e.JobId == jobId && e.Status == status)
            .OrderByDescending(e => e.StartedAt)
            .ToListAsync();
    }

    public async Task<int> GetCurrentlyRunningCountAsync(Guid jobId)
    {
        return await _dbSet
            .CountAsync(e => e.JobId == jobId && e.Status == ExecutionStatus.Running);
    }

    public async Task<int> GetConcurrentRunningCountAsync()
    {
        return await _dbSet
            .CountAsync(e => e.Status == ExecutionStatus.Running);
    }

    public async Task<IEnumerable<JobExecution>> GetRunningExecutionsAsync()
    {
        return await _dbSet
            .Where(e => e.Status == ExecutionStatus.Running)
            .OrderBy(e => e.StartedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<JobExecution>> GetFailedExecutionsRequiringRetryAsync()
    {
        return await _dbSet
            .Where(e => e.Status == ExecutionStatus.Failed && e.IsRetryable)
            .OrderBy(e => e.CompletedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<JobExecution>> GetExecutionsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Where(e => e.StartedAt >= startDate && e.StartedAt <= endDate)
            .OrderByDescending(e => e.StartedAt)
            .ToListAsync();
    }

    public async Task<long> GetAverageExecutionTimeAsync(Guid jobId, int? lastN = null)
    {
        var query = _dbSet
            .Where(e => e.JobId == jobId && e.Status == ExecutionStatus.Success);

        if (lastN.HasValue)
        {
            query = (IQueryable<JobExecution>)query
                .OrderByDescending(e => e.StartedAt)
                .Take(lastN.Value);
        }

        var executions = await query.ToListAsync();

        if (!executions.Any())
            return 0;

        return (long)executions.Average(e => e.DurationMilliseconds);
    }
}
