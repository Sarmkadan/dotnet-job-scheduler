#nullable enable

namespace JobScheduler.Core.Domain.Models;

/// <summary>
/// Extension methods for <see cref="CreatePipelineRequest"/> to provide common pipeline operations.
/// </summary>
public static class CreatePipelineRequestExtensions
{
    /// <summary>
    /// Validates that the pipeline request is properly configured.
    /// </summary>
    /// <param name="request">The pipeline request to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="request"/> is null</exception>
    public static bool IsValid(this CreatePipelineRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Name))
            return false;

        if (request.Steps == null || request.Steps.Count == 0)
            return false;

        return true;
    }

    /// <summary>
    /// Adds a new step to the pipeline with the specified job ID.
    /// </summary>
    /// <param name="request">The pipeline request</param>
    /// <param name="jobId">The job ID to add as a step</param>
    /// <param name="stopOnFailure">Whether to stop pipeline on failure (default: true)</param>
    /// <returns>The updated pipeline request</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="request"/> is null</exception>
    public static CreatePipelineRequest AddStep(this CreatePipelineRequest request, Guid jobId, bool stopOnFailure = true)
    {
        ArgumentNullException.ThrowIfNull(request);

        request.Steps.Add(new PipelineStepRequest
        {
            JobId = jobId,
            StopOnFailure = stopOnFailure
        });

        return request;
    }

    /// <summary>
    /// Adds multiple steps to the pipeline from a collection of job IDs.
    /// </summary>
    /// <param name="request">The pipeline request</param>
    /// <param name="jobIds">Collection of job IDs to add as steps</param>
    /// <param name="stopOnFailure">Whether to stop pipeline on failure (default: true)</param>
    /// <returns>The updated pipeline request</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="request"/> is null</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="jobIds"/> is null</exception>
    public static CreatePipelineRequest AddSteps(this CreatePipelineRequest request, IEnumerable<Guid> jobIds, bool stopOnFailure = true)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(jobIds);

        foreach (var jobId in jobIds)
        {
            request.Steps.Add(new PipelineStepRequest
            {
                JobId = jobId,
                StopOnFailure = stopOnFailure
            });
        }

        return request;
    }

    /// <summary>
    /// Sets the pipeline description if it's currently null or empty.
    /// </summary>
    /// <param name="request">The pipeline request</param>
    /// <param name="description">The description to set</param>
    /// <returns>The updated pipeline request</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="request"/> is null</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="description"/> is null</exception>
    public static CreatePipelineRequest SetDescriptionIfEmpty(this CreatePipelineRequest request, string description)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(description);

        if (string.IsNullOrEmpty(request.Description))
        {
            request.Description = description;
        }

        return request;
    }

    /// <summary>
    /// Creates a deep copy of the pipeline request.
    /// </summary>
    /// <param name="request">The pipeline request to copy</param>
    /// <returns>A new instance with copied values</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="request"/> is null</exception>
    public static CreatePipelineRequest Clone(this CreatePipelineRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new CreatePipelineRequest
        {
            Name = request.Name,
            Description = request.Description,
            Steps = request.Steps.Select(s => new PipelineStepRequest
            {
                JobId = s.JobId,
                StopOnFailure = s.StopOnFailure
            }).ToList()
        };
    }
}