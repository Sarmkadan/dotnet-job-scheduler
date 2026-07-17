using System;
using System.Collections.Generic;

namespace JobScheduler.Core.Exceptions
{
    /// <summary>
    /// Provides extension methods for <see cref="JobSchedulerException"/> to enhance error handling and diagnostics.
    /// </summary>
    public static class JobSchedulerExceptionExtensions
    {
        /// <summary>
        /// Formats the exception details into a human-readable string.
        /// </summary>
        /// <param name="exception">The exception to format.</param>
        /// <returns>A formatted string containing the exception message and error code.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="exception"/> is null.</exception>
        public static string FormatDetails(this JobSchedulerException exception)
        {
            ArgumentNullException.ThrowIfNull(exception);

            return exception.ErrorCode is not null
                ? $"{exception.Message} (Error Code: {exception.ErrorCode})"
                : exception.Message;
        }

        /// <summary>
        /// Determines whether the exception matches a specific error code.
        /// </summary>
        /// <param name="exception">The exception to check.</param>
        /// <param name="errorCode">The error code to compare against.</param>
        /// <returns>True if the exception's error code matches <paramref name="errorCode"/>; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="errorCode"/> is null or empty.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="exception"/> is null.</exception>
        public static bool IsSpecificError(this JobSchedulerException exception, string errorCode)
        {
            ArgumentNullException.ThrowIfNullOrEmpty(errorCode);
            ArgumentNullException.ThrowIfNull(exception);

            return string.Equals(exception.ErrorCode, errorCode, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets a summary of the exception for logging or diagnostic purposes.
        /// </summary>
        /// <param name="exception">The exception to summarize.</param>
        /// <returns>A dictionary containing the exception type, message, and error code.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="exception"/> is null.</exception>
        public static IReadOnlyDictionary<string, object> GetSummary(this JobSchedulerException exception)
        {
            ArgumentNullException.ThrowIfNull(exception);

            return new Dictionary<string, object>
            {
                ["Type"] = exception.GetType().Name,
                ["Message"] = exception.Message,
                ["ErrorCode"] = exception.ErrorCode ?? "N/A"
            };
        }
    }
}