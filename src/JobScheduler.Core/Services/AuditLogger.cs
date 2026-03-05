#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace JobScheduler.Core.Services;

/// <summary>
/// Audit logging service for compliance and security purposes.
/// Tracks all important scheduler operations and API calls.
/// WHY: Audit logs are essential for compliance requirements and security investigations.
/// </summary>
public class AuditLogger
{
    private readonly ILogger<AuditLogger> _logger;
    private readonly ConcurrentQueue<AuditLogEntry> _auditLogs;
    private readonly int _maxLogsRetained = 50000;

    public AuditLogger(ILogger<AuditLogger> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _auditLogs = new ConcurrentQueue<AuditLogEntry>();
    }

    /// <summary>
    /// Logs API call with method, endpoint, and status.
    /// </summary>
    public async Task LogApiCallAsync(ApiCallAudit audit)
    {
        var entry = new AuditLogEntry
        {
            EventId = Guid.NewGuid(),
            EventType = "API_CALL",
            Timestamp = DateTime.UtcNow,
            UserId = audit.UserId,
            Details = JsonSerializer.Serialize(audit),
            Severity = GetSeverity(audit.StatusCode)
        };

        _auditLogs.Enqueue(entry);
        TrimOldLogs();

        _logger.LogInformation("[AUDIT] API Call: {Method} {Path} - Status {StatusCode} - User: {UserId} - Time: {ExecutionTimeMs}ms",
            audit.Method, audit.Path, audit.StatusCode, audit.UserId, audit.ExecutionTimeMs);
    }

    /// <summary>
    /// Logs job creation event.
    /// </summary>
    public async Task LogJobCreationAsync(Guid jobId, string jobName, string? createdBy)
    {
        var entry = new AuditLogEntry
        {
            EventId = Guid.NewGuid(),
            EventType = "JOB_CREATED",
            Timestamp = DateTime.UtcNow,
            UserId = createdBy,
            EntityId = jobId,
            EntityType = "Job",
            Details = $"Job '{jobName}' created",
            Severity = AuditSeverity.Info
        };

        _auditLogs.Enqueue(entry);
        TrimOldLogs();

        _logger.LogInformation("[AUDIT] Job created: {JobId} ({JobName}) by {CreatedBy}",
            jobId, jobName, createdBy ?? "Unknown");
    }

    /// <summary>
    /// Logs job modification event.
    /// </summary>
    public async Task LogJobModificationAsync(Guid jobId, string jobName, string field, object? oldValue, object? newValue, string? modifiedBy)
    {
        var entry = new AuditLogEntry
        {
            EventId = Guid.NewGuid(),
            EventType = "JOB_MODIFIED",
            Timestamp = DateTime.UtcNow,
            UserId = modifiedBy,
            EntityId = jobId,
            EntityType = "Job",
            Details = JsonSerializer.Serialize(new { field, oldValue, newValue }),
            Severity = AuditSeverity.Info
        };

        _auditLogs.Enqueue(entry);
        TrimOldLogs();

        _logger.LogInformation("[AUDIT] Job modified: {JobId} ({JobName}) - Field: {Field} - Modified by: {ModifiedBy}",
            jobId, jobName, field, modifiedBy ?? "Unknown");
    }

    /// <summary>
    /// Logs job deletion event.
    /// </summary>
    public async Task LogJobDeletionAsync(Guid jobId, string jobName, string? deletedBy)
    {
        var entry = new AuditLogEntry
        {
            EventId = Guid.NewGuid(),
            EventType = "JOB_DELETED",
            Timestamp = DateTime.UtcNow,
            UserId = deletedBy,
            EntityId = jobId,
            EntityType = "Job",
            Details = $"Job '{jobName}' deleted",
            Severity = AuditSeverity.Warning // Deletions are significant events
        };

        _auditLogs.Enqueue(entry);
        TrimOldLogs();

        _logger.LogWarning("[AUDIT] Job deleted: {JobId} ({JobName}) by {DeletedBy}",
            jobId, jobName, deletedBy ?? "Unknown");
    }

    /// <summary>
    /// Logs security-related event (auth, permission denied, etc).
    /// </summary>
    public async Task LogSecurityEventAsync(string eventType, string? userId, string message, int severity = 2)
    {
        var entry = new AuditLogEntry
        {
            EventId = Guid.NewGuid(),
            EventType = eventType,
            Timestamp = DateTime.UtcNow,
            UserId = userId,
            Details = message,
            Severity = (AuditSeverity)severity
        };

        _auditLogs.Enqueue(entry);
        TrimOldLogs();

        var logLevel = severity switch
        {
            1 => LogLevel.Information,
            2 => LogLevel.Warning,
            3 => LogLevel.Error,
            _ => LogLevel.Critical
        };

        _logger.Log(logLevel, "[AUDIT] Security Event: {EventType} - User: {UserId} - {Message}",
            eventType, userId ?? "Unknown", message);
    }

