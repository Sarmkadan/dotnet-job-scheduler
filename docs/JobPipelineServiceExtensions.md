# JobPipelineServiceExtensions

JobPipelineServiceExtensions provides a set of static extension methods designed to simplify interactions with the job pipeline management system within the `dotnet-job-scheduler` project. These utilities abstract common database operations and data retrieval tasks, facilitating easier management and status monitoring of job pipelines.

## API

### Static Methods

*   **`public static async Task<bool> ExistsAsync`**
    Determines whether a job pipeline exists within the system. Returns `true` if the pipeline exists, otherwise `false`.

*   **`public static async Task<JobPipeline?> GetPipelineByNameAsync`**
    Retrieves a job pipeline by its name. Returns the `JobPipeline` object if found; otherwise, returns `null`.

*   **`public static async Task<IReadOnlyList<JobPipeline>> GetActivePipelinesAsync`**
    Retrieves a read-only list of all currently active job pipelines.

*   **`public static async Task<PipelineStatusWithStatsResponse?> GetPipelineStatusWithStatsAsync`**
    Retrieves the status and performance statistics for a specified job pipeline. Returns the `PipelineStatusWithStatsResponse` object if found; otherwise, returns `null`.

*   **`public static JobSchedulerContext GetDbContext`**
    Provides access to the underlying `JobSchedulerContext` used by the service extensions for database operations.

### Properties

*   **`public Guid PipelineId`**
    Unique identifier for the pipeline.
*   **`public string PipelineName`**
    The name of the pipeline.
*   **`public List<PipelineStepStatus> StepStatuses`**
    List of statuses for individual steps within the pipeline.
*   **`public List<PipelineStepExecutionStats> ExecutionStats`**
    List of execution statistics for individual steps within the pipeline.
*   **`public int StepOrder`**
    Defines the order of execution for pipeline steps.
*   **`public Guid JobId`**
    Unique identifier for the associated job.
*   **`public string? JobName`**
    The name of the associated job, if available.
*   **`public string Status`**
    The current operational status of the pipeline.
*   **`public DateTime? LastExecutedAt`**
    The timestamp of the last execution, if available.
*   **`public bool IsReady`**
    Indicates whether the pipeline is ready for execution.
*   **`public int SuccessCount`**
    Total number of successful executions.
*   **`public int FailureCount`**
    Total number of failed executions.
*   **`public TimeSpan AverageDuration`**
    The average duration of pipeline executions.
*   **`public int TotalExecutions`**
    The total number of executions performed.

## Usage

```csharp
// Example 1: Checking for a pipeline and retrieving details
if (await JobPipelineServiceExtensions.ExistsAsync("DataProcessingPipeline"))
{
    var pipeline = await JobPipelineServiceExtensions.GetPipelineByNameAsync("DataProcessingPipeline");
    Console.WriteLine($"Pipeline found: {pipeline?.PipelineName}");
}

// Example 2: Retrieving status and statistics
var stats = await JobPipelineServiceExtensions.GetPipelineStatusWithStatsAsync("DataProcessingPipeline");
if (stats != null)
{
    Console.WriteLine($"Status: {stats.Status}, Total Executions: {stats.TotalExecutions}");
}
```

## Notes

*   **Null Handling:** Methods returning `Task<T?>` may return `null` if the requested resource (e.g., a specific pipeline) is not found in the database. Callers should implement appropriate null checks.
*   **Thread Safety:** The extension methods are designed to be stateless and operate on the provided `JobSchedulerContext`, making them generally safe for concurrent use. However, database-level concurrency is subject to the limitations and isolation levels configured in `JobSchedulerContext`.
*   **Database Context:** The `GetDbContext` method exposes the internal `JobSchedulerContext`. Exercise caution when using this to ensure that manual database modifications do not interfere with the internal state management of the `JobPipeline` services.
