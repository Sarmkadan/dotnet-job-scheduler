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
public sealed class RetryPolicy
{
    /// <summary>
    /// Unique identifier for the retry policy.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Identifier of the job this retry policy applies to.
    /// </summary>
    public Guid JobId { get; set; }

    /// <summary>
    /// Maximum number of retry attempts allowed.
    /// </summary>
    public int MaxRetries { get; set; } = SchedulerConstants.DefaultMaxRetries;

    /// <summary>
    /// Initial delay in seconds before the first retry attempt.
    /// </summary>
    public int InitialBackoffSeconds { get; set; } = SchedulerConstants.DefaultRetryBackoffSeconds;

    /// <summary>
    /// Maximum delay in seconds between retry attempts.
    /// </summary>
    public int MaxBackoffSeconds { get; set; } = SchedulerConstants.DefaultMaxRetryBackoffSeconds;

    /// <summary>
    /// The backoff strategy used to calculate retry delays.
    /// </summary>
    public BackoffStrategy Strategy { get; set; } = BackoffStrategy.Exponential;

    /// <summary>
    /// Multiplier applied to the backoff delay for exponential strategies.
    /// </summary>
    public double BackoffMultiplier { get; set; } = SchedulerConstants.RetryBackoffMultiplier;

    /// <summary>
    /// Indicates whether to retry on timeout exceptions.
    /// </summary>
    public bool RetryOnTimeout { get; set; } = true;

    /// <summary>
    /// Indicates whether to retry on cancellation exceptions.
    /// </summary>
    public bool RetryOnCancellation { get; set; } = false;

    /// <summary>
    /// Comma-separated list of exception types that are eligible for retry.
    /// </summary>
    public string? RetryableExceptions { get; set; }

    /// <summary>
    /// Timestamp when the retry policy was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the retry policy was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Calculates the delay before the next retry attempt based on the strategy.
    /// </summary>
    /// <param name="attemptNumber">The current retry attempt number.</param>
    /// <returns>The calculated backoff delay in seconds.</returns>
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
    /// <param name="exceptionType">The type of the exception that occurred.</param>
    /// <returns>True if the exception is retryable; otherwise, false.</returns>
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
    /// <param name="lastFailureTime">The time when the last execution failed.</param>
    /// <param name="attemptNumber">The current retry attempt number.</param>
    /// <returns>The scheduled time for the next retry attempt.</returns>
    public DateTime GetNextRetryTime(DateTime lastFailureTime, int attemptNumber)
    {
        int backoffSeconds = CalculateBackoffDelay(attemptNumber);
        return lastFailureTime.AddSeconds(backoffSeconds);
    }

    /// <summary>
    /// Validates the retry policy configuration.
    /// </summary>
    /// <returns>True if the configuration is valid; otherwise, false.</returns>
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
    /// <returns>A human-readable description of the backoff strategy.</returns>
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

    /// <summary>
    /// Returns a concise string representation of the retry policy.
    /// </summary>
    /// <returns>A string containing the policy's key configuration details.</returns>
    public override string ToString() => $"RetryPolicy {{ Id = {Id}, JobId = {JobId}, MaxRetries = {MaxRetries}, InitialBackoffSeconds = {InitialBackoffSeconds}, MaxBackoffSeconds = {MaxBackoffSeconds}, Strategy = {Strategy} }}";
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
