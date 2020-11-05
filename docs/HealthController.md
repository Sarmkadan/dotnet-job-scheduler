# HealthController

The `HealthController` is an ASP.NET Core controller that exposes endpoints for monitoring the liveness, readiness, and detailed health status of the `dotnet-job-scheduler` application. It also provides read‑only properties that reflect the current state of various subsystems (database, jobs, executions, memory, etc.) as part of the health responses.

## API

### HealthController()
Initializes a new instance of the `HealthController` class. The controller relies on dependency injection to obtain any required services (e.g., health check services). It has no explicit parameters and does not throw exceptions during construction.

### GetLiveness()
```csharp
public IActionResult GetLiveness()
```
**Purpose:** Returns a simple liveness indicator to confirm that the application process is running.  
**Parameters:** None.  
**Return Value:** An `IActionResult` that yields `200 OK` when the process is alive; otherwise returns an appropriate error status (e.g., `500 Internal Server Error`) if an unexpected exception occurs.  
**Throws:** May propagate any unhandled exception from the hosting pipeline; the method itself does not throw directly.

### GetReadiness()
```csharp
public async Task<IActionResult> GetReadiness()
```
**Purpose:** Checks whether the application is ready to serve requests by verifying critical dependencies (e.g., database connectivity).  
**Parameters:** None.  
**Return Value:** A `Task<IActionResult>` that yields `200 OK` when all readiness checks pass, or `503 Service Unavailable` if any check fails.  
**Throws:** Propagates exceptions from asynchronous dependency checks; callers should treat unexpected exceptions as service unavailability.

### GetStatus()
```csharp
public async Task<ActionResult<HealthStatusResponse>> GetStatus()
```
**Purpose:** Provides a comprehensive health status report, including version, timestamps, and subsystem states.  
**Parameters:** None.  
**Return Value:** A `Task<ActionResult<HealthStatusResponse>>` containing a `HealthStatusResponse` payload wrapped in an `ActionResult`. On success, returns `200 OK` with the payload; on failure, returns an appropriate error status (e.g., `500 Internal Server Error`).  
**Throws:** May throw exceptions from the underlying health assessment logic; these are caught by the ASP.NET Core framework and transformed into error responses.

### GetDiagnostics()
```csharp
public async Task<ActionResult<DiagnosticsResponse>> GetDiagnostics()
```
**Purpose:** Returns detailed diagnostic information useful for troubleshooting, such as resource usage and internal counters.  
**Parameters:** None.  
**Return Value:** A `Task<ActionResult<DiagnosticsResponse>>` containing a `DiagnosticsResponse` payload. Successful calls yield `200 OK` with the payload; errors yield `500 Internal Server Error`.  
**Throws:** Similar to `GetStatus`, exceptions from diagnostic gathering are translated into HTTP error responses by the framework.

### Timestamp
```csharp
public DateTime Timestamp { get; }
```
**Purpose:** The UTC date and time when the health data was last sampled.  
**Parameters:** None.  
**Return Value:** A `DateTime` value.  
**Throws:** Property getters do not throw; the value is set during health evaluation and is immutable thereafter.

### Version
```csharp
public string Version { get; }
```
**Purpose:** The version identifier of the running application (e.g., assembly version or Git SHA).  
**Parameters:** None.  
**Return Value:** A string representing the version.  
**Throws:** None.

### Status
```csharp
public string Status { get; }
```
**Purpose:** A high‑level health status string (e.g., `"Healthy"`, `"Degraded"`, `"Unhealthy"`).  
**Parameters:** None.  
**Return Value:** A string; may be empty or `null` if not yet evaluated.  
**Throws:** None.

### Database
```csharp
public DatabaseStatus Database { get; }
```
**Purpose:** Encapsulates the health state of the configured database connection.  
**Parameters:** None.  
**Return Value:** An instance of `DatabaseStatus` containing properties such as `Available`, `LastChecked`, and `ErrorMessage`.  
**Throws:** None.

### Jobs
```csharp
public JobsStatus Jobs { get; }
```
**Purpose:** Provides status information about scheduled jobs (e.g., counts, success rates).  
**Parameters:** None.  
**Return Value:** An instance of `JobsStatus`.  
**Throws:** None.

