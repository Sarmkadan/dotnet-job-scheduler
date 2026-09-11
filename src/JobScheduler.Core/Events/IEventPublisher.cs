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
    /// <typeparam name="TEvent">The type of the event to publish.</typeparam>
    /// <param name="eventData">The event data to publish.</param>
    /// <returns>A task that represents the asynchronous publish operation.</returns>
    Task PublishAsync<TEvent>(TEvent eventData) where TEvent : ISchedulerEvent;

    /// <summary>
    /// Subscribes to events of a specific type.
    /// Returns a subscription token that can be used to unsubscribe.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event to subscribe to.</typeparam>
    /// <param name="handler">The handler to invoke when an event of type TEvent is published.</param>
    /// <returns>A disposable subscription token that can be used to unsubscribe.</returns>
    IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : ISchedulerEvent;

    /// <summary>
    /// Unsubscribes a handler from events.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event to unsubscribe from.</typeparam>
    /// <param name="subscriptionToken">The subscription token returned by Subscribe.</param>
    void Unsubscribe<TEvent>(object subscriptionToken) where TEvent : ISchedulerEvent;

    /// <summary>
    /// Waits for the next event of the specified type.
    /// Useful for testing and coordination scenarios.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event to wait for.</typeparam>
    /// <param name="timeout">The timeout to wait for the event.</param>
    /// <returns>A task that completes when an event of type TEvent is received, or times out.</returns>
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
/// <summary>
/// Event published when a job is created.
/// </summary>
public sealed class JobCreatedEvent : SchedulerEventBase
{
    /// <inheritdoc />
    public override string EventType => "job.created";
    /// <inheritdoc />
    public override Guid? ExecutionId => null; // JobCreated happens before execution, so no ExecutionId
    /// <summary>
    /// The name of the job that was created.
    /// </summary>
    public string JobName { get; init; } = string.Empty;
    /// <summary>
    /// The user or system that created the job.
    /// </summary>
    public string CreatedBy { get; init; } = string.Empty;

    /// <summary>
    /// Returns a concise single-line summary of the event.
    /// </summary>
    public override string ToString() =>
        $"{EventType} [JobId={JobId}, JobName={JobName}, CreatedBy={CreatedBy}, OccurredAtUtc={OccurredAtUtc:O}]";
}

/// <summary>
/// Event published when a job execution starts.
/// </summary>
public sealed class JobExecutionStartedEvent : SchedulerEventBase
{
    /// <inheritdoc />
    public override string EventType => "job.execution.started";
    /// <summary>
    /// The name of the job that started execution.
    /// </summary>
    public string JobName { get; init; } = string.Empty;

    /// <summary>
    /// Returns a concise single-line summary of the event.
    /// </summary>
    public override string ToString() =>
        $"{EventType} [JobId={JobId}, JobName={JobName}, OccurredAtUtc={OccurredAtUtc:O}]";
}

/// <summary>
/// Event published when a job execution completes.
/// </summary>
public sealed class JobExecutionCompletedEvent : SchedulerEventBase
{
    /// <inheritdoc />
    public override string EventType => "job.execution.completed";
    /// <summary>
    /// The name of the job that completed execution.
    /// </summary>
    public string JobName { get; init; } = string.Empty;
    /// <summary>
    /// Indicates whether the job execution succeeded.
    /// </summary>
    public bool Success { get; init; }
    /// <summary>
    /// The execution time in milliseconds.
    /// </summary>
    public long ExecutionTimeMs { get; init; }
    /// <summary>
    /// The error message if the execution failed, otherwise null.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Returns a concise single-line summary of the event.
    /// </summary>
    public override string ToString() =>
        $"{EventType} [JobId={JobId}, JobName={JobName}, Success={Success}, ExecutionTimeMs={ExecutionTimeMs}, ErrorMessage={ErrorMessage}, OccurredAtUtc={OccurredAtUtc:O}]";
}

