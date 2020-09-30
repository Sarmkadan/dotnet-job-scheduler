# JobPipeline

Represents a named, ordered collection of job steps that are executed as a single unit. Each `JobPipeline` contains a list of `JobPipelineStep` entries, each referencing a specific job and defining its execution order and whether the pipeline should halt if that step fails. Pipelines can be toggled on or off via `IsActive` and carry standard audit fields.

## API

### `JobPipeline` Members

| Member | Type | Description |
|--------|------|-------------|
| `Id` | `Guid` | Unique identifier for the pipeline. |
| `Name` | `string` | Human-readable name of the pipeline. |
| `Description` | `string` | Optional description of the pipeline’s purpose. |
| `IsActive` | `bool` | Indicates whether the pipeline is enabled for scheduling or execution. |
| `CreatedAt` | `DateTime` | Timestamp when the pipeline was created. |
| `UpdatedAt` | `DateTime?` | Timestamp of the last update, or `null` if never updated. |
| `CreatedBy` | `string?` | Identifier of the user or system that created the pipeline, or `null`. |
| `Steps` | `List<JobPipelineStep>` | Ordered list of steps belonging to this pipeline. May be empty. |

### `JobPipelineStep` Members

`JobPipelineStep` is a child entity that links a job to a pipeline with ordering and failure-handling rules.

| Member | Type | Description |
|--------|------|-------------|
| `Id` | `Guid` | Unique identifier for the step. |
| `PipelineId` | `Guid` | Foreign key to the parent `JobPipeline`. |
| `JobId` | `Guid` | Foreign key to the `Job` that this step executes. |
| `StepOrder` | `int` | Zero-based or sequential position of this step within the pipeline. Lower values execute first. |
| `StopOnFailure` | `bool` | If `true`, the pipeline execution stops when this step fails. If `false`, the pipeline continues with the next step. |
| `Pipeline` | `JobPipeline?` | Navigation property to the parent pipeline. May be `null` if not loaded. |
| `Job` | `Job?` | Navigation property to the referenced job. May be `null` if not loaded. |

No member throws exceptions on its own; exceptions may arise from data access or validation logic when persisting or loading these objects.

## Usage

### Example 1: Creating a Pipeline with Steps

```csharp
var pipeline = new JobPipeline
{
    Id = Guid.NewGuid(),
    Name = "Data Ingestion Pipeline",
    Description = "Extract, transform, and load daily data.",
    IsActive = true,
    CreatedAt = DateTime.UtcNow,
    CreatedBy = "system",
    Steps = new List<JobPipelineStep>
    {
        new JobPipelineStep
        {
            Id = Guid.NewGuid(),
            PipelineId = pipeline.Id,
            JobId = extractJobId,
            StepOrder = 0,
            StopOnFailure = true
        },
        new JobPipelineStep
        {
            Id = Guid.NewGuid(),
            PipelineId = pipeline.Id,
            JobId = transformJobId,
            StepOrder = 1,
            StopOnFailure = false
        }
    }
};
```

### Example 2: Iterating Steps and Checking Failure Behavior

```csharp
// Assume 'pipeline' is loaded from a data store with its Steps collection.
foreach (var step in pipeline.Steps.OrderBy(s => s.StepOrder))
{
    Console.WriteLine($"Step {step.StepOrder}: Job {step.JobId}");

    if (step.StopOnFailure)
    {
        Console.WriteLine("  -> Pipeline will halt if this step fails.");
    }
    else
    {
        Console.WriteLine("  -> Pipeline will continue on failure.");
    }

    // Access navigation properties if loaded.
    if (step.Job != null)
    {
        Console.WriteLine($"  Job name: {step.Job.Name}");
    }
}
```

## Notes

- **Empty Steps**: A pipeline with an empty `Steps` list is valid but will execute no jobs. Consider validating that at least one step exists before scheduling.
- **Null Navigation Properties**: `Pipeline` and `Job` on `JobPipelineStep` are `null` when the related entity has not been explicitly loaded (e.g., via lazy loading or explicit include). Accessing them without checking for `null` may cause a `NullReferenceException`.
- **Null Audit Fields**: `UpdatedAt` and `CreatedBy` can be `null`. Code that formats or displays these values should handle the `null` case.
- **Thread Safety**: `List<JobPipelineStep>` is not thread-safe. Concurrent modifications to the `Steps` collection (e.g., adding or removing steps from multiple threads) must be synchronized externally. The `JobPipeline` object itself has no built-in synchronization; thread safety is the responsibility of the caller.
- **Step Ordering**: The `StepOrder` property does not enforce uniqueness or contiguity. Duplicate or non-sequential values are allowed but may lead to ambiguous execution order. It is recommended to assign unique, sequential integers.
- **StopOnFailure**: This flag only affects runtime execution logic; it does not prevent the step from being recorded or the pipeline state from being updated. The pipeline runner should check this flag after each step completes.
