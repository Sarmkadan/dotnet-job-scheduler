#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace JobScheduler.Core.Exceptions;
using System;
using System.Text.Json.Serialization;

/// <summary>
/// Thrown when adding a job dependency would introduce a cycle in the dependency graph,
/// violating the directed acyclic graph (DAG) invariant required for deterministic execution ordering.
/// </summary>
public sealed class CyclicDependencyException : JobSchedulerException
{
    /// <summary>Gets the ID of the job that was requested to be made dependent.</summary>
[JsonInclude]
    public Guid JobId { get; private set; }

    /// <summary>Gets the ID of the job that was requested as the prerequisite.</summary>
[JsonInclude]
    public Guid DependsOnJobId { get; private set; }

    /// <summary>
    /// Initializes a new instance of <see cref="CyclicDependencyException"/>.
    /// </summary>
    /// <param name="jobId">The dependent job that would be part of the cycle.</param>
    /// <param name="dependsOnJobId">The prerequisite job that would be part of the cycle.</param>
    public CyclicDependencyException(Guid jobId, Guid dependsOnJobId)
        : base(
            $"Cannot add dependency: job '{jobId}' → '{dependsOnJobId}' would introduce a cycle in the dependency graph.",
            "CYCLIC_DEPENDENCY_DETECTED")
    {
        JobId = jobId;
        DependsOnJobId = dependsOnJobId;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="CyclicDependencyException"/> with an inner exception.
    /// </summary>
    /// <param name="jobId">The dependent job that would be part of the cycle.</param>
    /// <param name="dependsOnJobId">The prerequisite job that would be part of the cycle.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public CyclicDependencyException(Guid jobId, Guid dependsOnJobId, Exception innerException)
        : base(
            $"Cannot add dependency: job '{jobId}' → '{dependsOnJobId}' would introduce a cycle in the dependency graph.",
            "CYCLIC_DEPENDENCY_DETECTED",
            innerException)
    {
        JobId = jobId;
        DependsOnJobId = dependsOnJobId;
    }
}
