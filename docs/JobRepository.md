# JobRepository
The `JobRepository` provides asynchronous query methods for retrieving `Job` entities from the underlying data store. It wraps a `JobSchedulerContext` and exposes specialized look‑ups that are commonly needed by the scheduler service (e.g., fetching jobs by name, status, priority, or execution state).

## API
### Constructor
```csharp
public JobRepository(JobSchedulerContext context) : base(context)
```
* **Parameters**  
  * `context` – The Entity Framework Core `DbContext` used to access the `Jobs` table. Must not be `null`.
* **Exceptions**  
  * `ArgumentNullException` – Thrown when `context` is `null`.

### GetByNameAsync
```csharp
public async Task<Job?> GetByNameAsync(string name)
```
* **Purpose** – Retrieves a single job whose `Name` matches the supplied value.
* **Parameters**  
  * `name` – The exact name of the job to look up. Must not be `null` or whitespace.
* **Return Value** – The matching `Job` instance, or `null` if no job with that name exists.
* **Exceptions**  
  * `ArgumentException` – Thrown when `name` is `null`, empty, or consists only of white‑space.  
  * `ObjectDisposedException` – Thrown if the underlying `context` has been disposed.

### GetActiveJobsAsync
```csharp
public async Task<IEnumerable<Job>> GetJobsByStatusAsync(JobStatus status)
```
* **Purpose** – Returns all jobs whose `Status` equals the supplied `JobStatus` value.
* **Parameters**  
  * `status` – The status to filter by (e.g., `JobStatus.Running`, `JobStatus.Pending`).
* **Return Value** – An enumerable collection of `Job` objects matching the status. May be empty but never `null`.
* **Exceptions**  
  * `InvalidOperationException` – Thrown if the repository’s context is unable to execute the query (e.g., connection failure).  

### GetJobsByStatusAsync
```csharp
public async Task<IEnumerable<Job>> GetJobsByStatusAsync(JobStatus status)
```
* **Purpose** – Returns all jobs whose `Status` equals the supplied `JobStatus` value.
* **Parameters**  
  * `status` – The status to filter by (e.g., `JobStatus.Running`, `JobStatus.Pending`).
* **Return Value** – An enumerable collection of `Job` objects matching the status. May be empty but never `null`.
* **Exceptions**  
  * `InvalidOperationException` – Thrown if the repository’s context is unable to execute the query (e.g., connection failure).  

### GetJobsByPriorityAsync
```csharp
public async Task<IEnumerable<Job>> GetJobsByPriorityAsync(int priority)
```
* **Purpose** – Returns all jobs that have the specified priority level.
* **Parameters**  
  * `priority` – The priority value to match. Expected to be within the range defined by the scheduler (typically 0‑10). Values outside this range are still accepted but will likely return an empty set.
* **Return Value** – An enumerable collection of `Job` objects with the given priority. May be empty but never `null`.
* **Exceptions**  
  * `ArgumentOutOfRangeException` – Thrown if `priority` is negative and the domain model treats negative values as invalid.  
  * `InvalidOperationException` – Thrown on query execution failures.

### GetScheduledJobsForExecutionAsync
```csharp
public async Task<IEnumerable<Job>> GetScheduledJobsForExecutionAsync()
```
* **Purpose** – Returns jobs that are ready to be executed now, based on their `NextRunUtc` timestamp and current status (typically those with `NextRunUtc <= DateTime.UtcNow` and a status indicating they are eligible to run).
* **Parameters** – None.
* **Return Value** – An enumerable collection of `Job` instances awaiting execution. May be empty but never `null`.
* **Exceptions**  
  * `InvalidOperationException` – Thrown if the underlying query cannot be executed (e.g., due to a disposed context).

### GetFailedJobsAsync
```csharp
public async Task<IEnumerable<Job>> GetFailedJobsAsync()
```
* **Purpose** – Returns all jobs whose `Status` is `Failed`.
* **Parameters** – None.
* **Return Value** – An enumerable collection of `Job` objects that have failed. May be empty but never `null`.
* **Exceptions**  
  * `InvalidOperationException` – Thrown on query execution failures.

