#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace JobScheduler.Core.Constants;

/// <summary>
/// Extension methods for <see cref="ExecutionStatus"/>.
/// </summary>
public static class ExecutionStatusExtensions
{
    /// <summary>
    /// Determines whether the specified execution status represents a completed execution.
    /// </summary>
    /// <param name="status">The execution status to evaluate.</param>
    /// <returns>
    /// <c>true</c> if the status is Success, Failed, Cancelled, TimedOut, or Skipped; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsTerminal(this ExecutionStatus status)
    {
        return status switch
        {
            ExecutionStatus.Success => true,
            ExecutionStatus.Failed => true,
            ExecutionStatus.Cancelled => true,
            ExecutionStatus.TimedOut => true,
            ExecutionStatus.Skipped => true,
            _ => false
        };
    }
}
