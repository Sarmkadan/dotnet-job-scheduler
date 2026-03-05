#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace JobScheduler.Core.Events;

/// <summary>
/// Interface for event publishing system in the job scheduler.
/// Enables decoupled, event-driven architecture for job lifecycle events.
/// WHY: Decoupling allows multiple subscribers to react to events independently.
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes an event to all registered subscribers.
    /// Execution is fire-and-forget; subscribers should not block publishers.
    /// </summary>
    Task PublishAsync<TEvent>(TEvent eventData) where TEvent : ISchedulerEvent;

    /// <summary>
    /// Subscribes to events of a specific type.
    /// Returns a subscription token that can be used to unsubscribe.
    /// </summary>
    IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : ISchedulerEvent;

    /// <summary>
    /// Unsubscribes a handler from events.
    /// </summary>
    void Unsubscribe<TEvent>(object subscriptionToken) where TEvent : ISchedulerEvent;

    /// <summary>
    /// Waits for the next event of the specified type.
    /// Useful for testing and coordination scenarios.
    /// </summary>
    Task<TEvent> WaitForEventAsync<TEvent>(TimeSpan timeout) where TEvent : ISchedulerEvent;
}

/// <summary>
/// Base interface for all scheduler events.
/// All domain events should inherit from this.
/// </summary>
public interface ISchedulerEvent
{
    Guid EventId { get; }
    DateTime Timestamp { get; }
    string EventType { get; }
}

// Domain event classes
public class JobCreatedEvent : ISchedulerEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string EventType => "job.created";
    public Guid JobId { get; set; }
    public string JobName { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
}

public class JobExecutionStartedEvent : ISchedulerEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string EventType => "job.execution.started";
    public Guid JobId { get; set; }
    public Guid ExecutionId { get; set; }
    public string JobName { get; set; } = string.Empty;
}

public class JobExecutionCompletedEvent : ISchedulerEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string EventType => "job.execution.completed";
    public Guid JobId { get; set; }
    public Guid ExecutionId { get; set; }
    public string JobName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public long ExecutionTimeMs { get; set; }
    public string? ErrorMessage { get; set; }
}

public class JobExecutionFailedEvent : ISchedulerEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string EventType => "job.execution.failed";
    public Guid JobId { get; set; }
    public Guid ExecutionId { get; set; }
    public string JobName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public int RetryAttempt { get; set; }
    public bool WillRetry { get; set; }
}

public class JobSuspendedEvent : ISchedulerEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string EventType => "job.suspended";
    public Guid JobId { get; set; }
    public string JobName { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string? SuspendedBy { get; set; }
}

public class JobResumedEvent : ISchedulerEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string EventType => "job.resumed";
    public Guid JobId { get; set; }
    public string JobName { get; set; } = string.Empty;
    public string? ResumedBy { get; set; }
}

public class JobDeletedEvent : ISchedulerEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string EventType => "job.deleted";
    public Guid JobId { get; set; }
    public string JobName { get; set; } = string.Empty;
    public string? DeletedBy { get; set; }
}

public class SchedulerErrorEvent : ISchedulerEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string EventType => "scheduler.error";
    public string ErrorMessage { get; set; } = string.Empty;
    public string? Component { get; set; }
    public string? Details { get; set; }
    public int Severity { get; set; } // 1=Low, 2=Medium, 3=High, 4=Critical
}
