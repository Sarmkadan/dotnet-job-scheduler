# AuditLogger

Central logging facility for tracking API calls, job lifecycle events, security incidents, and execution outcomes within the dotnet-job-scheduler system. Provides structured audit trails, historical queries, and operational statistics for compliance, debugging, and auditing purposes.

## API

### `AuditLogger()`
Constructs a new instance of the audit logger. No parameters are required. This constructor initializes a new audit logger with default configuration for capturing system events.

### `async Task LogApiCallAsync(string method, string path, string? userId = null, string? details = null, AuditSeverity severity = AuditSeverity.Information)`
Records an API call event. Parameters:
- `method`: HTTP method (e.g., "GET", "POST").
- `path`: Request path or endpoint.
- `userId`: Optional identifier of the user making the request.
- `details`: Optional additional context about the call.
- `severity`: Severity level of the event (default: `Information`).
Returns: A task representing the asynchronous logging operation.
Throws: `ArgumentNullException` if `method` or `path` is null.

### `async Task LogJobCreationAsync(Guid jobId, string jobName, string? userId = null, string? details = null, AuditSeverity severity = AuditSeverity.Information)`
Records the creation of a new job. Parameters:
- `jobId`: Unique identifier of the created job.
- `jobName`: Human-readable name of the job.
- `userId`: Optional identifier of the user who created the job.
- `details`: Optional additional context about the creation.
- `severity`: Severity level of the event (default: `Information`).
Returns: A task representing the asynchronous logging operation.
Throws: `ArgumentNullException` if `jobId` or `jobName` is null.

### `async Task LogJobModificationAsync(Guid jobId, string jobName, string? userId = null, string? details = null, AuditSeverity severity = AuditSeverity.Information)`
Records a modification to an existing job. Parameters:
- `jobId`: Unique identifier of the modified job.
- `jobName`: Human-readable name of the job.
- `userId`: Optional identifier of the user who modified the job.
- `details`: Optional additional context about the modification.
- `severity`: Severity level of the event (default: `Information`).
Returns: A task representing the asynchronous logging operation.
Throws: `ArgumentNullException` if `jobId` or `jobName` is null.

### `async Task LogJobDeletionAsync(Guid jobId, string jobName, string? userId = null, string? details = null, AuditSeverity severity = AuditSeverity.Information)`
Records the deletion of a job. Parameters:
- `jobId`: Unique identifier of the deleted job.
- `jobName`: Human-readable name of the job.
- `userId`: Optional identifier of the user who deleted the job.
- `details`: Optional additional context about the deletion.
- `severity`: Severity level of the event (default: `Information`).
Returns: A task representing the asynchronous logging operation.
Throws: `ArgumentNullException` if `jobId` or `jobName` is null.

### `async Task LogSecurityEventAsync(string eventType, string? userId = null, string? details = null, AuditSeverity severity = AuditSeverity.Warning)`
Records a security-related event. Parameters:
- `eventType`: Type or category of the security event.
- `userId`: Optional identifier of the user associated with the event.
- `details`: Optional additional context about the event.
- `severity`: Severity level of the event (default: `Warning`).
Returns: A task representing the asynchronous logging operation.
Throws: `ArgumentNullException` if `eventType` is null.

### `async Task LogExecutionEventAsync(Guid jobId, string jobName, string? userId = null, string? details = null, AuditSeverity severity = AuditSeverity.Information)`
Records an execution event for a job. Parameters:
- `jobId`: Unique identifier of the job being executed.
- `jobName`: Human-readable name of the job.
- `userId`: Optional identifier of the user triggering the execution.
- `details`: Optional additional context about the execution.
- `severity`: Severity level of the event (default: `Information`).
Returns: A task representing the asynchronous logging operation.
Throws: `ArgumentNullException` if `jobId` or `jobName` is null.

### `List<AuditLogEntry> GetAuditLogs(DateTime? from = null, DateTime? to = null, string? userId = null, string? eventType = null)`
Retrieves a filtered list of audit log entries. Parameters:
- `from`: Optional start of the time range to query.
- `to`: Optional end of the time range to query.
- `userId`: Optional filter for entries associated with a specific user.
- `eventType`: Optional filter for entries of a specific event type.
Returns: A list of `AuditLogEntry` objects matching the specified filters, ordered chronologically.

### `int ClearOldLogsAsync(TimeSpan retentionPeriod)`
Removes audit log entries older than the specified retention period. Parameters:
- `retentionPeriod`: Time span representing the age threshold for deletion.
Returns: The number of log entries removed.
Throws: `ArgumentOutOfRangeException` if `retentionPeriod` is negative.

### `AuditStatistics GetStatistics(DateTime? from = null, DateTime? to = null)`
Retrieves aggregated statistics for audit logs within a time range. Parameters:
- `from`: Optional start of the time range to analyze.
- `to`: Optional end of the time range to analyze.
Returns: An `AuditStatistics` object containing counts and summaries of log entries by severity and event type.

### `Guid EventId`
Unique identifier for the audit event. Read-only property. Automatically assigned when the log entry is created.

### `string EventType`
Type or category of the audit event. Read-only property. Set during construction of the log entry.

### `DateTime Timestamp`
Timestamp of when the audit event occurred. Read-only property. Automatically set to the current UTC time when the log entry is created.

### `string? UserId`
Identifier of the user associated with the event. Read-only property. Optional and may be null.

### `Guid? EntityId`
Unique identifier of the entity involved in the event (e.g., job ID). Read-only property. Optional and may be null.

### `string? EntityType`
Human-readable type of the entity involved in the event (e.g., "Job"). Read-only property. Optional and may be null.

### `string Details`
Detailed context or description of the audit event. Read-only property. May be empty but not null.

### `AuditSeverity Severity`
Severity level of the audit event. Read-only property. Defaults to `Information` unless specified otherwise.

### `string Method`
HTTP method or action associated with the event (e.g., "POST"). Read-only property. Set during construction of the log entry.

### `string Path`
Path or endpoint associated with the event (e.g., "/api/jobs"). Read-only property. Set during construction of the log entry.

## Usage
