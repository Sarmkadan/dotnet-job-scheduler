// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;

namespace JobScheduler.Core.Exceptions;

/// <summary>
/// Thrown when a job execution is rejected due to concurrency control limits.
/// </summary>
public class ConcurrencyException : JobSchedulerException
{
    public Guid JobId { get; set; }

    public int CurrentConcurrentExecutions { get; set; }

    public int MaxAllowed { get; set; }

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
