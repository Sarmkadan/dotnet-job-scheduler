#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Globalization;

namespace JobScheduler.Core.Services;

/// <summary>
/// Validation helpers for <see cref="AuditLogEntry"/> to ensure audit log entries meet compliance requirements.
/// WHY: Validation prevents malformed audit logs that could fail compliance audits or security investigations.
/// </summary>
public static class AuditLoggerValidation
{
    /// <summary>
    /// Validates an <see cref="AuditLogEntry"/> instance for common issues.
    /// </summary>
    /// <param name="value">The audit log entry to validate.</param>
    /// <returns>A list of validation problems (empty if valid).</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this AuditLogEntry? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate required properties
        if (value.EventId == Guid.Empty)
        {
            problems.Add("EventId must not be empty (Guid.Empty).");
        }

        if (string.IsNullOrWhiteSpace(value.EventType))
        {
            problems.Add("EventType must not be null or whitespace.");
        }
        else if (value.EventType.Length > 100)
        {
            problems.Add("EventType must not exceed 100 characters.");
        }

        if (value.Timestamp == default)
        {
            problems.Add("Timestamp must not be default (DateTime.MinValue).");
        }
        else if (value.Timestamp > DateTime.UtcNow.AddMinutes(5))
        {
            problems.Add("Timestamp cannot be in the future.");
        }

        if (value.Severity < AuditSeverity.Debug || value.Severity > AuditSeverity.Critical)
        {
            problems.Add("Severity must be a valid AuditSeverity value.");
        }

        if (string.IsNullOrWhiteSpace(value.Details))
        {
            problems.Add("Details must not be null or whitespace.");
        }
        else if (value.Details.Length > 4000)
        {
            problems.Add("Details must not exceed 4000 characters.");
        }

        // Method and Path are not part of AuditLogEntry - they belong to ApiCallAudit
        // These would be validated separately if needed

        // Validate nullable properties
        if (value.UserId is not null)
        {
            if (string.IsNullOrWhiteSpace(value.UserId))
            {
                problems.Add("UserId must not be empty if specified.");
            }
            else if (value.UserId.Length > 100)
            {
                problems.Add("UserId must not exceed 100 characters.");
            }
        }

        if (value.EntityId.HasValue && value.EntityId == Guid.Empty)
        {
            problems.Add("EntityId must not be Guid.Empty if specified.");
        }

        if (value.EntityType is not null)
        {
            if (string.IsNullOrWhiteSpace(value.EntityType))
            {
                problems.Add("EntityType must not be empty if specified.");
            }
            else if (value.EntityType.Length > 50)
            {
                problems.Add("EntityType must not exceed 50 characters.");
            }
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="AuditLogEntry"/> is valid.
    /// </summary>
    /// <param name="value">The audit log entry to check.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this AuditLogEntry? value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="AuditLogEntry"/> is valid, throwing an <see cref="ArgumentException"/> if it is not.
    /// </summary>
    /// <param name="value">The audit log entry to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is not valid.</exception>
    public static void EnsureValid(this AuditLogEntry? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count == 0)
        {
            return;
        }

        throw new ArgumentException(
            $"AuditLogEntry is not valid. Problems: {string.Join("; ", problems)}",
            nameof(value));
    }
}