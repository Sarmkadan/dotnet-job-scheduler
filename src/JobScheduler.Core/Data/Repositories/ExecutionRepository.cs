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
    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionRepository"/> class.
    /// </summary>
    /// <param name="context">The database context to use for execution queries.</param>
    public ExecutionRepository(JobSchedulerContext context) : base(context)
    {
        ArgumentNullException.ThrowIfNull(context);
    }

    /// <summary>
    /// Gets the latest execution for the specified job.
    /// </summary>
    /// <param name="jobId">The identifier of the job to get the latest execution for.</param>
    /// <returns>The latest execution for the job, or null if no executions exist.</returns>
    public async Task<JobExecution?> GetLatestExecutionAsync(Guid jobId)
    {
        return await _dbSet
            .Where(e => e.JobId == jobId)
            .OrderByDescending(e => e.StartedAt)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Gets all executions for the specified job, ordered by start time descending.
    /// </summary>
    /// <param name="jobId">The identifier of the job to get executions for.</param>
    /// <returns>A collection of executions for the job, ordered by start time descending.</returns>
    public async Task<IEnumerable<JobExecution>> GetExecutionsByJobAsync(Guid jobId)
    {
        return await _dbSet
            .Where(e => e.JobId == jobId)
            .OrderByDescending(e => e.StartedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all executions with the specified status, ordered by start time descending.
    /// </summary>
    /// <param name="status">The execution status to filter by.</param>
    /// <returns>A collection of executions with the specified status, ordered by start time descending.</returns>
    public async Task<IEnumerable<JobExecution>> GetExecutionsByStatusAsync(ExecutionStatus status)
    {
        return await _dbSet
            .Where(e => e.Status == status)
            .OrderByDescending(e => e.StartedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all executions for the specified job with the specified status, ordered by start time descending.
    /// </summary>
    /// <param name="jobId">The identifier of the job to filter by.</param>
    /// <param name="status">The execution status to filter by.</param>
    /// <returns>A collection of executions for the job with the specified status, ordered by start time descending.</returns>
    public async Task<IEnumerable<JobExecution>> GetExecutionsByJobAndStatusAsync(Guid jobId, ExecutionStatus status)
    {
        return await _dbSet
            .Where(e => e.JobId == jobId && e.Status == status)
            .OrderByDescending(e => e.StartedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Gets the count of currently running executions for the specified job.
    /// </summary>
    /// <param name="jobId">The identifier of the job to get the running count for.</param>
    /// <returns>The number of executions with a status of Running for the job.</returns>
    public async Task<int> GetCurrentlyRunningCountAsync(Guid jobId)
    {
        return await _dbSet
            .CountAsync(e => e.JobId == jobId && e.Status == ExecutionStatus.Running);
    }

    /// <summary>
    /// Gets the count of all currently running executions across all jobs.
    /// </summary>
    /// <returns>The number of executions with a status of Running across all jobs.</returns>
    public async Task<int> GetConcurrentRunningCountAsync()
    {
        return await _dbSet
            .CountAsync(e => e.Status == ExecutionStatus.Running);
    }

    /// <summary>
    /// Gets all currently running executions, ordered by start time ascending.
    /// </summary>
    /// <returns>A collection of executions with a status of Running, ordered by start time ascending.</returns>
    public async Task<IEnumerable<JobExecution>> GetRunningExecutionsAsync()
    {
        return await _dbSet
            .Where(e => e.Status == ExecutionStatus.Running)
            .OrderBy(e => e.StartedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all failed executions that are retryable, ordered by completion time ascending.
    /// </summary>
    /// <returns>A collection of failed executions that are retryable, ordered by completion time ascending.</returns>
    public async Task<IEnumerable<JobExecution>> GetFailedExecutionsRequiringRetryAsync()
    {
        return await _dbSet
            .Where(e => e.Status == ExecutionStatus.Failed && e.IsRetryable)
            .OrderBy(e => e.CompletedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all executions that started within the specified date range, ordered by start time descending.
    /// </summary>
    /// <param name="startDate">The start of the date range to filter by (inclusive).</param>
    /// <param name="endDate">The end of the date range to filter by (inclusive).</param>
    /// <returns>A collection of executions that started within the specified date range, ordered by start time descending.</returns>
    public async Task<IEnumerable<JobExecution>> GetExecutionsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Where(e => e.StartedAt >= startDate && e.StartedAt <= endDate)
            .OrderByDescending(e => e.StartedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Gets the average execution time in milliseconds for successful executions of the specified job.
    /// </summary>
    /// <param name="jobId">The identifier of the job to get the average execution time for.</param>
    /// <param name="lastN">Optional. The number of most recent successful executions to consider. If null, considers all successful executions.</param>
    /// <returns>The average execution time in milliseconds for successful executions, or 0 if no successful executions exist.</returns>
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

    /// <summary>
    /// Gets all executions for the specified job, ordered by start time descending.
    /// </summary>
    /// <param name="jobId">The identifier of the job to get executions for.</param>
    /// <returns>A list of executions for the job, ordered by start time descending.</returns>
    public async Task<List<JobExecution>> GetByJobIdAsync(Guid jobId)
    {
        return await _dbSet
            .Where(e => e.JobId == jobId)
            .OrderByDescending(e => e.StartedAt)
            .ToListAsync();
    }
}