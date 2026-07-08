#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;

namespace JobScheduler.Core.Domain.Models;

/// <summary>
/// Response model containing comprehensive execution statistics for a job.
/// Includes success rates, execution time metrics, and trends.
/// </summary>
public sealed class ExecutionStatsResponse
{
    public Guid JobId { get; set; }
    public int TotalExecutions { get; set; }
    public int SuccessfulExecutions { get; set; }
    public int FailedExecutions { get; set; }
    public double SuccessRate { get; set; }
    public long AverageExecutionTimeMs { get; set; }
    public long MinExecutionTimeMs { get; set; }
    public long MaxExecutionTimeMs { get; set; }
    public DateTime? LastExecutionAt { get; set; }
}