/// <summary>
/// Event published when a job execution fails.
/// </summary>
public sealed class JobExecutionFailedEvent : SchedulerEventBase
{
    /// <inheritdoc />
    public override string EventType => "job.execution.failed";
    /// <summary>
    /// The name of the job that failed execution.
    /// </summary>
    public string JobName { get; init; } = string.Empty;
    /// <summary>
    /// The error message that caused the failure.
    /// </summary>
    public string ErrorMessage { get; init; } = string.Empty;
    /// <summary>
    /// The current retry attempt number (0-based).
    /// </summary>
    public int RetryAttempt { get; init; }
    /// <summary>
    /// Indicates whether the job will be retried again.
    /// </summary>
    public bool WillRetry { get; init; }

    /// <summary>
    /// Returns a concise single-line summary of the event.
    /// </summary>
    public override string ToString() =>
        $"{EventType} [JobId={JobId}, JobName={JobName}, ErrorMessage={ErrorMessage}, RetryAttempt={RetryAttempt}, WillRetry={WillRetry}, OccurredAtUtc={OccurredAtUtc:O}]";
}

/// <summary>
/// Event published when a job execution has exhausted all retry attempts.
/// </summary>
public sealed class JobExecutionExhaustedEvent : SchedulerEventBase
{
    /// <inheritdoc />
    public override string EventType => "job.execution.exhausted";
    /// <summary>
    /// The name of the job that exhausted retries.
    /// </summary>
    public string JobName { get; init; } = string.Empty;
    /// <summary>
    /// The final error message that caused the exhaustion.
    /// </summary>
    public string ErrorMessage { get; init; } = string.Empty;
    /// <summary>
    /// The total number of execution attempts made.
    /// </summary>
    public int TotalAttempts { get; init; }
    /// <summary>
    /// The maximum number of retry attempts allowed.
    /// </summary>
    public int MaxRetries { get; init; }

    /// <summary>
    /// Returns a concise single-line summary of the event.
    /// </summary>
    public override string ToString() =>
        $"{EventType} [JobId={JobId}, JobName={JobName}, ErrorMessage={ErrorMessage}, TotalAttempts={TotalAttempts}, MaxRetries={MaxRetries}, OccurredAtUtc={OccurredAtUtc:O}]";
}

/// <summary>
/// Event published when a job execution times out. Distinct from failures to allow
/// specialized handling and monitoring of timeout scenarios.
/// </summary>
public sealed class JobExecutionTimedOutEvent : SchedulerEventBase
{
    /// <inheritdoc />
    public override string EventType => "job.execution.timed_out";
    /// <summary>
    /// The name of the job that timed out.
    /// </summary>
    public string JobName { get; init; } = string.Empty;
    /// <summary>
    /// The error message associated with the timeout.
    /// </summary>
    public string ErrorMessage { get; init; } = string.Empty;
    /// <summary>
    /// The timeout duration in seconds that was exceeded.
    /// </summary>
    public int TimeoutSeconds { get; init; }
    /// <summary>
    /// The actual execution time in milliseconds before the timeout occurred.
    /// </summary>
    public long ExecutionTimeMs { get; init; }

    /// <summary>
    /// Returns a concise single-line summary of the event.
    /// </summary>
    public override string ToString() =>
        $"{EventType} [JobId={JobId}, JobName={JobName}, ErrorMessage={ErrorMessage}, TimeoutSeconds={TimeoutSeconds}, ExecutionTimeMs={ExecutionTimeMs}, OccurredAtUtc={OccurredAtUtc:O}]";
}

/// <summary>
/// Event published when a job execution is interrupted during graceful shutdown.
/// Allows handlers to perform cleanup and cleanup resources before termination.
/// </summary>
public sealed class JobExecutionInterruptedEvent : SchedulerEventBase
{
    /// <inheritdoc />
    public override string EventType => "job.execution.interrupted";
    /// <summary>
    /// The name of the job that was interrupted.
    /// </summary>
    public string JobName { get; init; } = string.Empty;
    /// <summary>
    /// The reason for the interruption.
    /// </summary>
    public string Reason { get; init; } = "Shutdown interrupted execution";

