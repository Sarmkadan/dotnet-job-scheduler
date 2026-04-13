#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace JobScheduler.Core.Domain.Entities;

/// <summary>
/// A named, ordered sequence of jobs that run in series.
/// Each step in the pipeline must complete successfully before the next step is triggered.
/// Pipelines are built on top of <see cref="JobDependency"/> edges registered via
/// <c>JobDependencyService</c>.
/// </summary>
public sealed class JobPipeline
{
    /// <summary>Gets or sets the unique pipeline identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Gets or sets the human-readable name of this pipeline.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets an optional description of the pipeline's purpose.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets whether this pipeline is active and available for use.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Gets or sets the UTC timestamp when this pipeline was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the UTC timestamp of the last update, if any.</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Gets or sets the identity of the actor who created this pipeline.</summary>
    public string? CreatedBy { get; set; }

    /// <summary>Gets or sets the ordered list of steps that form this pipeline.</summary>
    public List<JobPipelineStep> Steps { get; set; } = new();
}

/// <summary>
/// Represents one step in a <see cref="JobPipeline"/>.
/// The <see cref="StepOrder"/> determines execution position; lower values run first.
/// </summary>
public sealed class JobPipelineStep
{
    /// <summary>Gets or sets the unique step identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Gets or sets the owning pipeline identifier.</summary>
    public Guid PipelineId { get; set; }

    /// <summary>Gets or sets the job executed at this step.</summary>
    public Guid JobId { get; set; }

    /// <summary>Gets or sets the zero-based execution order within the pipeline.</summary>
    public int StepOrder { get; set; }

    /// <summary>
    /// When true the pipeline halts if this step fails.
    /// When false subsequent steps are still attempted even if this one fails.
    /// Defaults to true.
    /// </summary>
    public bool StopOnFailure { get; set; } = true;

    /// <summary>Gets or sets the navigation property for the owning pipeline.</summary>
    public JobPipeline? Pipeline { get; set; }

    /// <summary>Gets or sets the navigation property for the job at this step.</summary>
    public Job? Job { get; set; }
}
