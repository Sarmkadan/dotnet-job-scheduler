#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;

namespace JobScheduler.Core.Exceptions;

/// <summary>
/// Thrown when a job execution is rejected due to concurrency control limits.
/// </summary>
public sealed class ConcurrencyException : JobSchedulerException
{
    /// <summary>Gets or sets the job ID.</summary>
    public Guid JobId { get; set; }

    /// <summary>Gets or sets the current number of concurrent executions.</summary>
    public int CurrentConcurrentExecutions { get; set; }

    /// <summary>Gets or sets the maximum allowed concurrent executions.</summary>
    public int MaxAllowed { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyException"/> class.
    /// </summary>
    /// <param name="jobId">The job ID.</param>
    /// <param name="currentCount">The current number of concurrent executions.</param>
    /// <param name="maxAllowed">The maximum allowed concurrent executions.</param>
    public ConcurrencyException(Guid jobId, int currentCount, int maxAllowed)
        : base(
            $"Job {jobId} cannot execute: current concurrent executions ({currentCount}) exceed maximum allowed ({maxAllowed}).",
            "CONCURRENCY_LIMIT_EXCEEDED")
    {
        JobId = jobId;
        CurrentConcurrentExecutions = currentCount;
        MaxAllowed = maxAllowed;
    }
}