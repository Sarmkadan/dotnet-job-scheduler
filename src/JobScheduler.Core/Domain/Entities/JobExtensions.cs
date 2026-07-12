using System;
using System.Globalization;

namespace JobScheduler.Core.Domain.Entities;

/// <summary>
/// Provides extension methods for the <see cref="Job"/> entity.
/// </summary>
public static class JobExtensions
{
    /// <summary>
    /// Determines whether the job is active and due for execution at the current UTC time.
    /// </summary>
    /// <param name="job">The job to evaluate.</param>
    /// <returns>
    /// <c>true</c> if the job is active and its <see cref="Job.NextExecutionAt"/> is set and less than or equal to <see cref="DateTime.UtcNow"/>; otherwise <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="job"/> is <c>null</c>.</exception>
    public static bool IsActiveAndDueForExecution(this Job job)
    {
        ArgumentNullException.ThrowIfNull(job);

        return job.IsActive &&
               job.NextExecutionAt.HasValue &&
               job.NextExecutionAt.Value <= DateTime.UtcNow;
    }

    /// <summary>
    /// Calculates the success rate of the job as a value between 0 and 1.
    /// </summary>
    /// <param name="job">The job whose success rate is calculated.</param>
    /// <returns>
    /// The success rate as a <see cref="double"/>. If the job has never been executed, <c>0.0</c> is returned.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="job"/> is <c>null</c>.</exception>
    public static double GetSuccessRate(this Job job)
    {
        ArgumentNullException.ThrowIfNull(job);

        if (job.TotalExecutions == 0)
        {
            return 0.0;
        }

        return (double)job.SuccessfulExecutions / job.TotalExecutions;
    }

    /// <summary>
    /// Retrieves the <see cref="TimeZoneInfo"/> associated with the job.
    /// </summary>
    /// <param name="job">The job whose time zone is retrieved.</param>
    /// <returns>
    /// The <see cref="TimeZoneInfo"/> corresponding to <paramref name="job.TimeZoneId"/>; if <paramref name="job.TimeZoneId"/> is <c>null</c> or empty, <see cref="TimeZoneInfo.Utc"/> is returned.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="job"/> is <c>null</c>.</exception>
    /// <exception cref="TimeZoneNotFoundException">Thrown when the specified time zone ID cannot be found.</exception>
    public static TimeZoneInfo GetTimeZoneInfo(this Job job)
    {
        ArgumentNullException.ThrowIfNull(job);

        var timeZoneId = job.TimeZoneId;
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return TimeZoneInfo.Utc;
        }

        return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
    }

    /// <summary>
    /// Generates a concise summary string for the job, including its name, status, priority, and execution statistics.
    /// </summary>
    /// <param name="job">The job to summarize.</param>
    /// <returns>A formatted string containing key job information.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="job"/> is <c>null</c>.</exception>
    public static string GetSummary(this Job job)
    {
        ArgumentNullException.ThrowIfNull(job);

        return string.Format(
            CultureInfo.InvariantCulture,
            "Job '{0}' [{1}] - Status: {2}, Priority: {3}, Executions: {4} (Success: {5})",
            job.Name,
            job.Id,
            job.Status,
            job.Priority,
            job.TotalExecutions,
            job.SuccessfulExecutions);
    }
}
