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
    /// <summary>
    /// Unique identifier for the execution.
    /// </summary>
    public Guid Id { get; set; }
    /// <summary>
    /// Identifier of the job that this execution belongs to.
    /// </summary>
    public Guid JobId { get; set; }
    /// <summary>
    /// Current status of the execution (e.g., Running, Success, Failed).
    /// </summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>
    /// Date and time when the execution started.
    /// </summary>
    public DateTime StartedAt { get; set; }
    /// <summary>
    /// Date and time when the execution completed, or null if still running.
    /// </summary>
    public DateTime? CompletedAt { get; set; }
    /// <summary>
    /// Total duration of the execution in milliseconds (wall clock time).
    /// </summary>
    public long DurationMilliseconds { get; set; }
    /// <summary>
    /// Number of times this execution has been attempted (initial attempt plus retries).
    /// </summary>
    public int AttemptNumber { get; set; }
    /// <summary>
    /// Actual execution time in milliseconds (excluding waiting time between retries).
    /// </summary>
    public long ExecutionTimeMs { get; set; }
    /// <summary>
    /// Number of retry attempts made for this execution (excluding the initial attempt).
    /// </summary>
    public int RetryAttempt { get; set; }
    /// <summary>
    /// Error message if the execution failed, otherwise null.
    /// </summary>
    public string? ErrorMessage { get; set; }
    /// <summary>
    /// Name of the executor that ran the job.
    /// </summary>
    public string ExecutorName { get; set; } = string.Empty;
    /// <summary>
    /// Indicates whether the execution can be retried upon failure.
    /// </summary>
    public bool IsRetryable { get; set; }
    /// <summary>
    /// Date and time when the execution record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Creates an ExecutionResponse from a JobExecution entity.
    /// </summary>
    /// <param name="execution">The JobExecution entity to convert.</param>
    /// <returns>An ExecutionResponse representing the execution.</returns>
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

    /// <summary>
    /// Returns a human-readable status text based on the internal Status string.
    /// </summary>
    /// <returns>A localized status string for display.</returns>
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

    /// <summary>
    /// Returns a string representation of the ExecutionResponse object.
    /// </summary>
    /// <returns>A string with the object's property values.</returns>
    public override string ToString() => $"ExecutionResponse {{ Id = {Id}, JobId = {JobId}, Status = {Status}, StartedAt = {StartedAt}, CompletedAt = {CompletedAt}, DurationMilliseconds = {DurationMilliseconds}, AttemptNumber = {AttemptNumber}, ErrorMessage = {ErrorMessage} }}";
}