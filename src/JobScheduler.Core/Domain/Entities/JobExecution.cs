#nullable enable
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
    /// <summary>Unique identifier for this execution.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Identifier of the job this execution belongs to.</summary>
    public Guid JobId { get; set; }

    /// <summary>Current status of the execution.</summary>
    public ExecutionStatus Status { get; set; } = ExecutionStatus.Running;

    /// <summary>Timestamp when the execution started.</summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Timestamp when the execution completed, if applicable.</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>Duration of the execution in milliseconds.</summary>
    public long DurationMilliseconds { get; set; }

    /// <summary>Current attempt number for this execution (starting at 1).</summary>
    public int AttemptNumber { get; set; } = 1;

    /// <summary>Output or result of the execution.</summary>
    public string? Output { get; set; }

    /// <summary>Error message if the execution failed.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Stack trace if the execution failed with an exception.</summary>
    public string? StackTrace { get; set; }

    /// <summary>Name of the executor that ran this execution.</summary>
    public string ExecutorName { get; set; } = string.Empty;

    /// <summary>Optional instance identifier of the executor.</summary>
    public string? ExecutorInstance { get; set; }

    /// <summary>Whether this execution can be retried if it fails.</summary>
    public bool IsRetryable { get; set; }

    /// <summary>Timestamp when the execution was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Approximate memory usage recorded during this execution, in megabytes.</summary>
    public long MemoryUsageMb { get; set; }

    /// <summary>Approximate CPU usage recorded during this execution, as a percentage (0-100).</summary>
    public double CpuUsagePercent { get; set; }

    /// <summary>The job associated with this execution.</summary>
    public virtual Job Job { get; set; } = null!;

    /// <summary>
    /// Alias for <see cref="DurationMilliseconds"/> used by reporting and notification code.
    /// </summary>
    public long ExecutionTimeMs
    {
        get => DurationMilliseconds;
        set => DurationMilliseconds = value;
    }

    /// <summary>
    /// Alias for <see cref="AttemptNumber"/> used by reporting and notification code.
    /// </summary>
    public int RetryAttempt
    {
        get => AttemptNumber;
        set => AttemptNumber = value;
    }

    /// <summary>
    /// Alias for <see cref="Output"/> used by export/reporting code.
    /// </summary>
    public string? ExecutionOutput
    {
        get => Output;
        set => Output = value;
    }

    /// <summary>
    /// Marks the execution as completed with the given status.
    /// Calculates duration automatically.
    /// </summary>
    /// <param name="status">The status to set for the execution.</param>
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
    /// <param name="errorMessage">The error message.</param>
    /// <param name="stackTrace">The stack trace.</param>
    /// <param name="retryable">Whether the failure is retryable.</param>
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
    /// <returns>The total execution time as a TimeSpan.</returns>
    public TimeSpan GetExecutionDuration()
    {
        if (CompletedAt.HasValue)
            return CompletedAt.Value - StartedAt;
        return DateTime.UtcNow - StartedAt;
    }

    /// <summary>
    /// Determines if this execution should be retried based on status and configuration.
    /// </summary>
    /// <param name="maxRetries">The maximum number of retries allowed.</param>
    /// <returns>True if the execution should be retried; otherwise, false.</returns>
    public bool ShouldRetry(int maxRetries)
    {
        return Status == ExecutionStatus.Failed &&
               IsRetryable &&
               AttemptNumber <= maxRetries;
    }

    /// <summary>
    /// Validates execution data for persistence.
    /// </summary>
    /// <returns>True if the execution data is valid; otherwise, false.</returns>
    public bool IsValid()
    {
        if (JobId == Guid.Empty)
            return false;

        if (CompletedAt.HasValue && CompletedAt < StartedAt)
            return false;

        if (Status == ExecutionStatus.Success && CompletedAt is null)
            return false;

        if (Status == ExecutionStatus.Failed && string.IsNullOrWhiteSpace(ErrorMessage))
            return false;

        return true;
    }

    /// <summary>
    /// Sets output data from execution.
    /// </summary>
    /// <param name="output">The output data to set.</param>
    /// <param name="maxLength">The maximum length of the output (optional).</param>
    public void SetOutput(string? output, int? maxLength = 10000)
    {
        if (output is null)
            return;

        Output = maxLength.HasValue && output.Length > maxLength.Value
            ? output[..maxLength.Value]
            : output;
    }

    /// <summary>
    /// Gets execution status description for logging.
    /// </summary>
    /// <returns>A string describing the execution status.</returns>
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

    public override string ToString() => $"JobExecution {{ Id = {Id}, JobId = {JobId}, Status = {Status}, StartedAt = {StartedAt}, CompletedAt = {CompletedAt}, DurationMilliseconds = {DurationMilliseconds} }}";
}