namespace JobScheduler.Core.Domain.Entities;

/// <summary>
/// Provides extension methods for <see cref="JobPipeline"/>.
/// </summary>
public static class JobPipelineExtensions
{
    /// <summary>
    /// Determines whether a job pipeline is active and has at least one step.
    /// </summary>
    /// <param name="pipeline">The job pipeline to check.</param>
    /// <returns><c>true</c> if the pipeline is active and has at least one step; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="pipeline"/> is <c>null</c>.</exception>
    public static bool IsValidForExecution(this JobPipeline pipeline)
    {
        ArgumentNullException.ThrowIfNull(pipeline);

        return pipeline.IsActive && pipeline.Steps.Count > 0;
    }

    /// <summary>
    /// Gets a read-only list of steps in the job pipeline.
    /// </summary>
    /// <param name="pipeline">The job pipeline.</param>
    /// <returns>A read-only list of steps in the pipeline.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="pipeline"/> is <c>null</c>.</exception>
    public static IReadOnlyList<JobPipelineStep> GetSteps(this JobPipeline pipeline)
    {
        ArgumentNullException.ThrowIfNull(pipeline);

        return pipeline.Steps.AsReadOnly();
    }

    /// <summary>
    /// Determines whether a job pipeline has any steps with <see cref="JobPipelineStep.StopOnFailure"/> set to <c>true</c>.
    /// </summary>
    /// <param name="pipeline">The job pipeline to check.</param>
    /// <returns><c>true</c> if the pipeline has at least one step with <see cref="JobPipelineStep.StopOnFailure"/> set to <c>true</c>; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="pipeline"/> is <c>null</c>.</exception>
    public static bool HasStopOnFailureStep(this JobPipeline pipeline)
    {
        ArgumentNullException.ThrowIfNull(pipeline);

        return pipeline.Steps.Any(s => s.StopOnFailure);
    }
}
