# JobPipelineService

The `JobPipelineService` class provides the core operations for managing job pipelines within the `dotnet-job-scheduler` framework. It exposes asynchronous methods to create, retrieve, list, delete, and query the status of pipelines, as well as a static helper for mapping pipeline data to a response contract. This service is intended to be used as a singleton or scoped dependency in applications that orchestrate job execution pipelines.

## API

### `JobPipelineService()`

Initializes a new instance of the `JobPipelineService`. The constructor may accept dependencies (e.g., a repository or database context) depending on the hosting environment; the exact signature is determined by the dependency injection container.

### `async Task<JobPipeline> CreatePipelineAsync(...)`

Creates a new job pipeline and persists it to the underlying storage.

- **Parameters** (inferred): A pipeline definition or configuration object (e.g., a `JobPipeline` instance or a set of parameters such as name, schedule, and job steps).
- **Returns**: A `Task<JobPipeline>` that resolves to the newly created pipeline, including any generated identifiers and default state.
- **Throws**: `ArgumentNullException` if required parameters are null; `InvalidOperationException` if a pipeline with the same name already exists; `ArgumentException` if the pipeline configuration is invalid.

### `async Task<JobPipeline?> GetPipelineAsync(...)`

Retrieves a specific pipeline by its unique identifier.

- **Parameters** (inferred): A pipeline identifier (e.g., a `string` or `Guid`).
- **Returns**: A `Task<JobPipeline?>` that resolves to the pipeline if found, or `null` if no pipeline matches the given identifier.
- **Throws**: `ArgumentNullException` if the identifier is null; `ArgumentException` if the identifier is empty or malformed.

### `async Task<IReadOnlyList<JobPipeline>> GetAllPipelinesAsync()`

Returns a read-only list of all pipelines currently stored in the system.

- **Parameters**: None.
- **Returns**: A `Task<IReadOnlyList<JobPipeline>>` that resolves to an empty list if no pipelines exist, or a list of all pipelines.
- **Throws**: None (implementation-specific exceptions may propagate from the underlying data store).

### `async Task<bool> DeletePipelineAsync(...)`

Deletes a pipeline identified by its unique identifier.

- **Parameters** (inferred): A pipeline identifier (e.g., a `string` or `Guid`).
- **Returns**: A `Task<bool>` that resolves to `true` if the pipeline was found and deleted; `false` if no pipeline matched the identifier.
- **Throws**: `ArgumentNullException` if the identifier is null; `ArgumentException` if the identifier is empty or malformed.

### `async Task<PipelineStatusResponse?> GetPipelineStatusAsync(...)`

Retrieves the current execution status of a pipeline.

- **Parameters** (inferred): A pipeline identifier (e.g., a `string` or `Guid`).
- **Returns**: A `Task<PipelineStatusResponse?>` that resolves to a status object if the pipeline exists, or `null` if no pipeline matches the identifier.
- **Throws**: `ArgumentNullException` if the identifier is null; `ArgumentException` if the identifier is empty or malformed.

### `static PipelineResponse MapToResponse(...)`

Converts a pipeline domain object (e.g., `JobPipeline` or `PipelineStatusResponse`) into a `PipelineResponse` DTO suitable for API responses.

- **Parameters** (inferred): The source object to map.
- **Returns**: A `PipelineResponse` instance populated with data from the source.
- **Throws**: `ArgumentNullException` if the source object is null.

## Usage

### Example 1: Creating and retrieving a pipeline

```csharp
public class PipelineManager
{
    private readonly JobPipelineService _pipelineService;

    public PipelineManager(JobPipelineService pipelineService)
    {
        _pipelineService = pipelineService;
    }

    public async Task<JobPipeline> CreateAndVerifyPipelineAsync(string name, string cronExpression)
    {
        // Create a new pipeline (parameters depend on actual overload)
        var pipeline = await _pipelineService.CreatePipelineAsync(
            new JobPipeline { Name = name, Schedule = cronExpression });

        // Retrieve it by its generated ID
        var retrieved = await _pipelineService.GetPipelineAsync(pipeline.Id);
        if (retrieved == null)
            throw new InvalidOperationException("Pipeline was not persisted.");

        return retrieved;
    }
}
```

### Example 2: Listing, checking status, and deleting pipelines

```csharp
public class PipelineCleanupService
{
    private readonly JobPipelineService _pipelineService;

    public PipelineCleanupService(JobPipelineService pipelineService)
    {
        _pipelineService = pipelineService;
    }

    public async Task CleanupFailedPipelinesAsync()
    {
        var allPipelines = await _pipelineService.GetAllPipelinesAsync();

        foreach (var pipeline in allPipelines)
        {
            var status = await _pipelineService.GetPipelineStatusAsync(pipeline.Id);
            if (status?.State == PipelineState.Failed)
            {
                bool deleted = await _pipelineService.DeletePipelineAsync(pipeline.Id);
                Console.WriteLine($"Pipeline {pipeline.Id} deleted: {deleted}");
            }
        }
    }
}
```

## Notes

- **Thread safety**: Instance members of `JobPipelineService` are not guaranteed to be thread-safe. Concurrent calls to `CreatePipelineAsync`, `DeletePipelineAsync`, or any other mutating method from multiple threads may lead to inconsistent state or data races. Use external synchronization (e.g., a `SemaphoreSlim`) if concurrent access is required.
- **Null and empty identifiers**: Methods that accept a pipeline identifier throw `ArgumentNullException` for `null` and `ArgumentException` for empty or whitespace-only values. Always validate identifiers before calling these methods.
- **Pipeline not found**: `GetPipelineAsync` and `GetPipelineStatusAsync` return `null` (not throw) when the identifier does not match an existing pipeline. `DeletePipelineAsync` returns `false` in the same scenario. This allows callers to handle missing pipelines gracefully without exception handling.
- **Static method**: `MapToResponse` is thread-safe as it has no instance state. However, the source object passed to it must not be modified concurrently during mapping.
- **Underlying storage**: The service relies on an external data store (e.g., a database or in-memory collection). Transient failures from the store (e.g., network timeouts) may propagate as exceptions. Consider implementing retry logic for production use.