### Executions
```csharp
public ExecutionsStatus Executions { get; }
```
**Purpose:** Reflects the state of job executions (active, completed, failed).  
**Parameters:** None.  
**Return Value:** An instance of `ExecutionsStatus`.  
**Throws:** None.

### Memory
```csharp
public MemoryStatus Memory { get; }
```
**Purpose:** Shows memory‑related metrics for the process.  
**Parameters:** None.  
**Return Value:** An instance of `MemoryStatus`.  
**Throws:** None.

### Available
```csharp
public bool Available { get; }
```
**Purpose:** Indicates whether the overall service is considered available based on the latest health checks.  
**Parameters:** None.  
**Return Value:** `true` if available, `false` otherwise.  
**Throws:** None.

### LastChecked
```csharp
public DateTime LastChecked { get; }
```
**Purpose:** The timestamp of the most recent health check execution.  
**Parameters:** None.  
**Return Value:** A `DateTime` value.  
**Throws:** None.

### ErrorMessage
```csharp
public string? ErrorMessage { get; }
```
**Purpose:** Contains a descriptive message when the health status is unhealthy or degraded; otherwise `null`.  
**Parameters:** None.  
**Return Value:** A string or `null`.  
**Throws:** None.

### TotalCount
```csharp
public int TotalCount { get; }
```
**Purpose:** The total number of jobs (or executions) known to the system.  
**Parameters:** None.  
**Return Value:** An integer count.  
**Throws:** None.

### ActiveCount
```csharp
public int ActiveCount { get; }
```
**Purpose:** The number of jobs or executions currently active (running).  
**Parameters:** None.  
**Return Value:** An integer count.  
**Throws:** None.

### SuccessRate
```csharp
public double SuccessRate { get; }
```
**Purpose:** The proportion of successful executions expressed as a value between `0.0` and `1.0`.  
**Parameters:** None.  
**Return Value:** A double representing the success rate.  
**Throws:** None.

### UsageMb
```csharp
public long UsageMb { get; }
```
**Purpose:** Approximate memory usage of the process in megabytes.  
**Parameters:** None.  
**Return Value:** A long integer.  
**Throws:** None.

## Usage

### Example 1: Checking liveness with HttpClient
```csharp
using var client = new HttpClient();
var response = await client.GetAsync("/health/liveness");
response.EnsureSuccessStatusCode(); // throws if not 2xx
var content = await response.Content.ReadAsStringAsync();
// content typically empty or a simple OK payload
```

### Example 2: Instantiating the controller and reading health properties (unit‑test or middleware scenario)
```csharp
// Assuming dependencies are supplied via a test harness or mock factory
var controller = new HealthController(); // DI would provide required services in real use

var livenessResult = controller.GetLiveness();
if (livenessResult is OkResult)
{
    // Liveness OK
}

var statusResult = await controller.GetStatus();
if (statusResult.Result is OkObjectResult ok && ok.Value is HealthStatusResponse health)
{
    Console.WriteLine($"Version: {health.Version}");
    Console.WriteLine($"Overall status: {health.Status}");
    Console.WriteLine($"Database available: {health.Database.Available}");
    Console.WriteLine($"Job success rate: {health.Jobs.SuccessRate:P2}");
}
```

## Notes
- The controller is designed to be stateless per request; all health‑related properties are populated during the execution of the health check methods and remain immutable for the lifetime of the object. Consequently, reading these properties after a health check is thread‑safe, but concurrent writes to the underlying services (e.g., updating job counts) are managed by those services, not by the controller itself.
- If any subsystem check throws an exception, the corresponding action method (`GetStatus` or `GetDiagnostics`) will return an error HTTP status; the property getters will not throw.
- The `TotalCount` property appears twice in the source listing; it represents a single read‑only integer reflecting the total number of jobs/executions.
- `ErrorMessage` will be non‑null only when `Status` indicates a non‑healthy condition; otherwise it is `null`.
- Consumers should treat a `503` response from `GetReadiness` as a signal that the service is temporarily unable to handle traffic, while a non‑2xx from `GetLiveness` indicates a fundamental process failure.
