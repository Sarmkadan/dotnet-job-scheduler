// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using JobScheduler.Core.Data;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobScheduler.Core.Services;

/// <summary>
/// Encapsulates the outcome of a full dependency graph validation pass.
/// </summary>
public sealed class DependencyGraphValidationResult
{
    /// <summary>Gets whether the graph satisfies the DAG constraint (no cycles).</summary>
    public bool IsValid { get; init; }

    /// <summary>Gets the IDs of any jobs involved in a detected cycle, in traversal order.</summary>
    public IReadOnlyList<Guid> CycleNodes { get; init; } = Array.Empty<Guid>();

    /// <summary>Gets a human-readable summary of the validation outcome.</summary>
    public string Message { get; init; } = string.Empty;

    internal static DependencyGraphValidationResult Valid() =>
        new() { IsValid = true, Message = "Dependency graph is a valid DAG." };

    internal static DependencyGraphValidationResult WithCycle(IReadOnlyList<Guid> cycleNodes) =>
        new()
        {
            IsValid = false,
            CycleNodes = cycleNodes,
            Message = $"Cycle detected involving {cycleNodes.Count} job(s): {string.Join(" → ", cycleNodes)}."
        };
}

/// <summary>
/// Manages job dependency relationships and enforces directed acyclic graph (DAG) invariants.
/// Provides cycle detection, topological execution ordering, and full graph validation.
/// </summary>
public interface IJobDependencyService
{
    /// <summary>
    /// Registers a dependency so that <paramref name="jobId"/> only runs after
    /// <paramref name="dependsOnJobId"/> completes. Throws <see cref="CyclicDependencyException"/>
    /// if the edge would introduce a cycle.
    /// </summary>
    /// <param name="jobId">The dependent job.</param>
    /// <param name="dependsOnJobId">The prerequisite job.</param>
    /// <param name="createdBy">Optional actor identity for the audit trail.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddDependencyAsync(Guid jobId, Guid dependsOnJobId, string? createdBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an existing dependency between two jobs. No-ops if the dependency does not exist.
    /// </summary>
    /// <param name="jobId">The dependent job.</param>
    /// <param name="dependsOnJobId">The prerequisite job to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RemoveDependencyAsync(Guid jobId, Guid dependsOnJobId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the jobs that <paramref name="jobId"/> directly depends on (its prerequisites).
    /// </summary>
    Task<IReadOnlyList<Job>> GetDependenciesAsync(Guid jobId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns jobs that directly depend on <paramref name="jobId"/> (its immediate successors).
    /// </summary>
    Task<IReadOnlyList<Job>> GetDependentsAsync(Guid jobId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all jobs sorted in topological execution order so that each job appears
    /// only after all of its prerequisites. Jobs without dependencies come first.
    /// </summary>
    Task<IReadOnlyList<Job>> GetTopologicalOrderAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the entire dependency graph for cycles and returns a detailed result.
    /// </summary>
    Task<DependencyGraphValidationResult> ValidateGraphAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Default implementation of <see cref="IJobDependencyService"/>.
/// Uses DFS-based cycle detection and Kahn's algorithm for topological ordering.
/// </summary>
public sealed class JobDependencyService : IJobDependencyService
{
    private readonly JobSchedulerContext _context;
    private readonly ILogger<JobDependencyService>? _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="JobDependencyService"/>.
    /// </summary>
    /// <param name="context">The EF Core database context.</param>
    /// <param name="logger">Optional structured logger.</param>
    public JobDependencyService(JobSchedulerContext context, ILogger<JobDependencyService>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task AddDependencyAsync(Guid jobId, Guid dependsOnJobId, string? createdBy = null,
        CancellationToken cancellationToken = default)
    {
        if (jobId == dependsOnJobId)
            throw new JobValidationException("A job cannot depend on itself.", nameof(dependsOnJobId));

        var jobExists = await _context.Jobs.AnyAsync(j => j.Id == jobId, cancellationToken);
        if (!jobExists) throw new JobNotFoundException(jobId);

        var prereqExists = await _context.Jobs.AnyAsync(j => j.Id == dependsOnJobId, cancellationToken);
        if (!prereqExists) throw new JobNotFoundException(dependsOnJobId);

        var alreadyLinked = await _context.JobDependencies
            .AnyAsync(d => d.JobId == jobId && d.DependsOnJobId == dependsOnJobId, cancellationToken);

        if (alreadyLinked)
        {
            _logger?.LogDebug("Dependency {JobId} → {DependsOnJobId} already exists; skipping.", jobId, dependsOnJobId);
            return;
        }

        // Cycle check: if dependsOnJobId is reachable from jobId via existing successor edges,
        // adding this edge would close a cycle (jobId → ... → dependsOnJobId → jobId).
        var successors = await LoadSuccessorEdgesAsync(cancellationToken);
        if (IsReachable(jobId, dependsOnJobId, successors))
            throw new CyclicDependencyException(jobId, dependsOnJobId);

        _context.JobDependencies.Add(new JobDependency
        {
            JobId = jobId,
            DependsOnJobId = dependsOnJobId,
            CreatedBy = createdBy
        });

        await _context.SaveChangesAsync(cancellationToken);
        _logger?.LogInformation("Dependency registered: job {JobId} depends on {DependsOnJobId}.", jobId, dependsOnJobId);
    }

    /// <inheritdoc />
    public async Task RemoveDependencyAsync(Guid jobId, Guid dependsOnJobId,
        CancellationToken cancellationToken = default)
    {
        var dependency = await _context.JobDependencies
            .FirstOrDefaultAsync(d => d.JobId == jobId && d.DependsOnJobId == dependsOnJobId, cancellationToken);

        if (dependency is null)
            return;

        _context.JobDependencies.Remove(dependency);
        await _context.SaveChangesAsync(cancellationToken);
        _logger?.LogInformation("Dependency removed: job {JobId} no longer depends on {DependsOnJobId}.", jobId, dependsOnJobId);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Job>> GetDependenciesAsync(Guid jobId,
        CancellationToken cancellationToken = default)
    {
        return await _context.JobDependencies
            .Where(d => d.JobId == jobId)
            .Select(d => d.DependsOnJob!)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Job>> GetDependentsAsync(Guid jobId,
        CancellationToken cancellationToken = default)
    {
        return await _context.JobDependencies
            .Where(d => d.DependsOnJobId == jobId)
            .Select(d => d.Job!)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Job>> GetTopologicalOrderAsync(CancellationToken cancellationToken = default)
    {
        var allJobs = await _context.Jobs.ToListAsync(cancellationToken);
        if (allJobs.Count == 0)
            return Array.Empty<Job>();

        var allDeps = await _context.JobDependencies.ToListAsync(cancellationToken);
        var jobLookup = allJobs.ToDictionary(j => j.Id);

        // Kahn's algorithm: in-degree = number of unresolved prerequisites per job.
        var inDegree = allJobs.ToDictionary(j => j.Id, _ => 0);
        var successors = new Dictionary<Guid, List<Guid>>();

        foreach (var dep in allDeps)
        {
            inDegree[dep.JobId]++;
            if (!successors.TryGetValue(dep.DependsOnJobId, out var list))
                successors[dep.DependsOnJobId] = list = new List<Guid>();
            list.Add(dep.JobId);
        }

        var queue = new Queue<Guid>(inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
        var sorted = new List<Job>(allJobs.Count);

        while (queue.Count > 0)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var current = queue.Dequeue();
            if (jobLookup.TryGetValue(current, out var job))
                sorted.Add(job);

            if (!successors.TryGetValue(current, out var dependents))
                continue;

            foreach (var dependent in dependents)
            {
                if (--inDegree[dependent] == 0)
                    queue.Enqueue(dependent);
            }
        }

        if (sorted.Count < allJobs.Count)
            _logger?.LogWarning(
                "Topological sort produced {Sorted}/{Total} jobs — a cycle may exist in the dependency graph.",
                sorted.Count, allJobs.Count);

        return sorted;
    }

    /// <inheritdoc />
    public async Task<DependencyGraphValidationResult> ValidateGraphAsync(CancellationToken cancellationToken = default)
    {
        var allJobIds = await _context.Jobs.Select(j => j.Id).ToListAsync(cancellationToken);
        if (allJobIds.Count == 0)
            return DependencyGraphValidationResult.Valid();

        var successors = await LoadSuccessorEdgesAsync(cancellationToken);
        var color = allJobIds.ToDictionary(id => id, _ => NodeColor.White);
        var cyclePath = new List<Guid>();

        foreach (var id in allJobIds)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            if (color[id] != NodeColor.White)
                continue;

            var dfsStack = new Stack<Guid>();
            if (HasCycleDfs(id, successors, color, dfsStack, cyclePath))
            {
                _logger?.LogWarning("Dependency graph validation failed: cycle detected among jobs {Nodes}.",
                    string.Join(", ", cyclePath));
                return DependencyGraphValidationResult.WithCycle(cyclePath);
            }
        }

        _logger?.LogDebug("Dependency graph validation passed: {Count} job(s) form a valid DAG.", allJobIds.Count);
        return DependencyGraphValidationResult.Valid();
    }

    // Builds successor edges: successors[P] = { jobs that depend on P }.
    private async Task<Dictionary<Guid, HashSet<Guid>>> LoadSuccessorEdgesAsync(CancellationToken cancellationToken)
    {
        var deps = await _context.JobDependencies.ToListAsync(cancellationToken);
        var edges = new Dictionary<Guid, HashSet<Guid>>();

        foreach (var d in deps)
        {
            if (!edges.TryGetValue(d.DependsOnJobId, out var set))
                edges[d.DependsOnJobId] = set = new HashSet<Guid>();
            set.Add(d.JobId);
        }

        return edges;
    }

    // Iterative DFS to check whether `target` is reachable from `start` via successor edges.
    private static bool IsReachable(Guid start, Guid target, Dictionary<Guid, HashSet<Guid>> successors)
    {
        var visited = new HashSet<Guid>();
        var stack = new Stack<Guid>();
        stack.Push(start);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (current == target) return true;
            if (!visited.Add(current)) continue;

            if (successors.TryGetValue(current, out var neighbors))
                foreach (var neighbor in neighbors)
                    stack.Push(neighbor);
        }

        return false;
    }

    // Recursive DFS with three-color marking. Returns true when a back edge (cycle) is found.
    // Gray = currently on the DFS stack; discovering a gray neighbor means a cycle.
    private static bool HasCycleDfs(
        Guid node,
        Dictionary<Guid, HashSet<Guid>> successors,
        Dictionary<Guid, NodeColor> color,
        Stack<Guid> path,
        List<Guid> cycleNodes)
    {
        color[node] = NodeColor.Gray;
        path.Push(node);

        if (successors.TryGetValue(node, out var neighbors))
        {
            foreach (var neighbor in neighbors)
            {
                if (!color.ContainsKey(neighbor))
                    continue;

                if (color[neighbor] == NodeColor.Gray)
                {
                    // Collect the cycle: walk the path until we reach the gray neighbor again.
                    foreach (var n in path)
                    {
                        cycleNodes.Add(n);
                        if (n == neighbor) break;
                    }
                    cycleNodes.Reverse();
                    return true;
                }

                if (color[neighbor] == NodeColor.White &&
                    HasCycleDfs(neighbor, successors, color, path, cycleNodes))
                    return true;
            }
        }

        path.Pop();
        color[node] = NodeColor.Black;
        return false;
    }

    private enum NodeColor { White, Gray, Black }
}
