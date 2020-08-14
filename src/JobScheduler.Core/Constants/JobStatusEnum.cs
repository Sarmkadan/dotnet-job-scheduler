#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace JobScheduler.Core.Constants;

/// <summary>
/// Represents the status of a scheduled job in the system.
/// </summary>
public enum JobStatus
{
    /// <summary>Job has been created but not yet scheduled</summary>
    Pending = 0,

    /// <summary>Job is currently scheduled and waiting for execution</summary>
    Scheduled = 1,

    /// <summary>Job is currently executing</summary>
    Running = 2,

    /// <summary>Job execution completed successfully</summary>
    Completed = 3,

    /// <summary>Job failed and is awaiting retry</summary>
    Failed = 4,

    /// <summary>Job was suspended by user or system</summary>
    Suspended = 5,

    /// <summary>Job has been cancelled</summary>
    Cancelled = 6,

    /// <summary>Job failed permanently after all retries exhausted</summary>
    FailedPermanently = 7
}
