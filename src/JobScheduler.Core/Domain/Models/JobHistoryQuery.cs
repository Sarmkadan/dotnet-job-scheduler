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
    /// <summary>Filter executions to a specific status.</summary>
    public ExecutionStatus? Status { get; set; }

    /// <summary>Include only executions that started on or after this UTC timestamp.</summary>
    public DateTime? From { get; set; }

    /// <summary>Include only executions that started before or on this UTC timestamp.</summary>
    public DateTime? To { get; set; }

    /// <summary>Page number (1-based). Defaults to 1.</summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>Maximum records per page. Defaults to 20, capped at 200.</summary>
    public int PageSize { get; set; } = 20;

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
