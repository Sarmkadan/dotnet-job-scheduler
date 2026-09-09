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
    /// <summary>
    /// Unique identifier of the job.
    /// </summary>
    public Guid JobId { get; set; }

    /// <summary>
    /// Average execution time in milliseconds.
    /// </summary>
    public long AverageExecutionTimeMs { get; set; }

    /// <summary>
    /// Median execution time in milliseconds.
    /// </summary>
    public long MedianExecutionTimeMs { get; set; }

    /// <summary>
    /// 95th percentile execution time in milliseconds.
    /// </summary>
    public long P95ExecutionTimeMs { get; set; }

    /// <summary>
    /// 99th percentile execution time in milliseconds.
    /// </summary>
    public long P99ExecutionTimeMs { get; set; }

    /// <summary>
    /// Slowest execution time in milliseconds.
    /// </summary>
    public long SlowestExecutionTimeMs { get; set; }

    /// <summary>
    /// Fastest execution time in milliseconds.
    /// </summary>
    public long FastestExecutionTimeMs { get; set; }

    /// <summary>
    /// Timestamp of the slowest execution.
    /// </summary>
    public DateTime? SlowestExecutionAt { get; set; }

    /// <summary>
    /// Timestamp of the fastest execution.
    /// </summary>
    public DateTime? FastestExecutionAt { get; set; }

    /// <summary>
    /// Returns a string representation of the performance analysis response.
    /// </summary>
    /// <returns>A string representation of the object.</returns>
    public override string ToString() => $"PerformanceAnalysisResponse {{ JobId = {JobId}, AverageExecutionTimeMs = {AverageExecutionTimeMs}, MedianExecutionTimeMs = {MedianExecutionTimeMs}, P95ExecutionTimeMs = {P95ExecutionTimeMs}, P99ExecutionTimeMs = {P99ExecutionTimeMs}, SlowestExecutionTimeMs = {SlowestExecutionTimeMs} }}";
}
