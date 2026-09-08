#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace JobScheduler.Core.Constants;

/// <summary>
/// Extension methods for <see cref="MisfirePolicy"/>.
/// </summary>
public static class MisfirePolicyExtensions
{
    /// <summary>
    /// Gets a human-readable description of the misfire policy.
    /// </summary>
    /// <param name="policy">The misfire policy.</param>
    /// <returns>A description string.</returns>
    public static string GetDescription(this MisfirePolicy policy)
    {
        return policy switch
        {
            MisfirePolicy.FireOnceNow => "Fire the job once immediately when the scheduler restarts after a misfire.",
            MisfirePolicy.SkipToNext => "Skip the missed execution and schedule the next execution based on the cron expression.",
            MisfirePolicy.FireAll => "Fire all missed executions immediately when the scheduler restarts.",
            _ => policy.ToString()
        };
    }
}