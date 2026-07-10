#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using Microsoft.EntityFrameworkCore;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Constants;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace JobScheduler.Core.Data;

/// <summary>
/// Extension methods for <see cref="JobSchedulerContext"/> providing convenient database operations
/// and query helpers for common job scheduler scenarios.
/// </summary>
public static class JobSchedulerContextExtensions
{
    /// <summary>
    /// Finds the next job that should be executed based on scheduling rules and priority.
    /// Returns null if no jobs are ready to execute.
    /// </summary>
    /// <param name="context">The database context</param>
    /// <param name="now">Current time for scheduling calculations</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Job ready for execution or null</returns>
    public static async Task<Job?> FindNextExecutableJobAsync(
        this JobSchedulerContext context,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        return await context.Jobs
            .Where(j => j.IsActive && j.Status == JobStatus.Scheduled)
            .Where(j => j.NextExecutionAt.HasValue && j.NextExecutionAt <= now)
            .OrderBy(j => j.CalculateEffectivePriority(now))
            .ThenBy(j => j.Priority)
            .ThenBy(j => j.NextExecutionAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Gets all jobs that are currently executing or have pending executions within the specified time window.
    /// </summary>
    /// <param name="context">The database context</param>
    /// <param name="timeWindowMinutes">Time window in minutes to look back for executions</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of jobs with recent or current executions</returns>
    public static async Task<List<Job>> GetJobsWithRecentExecutionsAsync(
        this JobSchedulerContext context,
        int timeWindowMinutes = 30,
        CancellationToken cancellationToken = default)
    {
        var cutoffTime = DateTime.UtcNow.AddMinutes(-timeWindowMinutes);

        return await context.Jobs
            .Include(j => j.Executions)
            .Where(j => j.Executions.Any(e => e.StartedAt >= cutoffTime))
            .OrderByDescending(j => j.Executions.Max(e => e.StartedAt))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Creates a new job execution record for the specified job and marks it as started.
    /// </summary>
    /// <param name="context">The database context</param>
    /// <param name="jobId">ID of the job to execute</param>
    /// <param name="executorName">Name of the executor/handler</param>
    /// <param name="executorInstance">Instance identifier for distributed tracking</param>
    /// <param name="attemptNumber">Retry attempt number (1 for first execution)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created job execution entity</returns>
    public static async Task<JobExecution> CreateJobExecutionAsync(
        this JobSchedulerContext context,
        Guid jobId,
        string executorName,
        string? executorInstance = null,
        int attemptNumber = 1,
        CancellationToken cancellationToken = default)
    {
        var jobExecution = new JobExecution
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            ExecutorName = executorName,
            ExecutorInstance = executorInstance ?? Environment.MachineName,
            AttemptNumber = attemptNumber,
            Status = ExecutionStatus.Running,
            StartedAt = DateTime.UtcNow
        };

        context.JobExecutions.Add(jobExecution);
        await context.SaveChangesAsync(cancellationToken);

        return jobExecution;
    }

    /// <summary>
    /// Gets execution statistics for a specific job including success rate and recent performance.
    /// </summary>
    /// <param name="context">The database context</param>
    /// <param name="jobId">ID of the job</param>
    /// <param name="lookbackDays">Number of days to look back for metrics</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Execution statistics for the job</returns>
    public static async Task<JobExecutionStats> GetJobExecutionStatsAsync(
        this JobSchedulerContext context,
        Guid jobId,
        int lookbackDays = 7,
        CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-lookbackDays);

        var executions = await context.JobExecutions
            .Where(e => e.JobId == jobId && e.StartedAt >= cutoffDate)
            .ToListAsync(cancellationToken);

        var totalExecutions = executions.Count;
        var successfulExecutions = executions.Count(e => e.Status == ExecutionStatus.Success);
        var failedExecutions = executions.Count(e => e.Status == ExecutionStatus.Failed);
        var avgDurationMs = executions.Where(e => e.DurationMilliseconds > 0)
            .Average(e => (double)e.DurationMilliseconds);

        var metrics = await context.ExecutionMetrics
            .Where(m => m.JobId == jobId)
            .OrderByDescending(m => m.CalculatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return new JobExecutionStats
        {
            JobId = jobId,
            TotalExecutions = totalExecutions,
            SuccessfulExecutions = successfulExecutions,
            FailedExecutions = failedExecutions,
            SuccessRate = totalExecutions > 0 ? (double)successfulExecutions / totalExecutions * 100 : 0,
            AverageDurationMs = avgDurationMs,
            LastExecutionTime = executions.Max(e => e.StartedAt),
            LastSuccessTime = executions.Where(e => e.Status == ExecutionStatus.Success)
                .Max(e => e.CompletedAt),
            LastFailureTime = executions.Where(e => e.Status == ExecutionStatus.Failed)
                .Max(e => e.CompletedAt),
            CurrentMetrics = metrics
        };
    }
}

/// <summary>
/// Container for job execution statistics returned by GetJobExecutionStatsAsync.
/// </summary>
public class JobExecutionStats
{
    public Guid JobId { get; set; }
    public int TotalExecutions { get; set; }
    public int SuccessfulExecutions { get; set; }
    public int FailedExecutions { get; set; }
    public double SuccessRate { get; set; }
    public double? AverageDurationMs { get; set; }
    public DateTime? LastExecutionTime { get; set; }
    public DateTime? LastSuccessTime { get; set; }
    public DateTime? LastFailureTime { get; set; }
    public ExecutionMetrics? CurrentMetrics { get; set; }
}