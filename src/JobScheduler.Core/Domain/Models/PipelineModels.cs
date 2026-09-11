#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace JobScheduler.Core.Domain.Models;

/// <summary>
/// Request model for creating a new job pipeline.
/// </summary>
public sealed class CreatePipelineRequest
{
    /// <summary>Human-readable name for the pipeline (required, max 256 chars).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional description of the pipeline's purpose.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Ordered list of job IDs that form the pipeline.
    /// Jobs are executed in the order provided; each job waits for the previous to succeed.
    /// </summary>
    public List<PipelineStepRequest> Steps { get; set; } = new();

    public override string ToString()
    {
        return $"{GetType().Name} {{ Name = {Name}, Description = {Description}, StepsCount = {Steps.Count} }}";
    }
}

/// <summary>
/// Describes a single step within a <see cref="CreatePipelineRequest"/>.
/// </summary>
public sealed class PipelineStepRequest
{
    /// <summary>The job to execute at this step.</summary>
    public Guid JobId { get; set; }

    /// <summary>
    /// When true the pipeline stops if this step fails.
    /// Defaults to true.
    /// </summary>
    public bool StopOnFailure { get; set; } = true;

    public override string ToString()
    {
        return $"{GetType().Name} {{ JobId = {JobId}, StopOnFailure = {StopOnFailure} }}";
    }
}

/// <summary>
/// API response model representing a pipeline and its steps.
/// </summary>
public sealed class PipelineResponse
{
    /// <summary>Unique identifier of the pipeline.</summary>
    public Guid Id { get; set; }
    /// <summary>Human-readable name for the pipeline.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Optional description of the pipeline's purpose.</summary>
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    /// <summary>Whether the pipeline is currently active and can be executed.</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>Timestamp when the pipeline was created.</summary>
    public string? CreatedBy { get; set; }
    /// <summary>User or system that created the pipeline.</summary>
    public List<PipelineStepResponse> Steps { get; set; } = new();
    /// <summary>Collection of steps in the pipeline.</summary>

    public override string ToString()
    {
        return $"{GetType().Name} {{ Id = {Id}, Name = {Name}, Description = {Description}, IsActive = {IsActive}, CreatedAt = {CreatedAt}, CreatedBy = {CreatedBy}, StepsCount = {Steps.Count} }}";
    }
}

/// <summary>
/// API response model representing one step in a pipeline.
/// </summary>
public sealed class PipelineStepResponse
{
    public Guid StepId { get; set; }
    /// <summary>Unique identifier of the pipeline step.</summary>
    public Guid JobId { get; set; }
    /// <summary>Identifier of the job associated with this step.</summary>
    public string? JobName { get; set; }
    /// <summary>Optional name of the job for display purposes.</summary>
    public int StepOrder { get; set; }
    /// <summary>Zero-based index of this step in the pipeline execution order.</summary>
    public bool StopOnFailure { get; set; }
    /// <summary>Whether the pipeline should stop execution if this step fails.</summary>

    public override string ToString()
    {
        return $"{GetType().Name} {{ StepId = {StepId}, JobId = {JobId}, JobName = {JobName}, StepOrder = {StepOrder}, StopOnFailure = {StopOnFailure} }}";
    }
}

/// <summary>
/// Real-time status of each step in a pipeline run.
/// </summary>
public sealed class PipelineStatusResponse
{
    public Guid PipelineId { get; set; }
    /// <summary>Identifier of the pipeline this status belongs to.</summary>
    public string PipelineName { get; set; } = string.Empty;
    /// <summary>Human-readable name of the pipeline.</summary>
    public List<PipelineStepStatus> StepStatuses { get; set; } = new();
    /// <summary>Collection of statuses for each step in the pipeline.</summary>

    public override string ToString()
    {
        return $"{GetType().Name} {{ PipelineId = {PipelineId}, PipelineName = {PipelineName}, StepStatusesCount = {StepStatuses.Count} }}";
    }
}

/// <summary>
/// Status of a single pipeline step, including the latest execution outcome.
/// </summary>
public sealed class PipelineStepStatus
{
    public int StepOrder { get; set; }
    /// <summary>Zero-based index of this step in the pipeline execution order.</summary>
    public Guid JobId { get; set; }
    /// <summary>Identifier of the job associated with this step.</summary>
    public string? JobName { get; set; }
    /// <summary>Optional name of the job for display purposes.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>Current execution status of the step (e.g., Pending, Running, Completed, Failed).</summary>
    public DateTime? LastExecutedAt { get; set; }
    /// <summary>Timestamp of the last execution of this step, if any.</summary>
    public bool IsReady { get; set; }
    /// <summary>Whether the step is ready to be executed (dependencies satisfied).</summary>

    public override string ToString()
    {
        return $"{GetType().Name} {{ StepOrder = {StepOrder}, JobId = {JobId}, JobName = {JobName}, Status = {Status}, LastExecutedAt = {LastExecutedAt}, IsReady = {IsReady} }}";
    }
}