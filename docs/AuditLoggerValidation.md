# AuditLoggerValidation

Static validation helper for `AuditLogEntry` to ensure audit log entries meet compliance requirements before being written by `AuditLogger`.

## Methods

### `Validate(this AuditLogEntry? value)`

Validates an audit log entry and returns a list of validation problems.

**Signature**
```csharp
public static IReadOnlyList<string> Validate(this AuditLogEntry? value)
```

**Rules**
- Throws `ArgumentNullException` if `value` is null.
- Validates required properties:
  - `EventId` must not be `Guid.Empty`.
  - `EventType` must not be null, whitespace, or exceed 100 characters.
  - `Timestamp` must not be `DateTime.MinValue`, must not be more than 5 minutes in the future, and must not be more than one year in the past.
  - `Severity` must be a valid `AuditSeverity` value (between `Debug` and `Critical` inclusive).
  - `Details` must not be null, whitespace, or exceed 4000 characters.
- Validates nullable properties if they have values:
  - `UserId` must not be null/whitespace and must not exceed 100 characters if specified.
  - `EntityId` must not be `Guid.Empty` if specified.
  - `EntityType` must not be null/whitespace and must not exceed 50 characters if specified.

**Returns**
- An `IReadOnlyList<string>` containing validation error messages (empty if the entry is valid).

**Exceptions**
- `ArgumentNullException` if `value` is null.

### `IsValid(this AuditLogEntry? value)`

Determines whether the specified audit log entry is valid.

**Signature**
```csharp
public static bool IsValid(this AuditLogEntry? value)
```

**Rules**
- Throws `ArgumentNullException` if `value` is null.
- Returns `true` if `Validate(value)` returns an empty list; otherwise `false`.

**Returns**
- `true` if the audit log entry is valid; otherwise `false`.

**Exceptions**
- `ArgumentNullException` if `value` is null.

### `EnsureValid(this AuditLogEntry? value)`

Ensures the specified audit log entry is valid, throwing an exception if it is not.

**Signature**
```csharp
public static void EnsureValid(this AuditLogEntry? value)
```

**Rules**
- Throws `ArgumentNullException` if `value` is null.
- Calls `Validate(value)` and throws an `ArgumentException` containing all validation problems if the list is not empty.

**Exceptions**
- `ArgumentNullException` if `value` is null.
- `ArgumentException` if the audit log entry is not valid, with a message listing all validation problems.

## How it complements AuditLogger

The `AuditLoggerValidation` class provides pre-validation logic that complements the `AuditLogger` service by ensuring audit log entries meet strict compliance requirements before being written to the audit log. While `AuditLogger` handles the persistence and formatting of audit entries, `AuditLoggerValidation` acts as a guardrail to prevent malformed entries from compromising audit integrity.

Use these validation methods before calling `AuditLogger.WriteAsync` to:
- Prevent invalid entries from being written to the audit log
- Provide immediate feedback about validation failures
- Ensure compliance with audit log standards (character limits, required fields, temporal constraints)

See [AuditLogger](./AuditLogger.md) for details on the audit logging service that uses these validation helpers.