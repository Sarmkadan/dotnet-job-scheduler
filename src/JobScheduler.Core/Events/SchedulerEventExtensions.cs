#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace JobScheduler.Core.Events;

/// <summary>
/// Extension methods for scheduler event types.
/// Provides helper methods for event classification, naming, and logging.
/// </summary>
public static class SchedulerEventExtensions
{
    /// <summary>
    /// Determines whether the event represents a failure condition.
    /// </summary>
    /// <param name="e">The event to evaluate.</param>
    /// <returns>True if the event is a failure event (Failed, Exhausted, TimedOut, or SchedulerError); otherwise false.</returns>
    public static bool IsFailureEvent(this SchedulerEventBase e)
    {
        return e.EventType == "job.execution.failed" ||
               e.EventType == "job.execution.exhausted" ||
               e.EventType == "job.execution.timed_out" ||
               e.EventType == "scheduler.error";
    }

    /// <summary>
    /// Determines whether the event represents a lifecycle change.
    /// </summary>
    /// <param name="e">The event to evaluate.</param>
    /// <returns>True if the event is a lifecycle event (Created, Suspended, Resumed, or Deleted); otherwise false.</returns>
    public static bool IsLifecycleEvent(this SchedulerEventBase e)
    {
        return e.EventType == "job.created" ||
               e.EventType == "job.suspended" ||
               e.EventType == "job.resumed" ||
               e.EventType == "job.deleted";
    }

    /// <summary>
    /// Gets the event name without the 'Event' suffix.
    /// </summary>
    /// <param name="e">The event to get the name for.</param>
    /// <returns>The event type name with 'Event' suffix removed.</returns>
    public static string GetEventName(this SchedulerEventBase e)
    {
        var typeName = e.GetType().Name;
        if (typeName.EndsWith("Event"))
        {
            return typeName[0..^5];
        }
        return typeName;
    }

    /// <summary>
    /// Creates a log-friendly string representation of the event.
    /// </summary>
    /// <param name="e">The event to convert to a log string.</param>
    /// <returns>A string containing the event type, identifiers, and timestamp for logging.</returns>
    public static string ToLogString(this SchedulerEventBase e)
    {
        return $"[{e.EventType}] EventId={e.EventId}, JobId={e.JobId}, ExecutionId={e.ExecutionId}, OccurredAtUtc={e.OccurredAtUtc:O}";
    }
}