// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace JobScheduler.Core.Constants;

/// <summary>
/// Represents the result status of a single job execution attempt.
/// </summary>
public enum ExecutionStatus
{
    /// <summary>Execution is currently in progress</summary>
    Running = 0,

    /// <summary>Execution completed successfully</summary>
    Success = 1,

    /// <summary>Execution failed with an error</summary>
    Failed = 2,

    /// <summary>Execution was cancelled before completion</summary>
    Cancelled = 3,

    /// <summary>Execution timed out</summary>
    TimedOut = 4,

    /// <summary>Execution was skipped due to concurrency control</summary>
    Skipped = 5
}
