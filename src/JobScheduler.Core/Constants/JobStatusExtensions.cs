#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace JobScheduler.Core.Constants;

/// <summary>
/// Extension methods for <see cref="JobStatus"/>.
/// </summary>
public static class JobStatusExtensions
{
    /// <summary>
    /// Determines whether the specified job status is a final status (i.e., no further transitions are expected).
    /// </summary>
    /// <param name="status">The job status to evaluate.</param>
    /// <returns>
    /// <c>true</c> if the status is Completed, Cancelled, or FailedPermanently; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsFinal(this JobStatus status)
    {
        return status switch
        {
            JobStatus.Completed => true,
            JobStatus.Cancelled => true,
            JobStatus.FailedPermanently => true,
            _ => false
        };
    }
}