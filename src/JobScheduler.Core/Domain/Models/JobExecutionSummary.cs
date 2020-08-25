#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using JobScheduler.Core.Constants;

namespace JobScheduler.Core.Domain.Models;

/// <summary>
/// Aggregated execution statistics for a single job or the entire system.
/// Provides success rates, timing percentiles, and trend data.
/// </summary>
public sealed class JobExecutionSummary
{
    /// <summary>Job identifier. Null when representing system-wide statistics.</summary>
    public Guid? JobId { get; set; }

    /// <summary>Human-readable job name.</summary>
    public string? JobName { get; set; }

    /// <summary>Total number of executions in the evaluated window.</summary>
    public int TotalExecutions { get; set; }

    /// <summary>Number of successful executions.</summary>
    public int SuccessCount { get; set; }

    /// <summary>Number of failed executions.</summary>
    public int FailureCount { get; set; }

    /// <summary>Number of timed-out executions.</summary>
    public int TimedOutCount { get; set; }

    /// <summary>Number of cancelled executions.</summary>
    public int CancelledCount { get; set; }

    /// <summary>Success rate as a percentage (0–100).</summary>
    public double SuccessRate => TotalExecutions == 0 ? 0 : (double)SuccessCount / TotalExecutions * 100;

    /// <summary>Average duration of completed executions in milliseconds.</summary>
    public long AverageDurationMs { get; set; }

    /// <summary>Minimum observed duration in milliseconds.</summary>
    public long MinDurationMs { get; set; }

    /// <summary>Maximum observed duration in milliseconds.</summary>
    public long MaxDurationMs { get; set; }

    /// <summary>UTC timestamp of the most recent execution, if any.</summary>
    public DateTime? LastExecutedAt { get; set; }

    /// <summary>Status of the most recent execution, if any.</summary>
    public ExecutionStatus? LastStatus { get; set; }
}
