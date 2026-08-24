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
    /// <summary>Gets or sets the execution ID.</summary>
    public Guid ExecutionId { get; set; }

    /// <summary>Gets or sets the job ID.</summary>
    public Guid JobId { get; set; }

    /// <summary>Gets or sets the attempt number.</summary>
    public int AttemptNumber { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="executionId">The execution ID.</param>
    /// <param name="jobId">The job ID.</param>
    public ExecutionException(string message, Guid executionId, Guid jobId)
        : base(message, "EXECUTION_ERROR")
    {
        ExecutionId = executionId;
        JobId = jobId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="executionId">The execution ID.</param>
    /// <param name="jobId">The job ID.</param>
    /// <param name="attemptNumber">The attempt number.</param>
    public ExecutionException(string message, Guid executionId, Guid jobId, int attemptNumber)
        : base(message, "EXECUTION_ERROR")
    {
        ExecutionId = executionId;
        JobId = jobId;
        AttemptNumber = attemptNumber;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionException"/> class with an inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="executionId">The execution ID.</param>
    /// <param name="jobId">The job ID.</param>
    /// <param name="innerException">The inner exception.</param>
    public ExecutionException(string message, Guid executionId, Guid jobId, Exception innerException)
        : base(message, "EXECUTION_ERROR", innerException)
    {
        ExecutionId = executionId;
        JobId = jobId;
    }

   public override string ToString() => $"ExecutionException {{ ExecutionId = {ExecutionId}, JobId = {JobId}, AttemptNumber = {AttemptNumber} }}";
}