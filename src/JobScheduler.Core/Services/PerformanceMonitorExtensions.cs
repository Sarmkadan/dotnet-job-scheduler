#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace JobScheduler.Core.Services
{
    /// <summary>
    /// Extension methods for performance monitoring types.
    /// </summary>
    public static class PerformanceMonitorExtensions
    {
        /// <summary>
        /// Determines if the average execution time is slower than the specified threshold.
        /// </summary>
        /// <param name="s">The metrics summary to evaluate.</param>
        /// <param name="threshold">The time threshold to compare against.</param>
        /// <returns>True if the average execution time is greater than the threshold; otherwise, false.</returns>
        public static bool IsSlowerThan(this MetricsSummary s, TimeSpan threshold)
        {
            if (s == null) throw new ArgumentNullException(nameof(s));
            if (threshold == null) throw new ArgumentNullException(nameof(threshold));

            return s.AverageExecutionTimeMs > threshold.TotalMilliseconds;
        }

        /// <summary>
        /// Gets the success rate as a percentage.
        /// </summary>
        /// <param="s">The metrics summary to evaluate.</param>
        /// <returns>The success rate percentage (0-100).</returns>
        public static double GetSuccessRatePercent(this MetricsSummary s)
        {
            if (s == null) throw new ArgumentNullException(nameof(s));
            return s.SuccessRate;
        }

        /// <summary>
        /// Gets the slowest performance metrics by execution time.
        /// </summary>
        /// <param="source">The sequence of performance metrics.</param>
        /// <param="count">The number of slowest metrics to return.</param>
        /// <returns>A sequence containing the specified number of slowest performance metrics, ordered from slowest to fastest.</returns>
        public static IEnumerable<PerformanceMetric> Slowest(this IEnumerable<PerformanceMetric> source, int count)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));

            return source
                .OrderByDescending(m => m.ExecutionTimeMs)
                .Take(count);
        }

        /// <summary>
        /// Calculates the average duration from a sequence of performance timeline points.
        /// </summary>
        /// <param="source">The sequence of performance timeline points.</param>
        /// <returns>The average execution time in milliseconds across all timeline points.</returns>
        public static long AverageDuration(this IEnumerable<PerformanceTimelinePoint> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var points = source.ToList();
            if (!points.Any())
                return 0;

            return (long)points.Average(p => p.AverageExecutionTimeMs);
        }

        /// <summary>
        /// Gets a descriptive string representation of the metrics summary.
        /// </summary>
        /// <param="s">The metrics summary to describe.</param>
        /// <returns>A formatted string containing key metrics.</returns>
        public static string Describe(this MetricsSummary s)
        {
            if (s == null) throw new ArgumentNullException(nameof(s));

            return $"Total Executions: {s.TotalExecutions}, " +
                   $"Successful: {s.SuccessfulExecutions}, " +
                   $"Failed: {s.FailedExecutions}, " +
                   $"Success Rate: {s.SuccessRate:F1}%, " +
                   $"Avg Duration: {s.AverageExecutionTimeMs}ms, " +
                   $"Min Duration: {s.MinExecutionTimeMs}ms, " +
                   $"Max Duration: {s.MaxExecutionTimeMs}ms, " +
                   $"Memory Usage: {s.MemoryUsageMb}MB";
        }
    }
}