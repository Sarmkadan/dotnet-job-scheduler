# JobsController

`JobsController` is the primary API controller responsible for managing the lifecycle of scheduled jobs within the `dotnet-job-scheduler` system. It exposes endpoints to create, retrieve, update, delete, suspend, resume, and manually trigger jobs, as well as to query execution history. The controller also surfaces contextual information through its `Reason` property and provides paginated list responses via its `Data`, `TotalCount`, `PageNumber`, and `PageSize` members.

## API

### public JobsController

The default constructor for the controller. Initializes a new instance of `JobsController` with its required dependencies injected by the framework.

### public async Task<ActionResult<JobResponse>> CreateJob

Creates a new job definition and persists it to the scheduler store.

- **Parameters:** A job creation request model (bound from the request body).
- **Returns:** `ActionResult<JobResponse>` containing the newly created job's details on success, or an appropriate error response if validation fails or a conflict occurs.
- **Throws:** May throw if the underlying persistence layer encounters an unrecoverable error.

### public async Task<ActionResult<JobResponse>> GetJob

Retrieves a single job by its unique identifier.

- **Parameters:** The job ID, typically provided as a route parameter.
- **Returns:** `ActionResult<JobResponse>` with the job details if found; `NotFound` if the ID does not correspond to an existing job.
- **Throws:** Does not throw for missing resources; exceptions are limited to unexpected infrastructure failures.

### public async Task<ActionResult<PaginatedResponse<JobResponse>>> ListJobs

Returns a paginated list of all registered jobs.

- **Parameters:** Query string parameters for pagination (page number, page size) and optional filtering criteria.
- **Returns:** `ActionResult<PaginatedResponse<JobResponse>>` where the response body includes `Data` (the list of jobs), `TotalCount`, `PageNumber`, and `PageSize`.
- **Throws:** May throw if the underlying data query fails due to connectivity or serialization issues.

### public async Task<ActionResult<JobResponse>> UpdateJob

Updates the configuration of an existing job.

- **Parameters:** The job ID (route parameter) and an update request model (request body) containing the fields to modify.
- **Returns:** `ActionResult<JobResponse>` with the updated job representation on success; `NotFound` if the job does not exist; validation error responses if the update payload is invalid.
- **Throws:** May throw if a concurrency conflict is detected and not handled gracefully by the persistence layer.

### public async Task<IActionResult> DeleteJob

Permanently removes a job and its associated schedule from the system.

- **Parameters:** The job ID to delete.
- **Returns:** `IActionResult` — typically `NoContent` on successful deletion or `NotFound` if the job does not exist.
- **Throws:** May throw if cascading deletion of related execution records fails.

### public async Task<ActionResult<JobResponse>> SuspendJob

Pauses a job so that it will not execute according to its schedule until resumed.

- **Parameters:** The job ID to suspend.
- **Returns:** `ActionResult<JobResponse>` reflecting the job's updated status (suspended). Returns `NotFound` if the job does not exist; may return a conflict response if the job is already suspended.
- **Throws:** Throws only on unexpected infrastructure failures.

### public async Task<ActionResult<JobResponse>> ResumeJob

Resumes a previously suspended job, allowing it to execute according to its schedule again.

- **Parameters:** The job ID to resume.
- **Returns:** `ActionResult<JobResponse>` with the job's status updated to active. Returns `NotFound` if the job does not exist; may return a conflict response if the job is not currently suspended.
- **Throws:** Throws only on unexpected infrastructure failures.

### public async Task<ActionResult<ExecutionResponse>> TriggerJobExecution

Manually triggers an immediate, one-off execution of a job, independent of its regular schedule.

- **Parameters:** The job ID to execute.
- **Returns:** `ActionResult<ExecutionResponse>` containing details of the initiated execution. Returns `NotFound` if the job does not exist; may return a conflict response if the job is suspended or already executing.
- **Throws:** May throw if the execution engine fails to accept the trigger command.

### public async Task<ActionResult<IEnumerable<ExecutionResponse>>> GetJobExecutionHistory

Retrieves the execution history for a specific job.

- **Parameters:** The job ID and optional query parameters for time range filtering or result filtering.
- **Returns:** `ActionResult<IEnumerable<ExecutionResponse>>` with a collection of past execution records. Returns an empty collection if no history exists; `NotFound` if the job itself does not exist.
- **Throws:** Throws only on unexpected data access failures.

### public string? Reason

A string property that holds a human-readable explanation for the outcome of the last operation, such as a validation failure reason or a conflict description. It is `null` when no explanatory context is available.

