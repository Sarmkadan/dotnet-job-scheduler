namespace JobScheduler.Core.Domain.Entities;

/// <summary>
/// Provides extension methods for <see cref="JobDependency"/>.
/// </summary>
public static class JobDependencyExtensions
{
    /// <summary>
    /// Determines whether the <paramref name="dependency"/> depends on the specified <paramref name="jobId"/>.
    /// </summary>
    /// <param name="dependency">The job dependency.</param>
    /// <param name="jobId">The job ID to check.</param>
    /// <returns><c>true</c> if the dependency depends on the specified job; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="dependency"/> is <c>null</c>.</exception>
    public static bool DependsOnThisJob(this JobDependency dependency, Guid jobId)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        return dependency.DependsOnJobId == jobId;
    }

    /// <summary>
    /// Determines whether the <paramref name="dependency"/> is a dependency of the specified <paramref name="jobId"/>.
    /// </summary>
    /// <param name="dependency">The job dependency.</param>
    /// <param name="jobId">The job ID to check.</param>
    /// <returns><c>true</c> if the dependency is a dependency of the specified job; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="dependency"/> is <c>null</c>.</exception>
    public static bool IsDependencyOfThisJob(this JobDependency dependency, Guid jobId)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        return dependency.JobId == jobId;
    }
}