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
    /// <summary>
    /// Gets or sets the unique identifier for the schedule history entry.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the identifier of the job associated with this history entry.
    /// </summary>
    public Guid JobId { get; set; }

    /// <summary>
    /// Gets or sets the name of the property that was changed.
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the previous value of the changed property.
    /// </summary>
    public string? OldValue { get; set; }

    /// <summary>
    /// Gets or sets the new value of the changed property.
    /// </summary>
    public string? NewValue { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the change occurred.
    /// </summary>
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the identifier or name of the user who made the change.
    /// </summary>
    public string? ChangedBy { get; set; }

    /// <summary>
    /// Gets or sets the reason for the change.
    /// </summary>
    public string ChangeReason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional details or context about the change.
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Gets or sets the job entity associated with this history entry.
    /// </summary>
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
        ArgumentException.ThrowIfNullOrEmpty(propertyName);
        ArgumentException.ThrowIfNullOrEmpty(oldValue);
        ArgumentException.ThrowIfNullOrEmpty(newValue);
        ArgumentException.ThrowIfNullOrEmpty(changeReason);
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
        ArgumentException.ThrowIfNullOrEmpty(oldStatus);
        ArgumentException.ThrowIfNullOrEmpty(newStatus);
        ArgumentException.ThrowIfNullOrEmpty(reason);
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
        ArgumentException.ThrowIfNullOrEmpty(oldCron);
        ArgumentException.ThrowIfNullOrEmpty(newCron);
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

    public override string ToString() => $"JobScheduleHistory {{ Id = {Id}, JobId = {JobId}, PropertyName = {PropertyName}, OldValue = {OldValue}, NewValue = {NewValue}, ChangedAt = {ChangedAt} }}";
}
