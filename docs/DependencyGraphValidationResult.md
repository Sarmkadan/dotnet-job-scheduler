# DependencyGraphValidationResult

Represents the outcome of a dependency graph validation operation performed by `JobDependencyService`. It encapsulates whether the graph is acyclic, the set of nodes involved in any detected cycles, and a human-readable diagnostic message. This type is returned exclusively by `ValidateGraphAsync` and is designed to be inspected before scheduling or modifying job dependencies.

## API

### `public bool IsValid`

Gets a value indicating whether the dependency graph contains no cycles. Returns `true` when the graph is a valid directed acyclic graph (DAG); returns `false` when one or more cycles are detected.

### `public IReadOnlyList<Guid> CycleNodes`

Gets the collection of job identifiers that participate in at least one cycle within the dependency graph. When `IsValid` is `true`, this list is empty. When `IsValid` is `false`, it contains every node found in any cycle, deduplicated across multiple cycles. The order is deterministic but not guaranteed to reflect cycle traversal order.

### `public string Message`

Gets a diagnostic message describing the validation result. When `IsValid` is `true`, the message indicates successful validation. When `IsValid` is `false`, the message describes the cycle(s) detected, typically including the identifiers of participating nodes.

### `public JobDependencyService`

*This member is the containing service class and is not a member of `DependencyGraphValidationResult`. It is listed here for completeness of the public surface but belongs to the service type that produces this result.*

### `public async Task AddDependencyAsync`

*This member belongs to `JobDependencyService`, not `DependencyGraphValidationResult`. It adds a dependency edge between two jobs.*

### `public async Task RemoveDependencyAsync`

*This member belongs to `JobDependencyService`, not `DependencyGraphValidationResult`. It removes a dependency edge between two jobs.*

### `public async Task<IReadOnlyList<Job>> GetDependenciesAsync`

*This member belongs to `JobDependencyService`, not `DependencyGraphValidationResult`. It retrieves the direct dependencies of a given job.*

### `public async Task<IReadOnlyList<Job>> GetDependentsAsync`

*This member belongs to `JobDependencyService`, not `DependencyGraphValidationResult`. It retrieves the direct dependents of a given job.*

### `public async Task<IReadOnlyList<Job>> GetTopologicalOrderAsync`

*This member belongs to `JobDependencyService`, not `DependencyGraphValidationResult`. It returns a topologically sorted list of jobs when the graph is acyclic.*

### `public async Task<DependencyGraphValidationResult> ValidateGraphAsync`

*This member belongs to `JobDependencyService`, not `DependencyGraphValidationResult`. It validates the entire dependency graph and returns an instance of this result type.*

## Usage

### Example 1: Validating before adding a dependency

```csharp
JobDependencyService dependencyService = /* obtained via DI */;
Guid jobA = Guid.Parse("11111111-1111-1111-1111-111111111111");
Guid jobB = Guid.Parse("22222222-2222-2222-2222-222222222222");

// Check current graph state before modification
DependencyGraphValidationResult currentState = await dependencyService.ValidateGraphAsync();

if (!currentState.IsValid)
{
    Console.WriteLine($"Graph is invalid: {currentState.Message}");
    Console.WriteLine($"Nodes in cycles: {string.Join(", ", currentState.CycleNodes)}");
    return;
}

// Tentatively add the edge and re-validate
await dependencyService.AddDependencyAsync(jobA, jobB);
DependencyGraphValidationResult afterAdd = await dependencyService.ValidateGraphAsync();

if (!afterAdd.IsValid)
{
    // Roll back — the new edge created a cycle
    await dependencyService.RemoveDependencyAsync(jobA, jobB);
    Console.WriteLine($"Cannot add dependency: {afterAdd.Message}");
}
else
{
    Console.WriteLine("Dependency added successfully. Graph remains valid.");
}
```

### Example 2: Diagnosing cycles before scheduling

```csharp
JobDependencyService dependencyService = /* obtained via DI */;

DependencyGraphValidationResult validation = await dependencyService.ValidateGraphAsync();

if (!validation.IsValid)
{
    // Prevent scheduling until cycles are resolved
    throw new InvalidOperationException(
        $"Job graph contains cycles involving jobs: {string.Join(", ", validation.CycleNodes)}. " +
        $"Details: {validation.Message}");
}

// Graph is acyclic — safe to compute execution order
IReadOnlyList<Job> executionOrder = await dependencyService.GetTopologicalOrderAsync();

foreach (Job job in executionOrder)
{
    Console.WriteLine($"Scheduling job {job.Id} in order.");
}
```

## Notes

- **Immutability:** `DependencyGraphValidationResult` is a snapshot of validation state at the time `ValidateGraphAsync` completes. Subsequent modifications to the dependency graph via `AddDependencyAsync` or `RemoveDependencyAsync` do not update an existing instance; a new call to `ValidateGraphAsync` is required to obtain a current result.
- **Empty graphs:** A graph with zero jobs or zero edges is considered valid. `IsValid` returns `true`, `CycleNodes` is empty, and `Message` reflects successful validation.
- **Multiple cycles:** When the graph contains more than one disjoint cycle, `CycleNodes` includes all participating nodes from all cycles. The `Message` property describes the first detected cycle in detail and may summarize additional cycles.
- **Self-loops:** A dependency from a job to itself is treated as a cycle of length one. The job's identifier appears in `CycleNodes` and `IsValid` returns `false`.
- **Thread safety:** The result type itself is read-only and safe to access from any thread after construction. However, the underlying `JobDependencyService` that produces it may have its own synchronization guarantees; consult that service's documentation for concurrent access semantics.
- **Determinism:** For a given graph state, repeated calls to `ValidateGraphAsync` return results with identical `IsValid` and `CycleNodes` membership. The order of identifiers within `CycleNodes` is stable across calls but should not be relied upon for ordering logic.
