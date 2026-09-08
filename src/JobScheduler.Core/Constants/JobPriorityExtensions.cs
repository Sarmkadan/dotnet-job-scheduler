#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace JobScheduler.Core.Constants;

/// <summary>
/// Extension methods for <see cref="JobPriority"/>.
/// </summary>
public static class JobPriorityExtensions
{
    /// <summary>
    /// Gets a human-readable display name of the job priority.
    /// </summary>
    /// <param name="priority">The job priority.</param>
    /// <returns>A display name string.</returns>
    public static string ToDisplayName(this JobPriority priority)
    {
        return priority switch
        {
            JobPriority.Low => "Low",
            JobPriority.Normal => "Normal",
            JobPriority.High => "High",
            JobPriority.Critical => "Critical",
            _ => priority.ToString()
        };
    }
}