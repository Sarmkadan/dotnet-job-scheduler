#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace JobScheduler.Core.Controllers;

/// <summary>
/// Provides extension methods for aggregating <see cref="BulkOperationResult"/> collections
/// returned by bulk operations such as <see cref="JobsControllerExtensions.BulkSuspendJobs"/>.
/// </summary>
public static class BulkOperationResultExtensions
{
    /// <summary>
    /// Determines whether every result in the collection reports success.
    /// </summary>
    /// <param name="results">The bulk operation results to inspect.</param>
    /// <returns>
    /// <c>true</c> if all results succeeded or the collection is empty; otherwise <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="results"/> is null.</exception>
    public static bool AllSucceeded(this IEnumerable<BulkOperationResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        return results.All(r => r.Success);
    }

    /// <summary>
    /// Calculates the fraction of results that succeeded.
    /// </summary>
    /// <param name="results">The bulk operation results to inspect.</param>
    /// <returns>
    /// A value between 0 and 1 (e.g. 0.75 for 3 of 4 succeeded). Returns 0 when the collection is empty.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="results"/> is null.</exception>
    public static double SuccessRate(this IEnumerable<BulkOperationResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        var list = results.ToList();
        if (list.Count == 0)
        {
            return 0;
        }

        // WHY: Materialized once so a lazily-evaluated source is not enumerated twice.
        var succeeded = list.Count(r => r.Success);
        return (double)succeeded / list.Count;
    }

    /// <summary>
    /// Returns a message for each failed result. Uses <see cref="BulkOperationResult.ErrorMessage"/>
    /// when present, otherwise a generic message that includes the job ID.
    /// </summary>
    /// <param name="results">The bulk operation results to inspect.</param>
    /// <returns>A sequence of failure messages, one per failed result, in input order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="results"/> is null.</exception>
    public static IEnumerable<string> FailureMessages(this IEnumerable<BulkOperationResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        // WHY: Not an iterator method, so the null check above runs eagerly rather than on first enumeration.
        return results
            .Where(r => !r.Success)
            .Select(r => string.IsNullOrWhiteSpace(r.ErrorMessage)
                ? $"Job {r.JobId} failed"
                : r.ErrorMessage!);
    }

    /// <summary>
    /// Builds a single-line, human-readable summary of the bulk operation outcome,
    /// e.g. <c>"3 of 4 succeeded (75.0%), 1 failed"</c>.
    /// </summary>
    /// <param name="results">The bulk operation results to summarize.</param>
    /// <returns>A summary string formatted with the invariant culture.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="results"/> is null.</exception>
    public static string ToSummary(this IEnumerable<BulkOperationResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        var list = results.ToList();
        if (list.Count == 0)
        {
            return "0 of 0 succeeded (0.0%)";
        }

        var succeeded = list.Count(r => r.Success);
        var failed = list.Count - succeeded;
        var percentage = (double)succeeded / list.Count * 100;

        return FormattableString.Invariant($"{succeeded} of {list.Count} succeeded ({percentage:F1}%), {failed} failed");
    }
}
