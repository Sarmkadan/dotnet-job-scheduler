#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;

namespace JobScheduler.Core.Domain.Models;

/// <summary>
/// Response model containing performance analysis metrics for job executions.
/// Includes percentile data and timing information.
/// </summary>
public sealed class PerformanceAnalysisResponse
{
    public Guid JobId { get; set; }
    public long AverageExecutionTimeMs { get; set; }
    public long MedianExecutionTimeMs { get; set; }
    public long P95ExecutionTimeMs { get; set; }
    public long P99ExecutionTimeMs { get; set; }
    public long SlowestExecutionTimeMs { get; set; }
    public long FastestExecutionTimeMs { get; set; }
    public DateTime? SlowestExecutionAt { get; set; }
    public DateTime? FastestExecutionAt { get; set; }
}
