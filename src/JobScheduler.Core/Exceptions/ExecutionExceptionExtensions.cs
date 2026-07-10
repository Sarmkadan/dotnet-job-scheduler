using System;
using System.Collections.Generic;
using System.Globalization;

namespace JobScheduler.Core.Exceptions
{
    /// <summary>
    /// Provides extension methods for <see cref="ExecutionException"/>.
    /// </summary>
    public static class ExecutionExceptionExtensions
    {
        /// <summary>
        /// Creates a single-line log message that contains the most important data from the exception.
        /// </summary>
        /// <param name="ex">The exception to format.</param>
        /// <returns>A formatted log string.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="ex"/> is <c>null</c>.</exception>
        public static string ToLogMessage(this ExecutionException ex)
        {
            ArgumentNullException.ThrowIfNull(ex);
            return string.Format(
                CultureInfo.InvariantCulture,
                "ExecutionException: ExecutionId={0}, JobId={1}, AttemptNumber={2}, Message={3}",
                ex.ExecutionId,
                ex.JobId,
                ex.AttemptNumber,
                ex.Message);
        }

        /// <summary>
        /// Determines whether the exception indicates that the job may be retried based on a maximum number of attempts.
        /// </summary>
        /// <param name="ex">The exception to evaluate.</param>
        /// <param name="maxAttempts">The maximum number of allowed attempts (must be non‑negative).</param>
        /// <returns><c>true</c> if <see cref="ExecutionException.AttemptNumber"/> is less than <paramref name="maxAttempts"/>; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="ex"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxAttempts"/> is negative.</exception>
        public static bool IsRetryable(this ExecutionException ex, int maxAttempts)
        {
            ArgumentNullException.ThrowIfNull(ex);
            if (maxAttempts < 0)
                throw new ArgumentOutOfRangeException(nameof(maxAttempts), "Maximum attempts must be non‑negative.");

            return ex.AttemptNumber < maxAttempts;
        }

        /// <summary>
        /// Returns a read‑only dictionary that maps the exception's key properties to their string representations.
        /// </summary>
        /// <param name="ex">The exception to convert.</param>
        /// <returns>An <see cref="IReadOnlyDictionary{TKey,TValue}"/> containing the exception data.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="ex"/> is <c>null</c>.</exception>
        public static IReadOnlyDictionary<string, string> ToDictionary(this ExecutionException ex)
        {
            ArgumentNullException.ThrowIfNull(ex);
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["ExecutionId"] = ex.ExecutionId.ToString(),
                ["JobId"] = ex.JobId.ToString(),
                ["AttemptNumber"] = ex.AttemptNumber.ToString(CultureInfo.InvariantCulture),
                ["Message"] = ex.Message
            };
            return dict;
        }

        /// <summary>
        /// Retrieves the correlation identifiers associated with the exception.
        /// </summary>
        /// <param name="ex">The exception to query.</param>
        /// <returns>A tuple containing <see cref="ExecutionException.ExecutionId"/> and <see cref="ExecutionException.JobId"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="ex"/> is <c>null</c>.</exception>
        public static (Guid ExecutionId, Guid JobId) GetCorrelationInfo(this ExecutionException ex)
        {
            ArgumentNullException.ThrowIfNull(ex);
            return (ex.ExecutionId, ex.JobId);
        }
    }
}