### public List<T> Data

The generic list property used within `PaginatedResponse<JobResponse>` to carry the page of job records returned by `ListJobs`. It is populated only within the paginated response context.

### public int TotalCount

The total number of jobs matching the current query, used in `PaginatedResponse<JobResponse>` to allow clients to calculate total pages. It is populated only within the paginated response context.

### public int PageNumber

The current page number (1-based) reflected in the paginated response from `ListJobs`.

### public int PageSize

The maximum number of items per page as requested or defaulted in the paginated response from `ListJobs`.

## Usage

### Example 1: Creating a Job and Triggering Immediate Execution

```csharp
// Assume _client is an HttpClient pointed at the scheduler API base address.
var createPayload = new
{
    Name = "NightlyReport",
    CronExpression = "0 0 2 * * ?",
    Endpoint = "https://internal-api/reports/generate",
    HttpMethod = "POST"
};

var createResponse = await _client.PostAsJsonAsync("/api/jobs", createPayload);
createResponse.EnsureSuccessStatusCode();

var createdJob = await createResponse.Content.ReadFromJsonAsync<JobResponse>();

// Immediately trigger the newly created job for a test run.
var triggerResponse = await _client.PostAsync($"/api/jobs/{createdJob.Id}/trigger", null);
triggerResponse.EnsureSuccessStatusCode();

var execution = await triggerResponse.Content.ReadFromJsonAsync<ExecutionResponse>();
Console.WriteLine($"Triggered execution ID: {execution.Id}, Status: {execution.Status}");
```

### Example 2: Listing Jobs with Pagination and Suspending a Misbehaving Job

```csharp
// Fetch the second page of jobs with 10 items per page.
var listResponse = await _client.GetAsync("/api/jobs?page=2&pageSize=10");
listResponse.EnsureSuccessStatusCode();

var paginated = await listResponse.Content.ReadFromJsonAsync<PaginatedResponse<JobResponse>>();
Console.WriteLine($"Showing page {paginated.PageNumber} of {Math.Ceiling(paginated.TotalCount / (double)paginated.PageSize)}");

foreach (var job in paginated.Data)
{
    Console.WriteLine($"Job: {job.Name}, Status: {job.Status}");
}

// Suspend a specific job that is causing issues.
var targetJobId = paginated.Data.First(j => j.Name == "ProblematicSync").Id;
var suspendResponse = await _client.PostAsync($"/api/jobs/{targetJobId}/suspend", null);

if (suspendResponse.IsSuccessStatusCode)
{
    var suspendedJob = await suspendResponse.Content.ReadFromJsonAsync<JobResponse>();
    Console.WriteLine($"Job '{suspendedJob.Name}' is now {suspendedJob.Status}");
}
else if (suspendResponse.StatusCode == System.Net.HttpStatusCode.Conflict)
{
    // Read the Reason property from the error response body for details.
    var error = await suspendResponse.Content.ReadFromJsonAsync<ProblemDetails>();
    Console.WriteLine($"Conflict: {error?.Detail}");
}
```

## Notes

- **Idempotency:** `SuspendJob` and `ResumeJob` are not idempotent by design; suspending an already suspended job or resuming an active job will typically result in a conflict response. Clients should inspect the `Reason` property on error responses to determine the specific cause.
- **Pagination defaults:** When calling `ListJobs` without explicit pagination parameters, the controller applies default values for `PageNumber` and `PageSize`. The `TotalCount` reflects all matching records, not just the current page, enabling accurate client-side pagination controls.
- **Concurrency:** `UpdateJob` operates on a last-write-wins basis unless an explicit concurrency token (e.g., ETag) is implemented by the underlying store. Clients retrieving a job via `GetJob` and then calling `UpdateJob` may overwrite intervening changes without warning.
- **Execution history ordering:** `GetJobExecutionHistory` returns executions in reverse chronological order by default. The collection may be empty for newly created jobs that have never run.
- **Thread safety:** The controller itself is stateless beyond the scoped lifetime of a request. The `Reason` property is set per-operation and is not shared across requests; no thread-safety concerns exist at the controller level. Thread safety for the `Data`, `TotalCount`, `PageNumber`, and `PageSize` members is irrelevant as they are populated once per paginated response and returned immediately.
- **Cascading delete:** `DeleteJob` removes the job definition. Depending on the store configuration, associated execution history records may be cascade-deleted or retained for audit purposes. Clients should not assume history is preserved after job deletion.
