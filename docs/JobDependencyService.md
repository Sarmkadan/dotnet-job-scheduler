# JobDependencyService

The `JobDependencyService` class manages job dependency relationships and enforces directed acyclic graph (DAG) invariants. It provides cycle detection, topological execution ordering, and full graph validation. This service is intended to be used as a singleton or scoped dependency in applications that orchestrate job execution with dependencies.

## API

### `IJobDependencyService.AddDependencyAsync(...)`

Registers a dependency so that `<paramref name="jobId"/>` only runs after `<paramref name="dependsOnJobId"/>` completes. Throws `<see cref="CyclicDependencyException"/>` if the edge would introduce a cycle.

- **Parameters** (inferred): 
  - `jobId`: The dependent job (Guid).
  - `dependsOnJobId`: The prerequisite job (Guid).
  - `createdBy`: Optional actor identity for the audit trail (string?).
  - `cancellationToken`: Cancellation token.
- **Returns**: A `Task` that completes when the dependency is registered.
- **Throws**: 
  - `JobValidationException` if a job attempts to depend on itself.
  - `JobNotFoundException` if either job does not exist.
  - `CyclicDependencyException` if adding the dependency would create a cycle in the graph.

### `IJobDependencyService.RemoveDependencyAsync(...)`

Removes an existing dependency between two jobs. No-ops if the dependency does not exist.

- **Parameters** (inferred):
  - `jobId`: The dependent job (Guid).
  - `dependsOnJobId`: The prerequisite job to remove (Guid).
  - `cancellationToken`: Cancellation token.
- **Returns**: A `Task` that completes when the dependency is removed (or if it did not exist).
- **Throws**: None (implementation-specific exceptions may propagate from the underlying data store).

### `IJobDependencyService.GetDependenciesAsync(...)`

Returns the jobs that `<paramref name="jobId"/>` directly depends on (its prerequisites).

- **Parameters** (inferred):
  - `jobId`: The job to query dependencies for (Guid).
  - `cancellationToken`: Cancellation token.
- **Returns**: A `Task<IReadOnlyList<Job>>` that resolves to the list of prerequisite jobs, or an empty list if the job has no dependencies.
- **Throws**: None (implementation-specific exceptions may propagate from the underlying data store).

### `IJobDependencyService.GetDependentsAsync(...)`

Returns jobs that directly depend on `<paramref name="jobId"/>` (its immediate successors).

- **Parameters** (inferred):
  - `jobId`: The job to query dependents for (Guid).
  - `cancellationToken`: Cancellation token.
- **Returns**: A `Task<IReadOnlyList<Job>>` that resolves to the list of dependent jobs, or an empty list if no jobs depend on the specified job.
- **Throws**: None (implementation-specific exceptions may propagate from the underlying data store).

### `IJobDependencyService.GetTopologicalOrderAsync(...)`

Returns all jobs sorted in topological execution order so that each job appears only after all of its prerequisites. Jobs without dependencies come first.

- **Parameters** (inferred):
  - `cancellationToken`: Cancellation token.
- **Returns**: A `Task<IReadOnlyList<Job>>` that resolves to the list of jobs in topological order. If a cycle exists, the returned list may be incomplete (see Notes).
- **Throws**: None (implementation-specific exceptions may propagate from the underlying data store).

### `IJobDependencyService.ValidateGraphAsync(...)`

Validates the entire dependency graph for cycles and returns a detailed result.

- **Parameters** (inferred):
  - `cancellationToken`: Cancellation token.
- **Returns**: A `Task<DependencyGraphValidationResult>` containing:
  - `IsValid`: Boolean indicating whether the graph is a valid DAG.
  - `CycleNodes`: List of job IDs involved in a detected cycle (empty if valid).
  - `Message`: Human-readable summary of the validation outcome.
- **Throws**: None (implementation-specific exceptions may propagate from the underlying data store).

## `DependencyGraphValidationResult`

Encapsulates the outcome of a full dependency graph validation pass.

- **IsValid**: Gets whether the graph satisfies the DAG constraint (no cycles).
- **CycleNodes**: Gets the IDs of any jobs involved in a detected cycle, in traversal order.
- **Message**: Gets a human-readable summary of the validation outcome.

