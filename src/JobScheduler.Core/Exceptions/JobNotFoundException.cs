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
    /// <summary>Gets or sets the job ID.</summary>
    public Guid JobId { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="JobNotFoundException"/> class.
    /// </summary>
    /// <param name="jobId">The job ID that was not found.</param>
    public JobNotFoundException(Guid jobId)
        : base($"Job with ID '{jobId}' not found.", "JOB_NOT_FOUND")
    {
        JobId = jobId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JobNotFoundException"/> class.
    /// </summary>
    /// <param name="jobName">The job name that was not found.</param>
    public JobNotFoundException(string jobName)
        : base(string.IsNullOrEmpty(jobName)
            ? "Job with name '' not found."
            : $"Job with name '{jobName}' not found.",
            "JOB_NOT_FOUND")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JobNotFoundException"/> class with an inner exception.
    /// </summary>
    /// <param name="jobId">The job ID that was not found.</param>
    /// <param name="innerException">The inner exception.</param>
    public JobNotFoundException(Guid jobId, Exception innerException)
        : base($"Job with ID '{jobId}' not found.", "JOB_NOT_FOUND", innerException)
    {
        JobId = jobId;
    }
}