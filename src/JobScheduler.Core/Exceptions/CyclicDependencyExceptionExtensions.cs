namespace JobScheduler.Core.Exceptions;

/// <summary>
/// Provides extension methods for <see cref="CyclicDependencyException"/> to enhance error handling and diagnostics
/// for cyclic dependency detection in job scheduling scenarios.
/// </summary>
public static class CyclicDependencyExceptionExtensions
{
    /// <summary>
    /// Gets a human-readable description of the cyclic dependency.
    /// </summary>
    /// <param name="exception">The cyclic dependency exception.</param>
    /// <returns>A string describing the cyclic dependency.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="exception"/> is null.</exception>
    public static string GetDescription(this CyclicDependencyException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return $"Job {exception.JobId} has a cyclic dependency on job {exception.DependsOnJobId}.";
    }

    /// <summary>
    /// Determines whether the cyclic dependency involves a specific job.
    /// </summary>
    /// <param name="exception">The cyclic dependency exception.</param>
    /// <param name="jobId">The ID of the job to check.</param>
    /// <returns>True if the cyclic dependency involves the specified job; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="exception"/> is null.</exception>
    public static bool InvolvesJob(this CyclicDependencyException exception, Guid jobId)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.JobId == jobId || exception.DependsOnJobId == jobId;
    }

    /// <summary>
    /// Gets a detailed description of the cyclic dependency including both job IDs and the error code.
    /// </summary>
    /// <param name="exception">The cyclic dependency exception.</param>
    /// <returns>A formatted string containing the dependency details and error code.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="exception"/> is null.</exception>
    public static string FormatDetails(this CyclicDependencyException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.ErrorCode is not null
            ? $"Cyclic dependency detected: Job {exception.JobId} → {exception.DependsOnJobId} (Error Code: {exception.ErrorCode})"
            : $"Cyclic dependency detected: Job {exception.JobId} → {exception.DependsOnJobId}";
    }

    /// <summary>
    /// Determines whether this cyclic dependency exception matches a specific error code.
    /// </summary>
    /// <param name="exception">The cyclic dependency exception.</param>
    /// <param name="errorCode">The error code to compare against.</param>
    /// <returns>True if the exception's error code matches <paramref name="errorCode"/>; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="errorCode"/> is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="exception"/> is null.</exception>
    public static bool IsSpecificError(this CyclicDependencyException exception, string errorCode)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(errorCode);
        ArgumentNullException.ThrowIfNull(exception);

        return string.Equals(exception.ErrorCode, errorCode, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets a summary of the cyclic dependency exception for logging or diagnostic purposes.
    /// </summary>
    /// <param name="exception">The cyclic dependency exception.</param>
    /// <returns>A dictionary containing the exception type, message, error code, and dependency details.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="exception"/> is null.</exception>
    public static IReadOnlyDictionary<string, object> GetSummary(this CyclicDependencyException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return new Dictionary<string, object>
        {
            ["Type"] = exception.GetType().Name,
            ["Message"] = exception.Message,
            ["ErrorCode"] = exception.ErrorCode ?? "N/A",
            ["JobId"] = exception.JobId,
            ["DependsOnJobId"] = exception.DependsOnJobId
        };
    }
}
