#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using JobScheduler.Core.Constants;

namespace JobScheduler.Core.Domain.Models;

/// <summary>
/// Response model for job execution data in API responses.
/// Contains execution status, timing, and error information.
/// </summary>
public class ExecutionResponse
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public ExecutionStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public long DurationMilliseconds { get; set; }
    public int AttemptNumber { get; set; }
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
            Status = execution.Status,
            StartedAt = execution.StartedAt,
            CompletedAt = execution.CompletedAt,
            DurationMilliseconds = execution.DurationMilliseconds,
            AttemptNumber = execution.AttemptNumber,
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
            ExecutionStatus.Running => "Running",
            ExecutionStatus.Success => "Success",
            ExecutionStatus.Failed => "Failed",
            ExecutionStatus.Cancelled => "Cancelled",
            ExecutionStatus.TimedOut => "Timed Out",
            ExecutionStatus.Skipped => "Skipped",
            _ => "Unknown"
        };
    }
}
