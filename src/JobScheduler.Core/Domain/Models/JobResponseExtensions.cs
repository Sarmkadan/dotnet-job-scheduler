#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using JobScheduler.Core.Constants;

namespace JobScheduler.Core.Domain.Models;

/// <summary>
/// Extension methods for JobResponse providing common job scheduling operations.
/// </summary>
public static class JobResponseExtensions
{
    /// <summary>
    /// Determines if the job is currently due for execution based on its cron expression and next execution time.
    /// </summary>
    /// <param name="job">The job response instance</param>
    /// <returns>True if the job should be executed now or in the past; otherwise false</returns>
    public static bool IsDueForExecution(this JobResponse job)
    {
        if (job is null)
        {
            throw new ArgumentNullException(nameof(job));
        }

        if (!job.IsActive)
        {
            return false;
        }

        if (job.NextExecutionAt is null)
        {
            return false;
        }

        return job.NextExecutionAt <= DateTime.UtcNow;
    }

    /// <summary>
    /// Calculates the estimated time remaining until the next scheduled execution.
    /// </summary>
    /// <param name="job">The job response instance</param>
    /// <returns>TimeSpan representing the duration until next execution, or TimeSpan.Zero if already due</returns>
    public static TimeSpan TimeUntilNextExecution(this JobResponse job)
    {
        if (job is null)
        {
            throw new ArgumentNullException(nameof(job));
        }

        if (job.NextExecutionAt is null)
        {
            return TimeSpan.Zero;
        }

        var nextExecution = job.NextExecutionAt.Value;
        var now = DateTime.UtcNow;

        return nextExecution > now ? nextExecution - now : TimeSpan.Zero;
    }

    /// <summary>
    /// Determines if the job has exceeded its maximum allowed execution attempts.
    /// </summary>
    /// <param name="job">The job response instance</param>
    /// <returns>True if the job has failed more times than its MaxRetries setting; otherwise false</returns>
    public static bool HasExceededMaxRetries(this JobResponse job)
    {
        if (job is null)
        {
            throw new ArgumentNullException(nameof(job));
        }

        return job.FailedExecutions > job.MaxRetries;
    }

    /// <summary>
    /// Gets a human-readable status message for the job based on its current state.
    /// </summary>
    /// <param name="job">The job response instance</param>
    /// <returns>Formatted status message describing the job's current state</returns>
    public static string GetStatusMessage(this JobResponse job)
    {
        if (job is null)
        {
            throw new ArgumentNullException(nameof(job));
        }

        var priority = job.Priority;
        var nextExecution = job.NextExecutionAt;
        var successRate = job.SuccessRate;

        return job.Status.ToLowerInvariant() switch
        {
            "running" when job.IsDueForExecution() =>
                $"Job '{job.Name}' (Priority: {priority}) is RUNNING and due for execution at {nextExecution?.ToString("o") ?? "unknown"}",
            "running" =>
                $"Job '{job.Name}' (Priority: {priority}) is RUNNING, next execution at {nextExecution?.ToString("o") ?? "unknown"}",
            "scheduled" =>
                $"Job '{job.Name}' is SCHEDULED, next execution at {nextExecution?.ToString("o") ?? "unknown"}",
            "completed" =>
                $"Job '{job.Name}' is COMPLETED (Success rate: {successRate:P1}, {job.TotalExecutions} total runs)",
            "failed" =>
                $"Job '{job.Name}' is FAILED (Success rate: {successRate:P1}, {job.FailedExecutions} failures)",
            "suspended" =>
                $"Job '{job.Name}' is SUSPENDED (Success rate: {successRate:P1})",
            "cancelled" =>
                $"Job '{job.Name}' is CANCELLED (Success rate: {successRate:P1})",
            "failedpermanently" =>
                $"Job '{job.Name}' is FAILED PERMANENTLY (Success rate: {successRate:P1}, {job.FailedExecutions} failures)",
            _ => $"Job '{job.Name}' status: {job.Status} (Success rate: {successRate:P1})"
        };
    }
}