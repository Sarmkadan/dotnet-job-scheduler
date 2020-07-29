// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using JobScheduler.Core.Constants;
using JobScheduler.Core.Data.Repositories;
using JobScheduler.Core.Domain.Entities;

namespace JobScheduler.Core.Services;

/// <summary>
/// Service for handling job retry logic, backoff strategies, and retry scheduling.
/// Manages exponential, linear, and fixed backoff delays.
/// </summary>
public class RetryService
{
    private readonly IJobRepository _jobRepository;
    private readonly IExecutionRepository _executionRepository;
    private readonly ILogger<RetryService>? _logger;

    public RetryService(
        IJobRepository jobRepository,
        IExecutionRepository executionRepository,
        ILogger<RetryService>? logger = null)
    {
        _jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
        _executionRepository = executionRepository ?? throw new ArgumentNullException(nameof(executionRepository));
        _logger = logger;
    }

    /// <summary>
    /// Determines if a failed execution should be retried.
    /// </summary>
    public async Task<bool> ShouldRetryAsync(Job job, JobExecution execution)
    {
        if (job == null || execution == null)
            return false;

        // Check if max retries exceeded
        if (execution.AttemptNumber > job.MaxRetries)
        {
            _logger?.LogInformation("Job {JobId} execution {ExecutionId} exceeded max retries ({MaxRetries})",
                job.Id, execution.Id, job.MaxRetries);
            return false;
        }

        // Only retry if execution was marked as retryable
        if (!execution.IsRetryable)
        {
            _logger?.LogWarning("Job {JobId} execution {ExecutionId} marked as not retryable", job.Id, execution.Id);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Calculates the next retry time based on job's retry policy.
    /// </summary>
    public DateTime CalculateNextRetryTime(Job job, JobExecution failedExecution)
    {
        if (job == null || failedExecution == null)
            throw new ArgumentNullException(nameof(job));

        var delaySeconds = CalculateBackoffDelay(job, failedExecution.AttemptNumber);
        var nextRetryTime = failedExecution.CompletedAt!.Value.AddSeconds(delaySeconds);

        _logger?.LogDebug("Job {JobId} scheduled for retry at {RetryTime} (delay: {DelaySeconds}s, attempt: {Attempt})",
            job.Id, nextRetryTime, delaySeconds, failedExecution.AttemptNumber + 1);

        return nextRetryTime;
    }

    /// <summary>
    /// Calculates the backoff delay for a retry attempt.
    /// </summary>
    public int CalculateBackoffDelay(Job job, int attemptNumber)
    {
        if (attemptNumber <= 0)
            return job.RetryBackoffSeconds;

        // Simple exponential backoff: initial_delay * (2 ^ (attempt - 1))
        var delay = (int)(job.RetryBackoffSeconds * Math.Pow(2, attemptNumber - 1));

        // Cap at job's timeout seconds to prevent unreasonable delays
        return Math.Min(delay, job.ExecutionTimeoutSeconds);
    }

    /// <summary>
    /// Prepares a job execution for retry by incrementing attempt and resetting status.
    /// </summary>
    public JobExecution CreateRetryExecution(Job job, JobExecution failedExecution)
    {
        if (job == null || failedExecution == null)
            throw new ArgumentNullException(nameof(job));

        var retryExecution = new JobExecution
        {
            Id = Guid.NewGuid(),
            JobId = job.Id,
            Status = Constants.ExecutionStatus.Running,
            StartedAt = DateTime.UtcNow,
            AttemptNumber = failedExecution.AttemptNumber + 1,
            ExecutorName = failedExecution.ExecutorName,
            IsRetryable = true
        };

        _logger?.LogInformation("Created retry execution {ExecutionId} for job {JobId} (attempt {Attempt})",
            retryExecution.Id, job.Id, retryExecution.AttemptNumber);

        return retryExecution;
    }

    /// <summary>
    /// Checks if retry budget has been exceeded (too many retries in short period).
    /// </summary>
    public async Task<bool> IsRetryBudgetExceededAsync(Guid jobId, int retryBudgetCount = 5, int timeWindowMinutes = 5)
    {
        var startTime = DateTime.UtcNow.AddMinutes(-timeWindowMinutes);
        var recentFailures = await _executionRepository.GetExecutionsByDateRangeAsync(startTime, DateTime.UtcNow);

        var failureCount = recentFailures
            .Count(e => e.JobId == jobId && e.Status == ExecutionStatus.Failed);

        return failureCount > retryBudgetCount;
    }

    /// <summary>
    /// Gets retry statistics for a job.
    /// </summary>
    public async Task<RetryStatistics> GetRetryStatisticsAsync(Guid jobId)
    {
        var executions = await _executionRepository.GetExecutionsByJobAsync(jobId);
        var failedExecutions = executions.Where(e => e.Status == ExecutionStatus.Failed).ToList();

        var stats = new RetryStatistics
        {
            JobId = jobId,
            TotalExecutions = executions.Count(),
            TotalFailures = failedExecutions.Count,
            TotalRetries = failedExecutions.Sum(e => e.AttemptNumber - 1),
            AverageRetriesPerFailure = failedExecutions.Count > 0
                ? failedExecutions.Average(e => e.AttemptNumber - 1)
                : 0,
            LastFailureTime = failedExecutions.Max(e => (DateTime?)e.CompletedAt),
            RecentFailureRate = CalculateRecentFailureRate(executions)
        };

        return stats;
    }

    private double CalculateRecentFailureRate(IEnumerable<JobExecution> executions)
    {
        var recentExecutions = executions
            .Where(e => e.StartedAt > DateTime.UtcNow.AddHours(-1))
            .ToList();

        if (!recentExecutions.Any())
            return 0;

        var failureCount = recentExecutions.Count(e => e.Status == ExecutionStatus.Failed);
        return (double)failureCount / recentExecutions.Count * 100;
    }
}

/// <summary>
/// Statistics about retry behavior for a job.
/// </summary>
public class RetryStatistics
{
    public Guid JobId { get; set; }
    public int TotalExecutions { get; set; }
    public int TotalFailures { get; set; }
    public int TotalRetries { get; set; }
    public double AverageRetriesPerFailure { get; set; }
    public DateTime? LastFailureTime { get; set; }
    public double RecentFailureRate { get; set; }
}