### Static Factory Methods

- `Valid()`: Returns a validation result indicating a valid DAG.
- `WithCycle(IReadOnlyList<Guid> cycleNodes)`: Returns a validation result indicating a cycle was detected.

## Usage

### Example 1: Adding and validating dependencies

```csharp
public class DependencyManager
{
    private readonly IJobDependencyService _dependencyService;

    public DependencyManager(IJobDependencyService dependencyService)
    {
        _dependencyService = dependencyService;
    }

    public async Task AddJobDependenciesAsync(Guid jobA, Guid jobB, Guid jobC)
    {
        // jobB depends on jobA
        await _dependencyService.AddDependencyAsync(jobB, jobA);
        
        // jobC depends on jobB
        await _dependencyService.AddDependencyAsync(jobC, jobB);
        
        // Validate the graph
        var validation = await _dependencyService.ValidateGraphAsync();
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(
                $"Dependency graph contains a cycle: {validation.Message}");
        }
    }
}
```

### Example 2: Getting execution order and handling cycles

```csharp
public class JobScheduler
{
    private readonly IJobDependencyService _dependencyService;

    public JobScheduler(IJobDependencyService dependencyService)
    {
        _dependencyService = dependencyService;
    }

    public async Task<IReadOnlyList<Job>> GetExecutionOrderAsync()
    {
        // Get topological order (jobs with no dependencies first)
        var orderedJobs = await _dependencyService.GetTopologicalOrderAsync();
        
        // Validate to check for cycles
        var validation = await _dependencyService.ValidateGraphAsync();
        if (!validation.IsValid)
        {
            // Log or handle the cycle appropriately
            Console.WriteLine($"Warning: Dependency graph has cycle involving {validation.CycleNodes.Count} job(s).");
            // Depending on requirements, we might still return partial order or throw
        }
        
        return orderedJobs;
    }
}
```

### Example 3: Querying dependencies

```csharp
public class DependencyExplorer
{
    private readonly IJobDependencyService _dependencyService;

    public DependencyExplorer(IJobDependencyService dependencyService)
    {
        _dependencyService = dependencyService;
    }

    public async Task ExploreJobDependenciesAsync(Guid jobId)
    {
        var dependencies = await _dependencyService.GetDependenciesAsync(jobId);
        var dependents = await _dependencyService.GetDependentsAsync(jobId);
        
        Console.WriteLine($"Job {jobId} has {dependencies.Count} dependencies and {dependents.Count} dependents.");
        
        foreach (var dep in dependencies)
        {
            Console.WriteLine($"  Depends on: {dep.Id}");
        }
        
        foreach (var dependent in dependents)
        {
            Console.WriteLine($"  Required by: {dependent.Id}");
        }
    }
}
```

## Notes

- **Cycle detection**: The service uses DFS-based cycle detection in `ValidateGraphAsync` and proactive cycle checking in `AddDependencyAsync`. When a cycle is detected, `AddDependencyAsync` throws `CyclicDependencyException` containing the two jobs that would form the cycle edge. The full cycle path is available via `DependencyGraphValidationResult.CycleNodes`.
  
- **Topological sort with cycles**: `GetTopologicalOrderAsync` uses Kahn's algorithm and will return a partial order if cycles exist (jobs that can be ordered without violating dependencies). A warning is logged when the sorted count is less than the total job count, indicating a possible cycle.

- **Null and invalid IDs**: Methods that accept job IDs throw `JobNotFoundException` if the corresponding job does not exist in the database. `AddDependencyAsync` throws `JobValidationException` for self-dependencies.

- **Concurrency**: Instance members are not guaranteed to be thread-safe. Concurrent calls to mutating methods (`AddDependencyAsync`, `RemoveDependencyAsync`) from multiple threads may lead to inconsistent state. Use external synchronization if concurrent access is required.

- **Underlying storage**: The service relies on an external data store (EF Core `JobSchedulerContext`). Transient failures from the store (e.g., network timeouts) may propagate as exceptions. Consider implementing retry logic for production use.

- **Performance**: Validation and topological sort operations load the entire graph into memory. For very large graphs, consider pagination or streaming alternatives (not currently implemented).