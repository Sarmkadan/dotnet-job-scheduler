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
    /// Serializes job handler parameters to JSON.
    /// </summary>
    public static string SerializeParameters(object? parameters)
    {
        if (parameters == null)
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
        if (job == null)
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
        if (job.TotalExecutions == 0)
            return 50;

        var successRate = job.GetSuccessRate();
        var score = (int)(successRate * 0.7); // 70% weight for success rate

        // Adjust for recent failures
        var failureRate = 100 - successRate;
        if (failureRate > 20)
            score -= (int)(failureRate - 20);

        return Math.Max(0, Math.Min(100, score));
    }

    /// <summary>
    /// Gets recommended action based on job's current state.
    /// </summary>
    public static string GetRecommendedAction(Job job)
    {
        if (job.Status == JobStatus.FailedPermanently)
            return "Review job configuration and error details. Fix and reactivate if needed.";

        if (job.GetSuccessRate() < 50 && job.TotalExecutions > 5)
            return "Success rate is low. Review handler implementation and parameters.";

        if (job.Status == JobStatus.Failed)
            return $"Job is failing. Check logs and consider suspending until root cause is addressed.";

        if (job.ExecutionTimeoutSeconds < 10)
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

        if (milliseconds < 1000)
            return $"{milliseconds}ms";

        var seconds = milliseconds / 1000.0;
        if (seconds < 60)
            return $"{seconds:F2}s";

        var minutes = seconds / 60;
        if (minutes < 60)
            return $"{minutes:F2}m";

        var hours = minutes / 60;
        return $"{hours:F2}h";
    }

    /// <summary>
    /// Determines if a job should be marked as having concerning behavior.
    /// </summary>
    public static bool IsConcerning(Job job)
    {
        if (job.Status == JobStatus.FailedPermanently)
            return true;

        if (job.Status == JobStatus.Failed && job.TotalExecutions > 10)
            return true;

        if (job.GetSuccessRate() < 50 && job.TotalExecutions > 5)
            return true;

        return false;
    }
}
