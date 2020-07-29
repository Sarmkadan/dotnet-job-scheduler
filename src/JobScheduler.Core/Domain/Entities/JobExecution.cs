// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using JobScheduler.Core.Constants;

namespace JobScheduler.Core.Domain.Entities;

/// <summary>
/// Represents a single execution attempt of a job.
/// Tracks execution lifecycle, timing, and error details.
/// </summary>
public class JobExecution
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid JobId { get; set; }

    public ExecutionStatus Status { get; set; } = ExecutionStatus.Running;

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    public long DurationMilliseconds { get; set; }

    public int AttemptNumber { get; set; } = 1;

    public string? Output { get; set; }

    public string? ErrorMessage { get; set; }

    public string? StackTrace { get; set; }

    public string ExecutorName { get; set; } = string.Empty;

    public string? ExecutorInstance { get; set; }

    public bool IsRetryable { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual Job Job { get; set; } = null!;

    /// <summary>
    /// Marks the execution as completed with the given status.
    /// Calculates duration automatically.
    /// </summary>
    public void MarkAsCompleted(ExecutionStatus status)
    {
        Status = status;
        CompletedAt = DateTime.UtcNow;
        DurationMilliseconds = (long)(CompletedAt.Value - StartedAt).TotalMilliseconds;
    }

    /// <summary>
    /// Marks execution as failed with error details.
    /// Determines if the failure is retryable based on error type.
    /// </summary>
    public void MarkAsFailed(string? errorMessage, string? stackTrace = null, bool retryable = true)
    {
        Status = ExecutionStatus.Failed;
        ErrorMessage = errorMessage;
        StackTrace = stackTrace;
        IsRetryable = retryable;
        MarkAsCompleted(ExecutionStatus.Failed);
    }

    /// <summary>
    /// Gets the total execution time including retries.
    /// </summary>
    public TimeSpan GetExecutionDuration()
    {
        if (CompletedAt.HasValue)
            return CompletedAt.Value - StartedAt;
        return DateTime.UtcNow - StartedAt;
    }

    /// <summary>
    /// Determines if this execution should be retried based on status and configuration.
    /// </summary>
    public bool ShouldRetry(int maxRetries)
    {
        return Status == ExecutionStatus.Failed &&
               IsRetryable &&
               AttemptNumber <= maxRetries;
    }

    /// <summary>
    /// Validates execution data for persistence.
    /// </summary>
    public bool IsValid()
    {
        if (JobId == Guid.Empty)
            return false;

        if (CompletedAt.HasValue && CompletedAt < StartedAt)
            return false;

        if (Status == ExecutionStatus.Success && CompletedAt == null)
            return false;

        if (Status == ExecutionStatus.Failed && string.IsNullOrWhiteSpace(ErrorMessage))
            return false;

        return true;
    }

    /// <summary>
    /// Sets output data from execution.
    /// </summary>
    public void SetOutput(string? output, int? maxLength = 10000)
    {
        if (output == null)
            return;

        Output = maxLength.HasValue && output.Length > maxLength.Value
            ? output[..maxLength.Value]
            : output;
    }

    /// <summary>
    /// Gets execution status description for logging.
    /// </summary>
    public string GetStatusDescription()
    {
        return Status switch
        {
            ExecutionStatus.Success => $"Completed successfully in {DurationMilliseconds}ms",
            ExecutionStatus.Failed => $"Failed on attempt {AttemptNumber}: {ErrorMessage}",
            ExecutionStatus.TimedOut => $"Execution timed out after {DurationMilliseconds}ms",
            ExecutionStatus.Cancelled => "Execution was cancelled",
            ExecutionStatus.Skipped => "Execution was skipped",
            _ => "Execution in progress"
        };
    }
}
