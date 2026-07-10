#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using JobScheduler.Core.Domain.Entities;

namespace JobScheduler.Core.Domain.Entities;

/// <summary>
/// Provides extension methods for <see cref="RetryPolicy"/> to enhance retry behavior functionality.
/// </summary>
public static class RetryPolicyExtensions
{
    /// <summary>
    /// Determines if the retry policy should attempt a retry based on the current attempt count.
    /// </summary>
    /// <param name="policy">The retry policy instance.</param>
    /// <param name="attemptNumber">The current attempt number (1-based).</param>
    /// <returns>True if another retry should be attempted; otherwise false.</returns>
    public static bool ShouldRetry(this RetryPolicy policy, int attemptNumber)
    {
        if (policy == null)
            throw new ArgumentNullException(nameof(policy));

        return attemptNumber <= policy.MaxRetries;
    }

    /// <summary>
    /// Gets the total delay time that would be accumulated if all retries were to occur.
    /// Useful for determining if a retry policy will exceed a specific timeout threshold.
    /// </summary>
    /// <param name="policy">The retry policy instance.</param>
    /// <returns>The total accumulated delay in seconds across all retry attempts.</returns>
    public static int GetTotalAccumulatedDelay(this RetryPolicy policy)
    {
        if (policy == null)
            throw new ArgumentNullException(nameof(policy));

        if (policy.MaxRetries <= 0)
            return 0;

        int totalDelay = 0;
        for (int attempt = 1; attempt <= policy.MaxRetries; attempt++)
        {
            totalDelay += policy.CalculateBackoffDelay(attempt);
        }

        return totalDelay;
    }

    /// <summary>
    /// Gets the retry attempts configuration as a human-readable summary.
    /// Useful for logging and debugging purposes.
    /// </summary>
    /// <param name="policy">The retry policy instance.</param>
    /// <returns>A formatted string describing the retry configuration.</returns>
    public static string GetRetrySummary(this RetryPolicy policy)
    {
        if (policy == null)
            throw new ArgumentNullException(nameof(policy));

        return $"RetryPolicy: {policy.Strategy} - MaxRetries: {policy.MaxRetries}, " +
               $"InitialBackoff: {policy.InitialBackoffSeconds}s, " +
               $"MaxBackoff: {policy.MaxBackoffSeconds}s, " +
               $"Multiplier: {policy.BackoffMultiplier}x";
    }

    /// <summary>
    /// Creates a new retry policy with adjusted parameters based on the current policy.
    /// Useful for dynamically modifying retry behavior without creating a new policy from scratch.
    /// </summary>
    /// <param name="policy">The retry policy instance.</param>
    /// <param name="maxRetries">Optional new max retries value. If null, keeps current value.</param>
    /// <param name="initialBackoffSeconds">Optional new initial backoff in seconds. If null, keeps current value.</param>
    /// <param name="strategy">Optional new backoff strategy. If null, keeps current value.</param>
    /// <returns>A new RetryPolicy instance with the specified adjustments.</returns>
    public static RetryPolicy WithAdjustedParameters(
        this RetryPolicy policy,
        int? maxRetries = null,
        int? initialBackoffSeconds = null,
        BackoffStrategy? strategy = null)
    {
        if (policy == null)
            throw new ArgumentNullException(nameof(policy));

        return new RetryPolicy
        {
            Id = Guid.NewGuid(),
            JobId = policy.JobId,
            MaxRetries = maxRetries ?? policy.MaxRetries,
            InitialBackoffSeconds = initialBackoffSeconds ?? policy.InitialBackoffSeconds,
            MaxBackoffSeconds = policy.MaxBackoffSeconds,
            Strategy = strategy ?? policy.Strategy,
            BackoffMultiplier = policy.BackoffMultiplier,
            RetryOnTimeout = policy.RetryOnTimeout,
            RetryOnCancellation = policy.RetryOnCancellation,
            RetryableExceptions = policy.RetryableExceptions,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}