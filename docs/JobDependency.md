# JobDependency

Represents a dependency relationship between two jobs in the scheduling system, ensuring that a job (`JobId`) cannot execute until its prerequisite job (`DependsOnJobId`) has completed successfully. This entity enforces execution ordering and prevents circular dependencies through validation logic implemented elsewhere in the scheduler.

## API

### `Guid Id`

Unique identifier for this dependency record. Serves as the primary key and is immutable after creation.

### `Guid JobId`

Foreign key referencing the job that depends on another job. This job will be blocked from execution until the prerequisite job completes.

### `Guid DependsOnJobId`

Foreign key referencing the prerequisite job that must complete before the dependent job can run.

### `DateTime CreatedAt`

Timestamp indicating when this dependency was established. Set automatically at creation time and should not be modified thereafter.

### `string? CreatedBy`

Optional identifier of the user or system component that created this dependency. May be `null` if the dependency was established programmatically without user attribution.

### `virtual Job? Job`

Navigation property to the dependent `Job` entity. Returns `null` if the related job has not been loaded from the database (lazy loading or explicit inclusion required). This is the job that waits on the prerequisite.

### `virtual Job? DependsOnJob`

Navigation property to the prerequisite `Job` entity. Returns `null` if the related job has not been loaded from the database. This is the job that must complete first.

## Usage

### Example 1: Creating a Simple Dependency

```csharp
// Job B depends on Job A completing first
var dependency = new JobDependency
{
    Id = Guid.NewGuid(),
    JobId = jobB.Id,
    DependsOnJobId = jobA.Id,
    CreatedAt = DateTime.UtcNow,
    CreatedBy = "admin-user"
};

dbContext.JobDependencies.Add(dependency);
await dbContext.SaveChangesAsync();
```

### Example 2: Loading and Inspecting Dependencies with Navigation Properties

```csharp
// Load dependencies for a specific job, including related job details
var dependencies = await dbContext.JobDependencies
    .Where(d => d.JobId == targetJobId)
    .Include(d => d.Job)
    .Include(d => d.DependsOnJob)
    .ToListAsync();

foreach (var dep in dependencies)
{
    Console.WriteLine(
        $"Job '{dep.Job?.Name}' depends on '{dep.DependsOnJob?.Name}' " +
        $"(created by {dep.CreatedBy ?? "system"} at {dep.CreatedAt})");
}
```

## Notes

- **Circular dependencies**: The `JobDependency` entity itself does not validate against circular references. Validation must be performed at the application or service layer before persisting a new dependency to prevent deadlock scenarios where two jobs mutually depend on each other.
- **Self-referencing dependencies**: Setting `JobId` equal to `DependsOnJobId` creates a self-dependency that will permanently block the job. Guard against this at the business logic level.
- **Orphaned dependencies**: Deleting a job does not automatically remove its associated `JobDependency` records unless cascade delete is configured at the database level. Orphaned dependencies referencing non-existent jobs will cause resolution failures during scheduling.
- **Navigation properties**: The `Job` and `DependsOnJob` properties are marked `virtual` to support Entity Framework lazy loading. They will be `null` unless explicitly included via `.Include()` or accessed within an active DbContext scope with lazy loading enabled.
- **Thread safety**: This type is a plain data entity and provides no built-in thread synchronization. Concurrent modifications to the same dependency record across multiple threads or processes must be coordinated through database transactions or optimistic concurrency controls.
- **Immutability of keys**: `Id`, `JobId`, and `DependsOnJobId` should be treated as immutable after creation. Changing these values on an existing record would corrupt the dependency graph and is not supported by typical scheduling logic.
