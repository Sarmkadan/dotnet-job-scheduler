# HistoryController

The `HistoryController` is an ASP.NET Core controller responsible for providing access to historical execution data within the `dotnet-job-scheduler` system. It exposes endpoints that allow administrators and monitoring services to retrieve detailed, paginated execution logs and aggregated summary statistics for individual jobs or the entire system, ensuring traceability and insight into job performance.

## API

### HistoryController()
Initializes a new instance of the `HistoryController` class.

### GetJobHistory
Retrieves a paginated list of execution records for a specific job.
*   **Parameters**: `string jobId` (the unique identifier of the job), along with pagination parameters (typically page number and page size).
*   **Returns**: An `ActionResult<PagedResult<ExecutionResponse>>` containing the requested page of execution details.
*   **Throws**: May return `NotFound` if the specified `jobId` does not exist.

### GetJobSummary
Retrieves aggregated execution statistics for a specific job.
*   **Parameters**: `string jobId` (the unique identifier of the job).
*   **Returns**: An `ActionResult<JobExecutionSummary>` containing aggregated data such as total runs, success rate, and average duration.
*   **Throws**: May return `NotFound` if the specified `jobId` does not exist.

### GetSystemHistory
Retrieves a paginated list of execution records for all jobs managed by the system.
*   **Parameters**: Pagination parameters (typically page number and page size).
*   **Returns**: An `ActionResult<PagedResult<ExecutionResponse>>` containing the requested page of system-wide execution details.

### GetSystemSummary
Retrieves aggregated execution statistics for the entire scheduler system.
*   **Returns**: An `ActionResult<JobExecutionSummary>` containing high-level aggregated metrics across all configured jobs.

## Usage

### Consuming Job History via HttpClient
```csharp
using System.Net.Http.Json;

public async Task<PagedResult<ExecutionResponse>> FetchHistory(HttpClient client, string jobId)
{
    var response = await client.GetAsync($"/api/history/job/{jobId}?page=1&pageSize=10");
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadFromJsonAsync<PagedResult<ExecutionResponse>>();
}
```

### Retrieving System Summary
```csharp
using System.Net.Http.Json;

public async Task<JobExecutionSummary> GetSystemMetrics(HttpClient client)
{
    var response = await client.GetAsync("/api/history/system/summary");
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadFromJsonAsync<JobExecutionSummary>();
}
```

## Notes

*   **Thread Safety**: As an ASP.NET Core Controller, `HistoryController` instances are typically created per request (Transient lifecycle). Therefore, the controller itself does not maintain state, making it inherently thread-safe in the context of concurrent request handling.
*   **Data Consistency**: The data returned reflects the state of the underlying execution store at the time of the request.
*   **Error Handling**: If a provided `jobId` does not match any record in the system, endpoints will return an HTTP 404 Not Found response.
*   **Pagination**: For endpoints supporting pagination, request parameters exceeding defined system limits may result in default values being applied or validation errors depending on the underlying framework configuration.
