# JobHistoryService

`JobHistoryService` provides read‑only access to execution history data for both user‑defined jobs and internal system jobs managed by the **dotnet‑job‑scheduler**. It exposes asynchronous query methods that return paged results and summary information, allowing callers to retrieve historical execution records, filter by page, and obtain aggregate statistics.

## API

### `public JobHistoryService`

**Purpose**  
Constructs a new instance of the service. The concrete constructor parameters are not part of the public API surface; they are typically injected by the DI container with the required repositories and logging services.

**Exceptions**  
Throws `ArgumentNullException` if any required dependency is `null`.

---

### `public async Task<PagedResult<ExecutionResponse>> GetJobHistoryAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)`

**Purpose**  
Retrieves a paged list of `ExecutionResponse` objects representing the execution history of a specific user‑defined job.

**Parameters**  
| Name | Type | Description |
|------|------|-------------|
| `pageNumber` | `int` | 1‑based index of the page to retrieve. Must be greater than 0. |
| `pageSize` | `int` | Number of items per page. Must be greater than 0. |
| `cancellationToken` | `CancellationToken` | Optional token to cancel the operation. |

**Return Value**  
A `PagedResult<ExecutionResponse>` containing the requested page of execution records, together with pagination metadata (`Items`, `TotalCount`, `PageNumber`, `PageSize`).

**Exceptions**  
* `ArgumentOutOfRangeException` – if `pageNumber` or `pageSize` are less than 1.  
* `OperationCanceledException` – if the operation is cancelled via the token.  
* `DataAccessException` – if the underlying data store cannot be queried.

---

### `public async Task<PagedResult<ExecutionResponse>> GetSystemHistoryAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)`

**Purpose**  
Retrieves a paged list of `ExecutionResponse` objects for internal system jobs (e.g., housekeeping, cleanup tasks).

**Parameters** – identical to `GetJobHistoryAsync`.

**Return Value** – a `PagedResult<ExecutionResponse>` with system‑job records.

**Exceptions** – same as `GetJobHistoryAsync`.

---

### `public async Task<JobExecutionSummary> GetJobSummaryAsync(Guid jobId, CancellationToken cancellationToken = default)`

**Purpose**  
Provides aggregate statistics for a specific user‑defined job, such as total runs, success/failure counts, and average duration.

**Parameters**  
| Name | Type | Description |
|------|------|-------------|
| `jobId` | `Guid` | Identifier of the job whose summary is requested. |
| `cancellationToken` | `CancellationToken` | Optional cancellation token. |

**Return Value**  
A `JobExecutionSummary` instance containing the calculated metrics.

**Exceptions**  
* `ArgumentException` – if `jobId` is `Guid.Empty`.  
* `KeyNotFoundException` – if no history exists for the supplied `jobId`.  
* `OperationCanceledException` – if cancelled.  
* `DataAccessException` – on data‑layer failures.

---

### `public async Task<JobExecutionSummary> GetSystemSummaryAsync(CancellationToken cancellationToken = default)`

**Purpose**  
Returns aggregate statistics for all system‑managed jobs.

**Parameters**  
| Name | Type | Description |
|------|------|-------------|
| `cancellationToken` | `CancellationToken` | Optional cancellation token. |

**Return Value**  
A `JobExecutionSummary` covering system jobs.

**Exceptions** – same as `GetJobSummaryAsync` (except `KeyNotFoundException` is not applicable).

---

### `public IReadOnlyList<T> Items` *(member of `PagedResult<T>`)*

**Purpose**  
The collection of items on the current page.

**Remarks**  
The list is read‑only; attempts to modify it will throw `NotSupportedException`.

---

### `public int TotalCount` *(member of `PagedResult<T>`)*

**Purpose**  
Total number of items across all pages for the query.

---

### `public int PageNumber` *(member of `PagedResult<T>`)*

**Purpose**  
The 1‑based index of the page represented by this `PagedResult`.

---

### `public int PageSize` *(member of `PagedResult<T>`)*

**Purpose**  
Number of items that each page is expected to contain (except possibly the last page).

---

### `public PagedResult<T>` *(generic type)*

**Purpose**  
Encapsulates a single page of results together with pagination metadata. Used as the return type for the two history‑retrieval methods.

**Members**  
* `IReadOnlyList<T> Items` – items on the page.  
* `int TotalCount` – total items across all pages.  
* `int PageNumber` – current page index.  
* `int PageSize` – size of each page.

---

## Usage

### Example 1 – Retrieve the first page of a specific job’s history

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using DotNetJobScheduler.Services;

public class JobHistoryDemo
{
    private readonly JobHistoryService _historyService;

    public JobHistoryDemo(JobHistoryService historyService)
    {
        _historyService = historyService ?? throw new ArgumentNullException(nameof(historyService));
    }

    public async Task ShowFirstPageAsync(Guid jobId, CancellationToken ct = default)
    {
        // Get the first 20 executions for the given job
        var page = await _historyService.GetJobHistoryAsync(pageNumber: 1, pageSize: 20, cancellationToken: ct);

        Console.WriteLine($"Job {jobId} – page {page.PageNumber}/{(int)Math.Ceiling((double)page.TotalCount / page.PageSize)}");
        foreach (var exec in page.Items)
        {
            Console.WriteLine($"{exec.StartedAt:u} – {(exec.Succeeded ? "Success" : "Failure")}");
        }
    }
}
```

### Example 2 – Display a summary of system‑job activity

```csharp
using System;
using System.Threading.Tasks;
using DotNetJobScheduler.Services;

public static class SystemHistoryReport
{
    public static async Task PrintAsync(JobHistoryService historyService)
    {
        // Obtain aggregate statistics for all system jobs
        JobExecutionSummary summary = await historyService.GetSystemSummaryAsync();

        Console.WriteLine("System Job Summary");
        Console.WriteLine($"  Total Runs      : {summary.TotalRuns}");
        Console.WriteLine($"  Successful Runs : {summary.SuccessCount}");
        Console.WriteLine($"  Failed Runs     : {summary.FailureCount}");
        Console.WriteLine($"  Avg. Duration   : {summary.AverageDuration:g}");
    }
}
```

## Notes

* **Thread‑safety** – All public methods are safe to call concurrently. Internally the service uses read‑only queries against the data store; no mutable state is kept per call. Constructors may capture scoped dependencies, so the lifetime of the service should respect the DI container’s scope (e.g., registered as `Scoped`).

* **Paging edge cases**  
  * If `pageNumber` exceeds the total number of pages, the returned `Items` collection will be empty while `TotalCount`, `PageNumber`, and `PageSize` still reflect the request.  
  * `TotalCount` may be zero, in which case `Items` is an empty list and `PageNumber` will be the value supplied by the caller.

* **Cancellation** – Passing a `CancellationToken` that is already cancelled will cause the method to complete synchronously with an `OperationCanceledException`. The token is observed before any I/O operation begins.

* **Performance** – The service performs its queries asynchronously and relies on the underlying repository to apply pagination at the data‑source level (e.g., SQL `OFFSET/FETCH`). Ensure that indexes exist on the columns used for ordering (typically `StartedAt`) to avoid full‑table scans when retrieving large histories.

* **Error handling** – Consumers should anticipate `DataAccessException` (or a derived type) for transient failures and may implement retry logic. Validation errors (`ArgumentOutOfRangeException`, `ArgumentException`) are thrown immediately and do not involve I/O.
