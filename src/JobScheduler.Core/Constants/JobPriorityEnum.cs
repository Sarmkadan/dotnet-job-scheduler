// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace JobScheduler.Core.Constants;

/// <summary>
/// Defines priority levels for job execution in the scheduler queue.
/// Higher values indicate higher priority and execute first.
/// </summary>
public enum JobPriority
{
    /// <summary>Lowest priority - executes last</summary>
    Low = 0,

    /// <summary>Normal priority - default for most jobs</summary>
    Normal = 1,

    /// <summary>High priority - executes before normal priority jobs</summary>
    High = 2,

    /// <summary>Critical priority - executes before all other jobs</summary>
    Critical = 3
}
