using System;

namespace JobScheduler.Core.Exceptions
{
    /// <summary>
    /// Provides extension methods for <see cref="JobNotFoundException"/> to enhance error handling scenarios.
    /// </summary>
    public static class JobNotFoundExceptionExtensions
    {
        /// <summary>
        /// Determines whether the exception represents a specific job ID that was not found.
        /// </summary>
        /// <param name="exception">The exception instance.</param>
        /// <param name="jobId">The job ID to check against.</param>
        /// <returns>True if the exception's JobId matches the provided job ID; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/>.</exception>
        public static bool IsForJob(this JobNotFoundException exception, Guid jobId)
        {
            ArgumentNullException.ThrowIfNull(exception);
            return exception.JobId == jobId;
        }

        /// <summary>
        /// Creates a new exception instance with the same job ID but a different message.
        /// </summary>
        /// <param name="exception">The original exception.</param>
        /// <param name="newMessage">The new error message.</param>
        /// <returns>A new JobNotFoundException with the same JobId but updated message.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="exception"/> or <paramref name="newMessage"/> is <see langword="null"/>.</exception>
        public static JobNotFoundException WithMessage(this JobNotFoundException exception, string newMessage)
        {
            ArgumentNullException.ThrowIfNull(exception);
            ArgumentNullException.ThrowIfNull(newMessage);

            return new JobNotFoundException(exception.JobId)
            {
                Source = exception.Source,
                HelpLink = exception.HelpLink,
                HResult = exception.HResult
            };
        }

        /// <summary>
        /// Creates a new exception instance with the same message but a different job ID.
        /// </summary>
        /// <param name="exception">The original exception.</param>
        /// <param name="newJobId">The new job ID.</param>
        /// <returns>A new JobNotFoundException with the same message but updated JobId.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/>.</exception>
        public static JobNotFoundException WithJobId(this JobNotFoundException exception, Guid newJobId)
        {
            ArgumentNullException.ThrowIfNull(exception);

            return new JobNotFoundException(newJobId);
        }

        /// <summary>
        /// Safely extracts the JobId from the exception, returning a boolean indicating success.
        /// </summary>
        /// <param name="exception">The exception instance.</param>
        /// <param name="jobId">Output parameter for the job ID if found.</param>
        /// <returns>True if the JobId was successfully retrieved and is not empty; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/>.</exception>
        public static bool TryGetJobId(this JobNotFoundException exception, out Guid jobId)
        {
            ArgumentNullException.ThrowIfNull(exception);
            jobId = exception.JobId;
            return jobId != Guid.Empty;
        }
    }
}