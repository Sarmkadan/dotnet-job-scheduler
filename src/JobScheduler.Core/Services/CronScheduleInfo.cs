#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Diagnostics;

namespace JobScheduler.Core.Services;

/// <summary>
/// Provides a debugger-friendly representation of a parsed cron schedule.
/// Wraps the raw expression alongside its computed next occurrence and human-readable description.
/// </summary>
[DebuggerDisplay("{Expression} => next={NextOccurrence:O} ({Description})")]
public sealed class CronScheduleInfo
{
    /// <summary>
    /// The raw cron expression string.
    /// </summary>
    public string Expression { get; }

    /// <summary>
    /// The next occurrence computed from the cron expression.
    /// </summary>
    public DateTime NextOccurrence { get; }

    /// <summary>
    /// A human-readable description of what the cron expression means.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="CronScheduleInfo"/>.
    /// </summary>
    /// <param name="expression">The raw cron expression.</param>
    /// <param name="nextOccurrence">The next computed occurrence in UTC.</param>
    /// <param name="description">A human-readable description of the schedule.</param>
    public CronScheduleInfo(string expression, DateTime nextOccurrence, string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expression);
        ArgumentNullException.ThrowIfNull(description);

        Expression = expression;
        NextOccurrence = nextOccurrence;
        Description = description;
    }

    /// <summary>
    /// Returns a diagnostic string for the parsed cron schedule.
    /// </summary>
    public override string ToString() => $"{Expression} => next={NextOccurrence:O} ({Description})";
}
