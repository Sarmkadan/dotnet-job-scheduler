#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using JobScheduler.Core.Data;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Domain.Models;
using JobScheduler.Core.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobScheduler.Core.Services;

/// <summary>
/// Manages job pipelines — ordered chains of jobs where each step is triggered
/// only after the previous step succeeds.  Pipeline step ordering is enforced
/// through <see cref="JobDependency"/> edges in the dependency graph.
/// </summary>
public sealed class JobPipelineService
{
    private readonly JobSchedulerContext _context;
    private readonly IJobDependencyService _dependencyService;
    private readonly ILogger<JobPipelineService>? _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="JobPipelineService"/>.
    /// </summary>
    public JobPipelineService(
        JobSchedulerContext context,
        IJobDependencyService dependencyService,
        ILogger<JobPipelineService>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dependencyService = dependencyService ?? throw new ArgumentNullException(nameof(dependencyService));
        _logger = logger;
    }

    /// <summary>
    /// Creates a new pipeline from the supplied request.
    /// Validates that all referenced jobs exist and registers sequential dependency
    /// edges so that step N waits for step N-1 to complete.
    /// </summary>
    /// <param name="request">Pipeline creation parameters.</param>
    /// <param name="createdBy">Optional identity of the actor creating this pipeline.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ArgumentException">Thrown when the request has fewer than 2 steps or an empty name.</exception>
    /// <exception cref="JobNotFoundException">Thrown when any referenced job does not exist.</exception>
    public async Task<JobPipeline> CreatePipelineAsync(
        CreatePipelineRequest request,
        string? createdBy = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Pipeline name is required.", nameof(request));

        if (request.Steps.Count < 2)
            throw new ArgumentException("A pipeline must have at least 2 steps.", nameof(request));

        // Verify all jobs exist
        foreach (var step in request.Steps)
        {
            var exists = await _context.Jobs.AnyAsync(j => j.Id == step.JobId, cancellationToken);
            if (!exists)
                throw new JobNotFoundException(step.JobId);
        }

        var pipeline = new JobPipeline
        {
            Name = request.Name,
            Description = request.Description,
            CreatedBy = createdBy
        };

        for (var i = 0; i < request.Steps.Count; i++)
        {
            pipeline.Steps.Add(new JobPipelineStep
            {
                JobId = request.Steps[i].JobId,
                StepOrder = i,
                StopOnFailure = request.Steps[i].StopOnFailure,
                Pipeline = pipeline
            });
        }

        _context.Set<JobPipeline>().Add(pipeline);
        await _context.SaveChangesAsync(cancellationToken);

        // Register sequential dependency edges (step[i] depends on step[i-1])
        for (var i = 1; i < request.Steps.Count; i++)
        {
            await _dependencyService.AddDependencyAsync(
                request.Steps[i].JobId,
                request.Steps[i - 1].JobId,
                createdBy,
                cancellationToken);
        }

        _logger?.LogInformation(
            "Pipeline '{PipelineName}' ({PipelineId}) created with {StepCount} steps.",
            pipeline.Name, pipeline.Id, pipeline.Steps.Count);

        // Validate the pipeline's dependency graph to catch any cycles immediately
        await pipeline.ValidateAsync(_dependencyService, cancellationToken);

        return pipeline;
    }

    /// <summary>
    /// Returns a pipeline by identifier, including all steps.
    /// </summary>
    /// <param name="id">Pipeline identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<JobPipeline?> GetPipelineAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<JobPipeline>()
            .Include(p => p.Steps)
            .ThenInclude(s => s.Job)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    /// <summary>
    /// Returns all pipelines, including their steps, ordered by creation date descending.
    /// </summary>
    public async Task<IReadOnlyList<JobPipeline>> GetAllPipelinesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<JobPipeline>()
            .Include(p => p.Steps)
            .ThenInclude(s => s.Job)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Deletes a pipeline and removes the sequential dependency edges it introduced.
    /// Individual jobs are not deleted.
    /// </summary>
    /// <param name="id">Pipeline identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the pipeline existed and was deleted; <c>false</c> otherwise.</returns>
    public async Task<bool> DeletePipelineAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var pipeline = await _context.Set<JobPipeline>()
            .Include(p => p.Steps)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (pipeline is null)
            return false;

        var orderedSteps = pipeline.Steps.OrderBy(s => s.StepOrder).ToList();
        for (var i = 1; i < orderedSteps.Count; i++)
        {
            await _dependencyService.RemoveDependencyAsync(
                orderedSteps[i].JobId,
                orderedSteps[i - 1].JobId,
                cancellationToken);
        }

        _context.Set<JobPipeline>().Remove(pipeline);
        await _context.SaveChangesAsync(cancellationToken);

        _logger?.LogInformation("Pipeline '{PipelineName}' ({PipelineId}) deleted.", pipeline.Name, pipeline.Id);

        return true;
    }

    /// <summary>
    /// Returns the current status of each step in a pipeline by examining the latest
    /// execution record for each step's job.
    /// </summary>
    /// <param name="id">Pipeline identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<PipelineStatusResponse?> GetPipelineStatusAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var pipeline = await _context.Set<JobPipeline>()
            .Include(p => p.Steps)
            .ThenInclude(s => s.Job)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (pipeline is null)
            return null;

        var orderedSteps = pipeline.Steps.OrderBy(s => s.StepOrder).ToList();
        var stepStatuses = new List<PipelineStepStatus>();
        var previousStepSucceeded = true;

        foreach (var step in orderedSteps)
        {
            var latestExecution = await _context.JobExecutions
                .Where(e => e.JobId == step.JobId)
                .OrderByDescending(e => e.StartedAt)
                .FirstOrDefaultAsync(cancellationToken);

            var statusText = latestExecution is null ? "NotStarted" : latestExecution.Status.ToString();

            stepStatuses.Add(new PipelineStepStatus
            {
                StepOrder = step.StepOrder,
                JobId = step.JobId,
                JobName = step.Job?.Name,
                Status = statusText,
                LastExecutedAt = latestExecution?.StartedAt,
                IsReady = previousStepSucceeded
            });

            previousStepSucceeded = latestExecution?.Status == Constants.ExecutionStatus.Success;
        }

        return new PipelineStatusResponse
        {
            PipelineId = pipeline.Id,
            PipelineName = pipeline.Name,
            StepStatuses = stepStatuses
        };
    }

    /// <summary>
    /// Maps a <see cref="JobPipeline"/> entity to a <see cref="PipelineResponse"/> API model.
    /// </summary>
    public static PipelineResponse MapToResponse(JobPipeline pipeline) =>
        new()
        {
            Id = pipeline.Id,
            Name = pipeline.Name,
            Description = pipeline.Description,
            IsActive = pipeline.IsActive,
            CreatedAt = pipeline.CreatedAt,
            CreatedBy = pipeline.CreatedBy,
            Steps = pipeline.Steps
                .OrderBy(s => s.StepOrder)
                .Select(s => new PipelineStepResponse
                {
                    StepId = s.Id,
                    JobId = s.JobId,
                    JobName = s.Job?.Name,
                    StepOrder = s.StepOrder,
                    StopOnFailure = s.StopOnFailure
                })
                .ToList()
        };
}
