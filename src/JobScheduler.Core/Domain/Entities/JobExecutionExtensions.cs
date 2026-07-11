using System;

namespace JobScheduler.Core.Domain.Entities
{
    /// <summary>
    /// Extension methods for <see cref="JobExecution"/> operations.
    /// </summary>
    public static class JobExecutionExtensions
    {
        /// <summary>
        /// Determines whether the job execution is overdue based on its start time and duration.
        /// </summary>
        /// <param name="execution">The job execution to evaluate.</param>
        /// <param name="maxAllowedDurationMinutes">The maximum allowed duration in minutes before considering the job overdue.</param>
        /// <returns><c>true</c> if the job is overdue; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="execution"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="maxAllowedDurationMinutes"/> is non-positive.</exception>
        public static bool IsOverdue(this JobExecution execution, double maxAllowedDurationMinutes = 60)
        {
            ArgumentNullException.ThrowIfNull(execution);
            if (maxAllowedDurationMinutes <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxAllowedDurationMinutes), "Must be positive.");

            var duration = execution.GetExecutionDuration();
            return duration.TotalMinutes > maxAllowedDurationMinutes;
        }

        /// <summary>
        /// Determines whether the job execution should be retried based on retry policy and attempt count.
        /// </summary>
        /// <param name="execution">The job execution to evaluate.</param>
        /// <param name="maxRetryAttempts">The maximum allowed retry attempts.</param>
        /// <returns><c>true</c> if the job should be retried; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="execution"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="maxRetryAttempts"/> is non-positive.</exception>
        public static bool ShouldRetry(this JobExecution execution, int maxRetryAttempts = 3)
        {
            ArgumentNullException.ThrowIfNull(execution);
            if (maxRetryAttempts <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxRetryAttempts), "Must be positive.");

            return execution.IsRetryable && execution.AttemptNumber < maxRetryAttempts;
        }

        /// <summary>
        /// Calculates the execution rate in executions per second for completed jobs.
        /// </summary>
        /// <param name="execution">The job execution to evaluate.</param>
        /// <returns>The execution rate in executions per second, or 0 if the job hasn't completed.</returns>
        public static double GetExecutionRate(this JobExecution execution)
        {
            ArgumentNullException.ThrowIfNull(execution);

            if (execution.CompletedAt == null)
                return 0;

            var duration = execution.GetExecutionDuration().TotalSeconds;
            return duration > 0 ? 1.0 / duration : 0;
        }
    }
}