### GetLongRunningJobsAsync
```csharp
public async Task<IEnumerable<Job>> GetLongRunningJobsAsync(TimeSpan threshold)
```
* **Purpose** – Returns jobs that have been running longer than the supplied `threshold`.
* **Parameters**  
  * `threshold` – The minimum duration a job must have been executing to be considered “long running”. Must be greater than `TimeSpan.Zero`.
* **Return Value** – An enumerable collection of `Job` objects whose execution time exceeds `threshold`. May be empty but never `null`.
* **Exceptions**  
  * `ArgumentOutOfRangeException` – Thrown when `threshold` is less than or equal to `TimeSpan.Zero`.  
  * `InvalidOperationException` – Thrown on query execution failures.

### GetJobsWithoutRecentExecutionAsync
```csharp
public async Task<IEnumerable<Job>> GetJobsWithoutRecentExecutionAsync(TimeSpan threshold)
```
* **Purpose** – Returns jobs that have not been executed recently, based on their `LastRunUtc` timestamp.
* **Parameters**  
  * `threshold` – The maximum age of the last execution to be considered “recent”. Jobs whose `LastRunUtc` is older than `UtcNow - threshold` (or have never run) are returned. Must be greater than `TimeSpan.Zero`.
* **Return Value** – An enumerable collection of `Job` objects that have not run within the threshold period. May be empty but never `null`.
* **Exceptions**  
  * `ArgumentOutOfRangeException` – Thrown when `threshold` is less than or equal to `TimeSpan.Zero`.  
  * `InvalidOperationException` – Thrown on query execution failures.

## Usage
### Example 1: Retrieve a job by name and update its priority
```csharp
public async Task UpdateJobPriorityAsync(string jobName, int newPriority)
{
    await using var scope = _serviceFactory.CreateAsyncScope();
    var repo = scope.ServiceProvider.GetRequiredService<JobRepository>();

    var job = await repo.GetByNameAsync(jobName)
                        ?? throw new InvalidOperationException($"Job '{jobName}' not found.");

    job.Priority = newPriority;
    await scope.ServiceProvider.GetRequiredService<JobSchedulerContext>().SaveChangesAsync();
}
```

### Example 2: Process all active jobs that are due for execution
```csharp
public async Task ProcessDueJobsAsync()
{
    await using var scope = _serviceFactory.CreateAsyncScope();
    var repo = scope.ServiceProvider.GetRequiredService<JobRepository>();

    var dueJobs = await repo.GetScheduledJobsForExecutionAsync();

    foreach (var job in dueJobs)
    {
        try
        {
            await _jobExecutor.ExecuteAsync(job);
        }
        catch (Exception ex)
        {
            // Log failure; the repository can later be queried for failed jobs.
            _logger.LogError(ex, "Job {JobId} failed during execution.", job.Id);
        }
    }
}
```

## Notes
* The repository does **not** maintain any internal state beyond the injected `JobSchedulerContext`. Consequently, it is **not thread‑safe**; the same instance should not be used concurrently from multiple threads without external synchronization. Each asynchronous method relies on the underlying `DbContext`, which itself is not thread‑safe.
* All query methods return `IEnumerable<Job>` (or a nullable single `Job`) and never return `null` for the enumerable type—empty sequences are used to indicate the absence of matching data.
* Parameter validation is performed where applicable (e.g., null or empty strings, non‑positive time spans). Invalid arguments result in `ArgumentException` or `ArgumentOutOfRangeException`.
* If the underlying `JobSchedulerContext` has been disposed or encounters a connectivity issue, the methods will propagate an `ObjectDisposedException` or `InvalidOperationException`. Consumers should handle these exceptions according to their error‑handling strategy.
* The repository does **not** accept a `CancellationToken` in its public signatures; callers wishing to cancel operations must rely on the timeout or cancellation mechanisms of the surrounding infrastructure (e.g., hosting lifetime, Polly policies, or manual task cancellation).
