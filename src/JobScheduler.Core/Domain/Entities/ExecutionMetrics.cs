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
    /// <summary>
    /// Maximum average duration, in milliseconds, classified as excellent.
    /// </summary>
    private const long ExcellentThresholdMs = 100;

    /// <summary>
    /// Maximum average duration, in milliseconds, classified as good.
    /// </summary>
    private const long GoodThresholdMs = 500;

    /// <summary>
    /// Maximum average duration, in milliseconds, classified as acceptable.
    /// </summary>
    private const long AcceptableThresholdMs = 2000;

    /// <summary>
    /// Maximum average duration, in milliseconds, classified as slow.
    /// </summary>
    private const long SlowThresholdMs = 5000;

    /// <summary>
    /// Unique identifier for the metrics record.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Identifier of the job these metrics belong to.
    /// </summary>
    public Guid JobId { get; set; }

    /// <summary>
    /// Total number of executions.
    /// </summary>
    public int TotalExecutions { get; set; }

    /// <summary>
    /// Number of successful executions.
    /// </summary>
    public int SuccessfulExecutions { get; set; }

    /// <summary>
    /// Number of failed executions.
    /// </summary>
    public int FailedExecutions { get; set; }

    /// <summary>
    /// Number of executions that timed out.
    /// </summary>
    public int TimedOutExecutions { get; set; }

    /// <summary>
    /// Number of executions that were skipped.
    /// </summary>
    public int SkippedExecutions { get; set; }

    /// <summary>
    /// Number of executions that were cancelled.
    /// </summary>
    public int CancelledExecutions { get; set; }

    /// <summary>
    /// Average duration of executions in milliseconds.
    /// </summary>
    public long AverageDurationMs { get; set; }

    /// <summary>
    /// Minimum duration of executions in milliseconds.
    /// </summary>
    public long MinDurationMs { get; set; }

    /// <summary>
    /// Maximum duration of executions in milliseconds.
    /// </summary>
    public long MaxDurationMs { get; set; }

    /// <summary>
    /// Success rate as a percentage (0-100).
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Total number of retries across all executions.
    /// </summary>
    public long TotalRetries { get; set; }

    /// <summary>
    /// Timestamp of the last execution.
    /// </summary>
    public DateTime? LastExecutionTime { get; set; }

    /// <summary>
    /// Timestamp when these metrics were calculated.
    /// </summary>
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
            < ExcellentThresholdMs => "Excellent",
            < GoodThresholdMs => "Good",
            < AcceptableThresholdMs => "Acceptable",
            < SlowThresholdMs => "Slow",
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

    /// <summary>
    /// Returns a string representation of the ExecutionMetrics object.
    /// </summary>
    public override string ToString() => $"ExecutionMetrics {{ Id = {Id}, JobId = {JobId}, TotalExecutions = {TotalExecutions}, SuccessfulExecutions = {SuccessfulExecutions}, FailedExecutions = {FailedExecutions}, TimedOutExecutions = {TimedOutExecutions} }}";
}