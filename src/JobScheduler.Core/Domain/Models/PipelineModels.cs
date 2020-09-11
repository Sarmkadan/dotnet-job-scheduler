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
}

/// <summary>
/// API response model representing a pipeline and its steps.
/// </summary>
public sealed class PipelineResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public List<PipelineStepResponse> Steps { get; set; } = new();
}

/// <summary>
/// API response model representing one step in a pipeline.
/// </summary>
public sealed class PipelineStepResponse
{
    public Guid StepId { get; set; }
    public Guid JobId { get; set; }
    public string? JobName { get; set; }
    public int StepOrder { get; set; }
    public bool StopOnFailure { get; set; }
}

/// <summary>
/// Real-time status of each step in a pipeline run.
/// </summary>
public sealed class PipelineStatusResponse
{
    public Guid PipelineId { get; set; }
    public string PipelineName { get; set; } = string.Empty;
    public List<PipelineStepStatus> StepStatuses { get; set; } = new();
}

/// <summary>
/// Status of a single pipeline step, including the latest execution outcome.
/// </summary>
public sealed class PipelineStepStatus
{
    public int StepOrder { get; set; }
    public Guid JobId { get; set; }
    public string? JobName { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? LastExecutedAt { get; set; }
    public bool IsReady { get; set; }
}