    /// <summary>
    /// Logs execution event (start, complete, fail).
    /// </summary>
    public async Task LogExecutionEventAsync(Guid jobId, Guid executionId, string eventType, string status, long executionTimeMs)
    {
        var entry = new AuditLogEntry
        {
            EventId = Guid.NewGuid(),
            EventType = eventType,
            Timestamp = DateTime.UtcNow,
            EntityId = jobId,
            EntityType = "Execution",
            Details = JsonSerializer.Serialize(new { executionId, status, executionTimeMs }),
            Severity = status == "Failed" ? AuditSeverity.Warning : AuditSeverity.Debug
        };

        _auditLogs.Enqueue(entry);
        TrimOldLogs();

        _logger.LogDebug("[AUDIT] Job Execution: {JobId} - {Status} - Time: {ExecutionTimeMs}ms",
            jobId, status, executionTimeMs);
    }

    /// <summary>
    /// Retrieves audit logs with optional filtering.
    /// </summary>
    public List<AuditLogEntry> GetAuditLogs(DateTime? from = null, DateTime? to = null, string? userId = null, string? eventType = null)
    {
        var logs = _auditLogs.ToList();

        if (from.HasValue)
            logs = logs.Where(l => l.Timestamp >= from.Value).ToList();

        if (to.HasValue)
            logs = logs.Where(l => l.Timestamp <= to.Value).ToList();

        if (!string.IsNullOrEmpty(userId))
            logs = logs.Where(l => l.UserId == userId).ToList();

        if (!string.IsNullOrEmpty(eventType))
            logs = logs.Where(l => l.EventType == eventType).ToList();

        return logs.OrderByDescending(l => l.Timestamp).ToList();
    }

    /// <summary>
    /// Clears audit logs older than specified days.
    /// Part of retention policy implementation.
    /// </summary>
    public int ClearOldLogsAsync(int daysOld = 90)
    {
        var cutoff = DateTime.UtcNow.AddDays(-daysOld);
        var oldLogs = _auditLogs.Where(l => l.Timestamp < cutoff).ToList();

        foreach (var log in oldLogs)
        {
            _auditLogs.TryDequeue(out _);
        }

        _logger.LogInformation("Cleared {Count} audit logs older than {DaysOld} days", oldLogs.Count, daysOld);
        return oldLogs.Count;
    }

    /// <summary>
    /// Gets audit log statistics.
    /// </summary>
    public AuditStatistics GetStatistics()
    {
        var logs = _auditLogs.ToList();

        return new AuditStatistics
        {
            TotalLogs = logs.Count,
            LogsByEventType = logs.GroupBy(l => l.EventType).ToDictionary(g => g.Key, g => g.Count()),
            LogsBySeverity = logs.GroupBy(l => l.Severity).ToDictionary(g => g.Key.ToString(), g => g.Count()),
            OldestLog = logs.Any() ? logs.Min(l => l.Timestamp) : null,
            NewestLog = logs.Any() ? logs.Max(l => l.Timestamp) : null
        };
    }

    private void TrimOldLogs()
    {
        while (_auditLogs.Count > _maxLogsRetained)
        {
            _auditLogs.TryDequeue(out _);
        }
    }

    private AuditSeverity GetSeverity(int statusCode)
    {
        return statusCode switch
        {
            >= 500 => AuditSeverity.Error,
            >= 400 => AuditSeverity.Warning,
            >= 300 => AuditSeverity.Info,
            _ => AuditSeverity.Debug
        };
    }
}

public class AuditLogEntry
{
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? UserId { get; set; }
    public Guid? EntityId { get; set; }
    public string? EntityType { get; set; }
    public string Details { get; set; } = string.Empty;
    public AuditSeverity Severity { get; set; }
}

public class ApiCallAudit
{
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public long ExecutionTimeMs { get; set; }
    public string? UserId { get; set; }
    public DateTime Timestamp { get; set; }
}

public class AuditStatistics
{
    public int TotalLogs { get; set; }
    public Dictionary<string, int> LogsByEventType { get; set; } = new();
    public Dictionary<string, int> LogsBySeverity { get; set; } = new();
    public DateTime? OldestLog { get; set; }
    public DateTime? NewestLog { get; set; }
}

public enum AuditSeverity
{
    Debug = 0,
    Info = 1,
    Warning = 2,
    Error = 3,
    Critical = 4
}
