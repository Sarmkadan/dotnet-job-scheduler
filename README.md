<!-- ... existing content ... -->

## PipelinesController

The `PipelinesController` class provides RESTful API endpoints for managing job pipelines. It allows creating, retrieving, listing, deleting, and checking the status of pipelines.

### Usage

```csharp
using JobScheduler.Core.Controllers;
using JobScheduler.Core.Domain.Models;

// Create a new pipeline
var pipelineService = new JobPipelineService();
var pipeline = await pipelineService.CreatePipelineAsync(new CreatePipelineRequest
{
  Jobs = new[] { "job1", "job2", "job3" },
  Name = "My Pipeline"
});

// Get a pipeline by ID
var pipelineResponse = await new PipelinesController(pipelineService).GetPipelineAsync(pipeline.Id);

// List all pipelines
var pipelines = await new PipelinesController(pipelineService).ListPipelinesAsync();

// Delete a pipeline
await new PipelinesController(pipelineService).DeletePipelineAsync(pipeline.Id);

// Get the status of a pipeline
var pipelineStatus = await new PipelinesController(pipelineService).GetPipelineStatusAsync(pipeline.Id);
```

## DashboardController

The `DashboardController` class provides RESTful API endpoints for monitoring and analyzing job scheduler performance metrics. It exposes endpoints for retrieving overview statistics, queue status, performance timelines, health reports, and various job analytics.

### Usage

```csharp
using JobScheduler.Core.Controllers;
using JobScheduler.Core.Domain.Models;

// Create dashboard controller with required services
var jobService = new JobService();
var queueService = new QueueService();
var performanceService = new PerformanceService();
var healthService = new HealthService();

var controller = new DashboardController(
    jobService,
    queueService,
    performanceService,
    healthService
);

// Get dashboard overview with key metrics
var overview = await controller.GetOverview();
Console.WriteLine($"Total Jobs: {controller.TotalJobs}");
Console.WriteLine($"Active Jobs: {controller.ActiveJobs}");
Console.WriteLine($"Average Success Rate: {controller.AverageSuccessRate:P}");
Console.WriteLine($"Last Updated: {controller.LastUpdatedAt}");

// Get queue status
var queueStatus = await controller.GetQueueStatus();
Console.WriteLine($"Pending Jobs: {queueStatus.PendingJobs}");
Console.WriteLine($"Running Jobs: {queueStatus.RunningJobs}");
Console.WriteLine($"Failed Jobs: {queueStatus.FailedJobs}");

// Get performance timeline data
var timeline = await controller.GetPerformanceTimeline();
foreach (var point in timeline)
{
    Console.WriteLine($"{point.Timestamp}: {point.SuccessRate:P} success rate");
}

// Get health report
var health = await controller.GetHealthReport();
Console.WriteLine($"System Status: {health.Status}");
Console.WriteLine($"Uptime: {health.Uptime}");

// Get slowest jobs
var slowestJobs = await controller.GetSlowestJobs();
foreach (var job in slowestJobs.Take(5))
{
    Console.WriteLine($"{job.JobId}: {job.AverageExecutionTimeMs}ms avg");
}

// Get most failing jobs
var failingJobs = await controller.GetMostFailingJobs();
foreach (var job in failingJobs.Take(5))
{
    Console.WriteLine($"{job.JobId}: {job.FailureCount} failures");
}
```

## ExecutionsController

The `ExecutionsController` class provides RESTful API endpoints for accessing job execution history, logs, and detailed execution metrics. It enables tracking of individual execution attempts and failure reasons.

### Usage

```csharp
using JobScheduler.Core.Controllers;
using JobScheduler.Core.Domain.Models;

// Get paginated execution history for a specific job
var controller = new ExecutionsController(
    _schedulerService,
    _statisticsService,
    _logger
);

var executions = await controller.GetJobExecutionsAsync(jobId, pageNumber: 1, pageSize: 20);

// Get a single execution by ID with complete details
var execution = await controller.GetExecutionAsync(executionId);

// Get execution statistics for a specific job including success rates and performance metrics
var stats = await controller.GetJobStatisticsAsync(jobId);

// Get recent failed executions across all jobs for quick failure tracking
var failures = await controller.GetRecentFailuresAsync(days: 7, limit: 50);

// Get execution performance analysis including slowest and fastest runs
var analysis = await controller.GetJobPerformanceAsync(jobId);

// Clear old execution records based on retention policy
await controller.CleanupOldExecutionsAsync(olderThanDays: 90);
```

