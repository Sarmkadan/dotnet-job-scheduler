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
    Task<JobExecution?> GetLatestExecutionAsync(Guid jobId);
    Task<IEnumerable<JobExecution>> GetExecutionsByJobAsync(Guid jobId);
    Task<IEnumerable<JobExecution>> GetExecutionsByStatusAsync(ExecutionStatus status);
    Task<IEnumerable<JobExecution>> GetExecutionsByJobAndStatusAsync(Guid jobId, ExecutionStatus status);
    Task<int> GetCurrentlyRunningCountAsync(Guid jobId);
    Task<int> GetConcurrentRunningCountAsync();
    Task<IEnumerable<JobExecution>> GetRunningExecutionsAsync();
    Task<IEnumerable<JobExecution>> GetFailedExecutionsRequiringRetryAsync();
    Task<IEnumerable<JobExecution>> GetExecutionsByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<long> GetAverageExecutionTimeAsync(Guid jobId, int? lastN = null);
}
