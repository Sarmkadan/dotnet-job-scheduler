namespace JobScheduler.Core.Exceptions;

public static class CyclicDependencyExceptionExtensions
{
    /// <summary>
    /// Gets a human-readable description of the cyclic dependency.
    /// </summary>
    /// <param name="exception">The cyclic dependency exception.</param>
    /// <returns>A string describing the cyclic dependency.</returns>
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
    public static bool InvolvesJob(this CyclicDependencyException exception, Guid jobId)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.JobId == jobId || exception.DependsOnJobId == jobId;
    }
}
