#nullable enable
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
public sealed class RetryService
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
    public ValueTask<bool> ShouldRetryAsync(Job job, JobExecution execution)
    {
        if (job is null || execution is null)
            return ValueTask.FromResult(false);

        if (execution.AttemptNumber > job.MaxRetries)
        {
            _logger?.LogInformation("Job {JobId} execution {ExecutionId} exceeded max retries ({MaxRetries})",
                job.Id, execution.Id, job.MaxRetries);
            return ValueTask.FromResult(false);
        }

        if (!execution.IsRetryable)
        {
            _logger?.LogWarning("Job {JobId} execution {ExecutionId} marked as not retryable", job.Id, execution.Id);
            return ValueTask.FromResult(false);
        }

        return ValueTask.FromResult(true);
    }

    /// <summary>
    /// Calculates the next retry time based on job's retry policy.
    /// </summary>
    public DateTime CalculateNextRetryTime(Job job, JobExecution failedExecution)
    {
        if (job is null || failedExecution is null)
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
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public int CalculateBackoffDelay(Job job, int attemptNumber)
    {
        // Fix: Ensure job and attemptNumber are valid inputs.
        if (job is null) throw new ArgumentNullException(nameof(job));
        if (attemptNumber < 0) throw new ArgumentOutOfRangeException(nameof(attemptNumber), "Attempt number cannot be negative.");

        int baseDelay = job.RetryBackoffSeconds;
        // Fix: Ensure a minimum base delay of 1 second if RetryBackoffSeconds is not configured or is zero.
        if (baseDelay <= 0)
        {
            baseDelay = 1;
        }

        // Simple exponential backoff: initial_delay * (2 ^ (attempt - 1))
        // attemptNumber is the 1-based attempt number that failed. For the first retry, attemptNumber will be 1.
        var delay = (int)(baseDelay * Math.Pow(2, attemptNumber - 1));

        // Fix: Ensure minimum delay is 1 second after calculation to prevent immediate retries.
        if (delay <= 0)
        {
            delay = 1;
        }

        // Cap at job's timeout seconds to prevent unreasonable delays, also ensuring it's at least 1.
        return Math.Max(1, Math.Min(delay, job.ExecutionTimeoutSeconds));
    }

    /// <summary>
    /// Prepares a job execution for retry by incrementing attempt and resetting status.
    /// </summary>
    public JobExecution CreateRetryExecution(Job job, JobExecution failedExecution)
    {
        if (job is null || failedExecution is null)
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
            TotalRetries = failedExecutions.Count > 0
                ? failedExecutions.Sum(e => e.AttemptNumber - 1)
                : 0,
            AverageRetriesPerFailure = failedExecutions.Count > 0
                ? failedExecutions.Average(e => e.AttemptNumber - 1)
                : 0,
            LastFailureTime = failedExecutions.Count > 0
                ? failedExecutions.Max(e => (DateTime?)e.CompletedAt)
                : null,
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

    /// <summary>
    /// Calculates the retry delay for a given attempt number under the given backoff strategy.
    /// Standalone helper (independent of job/execution state) used by callers that want to
    /// preview delays without a full <see cref="Job"/>/<see cref="JobExecution"/> pair.
    /// </summary>
    public TimeSpan CalculateRetryDelay(int attemptNumber, JobRetryBackoffStrategy strategy, int baseDelaySeconds = 5)
    {
        var seconds = strategy switch
        {
            JobRetryBackoffStrategy.Exponential => baseDelaySeconds * Math.Pow(2, attemptNumber),
            JobRetryBackoffStrategy.Linear => baseDelaySeconds * (attemptNumber + 1),
            JobRetryBackoffStrategy.Fixed => baseDelaySeconds,
            _ => baseDelaySeconds
        };

        return TimeSpan.FromSeconds(seconds);
    }

    /// <summary>
    /// Determines if another retry attempt is allowed given a simple attempt/limit pair.
    /// </summary>
    public bool ShouldRetry(int currentAttempts, int maxRetries)
    {
        return currentAttempts < maxRetries;
    }

    /// <summary>
    /// Calculates the cumulative delay across all retry attempts for a given backoff strategy.
    /// </summary>
    public TimeSpan CalculateTotalRetryTime(int retries, JobRetryBackoffStrategy strategy, int baseDelaySeconds)
    {
        var total = TimeSpan.Zero;
        for (var attempt = 0; attempt < retries; attempt++)
            total += CalculateRetryDelay(attempt, strategy, baseDelaySeconds);

        return total;
    }

    /// <summary>
    /// Formats a human-readable retry message for logging or notifications.
    /// </summary>
    public string FormatRetryMessage(int attemptNumber, TimeSpan delay, string serverName)
    {
        return $"Retry attempt {attemptNumber} scheduled in {delay.TotalSeconds:F0}s on server '{serverName}'.";
    }
}

/// <summary>
/// Statistics about retry behavior for a job.
/// </summary>
public sealed class RetryStatistics
{
    public Guid JobId { get; set; }
    public int TotalExecutions { get; set; }
    public int TotalFailures { get; set; }
    public int TotalRetries { get; set; }
    public double AverageRetriesPerFailure { get; set; }
    public DateTime? LastFailureTime { get; set; }
    public double RecentFailureRate { get; set; }
}

/// <summary>
/// Backoff strategy used when computing standalone retry delays via
/// <see cref="RetryService.CalculateRetryDelay"/>.
/// </summary>
public enum JobRetryBackoffStrategy
{
    /// <summary>Delay doubles with each attempt.</summary>
    Exponential,

    /// <summary>Delay grows linearly with each attempt.</summary>
    Linear,

    /// <summary>Delay stays constant across attempts.</summary>
    Fixed
}

/// <summary>
/// Describes a retry policy configuration. Used by callers that want to pass
/// a policy object instead of individual parameters to retry helpers.
/// </summary>
public interface IRetryPolicy
{
    /// <summary>Maximum number of retry attempts allowed.</summary>
    int MaxAttempts { get; set; }

    /// <summary>Backoff strategy to apply between attempts.</summary>
    JobRetryBackoffStrategy BackoffStrategy { get; set; }

    /// <summary>Base delay, in seconds, used by the backoff calculation.</summary>
    int BaseDelaySeconds { get; set; }
}
