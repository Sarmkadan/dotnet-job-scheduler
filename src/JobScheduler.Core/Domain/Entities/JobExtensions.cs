using System;
using System.Globalization;
using JobScheduler.Core.Constants;

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
    /// Determines whether the job has missed its scheduled execution time (misfired).
    /// A job is considered misfired if its NextExecutionAt is in the past by more than the tolerance.
    /// </summary>
    /// <param name="job">The job to evaluate.</param>
    /// <param name="toleranceSeconds">The number of seconds after the scheduled time that is still considered on-time.</param>
    /// <returns>
    /// <c>true</c> if the job is misfired; otherwise <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="job"/> is <c>null</c>.</exception>
    public static bool IsMisfired(this Job job, int toleranceSeconds = 60)
    {
        ArgumentNullException.ThrowIfNull(job);

        if (!job.NextExecutionAt.HasValue || !job.IsActive)
            return false;

        var now = DateTime.UtcNow;
        var scheduledTime = job.NextExecutionAt.Value;

        // If scheduled time is in the future or within tolerance, it's not misfired
        if (scheduledTime > now || (now - scheduledTime).TotalSeconds <= toleranceSeconds)
            return false;

        return true;
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

        return job.TotalExecutions == 0
            ? 0.0
            : (double)job.SuccessfulExecutions / job.TotalExecutions;
    }

    /// <summary>
    /// Retrieves the <see cref="TimeZoneInfo"/> associated with the job.
    /// </summary>
    /// <param name="job">The job whose time zone is retrieved.</param>
    /// <returns>
    /// The <see cref="TimeZoneInfo"/> corresponding to <paramref name="job.TimeZoneId"/>; if <paramref name="job.TimeZoneId"/> is <c>null</c>, empty, or whitespace, <see cref="TimeZoneInfo.Utc"/> is returned.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="job"/> is <c>null</c>.</exception>
    /// <exception cref="TimeZoneNotFoundException">
    /// Thrown when <paramref name="job.TimeZoneId"/> is not <c>null</c> or whitespace but does not correspond to a valid system time zone ID.
    /// </exception>
    public static TimeZoneInfo GetTimeZoneInfo(this Job job) =>
        string.IsNullOrWhiteSpace(job.TimeZoneId)
            ? TimeZoneInfo.Utc
            : TimeZoneInfo.FindSystemTimeZoneById(job.TimeZoneId);

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
            "Job '{0}' [{1}] - Status: {2}, Priority: {3}, Executions: {4} (Success: {5}) ({6:P0})",
            job.Name,
            job.Id,
            job.Status,
            job.Priority,
            job.TotalExecutions,
            job.SuccessfulExecutions,
            job.GetSuccessRate());
    }
}
