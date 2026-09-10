#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace JobScheduler.Core.Services;

/// <summary>
/// Extension methods for <see cref="AuditLogEntry"/> to provide common filtering and formatting operations.
/// </summary>
public static class AuditLogEntryExtensions
{
    /// <summary>
    /// Determines whether the audit log entry is recent within the specified time window.
    /// </summary>
    /// <param name="e">The audit log entry to check.</param>
    /// <param name="window">The time window to check against (e.g., TimeSpan.FromHours(1)).</param>
    /// <returns>True if the entry's timestamp is within the last <paramref name="window"/>; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="e"/> is null.</exception>
    public static bool IsRecent(this AuditLogEntry e, TimeSpan window)
    {
        if (e is null)
            throw new ArgumentNullException(nameof(e));

        return e.Timestamp >= DateTime.UtcNow - window;
    }

    /// <summary>
    /// Filters a sequence of audit log entries by the specified event type.
    /// </summary>
    /// <param name="entries">The sequence of audit log entries to filter.</param>
    /// <param name="eventType">The event type to filter by (case-sensitive).</param>
    /// <returns>An IEnumerable<AuditLogEntry> containing entries with the matching event type.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entries"/> is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="eventType"/> is null.</exception>
    public static IEnumerable<AuditLogEntry> FilterByEventType(this IEnumerable<AuditLogEntry> entries, string eventType)
    {
        if (entries is null)
            throw new ArgumentNullException(nameof(entries));
        if (eventType is null)
            throw new ArgumentNullException(nameof(eventType));

        return entries.Where(e => e.EventType == eventType);
    }

    /// <summary>
    /// Filters a sequence of audit log entries by the specified user ID.
    /// </summary>
    /// <param name="entries">The sequence of audit log entries to filter.</param>
    /// <param name="userId">The user ID to filter by. Use null to filter for entries with no user ID.</param>
    /// <returns>An IEnumerable<AuditLogEntry> containing entries with the matching user ID.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entries"/> is null.</exception>
    public static IEnumerable<AuditLogEntry> FilterByUser(this IEnumerable<AuditLogEntry> entries, string? userId)
    {
        if (entries is null)
            throw new ArgumentNullException(nameof(entries));

        return entries.Where(e => e.UserId == userId);
    }

    /// <summary>
    /// Groups audit log entries by event type and returns a dictionary with counts.
    /// </summary>
    /// <param name="entries">The sequence of audit log entries to count.</param>
    /// <returns>A dictionary where the key is the event type and the value is the count of entries for that type.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entries"/> is null.</exception>
    public static Dictionary<string, int> CountByEventType(this IEnumerable<AuditLogEntry> entries)
    {
        if (entries is null)
            throw new ArgumentNullException(nameof(entries));

        return entries.GroupBy(e => e.EventType)
                     .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>
    /// Converts an audit log entry to a formatted log line string.
    /// </summary>
    /// <param name="e">The audit log entry to format.</param>
    /// <returns>A string representation of the audit log entry suitable for logging.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="e"/> is null.</exception>
    public static string ToLogLine(this AuditLogEntry e)
    {
        if (e is null)
            throw new ArgumentNullException(nameof(e));

        return $"[{e.Timestamp:O}] {e.EventType} - {e.Details} (User: {e.UserId ?? "None"}, Severity: {e.Severity})";
    }
}