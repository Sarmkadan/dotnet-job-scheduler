#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using JobScheduler.Core.Constants;

namespace JobScheduler.Core.Domain.Models
{
    /// <summary>
    /// Extension methods for <see cref="ExecutionResponse"/>.
    /// </summary>
    public static class ExecutionResponseExtensions
    {
        /// <summary>
        /// Determines whether the execution was successful.
        /// </summary>
        /// <param name="r">The execution response.</param>
        /// <returns>True if the execution status is Success; otherwise, false.</returns>
        public static bool IsSuccessful(this ExecutionResponse r)
        {
            if (r is null)
                throw new ArgumentNullException(nameof(r));

            return r.Status == nameof(ExecutionStatus.Success);
        }

        /// <summary>
        /// Determines whether the execution is in a terminal state (completed, failed, cancelled, timed out, or skipped).
        /// </summary>
        /// <param name="r">The execution response.</param>
        /// <returns>True if the execution is not running; otherwise, false.</returns>
        public static bool IsTerminal(this ExecutionResponse r)
        {
            if (r is null)
                throw new ArgumentNullException(nameof(r));

            return r.Status != nameof(ExecutionStatus.Running);
        }

        /// <summary>
        /// Gets the duration of the execution as a <see cref="TimeSpan"/>.
        /// </summary>
        /// <param name="r">The execution response.</param>
        /// <returns>A <see cref="TimeSpan"/> representing the duration.</returns>
        public static TimeSpan GetDuration(this ExecutionResponse r)
        {
            if (r is null)
                throw new ArgumentNullException(nameof(r));

            return TimeSpan.FromMilliseconds(r.DurationMilliseconds);
        }

        /// <summary>
        /// Determines whether the execution involved a retry attempt.
        /// </summary>
        /// <param name="r">The execution response.</param>
        /// <returns>True if the retry attempt count is greater than zero; otherwise, false.</returns>
        public static bool IsRetry(this ExecutionResponse r)
        {
            if (r is null)
                throw new ArgumentNullException(nameof(r));

            return r.RetryAttempt > 0;
        }

        /// <summary>
        /// Summarizes a sequence of execution responses by status.
        /// </summary>
        /// <param name="responses">The sequence of execution responses.</param>
        /// <returns>A dictionary mapping status strings to their counts.</returns>
        public static Dictionary<string, int> Summarize(this IEnumerable<ExecutionResponse> responses)
        {
            if (responses is null)
                throw new ArgumentNullException(nameof(responses));

            var summary = new Dictionary<string, int>();
            foreach (var response in responses)
            {
                if (response is null)
                    continue;

                if (summary.ContainsKey(response.Status))
                    summary[response.Status]++;
                else
                    summary[response.Status] = 1;
            }

            return summary;
        }
    }
}