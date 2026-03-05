#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;

namespace JobScheduler.Core.Domain.Entities;

/// <summary>
/// Aggregated metrics and statistics for job executions.
/// Provides insights into job performance and reliability.
/// </summary>
public sealed class ExecutionMetrics
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid JobId { get; set; }

    public int TotalExecutions { get; set; }

    public int SuccessfulExecutions { get; set; }

    public int FailedExecutions { get; set; }

    public int TimedOutExecutions { get; set; }

    public int SkippedExecutions { get; set; }

    public int CancelledExecutions { get; set; }

    public long AverageDurationMs { get; set; }

    public long MinDurationMs { get; set; }

    public long MaxDurationMs { get; set; }

    public double SuccessRate { get; set; }

    public long TotalRetries { get; set; }

    public DateTime? LastExecutionTime { get; set; }

    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Calculates success rate as a percentage.
    /// </summary>
    public double CalculateSuccessRate()
    {
        if (TotalExecutions == 0)
            return 0;

        return (double)SuccessfulExecutions / TotalExecutions * 100;
    }

    /// <summary>
    /// Determines if the job has acceptable reliability.
    /// </summary>
    public bool IsReliable(double minimumSuccessRatePercent = 90.0)
    {
        return CalculateSuccessRate() >= minimumSuccessRatePercent;
    }

    /// <summary>
    /// Gets the failure count (excludes skipped and cancelled).
    /// </summary>
    public int GetActualFailureCount()
    {
        return FailedExecutions + TimedOutExecutions;
    }

    /// <summary>
    /// Gets a summary of metrics.
    /// </summary>
    public string GetSummary()
    {
        var successRate = CalculateSuccessRate();
        var avgDuration = AverageDurationMs;

        return $"Executions: {TotalExecutions} | " +
               $"Success Rate: {successRate:F1}% | " +
               $"Avg Duration: {avgDuration}ms | " +
               $"Retries: {TotalRetries}";
    }

    /// <summary>
    /// Checks if there's a concerning trend in failures.
    /// </summary>
    public bool HasFailureTrend(int failuresInLastN = 5)
    {
        return FailedExecutions >= failuresInLastN && CalculateSuccessRate() < 75.0;
    }

    /// <summary>
    /// Gets performance classification based on average duration.
    /// </summary>
    public string GetPerformanceClass()
    {
        return AverageDurationMs switch
        {
            < 100 => "Excellent",
            < 500 => "Good",
            < 2000 => "Acceptable",
            < 5000 => "Slow",
            _ => "Very Slow"
        };
    }

    /// <summary>
    /// Validates metrics data.
    /// </summary>
    public bool IsValid()
    {
        if (JobId == Guid.Empty)
            return false;

        if (TotalExecutions < 0 || SuccessfulExecutions < 0 || FailedExecutions < 0)
            return false;

        if (TotalExecutions > 0 && (SuccessfulExecutions + FailedExecutions + TimedOutExecutions + SkippedExecutions + CancelledExecutions) > TotalExecutions)
            return false;

        if (AverageDurationMs < 0 || MinDurationMs < 0 || MaxDurationMs < 0)
            return false;

        if (MinDurationMs > MaxDurationMs)
            return false;

        return true;
    }
}
