#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using JobScheduler.Core.Domain.Models;

namespace JobScheduler.Core.Domain.Models;

/// <summary>
/// Extension methods for <see cref="JobExecutionSummary"/> providing additional
/// functionality for analyzing job execution statistics.
/// </summary>
public static class JobExecutionSummaryExtensions
{
    /// <summary>
    /// Calculates the failure rate percentage for the job execution.
    /// </summary>
    /// <param name="summary">The job execution summary to analyze.</param>
    /// <returns>Failure rate as percentage (0-100), or 0 if no executions.</returns>
    public static double GetFailureRate(this JobExecutionSummary summary)
    {
        if (summary.TotalExecutions == 0)
        {
            return 0;
        }

        return (double)summary.FailureCount / summary.TotalExecutions * 100;
    }

    /// <summary>
    /// Calculates the percentage of executions that were either timed out or cancelled.
    /// </summary>
    /// <param name="summary">The job execution summary to analyze.</param>
    /// <returns>Timeout/cancelled rate as percentage (0-100), or 0 if no executions.</returns>
    public static double GetTimeoutCancelledRate(this JobExecutionSummary summary)
    {
        if (summary.TotalExecutions == 0)
        {
            return 0;
        }

        var timeoutCancelledCount = summary.TimedOutCount + summary.CancelledCount;
        return (double)timeoutCancelledCount / summary.TotalExecutions * 100;
    }

    /// <summary>
    /// Determines if the job execution has any failures (failed, timed out, or cancelled).
    /// </summary>
    /// <param name="summary">The job execution summary to check.</param>
    /// <returns>True if there are any failures; otherwise, false.</returns>
    public static bool HasFailures(this JobExecutionSummary summary)
    {
        return summary.FailureCount > 0 || summary.TimedOutCount > 0 || summary.CancelledCount > 0;
    }

    /// <summary>
    /// Gets the duration range (min to max) for job executions.
    /// </summary>
    /// <param name="summary">The job execution summary to analyze.</param>
    /// <returns>A tuple containing (MinDurationMs, MaxDurationMs).</returns>
    public static (long Min, long Max) GetDurationRange(this JobExecutionSummary summary)
    {
        return (summary.MinDurationMs, summary.MaxDurationMs);
    }

    /// <summary>
    /// Calculates the standard deviation of execution durations (simplified estimation).
    /// Uses the range-based approximation: stdDev ≈ (max - min) / 6 for normal distributions.
    /// </summary>
    /// <param name="summary">The job execution summary to analyze.</param>
    /// <returns>Estimated standard deviation of execution durations in milliseconds.</returns>
    public static double GetDurationStandardDeviation(this JobExecutionSummary summary)
    {
        if (summary.TotalExecutions == 0 || summary.MinDurationMs == summary.MaxDurationMs)
        {
            return 0;
        }

        // Range-based approximation for standard deviation
        // For a normal distribution, 99.7% of values fall within ±3σ
        // So σ ≈ range / 6
        return (summary.MaxDurationMs - summary.MinDurationMs) / 6.0;
    }
}