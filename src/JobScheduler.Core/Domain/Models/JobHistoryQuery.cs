#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using JobScheduler.Core.Constants;

namespace JobScheduler.Core.Domain.Models;

/// <summary>
/// Query parameters for filtering job execution history records.
/// All filters are optional and combined with AND logic when specified.
/// </summary>
public sealed class JobHistoryQuery
{
    /// <summary>
    /// Filters executions to a specific <see cref="ExecutionStatus"/>.
    /// <c>null</c> (the default) applies no status filter.
    /// </summary>
    public ExecutionStatus? Status { get; set; }

    /// <summary>
    /// Includes only executions that started on or after this UTC timestamp.
    /// <c>null</c> (the default) applies no lower bound. Must not be <c>default(DateTime)</c>
    /// and must not be later than <see cref="To"/> when both are set.
    /// </summary>
    public DateTime? From { get; set; }

    /// <summary>
    /// Includes only executions that started before or on this UTC timestamp.
    /// <c>null</c> (the default) applies no upper bound. Must not be <c>default(DateTime)</c>
    /// and must not be earlier than <see cref="From"/> when both are set.
    /// </summary>
    public DateTime? To { get; set; }

    /// <summary>
    /// Page number (1-based). Defaults to 1. Values below 1 are raised to 1 by <see cref="Normalize"/>.
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Maximum number of records per page. Defaults to 20. Valid range is 1 to 200;
    /// <see cref="Normalize"/> clamps values outside this range.
    /// </summary>
    public int PageSize { get; set; } = 20;

    public override string ToString() => $"JobHistoryQuery {{ Status = {Status}, From = {From}, To = {To}, PageNumber = {PageNumber}, PageSize = {PageSize} }}";

    /// <summary>Returns a validated, clamped copy of this query.</summary>
    public JobHistoryQuery Normalize()
    {
        return new JobHistoryQuery
        {
            Status = Status,
            From = From,
            To = To,
            PageNumber = Math.Max(1, PageNumber),
            PageSize = Math.Clamp(PageSize, 1, 200)
        };
    }
}
