#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using JobScheduler.Core.Constants;

namespace JobScheduler.Core.Domain.Entities;

/// <summary>
/// Defines the retry behavior and backoff strategy for failed job executions.
/// Supports exponential, linear, and fixed backoff strategies.
/// </summary>
public class RetryPolicy
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid JobId { get; set; }

    public int MaxRetries { get; set; } = SchedulerConstants.DefaultMaxRetries;

    public int InitialBackoffSeconds { get; set; } = SchedulerConstants.DefaultRetryBackoffSeconds;

    public int MaxBackoffSeconds { get; set; } = SchedulerConstants.DefaultMaxRetryBackoffSeconds;

    public BackoffStrategy Strategy { get; set; } = BackoffStrategy.Exponential;

    public double BackoffMultiplier { get; set; } = SchedulerConstants.RetryBackoffMultiplier;

    public bool RetryOnTimeout { get; set; } = true;

    public bool RetryOnCancellation { get; set; } = false;

    public string? RetryableExceptions { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Calculates the delay before the next retry attempt based on the strategy.
    /// </summary>
    public int CalculateBackoffDelay(int attemptNumber)
    {
        if (attemptNumber <= 0)
            return InitialBackoffSeconds;

        int delay = Strategy switch
        {
            BackoffStrategy.Fixed => InitialBackoffSeconds,
            BackoffStrategy.Linear => InitialBackoffSeconds * attemptNumber,
            BackoffStrategy.Exponential => (int)(InitialBackoffSeconds * Math.Pow(BackoffMultiplier, attemptNumber - 1)),
            _ => InitialBackoffSeconds
        };

        return Math.Min(delay, MaxBackoffSeconds);
    }

    /// <summary>
    /// Determines if a retry should be attempted based on exception type.
    /// </summary>
    public bool ShouldRetryOnException(string exceptionType)
    {
        if (string.IsNullOrWhiteSpace(RetryableExceptions))
            return true;

        var retryableTypes = RetryableExceptions.Split(',', StringSplitOptions.TrimEntries);
        return retryableTypes.Any(rt => exceptionType.Contains(rt));
    }

    /// <summary>
    /// Gets the next scheduled retry time for an execution.
    /// </summary>
    public DateTime GetNextRetryTime(DateTime lastFailureTime, int attemptNumber)
    {
        int backoffSeconds = CalculateBackoffDelay(attemptNumber);
        return lastFailureTime.AddSeconds(backoffSeconds);
    }

    /// <summary>
    /// Validates the retry policy configuration.
    /// </summary>
    public bool IsValid()
    {
        if (MaxRetries < 0 || MaxRetries > 100)
            return false;

        if (InitialBackoffSeconds <= 0 || InitialBackoffSeconds > MaxBackoffSeconds)
            return false;

        if (BackoffMultiplier < 1.0 || BackoffMultiplier > 10.0)
            return false;

        return true;
    }

    /// <summary>
    /// Gets a string description of the retry strategy.
    /// </summary>
    public string GetStrategyDescription()
    {
        return Strategy switch
        {
            BackoffStrategy.Fixed => $"Fixed backoff: {InitialBackoffSeconds}s between retries",
            BackoffStrategy.Linear => $"Linear backoff: increases by {InitialBackoffSeconds}s per attempt",
            BackoffStrategy.Exponential => $"Exponential backoff: multiplier {BackoffMultiplier}x",
            _ => "Unknown strategy"
        };
    }
}

/// <summary>
/// Enum for different retry backoff strategies.
/// </summary>
public enum BackoffStrategy
{
    /// <summary>Fixed delay between retries</summary>
    Fixed = 0,

    /// <summary>Linear increase in delay per retry attempt</summary>
    Linear = 1,

    /// <summary>Exponential increase in delay per retry attempt</summary>
    Exponential = 2
}
