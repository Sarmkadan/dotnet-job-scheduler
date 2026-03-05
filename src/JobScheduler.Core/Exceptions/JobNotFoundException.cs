#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;

namespace JobScheduler.Core.Exceptions;

/// <summary>
/// Thrown when a requested job cannot be found in the system.
/// </summary>
public sealed class JobNotFoundException : JobSchedulerException
{
    public Guid JobId { get; set; }

    public JobNotFoundException(Guid jobId)
        : base($"Job with ID '{jobId}' not found.", "JOB_NOT_FOUND")
    {
        JobId = jobId;
    }

    public JobNotFoundException(string jobName)
        : base($"Job with name '{jobName}' not found.", "JOB_NOT_FOUND") { }

    public JobNotFoundException(Guid jobId, Exception innerException)
        : base($"Job with ID '{jobId}' not found.", "JOB_NOT_FOUND", innerException)
    {
        JobId = jobId;
    }
}
