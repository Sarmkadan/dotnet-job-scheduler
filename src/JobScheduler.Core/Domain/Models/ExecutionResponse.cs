#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using JobScheduler.Core.Constants;
using JobScheduler.Core.Domain.Entities;

namespace JobScheduler.Core.Domain.Models;

/// <summary>
/// Response model for job execution data in API responses.
/// Contains execution status, timing, and error information.
/// </summary>
public sealed class ExecutionResponse
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public long DurationMilliseconds { get; set; }
    public int AttemptNumber { get; set; }
    public long ExecutionTimeMs { get; set; }
    public int RetryAttempt { get; set; }
    public string? ErrorMessage { get; set; }
    public string ExecutorName { get; set; } = string.Empty;
    public bool IsRetryable { get; set; }
    public DateTime CreatedAt { get; set; }

    public static ExecutionResponse FromExecution(JobExecution execution)
    {
        return new ExecutionResponse
        {
            Id = execution.Id,
            JobId = execution.JobId,
            Status = execution.Status.ToString(),
            StartedAt = execution.StartedAt,
            CompletedAt = execution.CompletedAt,
            DurationMilliseconds = execution.DurationMilliseconds,
            AttemptNumber = execution.AttemptNumber,
            ExecutionTimeMs = execution.ExecutionTimeMs,
            RetryAttempt = execution.RetryAttempt,
            ErrorMessage = execution.ErrorMessage,
            ExecutorName = execution.ExecutorName,
            IsRetryable = execution.IsRetryable,
            CreatedAt = execution.CreatedAt
        };
    }

    public string GetStatusText()
    {
        return Status switch
        {
            nameof(ExecutionStatus.Running) => "Running",
            nameof(ExecutionStatus.Success) => "Success",
            nameof(ExecutionStatus.Failed) => "Failed",
            nameof(ExecutionStatus.Cancelled) => "Cancelled",
            nameof(ExecutionStatus.TimedOut) => "Timed Out",
            nameof(ExecutionStatus.Skipped) => "Skipped",
            _ => "Unknown"
        };
    }
}
