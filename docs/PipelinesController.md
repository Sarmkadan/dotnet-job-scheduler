# PipelinesController

`PipelinesController` is an ASP.NET Core API controller responsible for managing the lifecycle of job pipelines within the `dotnet-job-scheduler` system. It exposes endpoints to create, retrieve, list, delete, and monitor the status of pipelines, delegating business logic to an underlying pipeline service.

## API

### `public PipelinesController`

Constructor. Accepts an injected pipeline service instance used by all action methods to perform pipeline operations.

- **Parameters:**
  - *(constructor dependency)* The pipeline service implementation.
- **Returns:** *(constructor)*
- **Throws:** `ArgumentNullException` if the injected service is `null`.

---

### `public async Task<ActionResult<PipelineResponse>> CreatePipeline`

Creates a new pipeline based on the supplied definition and returns the created pipeline’s details.

- **Parameters:**
  - `PipelineCreateRequest request` — The pipeline definition containing schedule, steps, and configuration.
- **Returns:**
  - `201 Created` with a `PipelineResponse` body on success.
  - `400 Bad Request` when the request is malformed or validation fails.
- **Throws:** Relies on the underlying service to throw domain-specific exceptions (e.g., duplicate name, invalid cron expression); these are translated to appropriate HTTP error responses by the framework or middleware.

---

### `public async Task<ActionResult<PipelineResponse>> GetPipeline`

Retrieves a single pipeline by its unique identifier.

- **Parameters:**
  - `string pipelineId` — The pipeline identifier (from route).
- **Returns:**
  - `200 OK` with a `PipelineResponse` body if found.
  - `404 Not Found` if no pipeline matches the given ID.
- **Throws:** No direct exceptions; not-found cases are handled via the return type.

---

### `public async Task<ActionResult<IEnumerable<PipelineResponse>>> ListPipelines`

Returns all pipelines known to the system, optionally filtered or paginated via query parameters.

- **Parameters:**
  - *(query parameters)* Typically `page`, `pageSize`, or status filters as defined by the implementation.
- **Returns:**
  - `200 OK` with an `IEnumerable<PipelineResponse>` body (may be empty).
- **Throws:** No direct exceptions under normal operation.

---

### `public async Task<IActionResult> DeletePipeline`

Removes a pipeline permanently. If the pipeline is currently running, behaviour depends on the service implementation (cancellation may occur).

- **Parameters:**
  - `string pipelineId` — The pipeline identifier (from route).
- **Returns:**
  - `204 No Content` on successful deletion.
  - `404 Not Found` if the pipeline does not exist.
  - `409 Conflict` if the pipeline cannot be deleted in its current state (e.g., protected or locked).
- **Throws:** No direct exceptions; error states are communicated via status codes.

---

### `public async Task<ActionResult<PipelineStatusResponse>> GetPipelineStatus`

Returns the current execution status and progress of a pipeline, including last run timestamp, current state, and any error information.

- **Parameters:**
  - `string pipelineId` — The pipeline identifier (from route).
- **Returns:**
  - `200 OK` with a `PipelineStatusResponse` body.
  - `404 Not Found` if the pipeline does not exist.
- **Throws:** No direct exceptions.

## Usage

### Example 1: Creating and monitoring a pipeline

```csharp
using var client = new HttpClient { BaseAddress = new Uri("https://scheduler.example.com") };

// Create a new pipeline
var createPayload = new
{
    name = "nightly-export",
    cron = "0 2 * * *",
    steps = new[]
    {
        new { type = "extract", config = new { source = "db-primary" } },
        new { type = "transform", config = new { script = "cleanup.sql" } },
        new { type = "load", config = new { destination = "warehouse" } }
    }
};

var createResponse = await client.PostAsJsonAsync("/api/pipelines", createPayload);
createResponse.EnsureSuccessStatusCode();
var pipeline = await createResponse.Content.ReadFromJsonAsync<PipelineResponse>();

// Poll status until completion
PipelineStatusResponse status;
do
{
    var statusResponse = await client.GetAsync($"/api/pipelines/{pipeline!.Id}/status");
    status = await statusResponse.Content.ReadFromJsonAsync<PipelineStatusResponse>();
    await Task.Delay(TimeSpan.FromSeconds(5));
} while (status!.State is "Pending" or "Running");

Console.WriteLine($"Pipeline finished with state: {status.State}");
```

### Example 2: Listing and cleaning up pipelines

```csharp
// Retrieve all pipelines
var listResponse = await client.GetAsync("/api/pipelines?page=1&pageSize=50");
listResponse.EnsureSuccessStatusCode();
var pipelines = await listResponse.Content.ReadFromJsonAsync<IEnumerable<PipelineResponse>>();

// Delete pipelines that have been disabled for more than 30 days
var cutoff = DateTime.UtcNow.AddDays(-30);
foreach (var p in pipelines!)
{
    var statusResponse = await client.GetAsync($"/api/pipelines/{p.Id}/status");
    var status = await statusResponse.Content.ReadFromJsonAsync<PipelineStatusResponse>();

    if (status!.State == "Disabled" && status.LastModified < cutoff)
    {
        var deleteResponse = await client.DeleteAsync($"/api/pipelines/{p.Id}");
        if (deleteResponse.IsSuccessStatusCode)
            Console.WriteLine($"Deleted pipeline {p.Name}");
    }
}
```

## Notes

- **Idempotency:** `DeletePipeline` is idempotent in the sense that deleting a non-existent pipeline yields `404`, but repeating a successful deletion returns `404` rather than `204`. Clients should handle both as successful outcomes.
- **Concurrency:** The controller itself is stateless; thread safety depends on the injected pipeline service. Concurrent modifications to the same pipeline (e.g., simultaneous delete and status requests) may result in race conditions handled at the service layer, typically returning `404` or `409` as appropriate.
- **Partial failures:** `CreatePipeline` may accept a structurally valid request that fails during service-level validation (e.g., unreachable step dependency). Such failures surface as `400 Bad Request` with error details in the response body.
- **Pagination defaults:** When calling `ListPipelines` without explicit pagination parameters, the service applies its own defaults. Clients should not assume all results are returned in a single response.
- **Status polling:** `GetPipelineStatus` reflects a point-in-time snapshot. Long-running pipelines may transition states between polls; clients should implement appropriate retry and backoff strategies.
