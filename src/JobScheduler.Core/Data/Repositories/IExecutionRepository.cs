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
/// Repository for job execution tracking and queries.
/// Provides methods to retrieve execution history and status.
/// </summary>
public interface IExecutionRepository : IRepository<JobExecution>
{
    /// <summary>
    /// Gets the latest execution for a given job.
    /// </summary>
    /// <param name="jobId">The identifier of the job.</param>
    /// <returns>The latest job execution, or null if none exists.</returns>
    Task<JobExecution?> GetLatestExecutionAsync(Guid jobId);

    /// <summary>
    /// Gets all executions for a given job, ordered by start time descending.
    /// </summary>
    /// <param name="jobId">The identifier of the job.</param>
    /// <returns>An enumerable of job executions for the specified job.</returns>
    Task<IEnumerable<JobExecution>> GetExecutionsByJobAsync(Guid jobId);

    /// <summary>
    /// Gets all executions with the specified status, ordered by start time descending.
    /// </summary>
    /// <param name="status">The execution status to filter by.</param>
    /// <returns>An enumerable of job executions with the specified status.</returns>
    Task<IEnumerable<JobExecution>> GetExecutionsByStatusAsync(ExecutionStatus status);

    /// <summary>
    /// Gets all executions for a given job and status, ordered by start time descending.
    /// </summary>
    /// <param name="jobId">The identifier of the job.</param>
    /// <param name="status">The execution status to filter by.</param>
    /// <returns>An enumerable of job executions for the specified job and status.</returns>
    Task<IEnumerable<JobExecution>> GetExecutionsByJobAndStatusAsync(Guid jobId, ExecutionStatus status);

    /// <summary>
    /// Gets the count of currently running executions for a given job.
    /// </summary>
    /// <param name="jobId">The identifier of the job.</param>
    /// <returns>The number of executions that are currently running for the specified job.</returns>
    Task<int> GetCurrentlyRunningCountAsync(Guid jobId);

    /// <summary>
    /// Gets the total count of currently running executions across all jobs.
    /// </summary>
    /// <returns>The number of executions that are currently running.</returns>
    Task<int> GetConcurrentRunningCountAsync();

    /// <summary>
    /// Gets all executions that are currently running, ordered by start time ascending.
    /// </summary>
    /// <returns>An enumerable of job executions that are currently running.</returns>
    Task<IEnumerable<JobExecution>> GetRunningExecutionsAsync();

    /// <summary>
    /// Gets all failed executions that are retryable, ordered by completion time ascending.
    /// </summary>
    /// <returns>An enumerable of failed job executions that require retry.</returns>
    Task<IEnumerable<JobExecution>> GetFailedExecutionsRequiringRetryAsync();

    /// <summary>
    /// Gets all executions that started within the specified date range, ordered by start time descending.
    /// </summary>
    /// <param name="startDate">The start of the date range (inclusive).</param>
    /// <param name="endDate">The end of the date range (inclusive).</param>
    /// <returns>An enumerable of job executions that started within the specified date range.</returns>
    Task<IEnumerable<JobExecution>> GetExecutionsByDateRangeAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Gets the average execution time (in milliseconds) for successful executions of a given job.
    /// Optionally, only the last N executions are considered.
    /// </summary>
    /// <param name="jobId">The identifier of the job.</param>
    /// <param name="lastN">The number of most recent successful executions to consider for the average. If null, all successful executions are considered.</param>
    /// <returns>The average execution time in milliseconds, or 0 if there are no successful executions.</returns>
    Task<long> GetAverageExecutionTimeAsync(Guid jobId, int? lastN = null);

    /// <summary>
    /// Returns all executions for a job as a materialized list, newest first.
    /// Used by reporting/statistics code that needs list-style access (e.g. Count).
    /// </summary>
    /// <param name="jobId">The identifier of the job.</param>
    /// <returns>A list of job executions for the specified job, newest first.</returns>
    Task<List<JobExecution>> GetByJobIdAsync(Guid jobId);
}
