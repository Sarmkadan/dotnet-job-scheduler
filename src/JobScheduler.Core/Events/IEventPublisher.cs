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
/// Defines the unified contract for event correlation and tracking.
/// All domain events should inherit from this.
/// </summary>
public interface ISchedulerEvent
{
    /// <summary>
    /// Unique identifier for this event instance.
    /// Used for deduplication, tracking, and correlation.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// The job this event relates to.
    /// Present on all job-related events for consistent correlation.
    /// </summary>
    Guid JobId { get; }

    /// <summary>
    /// Unique identifier for the execution lifecycle.
    /// Correlates Started/Completed/Failed/Exhausted events for the same run.
    /// Present on all execution-related events for proper correlation.
    /// </summary>
    Guid? ExecutionId { get; }

    /// <summary>
    /// When the event occurred in UTC.
    /// More explicit than generic Timestamp and emphasizes UTC semantics.
    /// </summary>
    DateTime OccurredAtUtc { get; }

    /// <summary>
    /// Type discriminator for the event.
    /// Enables type-safe event handling and routing.
    /// </summary>
    string EventType { get; }
}

/// <summary>
/// Abstract base class for all scheduler events.
/// Provides a unified contract and common properties for event correlation and tracking.
/// </summary>
public abstract class SchedulerEventBase : ISchedulerEvent
{
    /// <inheritdoc />
    public Guid EventId { get; } = Guid.NewGuid();

    /// <inheritdoc />
    public virtual Guid JobId { get; init; }

    /// <inheritdoc />
    public virtual Guid? ExecutionId { get; init; }

    /// <inheritdoc />
    public DateTime OccurredAtUtc { get; } = DateTime.UtcNow;

    /// <inheritdoc />
    public abstract string EventType { get; }
}

// Domain event classes
public sealed class JobCreatedEvent : SchedulerEventBase
{
    public override string EventType => "job.created";
    public override Guid? ExecutionId => null; // JobCreated happens before execution, so no ExecutionId
    public string JobName { get; init; } = string.Empty;
    public string CreatedBy { get; init; } = string.Empty;
}

public sealed class JobExecutionStartedEvent : SchedulerEventBase
{
    public override string EventType => "job.execution.started";
    public string JobName { get; init; } = string.Empty;
}

public sealed class JobExecutionCompletedEvent : SchedulerEventBase
{
    public override string EventType => "job.execution.completed";
    public string JobName { get; init; } = string.Empty;
    public bool Success { get; init; }
    public long ExecutionTimeMs { get; init; }
    public string? ErrorMessage { get; init; }
}

public sealed class JobExecutionFailedEvent : SchedulerEventBase
{
    public override string EventType => "job.execution.failed";
    public string JobName { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
    public int RetryAttempt { get; init; }
    public bool WillRetry { get; init; }
}

public sealed class JobExecutionExhaustedEvent : SchedulerEventBase
{
    public override string EventType => "job.execution.exhausted";
    public string JobName { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
    public int TotalAttempts { get; init; }
    public int MaxRetries { get; init; }
}

/// <summary>
/// Event published when a job execution times out. Distinct from failures to allow
/// specialized handling and monitoring of timeout scenarios.
/// </summary>
public sealed class JobExecutionTimedOutEvent : SchedulerEventBase
{
    public override string EventType => "job.execution.timed_out";
    public string JobName { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; }
    public long ExecutionTimeMs { get; init; }
}

/// <summary>
/// Event published when a job execution is interrupted during graceful shutdown.
/// Allows handlers to perform cleanup and cleanup resources before termination.
/// </summary>
public sealed class JobExecutionInterruptedEvent : SchedulerEventBase
{
    public override string EventType => "job.execution.interrupted";
    public string JobName { get; init; } = string.Empty;
    public string Reason { get; init; } = "Shutdown interrupted execution";
}

public sealed class JobSuspendedEvent : SchedulerEventBase
{
    public override string EventType => "job.suspended";
    public override Guid? ExecutionId => null; // Job suspension is not execution-specific
    public string JobName { get; init; } = string.Empty;
    public string? Reason { get; init; }
    public string? SuspendedBy { get; init; }
}

public sealed class JobResumedEvent : SchedulerEventBase
{
    public override string EventType => "job.resumed";
    public override Guid? ExecutionId => null; // Job resumption is not execution-specific
    public string JobName { get; init; } = string.Empty;
    public string? ResumedBy { get; init; }
}

public sealed class JobDeletedEvent : SchedulerEventBase
{
    public override string EventType => "job.deleted";
    public override Guid? ExecutionId => null; // Job deletion is not execution-specific
    public string JobName { get; init; } = string.Empty;
    public string? DeletedBy { get; init; }
}

public sealed class SchedulerErrorEvent : SchedulerEventBase
{
    public override string EventType => "scheduler.error";
    public override Guid JobId => Guid.Empty; // Not job-specific
    public override Guid? ExecutionId => null; // Not execution-specific
    public string ErrorMessage { get; init; } = string.Empty;
    public string? Component { get; init; }
    public string? Details { get; init; }
    public int Severity { get; init; } // 1=Low, 2=Medium, 3=High, 4=Critical
}

