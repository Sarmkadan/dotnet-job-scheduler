#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;

namespace JobScheduler.Core.Exceptions;

/// <summary>
/// Thrown when a job execution fails or encounters an error.
/// </summary>
public sealed class ExecutionException : JobSchedulerException
{
    public Guid ExecutionId { get; set; }

    public Guid JobId { get; set; }

    public int AttemptNumber { get; set; }

    public ExecutionException(string message, Guid executionId, Guid jobId)
        : base(message, "EXECUTION_ERROR")
    {
        ExecutionId = executionId;
        JobId = jobId;
    }

    public ExecutionException(string message, Guid executionId, Guid jobId, int attemptNumber)
        : base(message, "EXECUTION_ERROR")
    {
        ExecutionId = executionId;
        JobId = jobId;
        AttemptNumber = attemptNumber;
    }

    public ExecutionException(string message, Guid executionId, Guid jobId, Exception innerException)
        : base(message, "EXECUTION_ERROR", innerException)
    {
        ExecutionId = executionId;
        JobId = jobId;
    }
}
