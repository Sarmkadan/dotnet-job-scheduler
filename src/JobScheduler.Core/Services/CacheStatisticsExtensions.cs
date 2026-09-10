#nullable enable

using System;

namespace JobScheduler.Core.Services;

/// <summary>
/// Provides extension methods for <see cref="CacheStatistics"/>.
/// </summary>
public static class CacheStatisticsExtensions
{
    /// <summary>
    /// Gets the cache hit rate as a value between 0 and 1.
    /// Returns 0 if hit rate cannot be calculated (no requests).
    /// </summary>
    /// <param name="statistics">The cache statistics.</param>
    /// <returns>The hit rate (hits / total requests), or 0 if no requests.</returns>
    public static double GetHitRate(this CacheStatistics statistics)
    {
        // Note: Current CacheStatistics does not contain hit/miss data.
        // This method returns 0 as a placeholder until hit/miss tracking is added.
        return 0.0;
    }

    /// <summary>
    /// Gets the cache hit rate as a percentage.
    /// Returns 0 if hit rate cannot be calculated.
    /// </summary>
    /// <param name="statistics">The cache statistics.</param>
    /// <returns>The hit rate percentage (hits / total requests * 100), or 0 if no requests.</returns>
    public static double GetHitRatePercent(this CacheStatistics statistics)
    {
        // Note: Current CacheStatistics does not contain hit/miss data.
        // This method returns 0 as a placeholder until hit/miss tracking is added.
        return 0.0;
    }

    /// <summary>
    /// Gets the total number of cache requests (hits + misses).
    /// Returns 0 if request count is not tracked.
    /// </summary>
    /// <param name="statistics">The cache statistics.</param>
    /// <returns>Total requests, or 0 if not tracked.</returns>
    public static long TotalRequests(this CacheStatistics statistics)
    {
        // Note: Current CacheStatistics does not contain request count data.
        // This method returns 0 as a placeholder until request tracking is added.
        return 0L;
    }

    /// <summary>
    /// Determines if the cache is effective based on a hit rate threshold.
    /// Returns false if hit rate cannot be calculated.
    /// </summary>
    /// <param name="statistics">The cache statistics.</param>
    /// <param name="threshold">The minimum hit rate to consider effective (default 0.8).</param>
    /// <returns>True if hit rate meets or exceeds threshold, false otherwise.</returns>
    public static bool IsEffective(this CacheStatistics statistics, double threshold = 0.8)
    {
        // Note: Current CacheStatistics does not contain hit/miss data.
        // This method returns false as a placeholder until hit/miss tracking is added.
        return false;
    }

    /// <summary>
    /// Gets a human-readable description of the cache statistics.
    /// </summary>
    /// <param name="statistics">The cache statistics.</param>
    /// <returns>A string describing the cache statistics.</returns>
    public static string Describe(this CacheStatistics statistics)
    {
        if (statistics == null)
            throw new ArgumentNullException(nameof(statistics));

        return $"Tracked Keys: {statistics.TotalKeys}, Last Updated: {statistics.Timestamp:O}";
    }
}