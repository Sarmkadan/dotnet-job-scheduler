# IEventPublisher

The `IEventPublisher` interface defines the contract for emitting structured lifecycle events within the `dotnet-job-scheduler` ecosystem. It serves as the primary mechanism for broadcasting state changes regarding job registration, execution initiation, and completion outcomes to subscribed listeners or external monitoring systems. Implementations of this interface are responsible for capturing contextual metadata, such as execution identifiers and timing metrics, to ensure accurate audit trails and real-time observability of scheduled tasks.

## API

The interface exposes a set of properties that constitute the payload of an event. These members represent the data available at the time of publication rather than methods invoked to publish.

### `EventId`
*   **Type:** `Guid`
*   **Purpose:** Uniquely identifies the specific event instance being published. This identifier allows consumers to deduplicate messages or trace specific occurrences across distributed logs.
*   **Parameters:** None (Property).
*   **Return Value:** A globally unique identifier for the event.
*   **Throws:** Does not throw; returns the assigned GUID.

### `Timestamp`
*   **Type:** `DateTime`
*   **Purpose:** Records the precise point in time when the event was generated. This is critical for calculating latency, ordering events chronologically, and auditing historical job activity.
*   **Parameters:** None (Property).
*   **Return Value:** The UTC date and time of the event occurrence.
*   **Throws:** Does not throw.

### `JobId`
*   **Type:** `Guid`
*   **Purpose:** Identifies the specific job definition associated with the event. This links the event to the persistent configuration of the scheduled task.
*   **Parameters:** None (Property).
*   **Return Value:** The unique identifier of the job.
*   **Throws:** Does not throw.

### `JobName`
*   **Type:** `string`
*   **Purpose:** Provides the human-readable name of the job. This facilitates easier identification in logs and dashboards without requiring a lookup of the `JobId`.
*   **Parameters:** None (Property).
*   **Return Value:** The name assigned to the job.
*   **Throws:** Does not throw.

### `CreatedBy`
*   **Type:** `string`
*   **Purpose:** Indicates the principal or system entity responsible for creating the job definition. This is typically populated during job registration events to maintain ownership accountability.
*   **Parameters:** None (Property).
*   **Return Value:** The identifier or name of the creator.
*   **Throws:** Does not throw.

### `ExecutionId`
*   **Type:** `Guid`
*   **Purpose:** Uniquely identifies a specific run or invocation of a job. While `JobId` remains constant for the definition, `ExecutionId` changes for every distinct execution attempt, allowing for granular tracking of retries and individual runs.
*   **Parameters:** None (Property).
*   **Return Value:** The unique identifier for the execution instance.
*   **Throws:** Does not throw.

### `Success`
*   **Type:** `bool`
*   **Purpose:** Indicates the final outcome of a job execution. A value of `true` signifies successful completion, while `false` indicates failure. This property is primarily relevant for completion events.
*   **Parameters:** None (Property).
*   **Return Value:** `true` if the job completed successfully; otherwise, `false`.
*   **Throws:** Does not throw.

### `ExecutionTimeMs`
*   **Type:** `long`
*   **Purpose:** Measures the duration of the job execution in milliseconds. This metric is essential for performance monitoring, detecting long-running tasks, and optimizing scheduling intervals.
*   **Parameters:** None (Property).
*   **Return Value:** The elapsed time in milliseconds.
*   **Throws:** Does not throw.

### `ErrorMessage`
*   **Type:** `string?`
*   **Purpose:** Contains the error details if the job execution failed. This property is nullable; it will contain an exception message or stack trace summary when `Success` is `false`, and `null` otherwise.
*   **Parameters:** None (Property).
*   **Return Value:** The error description or `null`.
*   **Throws:** Does not throw.

## Usage

The following examples demonstrate how to consume event data provided by an implementation of `IEventPublisher` within a logging service and a monitoring dashboard context.

### Example 1: Logging Job Completion Metrics
This example illustrates extracting performance data and error states from a completion event to write structured logs.

```csharp
public void HandleJobCompletion(IEventPublisher eventContext)
{
    // Ensure this event relates to a completion by checking for ExecutionId and Success status
    if (eventContext.ExecutionId != Guid.Empty)
    {
        var logLevel = eventContext.Success ? LogLevel.Information : LogLevel.Error;
        
        Console.WriteLine($"[{logLevel}] Job '{eventContext.JobName}' ({eventContext.JobId})");
        Console.WriteLine($"Execution ID: {eventContext.ExecutionId}");
        Console.WriteLine($"Duration: {eventContext.ExecutionTimeMs}ms");
        
        if (!eventContext.Success && !string.IsNullOrEmpty(eventContext.ErrorMessage))
        {
            Console.WriteLine($"Failure Reason: {eventContext.ErrorMessage}");
        }
    }
}
```

### Example 2: Auditing Job Creation
This example demonstrates using the `CreatedBy` and `Timestamp` properties to audit when and by whom a new job was registered.

```csharp
public void AuditJobRegistration(IEventPublisher eventContext)
{
    // Verify this is a creation event (ExecutionId typically empty, CreatedBy populated)
    if (!string.IsNullOrEmpty(eventContext.CreatedBy) && eventContext.ExecutionId == Guid.Empty)
    {
        var auditRecord = new 
        {
            EventTraceId = eventContext.EventId,
            OccurredAt = eventContext.Timestamp,
            Actor = eventContext.CreatedBy,
            TargetJob = eventContext.JobName,
            TargetJobId = eventContext.JobId
        };

        // Serialize and send to audit storage
        AuditService.Record(auditRecord);
    }
}
```

## Notes

*   **Event State Variability:** Not all properties are populated for every event type. For instance, `ExecutionId`, `Success`, `ExecutionTimeMs`, and `ErrorMessage` are typically only relevant for execution lifecycle events (start/complete) and may be default values (e.g., `Guid.Empty`, `0`, `null`) during job registration events. Conversely, `CreatedBy` is specific to registration events. Consumers must validate the context (e.g., checking if `ExecutionId` is non-empty) before accessing execution-specific metrics.
*   **Thread Safety:** The properties defined in `IEventPublisher` represent a snapshot of data at the moment of publication. Implementations should ensure that the object state is immutable once published to prevent race conditions where a consumer reads partially updated data. If the underlying implementation allows mutable state, external synchronization is required when accessing these members across multiple threads.
*   **Time Consistency:** The `Timestamp` property should consistently reflect UTC time to avoid issues with daylight saving time or server timezone discrepancies in distributed environments.
*   **Duplicate Event IDs:** While `EventId` is designed to be unique, consumers implementing retry logic or idempotent handlers should use this GUID to detect and discard duplicate transmissions of the same logical event.
