#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace JobScheduler.Core.Domain.Entities;

/// <summary>
/// Represents a directed dependency edge between two jobs in the execution graph.
/// A record (JobId=A, DependsOnJobId=B) encodes "A must not start until B has completed successfully",
/// forming one edge of the broader directed acyclic graph (DAG).
/// </summary>
public class JobDependency
{
    /// <summary>Gets or sets the unique identifier of this dependency record.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the ID of the dependent job — the one that waits for its prerequisite.
    /// </summary>
    public Guid JobId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the prerequisite job — the one that must complete first.
    /// </summary>
    public Guid DependsOnJobId { get; set; }

    /// <summary>Gets or sets the UTC timestamp when this dependency was registered.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the identity of the actor who created this dependency.</summary>
    public string? CreatedBy { get; set; }

    /// <summary>Gets or sets the navigation property for the dependent job.</summary>
    public virtual Job? Job { get; set; }

    /// <summary>Gets or sets the navigation property for the prerequisite job.</summary>
    public virtual Job? DependsOnJob { get; set; }
}
