# JobScheduleHistory

`JobScheduleHistory` is an entity that records an auditable change event for a scheduled job. It captures the property that was modified, the old and new values, the timestamp, the identity of the user or process that made the change, and a reason for the change. The type provides factory methods for common change categories (general property changes, status transitions, and cron expression updates) and a helper to produce a human-readable description of the change.

## API

### Properties

- **`public Guid Id`**  
  The unique identifier of the history record.

- **`public Guid JobId`**  
  The identifier of the `Job` to which this history entry belongs.

- **`public string PropertyName`**  
  The name of the property that was changed (e.g. `"CronExpression"`, `"Status"`).

- **`public string? OldValue`**  
  The value of the property before the change. Null if the previous value was absent or not captured.

- **`public string? NewValue`**  
  The value of the property after the change. Null if the new value is absent or not captured.

- **`public DateTime ChangedAt`**  
  The UTC timestamp when the change occurred.

- **`public string? ChangedBy`**  
  An identifier for the user or system component that performed the change. Null when the actor is unknown.

- **`public string ChangeReason`**  
  A mandatory description of why the change was made (e.g. `"Manual adjustment"`, `"System deactivation"`).

- **`public string? Details`**  
  Optional supplementary information about the change (e.g. a full stack trace, a comment, or serialised context).

- **`public virtual Job Job`**  
  Navigation property to the parent `Job` entity. Lazy-loaded by default in Entity Framework Core scenarios.

### Static Factory Methods

- **`public static JobScheduleHistory CreateChange(...)`**  
  Creates a history record for a generic property change.  
  *Parameters*: typically accepts the `Job`, the property name, old and new values, the actor, and a reason.  
  *Returns*: a new `JobScheduleHistory` instance with `PropertyName` set to the supplied property and `ChangeReason` populated.  
  *Throws*: `ArgumentNullException` when required arguments such as `Job` or `ChangeReason` are null.

- **`public static JobScheduleHistory CreateStatusChange(...)`**  
  Creates a history record specifically for a job status transition (e.g. `Running` → `Paused`).  
  *Parameters*: the `Job`, the old status, the new status, the actor, and a reason.  
  *Returns*: a new `JobScheduleHistory` instance with `PropertyName` set to `"Status"`.  
  *Throws*: `ArgumentNullException` when required arguments are null.

- **`public static JobScheduleHistory CreateCronChange(...)`**  
  Creates a history record specifically for a cron expression change.  
  *Parameters*: the `Job`, the old cron expression, the new cron expression, the actor, and a reason.  
  *Returns*: a new `JobScheduleHistory` instance with `PropertyName` set to `"CronExpression"`.  
  *Throws*: `ArgumentNullException` when required arguments are null.

### Instance Methods

- **`public string GetChangeDescription()`**  
  Produces a human-readable summary of the change, typically combining the property name, old and new values, and the reason.  
  *Returns*: a formatted string such as `"Status changed from 'Running' to 'Paused'. Reason: Manual pause."`.  
  *Throws*: no exceptions under normal operation; relies on the integrity of its own property values.

- **`public bool IsValid`**  
  Indicates whether the history record is in a valid state. Implementations typically verify that `JobId` is not empty, `PropertyName` is not null or whitespace, `ChangeReason` is not null or whitespace, and `ChangedAt` is a non-default `DateTime`.  
  *Returns*: `true` if all validation rules pass; otherwise `false`.  
  *Throws*: no exceptions; performs a pure check.

## Usage

### Example 1: Recording a status change

```csharp
Job job = jobRepository.GetById(jobId);
JobScheduleHistory history = JobScheduleHistory.CreateStatusChange(
    job,
    oldStatus: "Running",
    newStatus: "Paused",
    changedBy: "admin@example.com",
    changeReason: "Maintenance window"
);

if (history.IsValid)
{
    historyRepository.Add(history);
    await historyRepository.SaveChangesAsync();
    Console.WriteLine(history.GetChangeDescription());
}
```

### Example 2: Recording a cron expression update

```csharp
Job job = jobRepository.GetById(jobId);
string oldCron = "0 0 * * *";
string newCron = "0 6 * * 1";

JobScheduleHistory history = JobScheduleHistory.CreateCronChange(
    job,
    oldCron,
    newCron,
    changedBy: "scheduler-service",
    changeReason: "Shifted to Monday mornings per business request"
);

history.Details = "Approved in change ticket CT-4521.";

if (history.IsValid)
{
    historyRepository.Add(history);
    await historyRepository.SaveChangesAsync();
}
```

## Notes

- **Validation timing**: `IsValid` is a synchronous check that does not enforce validity at construction time. Consumers should inspect `IsValid` before persisting a record, especially when using factory methods that may receive incomplete data.
- **Nullability**: `OldValue`, `NewValue`, `ChangedBy`, and `Details` are explicitly nullable. Code that consumes these members must handle null to avoid formatting errors in `GetChangeDescription()` or downstream display logic.
- **Navigation property**: `Job` is marked `virtual`, enabling Entity Framework Core lazy loading. In detached or non-ORM scenarios this property may be null unless explicitly loaded.
- **Thread safety**: The type is not designed for concurrent mutation. Static factory methods and `GetChangeDescription()` do not mutate shared state, but modifying properties on an instance from multiple threads without external synchronisation can lead to inconsistent reads.
- **Immutability after persistence**: Once persisted, history records should be treated as immutable audit entries. There are no built-in guards preventing modification of properties after creation; such discipline must be enforced at the application layer.
- **`GetChangeDescription()` formatting**: The exact format of the returned string depends on the implementation. It may omit null old/new values or truncate long strings. Always verify the output meets your logging or UI requirements.
