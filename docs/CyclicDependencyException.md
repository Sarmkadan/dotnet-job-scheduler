# CyclicDependencyException

Represents an error that occurs when a job scheduling operation would introduce a circular dependency chain in the job graph. This exception is thrown by the scheduler when an attempt is made to register a dependency that would create a cycle, ensuring the directed acyclic graph (DAG) constraint of the job dependency system remains intact.

## API

### `public Guid JobId`

Gets the unique identifier of the job that was being configured when the cyclic dependency was detected. This is the job whose dependency specification triggered the cycle.

**Type:** `Guid`
**Access:** Read-only property

### `public Guid DependsOnJobId`

Gets the unique identifier of the job that `JobId` was attempting to depend on, which would have completed the cycle. This is the target dependency that caused the violation.

**Type:** `Guid`
**Access:** Read-only property

### `public CyclicDependencyException()`

Initializes a new instance of the `CyclicDependencyException` class with default error details.

**Parameters:** None
**Return value:** A new `CyclicDependencyException` instance with empty `JobId` and `DependsOnJobId` values (`Guid.Empty`).
**Throws:** Nothing

### `public CyclicDependencyException(Guid jobId, Guid dependsOnJobId)`

Initializes a new instance of the `CyclicDependencyException` class with the identifiers of the jobs involved in the detected cycle.

**Parameters:**
- `jobId` (`Guid`): The identifier of the job being configured.
- `dependsOnJobId` (`Guid`): The identifier of the dependency that would create the cycle.

**Return value:** A new `CyclicDependencyException` instance with the specified job identifiers.
**Throws:** Nothing

## Usage

### Example 1: Catching a Cyclic Dependency During Job Registration

```csharp
var scheduler = new JobScheduler();
var jobA = scheduler.RegisterJob("JobA", () => Console.WriteLine("A"));
var jobB = scheduler.RegisterJob("JobB", () => Console.WriteLine("B"));
var jobC = scheduler.RegisterJob("JobC", () => Console.WriteLine("C"));

scheduler.AddDependency(jobB.Id, jobA.Id); // B depends on A
scheduler.AddDependency(jobC.Id, jobB.Id); // C depends on B

try
{
    // Attempting to make A depend on C would create a cycle: A -> C -> B -> A
    scheduler.AddDependency(jobA.Id, jobC.Id);
}
catch (CyclicDependencyException ex)
{
    Console.WriteLine($"Cycle detected: Job {ex.JobId} cannot depend on {ex.DependsOnJobId}");
    // Output: Cycle detected: Job <jobA.Id> cannot depend on <jobC.Id>
}
```

### Example 2: Validating Dependencies Before Applying

```csharp
public bool TryAddDependency(JobScheduler scheduler, Guid jobId, Guid dependsOnId)
{
    try
    {
        scheduler.AddDependency(jobId, dependsOnId);
        return true;
    }
    catch (CyclicDependencyException ex)
    {
        LogWarning($"Cyclic dependency prevented: {ex.JobId} -> {ex.DependsOnJobId}");
        return false;
    }
}

// Usage
var scheduler = new JobScheduler();
var jobX = scheduler.RegisterJob("X", () => { });
var jobY = scheduler.RegisterJob("Y", () => { });
var jobZ = scheduler.RegisterJob("Z", () => { });

scheduler.AddDependency(jobY.Id, jobX.Id);
scheduler.AddDependency(jobZ.Id, jobY.Id);

bool success = TryAddDependency(scheduler, jobX.Id, jobZ.Id);
// success is false; cycle X -> Z -> Y -> X would have been created
```

## Notes

- The `CyclicDependencyException` is thrown synchronously at the point of dependency registration, not during job execution. Detection occurs before the dependency edge is added to the graph.
- The default constructor produces an exception with `Guid.Empty` for both `JobId` and `DependsOnJobId`. This is suitable for deserialization scenarios or cases where the specific job identifiers are not available, but callers should prefer the parameterized constructor for meaningful diagnostics.
- This exception type does not carry any mutable state; both properties are read-only and set only during construction. Instances are safe to share across threads without synchronization.
- The scheduler implementation that throws this exception must perform a graph traversal (typically depth-first or breadth-first search) to detect cycles. This operation is O(V + E) relative to the current dependency graph size and is not thread-safe if the graph is being mutated concurrently by other callers. Callers should serialize dependency modifications externally if multi-threaded access is expected.
- If `JobId` and `DependsOnJobId` refer to the same job identifier, the exception still applies—a self-dependency is the simplest form of a cycle and should be rejected with this exception type.