### Response Types

- `GetJobExecutionsAsync`: Returns `PaginatedResponse<ExecutionResponse>` with execution history
- `GetExecutionAsync`: Returns `ExecutionDetailsResponse` with complete execution details
- `GetJobStatisticsAsync`: Returns `ExecutionStatsResponse` with aggregated statistics
- `GetRecentFailuresAsync`: Returns `List<ExecutionResponse>` with recent failures
- `GetJobPerformanceAsync`: Returns `PerformanceAnalysisResponse` with performance metrics
- `CleanupOldExecutionsAsync`: Returns `CleanupResponse` with cleanup results

### Properties

- `Id`: Unique identifier for the execution
- `JobId`: Identifier of the job this execution belongs to
- `JobName`: Name of the job this execution belongs to
- `Status`: Status of the execution (e.g., "Success", "Failed")
- `StartedAt`: Timestamp when the execution started
- `CompletedAt`: Timestamp when the execution completed
- `ExecutionTimeMs`: Duration of the execution in milliseconds
- `ErrorMessage`: Error message if the execution failed
- `RetryAttempt`: Number of retry attempts made for this execution
- `MaxRetries`: Maximum number of retries configured for the job
- `Output`: Output or result of the execution

## HistoryController

The `HistoryController` class provides RESTful API endpoints for querying job execution history and aggregated statistics. It exposes endpoints for retrieving both per-job and system-wide execution history with flexible filtering and pagination capabilities.

### Usage

```csharp
using JobScheduler.Core.Controllers;
using JobScheduler.Core.Domain.Models;

// Create history controller with required services
var historyService = new JobHistoryService();
var logger = new Logger<HistoryController>(new LoggerFactory());

var controller = new HistoryController(historyService, logger);

// Get paginated execution history for a specific job
var jobHistory = await controller.GetJobHistory(
    jobId: Guid.Parse("123e4567-e89b-12d3-a456-426614174000"),
    status: ExecutionStatus.Success,
    from: DateTime.UtcNow.AddDays(-7),
    to: DateTime.UtcNow,
    pageNumber: 1,
    pageSize: 50
);

// Get aggregated execution statistics for a specific job
var jobSummary = await controller.GetJobSummary(
    jobId: Guid.Parse("123e4567-e89b-12d3-a456-426614174000"),
    from: DateTime.UtcNow.AddDays(-30),
    to: DateTime.UtcNow
);

// Get paginated execution history across all jobs (system-wide)
var systemHistory = await controller.GetSystemHistory(
    status: ExecutionStatus.Failed,
    from: DateTime.UtcNow.AddDays(-1),
    to: DateTime.UtcNow,
    pageNumber: 1,
    pageSize: 20
);

// Get aggregated execution statistics across all jobs (system-wide)
var systemSummary = await controller.GetSystemSummary(
    from: DateTime.UtcNow.AddDays(-7),
    to: DateTime.UtcNow
);
```

### Endpoints

- `GET /api/history/jobs/{jobId}`: Returns paginated execution history for a specific job
- `GET /api/history/jobs/{jobId}/summary`: Returns aggregated statistics for a specific job
- `GET /api/history`: Returns paginated execution history across all jobs
- `GET /api/history/summary`: Returns aggregated statistics across all jobs

### Response Types

- `GetJobHistory`: Returns `PagedResult<ExecutionResponse>` with filtered, paginated execution history for a specific job
- `GetJobSummary`: Returns `JobExecutionSummary` with aggregated execution statistics for a specific job
- `GetSystemHistory`: Returns `PagedResult<ExecutionResponse>` with filtered, paginated execution history across all jobs
- `GetSystemSummary`: Returns `JobExecutionSummary` with aggregated execution statistics across all jobs

### Query Parameters

- `status`: Filter by execution status (Success, Failed, Running, etc.)
- `from`: Start date for filtering execution records
- `to`: End date for filtering execution records
- `pageNumber`: Page number for pagination (default: 1)
- `pageSize`: Number of items per page (default: 20)

<!-- ... rest of README content -->