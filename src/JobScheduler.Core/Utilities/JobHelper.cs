#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Text.Json;
using JobScheduler.Core.Constants;
using JobScheduler.Core.Domain.Entities;

namespace JobScheduler.Core.Utilities;

/// <summary>
/// Helper methods for job operations and data handling.
/// </summary>
public static class JobHelper
{
    /// <summary>
    /// The failure-rate percentage above which the reliability score is reduced.
    /// </summary>
    private const int HighFailureRatePercent = 20;

    /// <summary>
    /// The success-rate percentage below which a job may require review.
    /// </summary>
    private const int LowSuccessRatePercent = 50;

    /// <summary>
    /// The minimum execution count that must be exceeded before recommending review.
    /// </summary>
    private const int MinExecutionsForRecommendation = 5;

    /// <summary>
    /// The minimum timeout in seconds considered reasonable for job execution.
    /// </summary>
    private const int MinReasonableTimeoutSeconds = 10;

    /// <summary>
    /// The number of milliseconds in one second.
    /// </summary>
    private const int MillisecondsPerSecond = 1000;

    /// <summary>
    /// The number of seconds in one minute.
    /// </summary>
    private const int SecondsPerMinute = 60;

    /// <summary>
    /// The number of minutes in one hour.
    /// </summary>
    private const int MinutesPerHour = 60;

    /// <summary>
    /// Serializes job handler parameters to JSON.
    /// </summary>
    public static string SerializeParameters(object? parameters)
    {
        if (parameters is null)
            return string.Empty;

        try
        {
            return JsonSerializer.Serialize(parameters);
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Deserializes job handler parameters from JSON.
    /// </summary>
    public static T? DeserializeParameters<T>(string? jsonParameters)
    {
        if (string.IsNullOrWhiteSpace(jsonParameters))
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(jsonParameters);
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// Gets a human-readable status description for a job.
    /// </summary>
    public static string GetJobStatusDescription(Job job)
    {
        if (job is null)
            return "Unknown";

        return job.Status switch
        {
            JobStatus.Pending => "Awaiting scheduling",
            JobStatus.Scheduled => $"Scheduled for {job.NextExecutionAt:g}",
            JobStatus.Running => "Currently executing",
            JobStatus.Completed => $"Last completed: {job.LastExecutedAt:g}",
            JobStatus.Failed => "Failed, awaiting retry",
            JobStatus.Suspended => "Suspended by user",
            JobStatus.Cancelled => "Cancelled",
            JobStatus.FailedPermanently => "Failed permanently - manual intervention needed",
            _ => "Unknown status"
        };
    }

    /// <summary>
    /// Validates handler type format.
    /// Expected format: "Namespace.ClassName, AssemblyName"
    /// </summary>
    public static bool IsValidHandlerType(string? handlerType)
    {
        if (string.IsNullOrWhiteSpace(handlerType))
            return false;

        var parts = handlerType.Split(',');
        if (parts.Length < 2)
            return false;

        return !string.IsNullOrWhiteSpace(parts[0]) && !string.IsNullOrWhiteSpace(parts[1]);
    }

    /// <summary>
    /// Gets execution frequency description based on cron expression.
    /// </summary>
    public static string GetExecutionFrequencyDescription(string cronExpression)
    {
        if (string.IsNullOrWhiteSpace(cronExpression))
            return "Never";

        // Simple pattern matching for common schedules
        var trimmed = cronExpression.Trim();

        return trimmed switch
        {
            "* * * * *" => "Every minute",
            "0 * * * *" => "Every hour",
            "0 0 * * *" => "Daily at midnight",
            "0 12 * * *" => "Daily at noon",
            "0 0 * * 0" => "Weekly on Sunday",
            "0 0 1 * *" => "Monthly on the 1st",
            "0 0 * * 1-5" => "Weekdays at midnight",
            "0 0 * * 0,6" => "Weekends at midnight",
            _ => "Custom schedule"
        };
    }

    /// <summary>
    /// Calculates the job reliability score (0-100) based on execution history.
    /// </summary>
    public static int CalculateReliabilityScore(Job job)
    {
        ArgumentNullException.ThrowIfNull(job);
        if (job.TotalExecutions == 0)
            return 50;

        var successRate = job.GetSuccessRate();
        var score = (int)(successRate * 0.7); // 70% weight for success rate

        // Adjust for recent failures
        var failureRate = 100 - successRate;
        if (failureRate > HighFailureRatePercent)
            score -= (int)(failureRate - HighFailureRatePercent);

        return Math.Max(0, Math.Min(100, score));
    }

    /// <summary>
    /// Gets recommended action based on job's current state.
    /// </summary>
    public static string GetRecommendedAction(Job job)
    {
        ArgumentNullException.ThrowIfNull(job);
        if (job.Status == JobStatus.FailedPermanently)
            return "Review job configuration and error details. Fix and reactivate if needed.";

        if (job.GetSuccessRate() < LowSuccessRatePercent &&
            job.TotalExecutions > MinExecutionsForRecommendation)
            return "Success rate is low. Review handler implementation and parameters.";

        if (job.Status == JobStatus.Failed)
            return $"Job is failing. Check logs and consider suspending until root cause is addressed.";

        if (job.ExecutionTimeoutSeconds < MinReasonableTimeoutSeconds)
            return "Execution timeout is very short. Consider increasing if jobs are timing out unexpectedly.";

        return "Job is operating normally.";
    }

    /// <summary>
    /// Formats duration in milliseconds to human-readable format.
    /// </summary>
    public static string FormatDuration(long milliseconds)
    {
        if (milliseconds < 0)
            return "Invalid";

        if (milliseconds < MillisecondsPerSecond)
            return $"{milliseconds}ms";

        var seconds = milliseconds / (double)MillisecondsPerSecond;
        if (seconds < SecondsPerMinute)
            return $"{seconds:F2}s";

        var minutes = seconds / SecondsPerMinute;
        if (minutes < MinutesPerHour)
            return $"{minutes:F2}m";

        var hours = minutes / MinutesPerHour;
        return $"{hours:F2}h";
    }

    /// <summary>
    /// Determines if a job should be marked as having concerning behavior.
    /// </summary>
    public static bool IsConcerning(Job job)
    {
        ArgumentNullException.ThrowIfNull(job);
        if (job.Status == JobStatus.FailedPermanently)
            return true;

        if (job.Status == JobStatus.Failed && job.TotalExecutions > 10)
            return true;

        if (job.GetSuccessRate() < LowSuccessRatePercent &&
            job.TotalExecutions > MinExecutionsForRecommendation)
            return true;

        return false;
    }
}
