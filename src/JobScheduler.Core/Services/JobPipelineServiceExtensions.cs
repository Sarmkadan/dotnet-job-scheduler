#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using JobScheduler.Core.Data;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace JobScheduler.Core.Services;

/// <summary>
/// Extension methods for <see cref="JobPipelineService"/> providing additional
/// convenience methods for pipeline management and querying.
/// </summary>
public static class JobPipelineServiceExtensions
{
    /// <summary>
    /// Checks if a pipeline with the specified identifier exists.
    /// </summary>
    /// <param name="service">The pipeline service instance.</param>
    /// <param name="id">Pipeline identifier to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the pipeline exists; otherwise, <c>false</c>.</returns>
    public static async Task<bool> ExistsAsync(
        this JobPipelineService service,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (service is null)
            throw new ArgumentNullException(nameof(service));

        return await service.GetPipelineAsync(id, cancellationToken) is not null;
    }

    /// <summary>
    /// Gets a pipeline by name (case-insensitive).
    /// </summary>
    /// <param name="service">The pipeline service instance.</param>
    /// <param name="name">Pipeline name to search for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The pipeline if found; otherwise, <c>null</c>.</returns>
    public static async Task<JobPipeline?> GetPipelineByNameAsync(
        this JobPipelineService service,
        string name,
        CancellationToken cancellationToken = default)
    {
        if (service is null)
            throw new ArgumentNullException(nameof(service));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Pipeline name cannot be null or empty.", nameof(name));

        return await service.GetAllPipelinesAsync(cancellationToken)
            .ContinueWith(t => t.Result.FirstOrDefault(p =>
                p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)),
                cancellationToken);
    }

    /// <summary>
    /// Gets all active pipelines (where IsActive = true).
    /// </summary>
    /// <param name="service">The pipeline service instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Read-only list of active pipelines.</returns>
    public static async Task<IReadOnlyList<JobPipeline>> GetActivePipelinesAsync(
        this JobPipelineService service,
        CancellationToken cancellationToken = default)
    {
        if (service is null)
            throw new ArgumentNullException(nameof(service));

        var allPipelines = await service.GetAllPipelinesAsync(cancellationToken);
        return allPipelines.Where(p => p.IsActive).ToList().AsReadOnly();
    }

    /// <summary>
    /// Gets the status response for a pipeline and includes execution statistics
    /// for each step, showing success/failure counts and average execution times.
    /// </summary>
    /// <param name="service">The pipeline service instance.</param>
    /// <param name="id">Pipeline identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Pipeline status response with execution statistics; or <c>null</c> if pipeline not found.</returns>
    public static async Task<PipelineStatusWithStatsResponse?> GetPipelineStatusWithStatsAsync(
        this JobPipelineService service,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (service is null)
            throw new ArgumentNullException(nameof(service));

        var statusResponse = await service.GetPipelineStatusAsync(id, cancellationToken);
        if (statusResponse is null)
            return null;

        var pipeline = await service.GetPipelineAsync(id, cancellationToken);
        if (pipeline is null)
            return null;

        var executionStats = new List<PipelineStepExecutionStats>();

        foreach (var step in statusResponse.StepStatuses)
        {
            var jobExecutions = await service.GetDbContext()
                .JobExecutions
                .Where(e => e.JobId == step.JobId)
                .OrderByDescending(e => e.StartedAt)
                .Take(100)
                .ToListAsync(cancellationToken);

            var successCount = jobExecutions.Count(e => e.Status == Constants.ExecutionStatus.Success);
            var failureCount = jobExecutions.Count(e => e.Status == Constants.ExecutionStatus.Failed);
            var avgDuration = jobExecutions.Any()
                ? TimeSpan.FromMilliseconds(jobExecutions.Average(e => e.DurationMilliseconds))
                : TimeSpan.Zero;

            executionStats.Add(new PipelineStepExecutionStats
            {
                StepOrder = step.StepOrder,
                JobId = step.JobId,
                JobName = step.JobName,
                Status = step.Status,
                LastExecutedAt = step.LastExecutedAt,
                IsReady = step.IsReady,
                SuccessCount = successCount,
                FailureCount = failureCount,
                AverageDuration = avgDuration,
                TotalExecutions = jobExecutions.Count
            });
        }

        return new PipelineStatusWithStatsResponse
        {
            PipelineId = statusResponse.PipelineId,
            PipelineName = statusResponse.PipelineName,
            StepStatuses = statusResponse.StepStatuses,
            ExecutionStats = executionStats
        };
    }

    /// <summary>
    /// Gets the underlying DbContext used by the service.
    /// Useful for advanced queries and operations beyond the service's API.
    /// </summary>
    /// <param name="service">The pipeline service instance.</param>
    /// <returns>The Entity Framework DbContext.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the service's context is not available.</exception>
    public static JobSchedulerContext GetDbContext(this JobPipelineService service)
    {
        if (service is null)
            throw new ArgumentNullException(nameof(service));

        var contextField = typeof(JobPipelineService).GetField(
            "_context",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (contextField?.GetValue(service) is not JobSchedulerContext context)
            throw new InvalidOperationException("Could not access the service's DbContext.");

        return context;
    }
}

/// <summary>
/// Response model that includes execution statistics alongside pipeline status.
/// </summary>
public sealed class PipelineStatusWithStatsResponse
{
    /// <summary>Pipeline identifier.</summary>
    public Guid PipelineId { get; set; }

    /// <summary>Pipeline name.</summary>
    public string PipelineName { get; set; } = string.Empty;

    /// <summary>Status of each pipeline step.</summary>
    public List<PipelineStepStatus> StepStatuses { get; set; } = new();

    /// <summary>Execution statistics for each pipeline step.</summary>
    public List<PipelineStepExecutionStats> ExecutionStats { get; set; } = new();
}

/// <summary>
/// Execution statistics for a single pipeline step.
/// </summary>
public sealed class PipelineStepExecutionStats
{
    /// <summary>Step order in the pipeline.</summary>
    public int StepOrder { get; set; }

    /// <summary>Job identifier for this step.</summary>
    public Guid JobId { get; set; }

    /// <summary>Job name for this step.</summary>
    public string? JobName { get; set; }

    /// <summary>Current execution status.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>When the job was last executed.</summary>
    public DateTime? LastExecutedAt { get; set; }

    /// <summary>Whether this step is ready to execute (previous step succeeded).</summary>
    public bool IsReady { get; set; }

    /// <summary>Number of successful executions.</summary>
    public int SuccessCount { get; set; }

    /// <summary>Number of failed executions.</summary>
    public int FailureCount { get; set; }

    /// <summary>Average duration of executions.</summary>
    public TimeSpan AverageDuration { get; set; }

    /// <summary>Total number of executions tracked.</summary>
    public int TotalExecutions { get; set; }
}