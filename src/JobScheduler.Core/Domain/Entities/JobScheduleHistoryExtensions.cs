#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace JobScheduler.Core.Domain.Entities
{
    /// <summary>
    /// Extension methods for <see cref="JobScheduleHistory"/>.
    /// </summary>
    public static class JobScheduleHistoryExtensions
    {
        /// <summary>
        /// Determines whether the history entry represents an actual value change (old value differs from new value).
        /// </summary>
        /// <param name="h">The job schedule history instance.</param>
        /// <returns><see langword="true"/> if the old and new values are different; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="h"/> is <see langword="null"/>.</exception>
        public static bool HasValueChanged(this JobScheduleHistory h)
        {
            ArgumentNullException.ThrowIfNull(h);

            // Handle null comparisons: if both are null, they are equal; if only one is null, they are different.
            if (h.OldValue == null && h.NewValue == null)
                return false;
            if (h.OldValue == null || h.NewValue == null)
                return true;
            return !h.OldValue.Equals(h.NewValue);
        }

        /// <summary>
        /// Gets a description of the change in the format: 'PropertyName: old -> new'.
        /// </summary>
        /// <param name="h">The job schedule history instance.</param>
        /// <returns>A string describing the change.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="h"/> is <see langword="null"/>.</exception>
        public static string Describe(this JobScheduleHistory h)
        {
            ArgumentNullException.ThrowIfNull(h);

            string oldValue = h.OldValue ?? "(null)";
            string newValue = h.NewValue ?? "(null)";
            return $"{h.PropertyName}: {oldValue} -> {newValue}";
        }

        /// <summary>
        /// Filters a sequence of job schedule history entries by the specified property name.
        /// </summary>
        /// <param name="source">The sequence to filter.</param>
        /// <param name="propertyName">The property name to filter by.</param>
        /// <returns>An sequence containing entries where <see cref="JobScheduleHistory.PropertyName"/> matches <paramref name="propertyName"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="propertyName"/> is <see langword="null"/>.</exception>
        public static IEnumerable<JobScheduleHistory> FilterByProperty(this IEnumerable<JobScheduleHistory> source, string propertyName)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(propertyName);

            return source.Where(h => h.PropertyName == propertyName);
        }

        /// <summary>
        /// Returns the most recent job schedule history entry based on the <see cref="JobScheduleHistory.ChangedAt"/> timestamp.
        /// </summary>
        /// <param name="source">The sequence to search.</param>
        /// <returns>The most recent history entry, or <see langword="null"/> if the sequence is empty.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        public static JobScheduleHistory? Latest(this IEnumerable<JobScheduleHistory> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source.OrderByDescending(h => h.ChangedAt).FirstOrDefault();
        }

        /// <summary>
        /// Groups a sequence of job schedule history entries by job identifier.
        /// </summary>
        /// <param name="source">The sequence to group.</param>
        /// <returns>An <see cref="ILookup{TKey,TElement}"/> where each key is a <see cref="JobScheduleHistory.JobId"/> and each element is a history entry for that job.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        public static ILookup<Guid, JobScheduleHistory> GroupByJob(this IEnumerable<JobScheduleHistory> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source.ToLookup(h => h.JobId);
        }
    }
}