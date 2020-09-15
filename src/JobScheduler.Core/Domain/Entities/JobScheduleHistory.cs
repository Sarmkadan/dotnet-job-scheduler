#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;

namespace JobScheduler.Core.Domain.Entities;

/// <summary>
/// Tracks historical changes to job schedules and configurations.
/// Provides audit trail for schedule modifications and status changes.
/// </summary>
public class JobScheduleHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid JobId { get; set; }

    public string PropertyName { get; set; } = string.Empty;

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    public string? ChangedBy { get; set; }

    public string ChangeReason { get; set; } = string.Empty;

    public string? Details { get; set; }

    public virtual Job Job { get; set; } = null!;

    /// <summary>
    /// Creates a new schedule history entry for a property change.
    /// </summary>
    public static JobScheduleHistory CreateChange(
        Guid jobId,
        string propertyName,
        string? oldValue,
        string? newValue,
        string changeReason,
        string? changedBy = null)
    {
        return new JobScheduleHistory
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            PropertyName = propertyName,
            OldValue = oldValue,
            NewValue = newValue,
            ChangeReason = changeReason,
            ChangedBy = changedBy,
            ChangedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a new schedule history entry for status change.
    /// </summary>
    public static JobScheduleHistory CreateStatusChange(
        Guid jobId,
        string oldStatus,
        string newStatus,
        string reason,
        string? changedBy = null)
    {
        return CreateChange(jobId, "Status", oldStatus, newStatus, reason, changedBy);
    }

    /// <summary>
    /// Creates a new schedule history entry for cron expression change.
    /// </summary>
    public static JobScheduleHistory CreateCronChange(
        Guid jobId,
        string oldCron,
        string newCron,
        string? changedBy = null)
    {
        return CreateChange(jobId, "CronExpression", oldCron, newCron, "Cron schedule modified", changedBy);
    }

    /// <summary>
    /// Gets a formatted description of the change.
    /// </summary>
    public string GetChangeDescription()
    {
        if (string.IsNullOrWhiteSpace(OldValue))
            return $"{PropertyName} set to: {NewValue}";

        if (string.IsNullOrWhiteSpace(NewValue))
            return $"{PropertyName} removed (was: {OldValue})";

        return $"{PropertyName} changed from '{OldValue}' to '{NewValue}'";
    }

    /// <summary>
    /// Validates the history entry data.
    /// </summary>
    public bool IsValid()
    {
        if (JobId == Guid.Empty || string.IsNullOrWhiteSpace(PropertyName))
            return false;

        if (string.IsNullOrWhiteSpace(ChangeReason))
            return false;

        return true;
    }
}