    /// <summary>
    /// Returns a concise single-line summary of the event.
    /// </summary>
    public override string ToString() =>
        $"{EventType} [JobId={JobId}, JobName={JobName}, Reason={Reason}, OccurredAtUtc={OccurredAtUtc:O}]";
}

/// <summary>
/// Event published when a job is suspended.
/// </summary>
public sealed class JobSuspendedEvent : SchedulerEventBase
{
    /// <inheritdoc />
    public override string EventType => "job.suspended";
    /// <inheritdoc />
    public override Guid? ExecutionId => null; // Job suspension is not execution-specific
    /// <summary>
    /// The name of the job that was suspended.
    /// </summary>
    public string JobName { get; init; } = string.Empty;
    /// <summary>
    /// The reason for the suspension.
    /// </summary>
    public string? Reason { get; init; }
    /// <summary>
    /// The user or system that suspended the job.
    /// </summary>
    public string? SuspendedBy { get; init; }

    /// <summary>
    /// Returns a concise single-line summary of the event.
    /// </summary>
    public override string ToString() =>
        $"{EventType} [JobId={JobId}, JobName={JobName}, Reason={Reason}, SuspendedBy={SuspendedBy}, OccurredAtUtc={OccurredAtUtc:O}]";
}

/// <summary>
/// Event published when a job is resumed.
/// </summary>
public sealed class JobResumedEvent : SchedulerEventBase
{
    /// <inheritdoc />
    public override string EventType => "job.resumed";
    /// <inheritdoc />
    public override Guid? ExecutionId => null; // Job resumption is not execution-specific
    /// <summary>
    /// The name of the job that was resumed.
    /// </summary>
    public string JobName { get; init; } = string.Empty;
    /// <summary>
    /// The user or system that resumed the job.
    /// </summary>
    public string? ResumedBy { get; init; }

    /// <summary>
    /// Returns a concise single-line summary of the event.
    /// </summary>
    public override string ToString() =>
        $"{EventType} [JobId={JobId}, JobName={JobName}, ResumedBy={ResumedBy}, OccurredAtUtc={OccurredAtUtc:O}]";
}

/// <summary>
/// Event published when a job is deleted.
/// </summary>
public sealed class JobDeletedEvent : SchedulerEventBase
{
    /// <inheritdoc />
    public override string EventType => "job.deleted";
    /// <inheritdoc />
    public override Guid? ExecutionId => null; // Job deletion is not execution-specific
    /// <summary>
    /// The name of the job that was deleted.
    /// </summary>
    public string JobName { get; init; } = string.Empty;
    /// <summary>
    /// The user or system that deleted the job.
    /// </summary>
    public string? DeletedBy { get; init; }

    /// <summary>
    /// Returns a concise single-line summary of the event.
    /// </summary>
    public override string ToString() =>
        $"{EventType} [JobId={JobId}, JobName={JobName}, DeletedBy={DeletedBy}, OccurredAtUtc={OccurredAtUtc:O}]";
}

/// <summary>
/// Event published when an error occurs in the scheduler.
/// </summary>
public sealed class SchedulerErrorEvent : SchedulerEventBase
{
    /// <inheritdoc />
    public override string EventType => "scheduler.error";
    /// <inheritdoc />
    public override Guid JobId => Guid.Empty; // Not job-specific
    /// <inheritdoc />
    public override Guid? ExecutionId => null; // Not execution-specific
    /// <summary>
    /// The error message.
    /// </summary>
    public string ErrorMessage { get; init; } = string.Empty;
    /// <summary>
    /// The component where the error occurred.
    /// </summary>
    public string? Component { get; init; }
    /// <summary>
    /// Additional details about the error.
    /// </summary>
    public string? Details { get; init; }
    /// <summary>
    /// The severity of the error (1=Low, 2=Medium, 3=High, 4=Critical).
    /// </summary>
    public int Severity { get; init; }

    /// <summary>
    /// Returns a concise single-line summary of the event.
    /// </summary>
    public override string ToString() =>
        $"{EventType} [JobId={JobId}, ErrorMessage={ErrorMessage}, Component={Component}, Severity={Severity}, OccurredAtUtc={OccurredAtUtc:O}]";
}