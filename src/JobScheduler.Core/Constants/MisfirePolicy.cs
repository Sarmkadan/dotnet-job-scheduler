#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace JobScheduler.Core.Constants;

/// <summary>
/// Defines the policy for handling misfired jobs (jobs that were scheduled to run
/// but the scheduler was not running at the scheduled time).
/// </summary>
public enum MisfirePolicy
{
    /// <summary>
    /// Fire the job once immediately when the scheduler restarts after a misfire.
    /// This can cause a "thundering herd" problem if many jobs have missed their
    /// execution windows.
    /// </summary>
    FireOnceNow = 0,

    /// <summary>
    /// Skip the missed execution and schedule the next execution based on the
    /// cron expression. This is the safest default for recurring jobs.
    /// </summary>
    SkipToNext = 1,

    /// <summary>
    /// Fire all missed executions immediately when the scheduler restarts.
    /// This can cause performance issues if many executions were missed.
    /// </summary>
    FireAll = 2
}
