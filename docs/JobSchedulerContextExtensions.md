# JobSchedulerContextExtensions

`JobSchedulerContextExtensions` provides a set of static extension methods designed to simplify interactions with job scheduler database contexts. These methods streamline common operations, including identifying the next pending job, retrieving historical execution data, initializing new execution records, and aggregating performance metrics for individual jobs.

## API

### Extension Methods

- **`public static async Task<Job?> FindNextExecutableJobAsync`**
  Searches the database context for the next job scheduled for immediate execution. Returns a `Job` object if a pending job is found; otherwise, returns `null`.

- **`public static async Task<List<Job>> GetJobsWithRecentExecutionsAsync`**
  Retrieves a list of `Job` entities that have reported execution activity within the recent timeframe defined by the scheduler's configuration.

- **`public static async Task<JobExecution> CreateJobExecutionAsync`**
  Initializes and persists a new `JobExecution` record to track the lifecycle of a job run.

- **`public static async Task<JobExecutionStats> GetJobExecutionStatsAsync`**
  Calculates and returns the aggregated performance statistics for a specific job, represented by a `JobExecutionStats` instance.

### JobExecutionStats

The `JobExecutionStats` class provides a snapshot of a job's performance based on its execution history:

- **`Guid JobId`**: The unique identifier of the job.
- **`int TotalExecutions`**: Total count of attempted executions.
- **`int SuccessfulExecutions`**: Count of completed executions marked as successful.
- **`int FailedExecutions`**: Count of completed executions marked as failed.
- **`double SuccessRate`**: The calculated ratio of successful executions to total executions.
- **`double? AverageDurationMs`**: The average duration of successful executions in milliseconds, if applicable.
- **`DateTime? LastExecutionTime`**: The timestamp of the most recent execution attempt.
- **`DateTime? LastSuccessTime`**: The timestamp of the most recent successful execution.
- **`DateTime? LastFailureTime`**: The timestamp of the most recent failed execution.
- **`ExecutionMetrics? CurrentMetrics`**: The current performance metrics captured for the job.

## Usage

### Example 1: Finding and Starting a Job
```csharp
using var context = new JobSchedulerDbContext();
var job = await context.FindNextExecutableJobAsync();

if (job != null)
{
    var execution = await context.CreateJobExecutionAsync(job.Id);
    // Proceed with job execution logic
}
```

### Example 2: Retrieving Job Statistics
```csharp
using var context = new JobSchedulerDbContext();
var stats = await context.GetJobExecutionStatsAsync(jobId);

Console.WriteLine($"Job: {stats.JobId}, Success Rate: {stats.SuccessRate:P}");
if (stats.LastFailureTime.HasValue)
{
    Console.WriteLine($"Last failure occurred at: {stats.LastFailureTime.Value}");
}
```

## Notes

- **Thread Safety**: These extension methods rely on the underlying `DbContext` instance. As `DbContext` is not thread-safe, ensure that each operation is performed using a properly scoped context instance. Do not share context instances across multiple threads or concurrent asynchronous operations.
- **Exceptions**: These methods may throw database-related exceptions (e.g., connection issues or concurrency conflicts) if the underlying data store is unreachable or if data integrity constraints are violated.
- **Data Consistency**: Because these methods perform asynchronous operations on a database, results reflect the state of the data at the time the query is executed. In a distributed environment, the application state may change immediately after the method returns.
