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

## JobsController

The `JobsController` class provides RESTful API endpoints for managing job CRUD operations, lifecycle management, and status updates. It handles job scheduling, configuration, and execution control with comprehensive validation and error handling.

### Usage

```csharp
using JobScheduler.Core.Controllers;
using JobScheduler.Core.Domain.Models;
using Microsoft.Extensions.Logging;

// Create required services
var schedulerService = new JobSchedulerService();
var logger = new Logger<JobsController>(new LoggerFactory());

// Create controller instance
var controller = new JobsController(schedulerService, logger);

// Create a new job with cron schedule
var newJob = new CreateJobRequest
{
    Name = "Data Backup Job",
    Description = "Daily database backup at midnight",
    CronExpression = "0 0 * * *",
    HandlerType = "DatabaseBackupHandler",
    HandlerParameters = "{\"backupPath\": \"/backups\"}",
    Priority = 1,
    MaxRetries = 3,
    RetryBackoffSeconds = 60,
    ExecutionTimeoutSeconds = 3600,
    MaxConcurrentExecutions = 1
};

var createdJob = await controller.CreateJob(newJob);
Console.WriteLine($"Created job: {createdJob.Value.Name} with ID: {createdJob.Value.Id}");

// Get a specific job by ID
var jobResponse = await controller.GetJob(createdJob.Value.Id);
Console.WriteLine($"Job status: {jobResponse.Value.Status}, Next execution: {jobResponse.Value.NextExecutionAt}");

// List all jobs with pagination
var allJobs = await controller.ListJobs(pageNumber: 1, pageSize: 10);
Console.WriteLine($"Total jobs: {allJobs.Value.TotalCount}");

// Update a job's configuration
var updateRequest = new CreateJobRequest
{
    Name = "Data Backup Job - Updated",
    Description = "Daily database backup at midnight with encryption",
    CronExpression = "0 0 * * *",
    HandlerType = "DatabaseBackupHandler",
    HandlerParameters = "{\"backupPath\": \"/encrypted-backups\", \"enableEncryption\": true}",
    Priority = 2,
    MaxRetries = 5,
    ExecutionTimeoutSeconds = 7200
};

var updatedJob = await controller.UpdateJob(createdJob.Value.Id, updateRequest);
Console.WriteLine($"Updated job priority to: {updatedJob.Value.Priority}");

// Suspend a job temporarily
var suspendRequest = new SuspendJobRequest { Reason = "Maintenance window" };
var suspendedJob = await controller.SuspendJob(createdJob.Value.Id, suspendRequest);
Console.WriteLine($"Job suspended: {suspendedJob.Value.Status}");

// Resume the job after maintenance
var resumedJob = await controller.ResumeJob(createdJob.Value.Id);
Console.WriteLine($"Job resumed: {resumedJob.Value.Status}");

// Trigger immediate execution (bypasses cron schedule)
var execution = await controller.TriggerJobExecution(createdJob.Value.Id);
Console.WriteLine($"Execution started: {execution.Value.Status} at {execution.Value.StartedAt}");

// Get execution history for the job
var history = await controller.GetJobExecutionHistory(createdJob.Value.Id, limit: 5);
foreach (var exec in history.Value)
{
    Console.WriteLine($"Execution {exec.Id}: {exec.Status} in {exec.ExecutionTimeMs}ms");
}

// Delete a job (and its execution history)
await controller.DeleteJob(createdJob.Value.Id);
Console.WriteLine("Job deleted successfully");
```

### Response Types

- `CreateJob`: Returns `ActionResult<JobResponse>` with 201 Created on success
- `GetJob`: Returns `ActionResult<JobResponse>` with job details
- `ListJobs`: Returns `ActionResult<PaginatedResponse<JobResponse>>` with paginated results
- `UpdateJob`: Returns `ActionResult<JobResponse>` with updated job details
- `DeleteJob`: Returns `IActionResult` with 204 No Content on success
- `SuspendJob`: Returns `ActionResult<JobResponse>` with suspended job details
- `ResumeJob`: Returns `ActionResult<JobResponse>` with resumed job details
- `TriggerJobExecution`: Returns `ActionResult<ExecutionResponse>` with execution details
- `GetJobExecutionHistory`: Returns `ActionResult<IEnumerable<ExecutionResponse>>` with execution history

### Request/Response Models

#### CreateJobRequest / UpdateJobRequest
- `Name` (string): Job name (required)
- `Description` (string?): Optional job description
- `CronExpression` (string): Cron schedule expression (required)
- `HandlerType` (string): Type of handler to execute the job (required)
- `HandlerParameters` (string?): JSON parameters for the handler
- `Priority` (int): Job priority (1-10, where 1 is highest)
- `MaxRetries` (int): Maximum retry attempts on failure
- `RetryBackoffSeconds` (int): Delay between retries in seconds
- `ExecutionTimeoutSeconds` (int): Maximum execution time in seconds
- `MaxConcurrentExecutions` (int): Maximum concurrent executions allowed

#### JobResponse
- `Id` (Guid): Unique job identifier
- `Name` (string): Job name
- `Description` (string?): Job description
- `CronExpression` (string): Cron schedule expression
- `Priority` (string): Job priority as string
- `Status` (string): Current job status (Active, Suspended, Completed, etc.)
- `IsActive` (bool): Whether the job is active
- `HandlerType` (string): Handler type
- `MaxRetries` (int): Maximum retry attempts
- `ExecutionTimeoutSeconds` (int): Execution timeout in seconds
- `LastExecutedAt` (DateTimeOffset?): When the job was last executed
- `NextExecutionAt` (DateTimeOffset?): When the job will next execute
- `TotalExecutions` (int): Total number of executions
- `SuccessfulExecutions` (int): Number of successful executions
- `SuccessRate` (decimal): Success rate (0.0 to 1.0)
- `CreatedAt` (DateTimeOffset): When the job was created
- `UpdatedAt` (DateTimeOffset): When the job was last updated

#### ExecutionResponse
- `Id` (Guid): Unique execution identifier
- `JobId` (Guid): Parent job identifier
- `Status` (string): Execution status (Pending, Running, Success, Failed, etc.)
- `StartedAt` (DateTimeOffset): When execution started
- `CompletedAt` (DateTimeOffset?): When execution completed
- `ExecutionTimeMs` (int): Duration in milliseconds
- `ErrorMessage` (string?): Error message if execution failed
- `RetryAttempt` (int): Which retry attempt this was

#### SuspendJobRequest
- `Reason` (string?): Optional reason for suspension

#### PaginatedResponse<T>
- `Data` (List<T>): List of items
- `TotalCount` (int): Total number of items available
- `PageNumber` (int): Current page number
- `PageSize` (int): Items per page

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

## BaseController

The `BaseController` class serves as the foundational abstract controller for all API controllers in the JobScheduler application. It provides standardized response patterns, audit logging, and security utilities to ensure consistent behavior across the API surface.

### Purpose

BaseController eliminates boilerplate code by offering:
- Standardized success and error response envelopes
- Built-in audit logging for security events
- Helper methods for user identification and correlation tracking
- Security and caching header management

### Usage

```csharp
using JobScheduler.Core.Controllers;
using JobScheduler.Core.Services;
using Microsoft.Extensions.Logging;

// Create required services
var logger = new Logger<BaseController>(new LoggerFactory());
var auditLogger = new AuditLogger();
var cacheService = new CacheService();

// Create a concrete controller inheriting from BaseController
public class JobsController : BaseController
{
    public JobsController(
        ILogger logger,
        AuditLogger auditLogger,
        CacheService cacheService)
        : base(logger, auditLogger, cacheService)
    {
    }

    // Example action returning success response
    public async Task<IActionResult> GetJob(Guid jobId)
    {
        var job = await _jobService.GetJobAsync(jobId);
        
        // Return standardized success response
        return Success(job, "Job retrieved successfully");
    }

    // Example action returning error response
    public IActionResult GetJob(Guid jobId)
    {
        if (jobId == Guid.Empty)
        {
            // Return standardized error response
            return Error("Invalid job ID", 400);
        }
        
        return Ok();
    }

    // Example action returning validation error response
    public IActionResult CreateJob(CreateJobRequest request)
    {
        if (string.IsNullOrEmpty(request.Name))
        {
            return ValidationError(new Dictionary<string, string[]>
            {
                { "name", new[] { "Name is required" } }
            });
        }
        
        return Ok();
    }

    // Example using helper methods
    public IActionResult GetUserActivity()
    {
        // Get authenticated user ID
        var userId = GetUserId();
        
        // Get client IP address
        var clientIp = GetClientIp();
        
        // Get correlation ID for tracing
        var correlationId = GetCorrelationId();
        
        // Log audit event
        await LogAuditAsync("UserActivity.View", $"User {userId} viewed activity from {clientIp}");
        
        // Set security headers
        SetSecurityHeaders();
        
        return Ok();
    }
}

// Example response envelopes

// Success response with data
var successResponse = new ApiSuccessResponse<JobDto>
{
    Success = true,
    Message = "Job retrieved successfully",
    Data = new JobDto { Id = Guid.NewGuid(), Name = "Sample Job" },
    Timestamp = DateTime.UtcNow
};

// Error response
var errorResponse = new ApiErrorResponse
{
    Success = false,
    Message = "Job not found",
    Timestamp = DateTime.UtcNow,
    CorrelationId = "corr-12345"
};

// Validation error response
var validationErrorResponse = new ApiValidationErrorResponse
{
    Success = false,
    Message = "Validation failed",
    Errors = new Dictionary<string, string[]>
    {
        { "name", new[] { "Name is required" } },
        { "schedule", new[] { "Schedule is invalid" } }
    },
    Timestamp = DateTime.UtcNow
};
```

### Response Types

- `ApiSuccessResponse<T>`: Standard success envelope with typed data
  - `Success` (bool): Indicates success status
  - `Message` (string): Human-readable message
  - `Data` (T?): The actual response data
  - `Timestamp` (DateTime): When the response was generated

- `ApiErrorResponse`: Standard error envelope
  - `Success` (bool): Indicates failure status
  - `Message` (string): Error message
  - `Timestamp` (DateTime): When the error occurred
  - `CorrelationId` (string?): Request correlation ID for tracing

- `ApiValidationErrorResponse`: Validation error envelope extending `ApiErrorResponse`
  - `Errors` (Dictionary<string, string[]>): Field-level validation errors

### Helper Methods

- `GetUserId()`: Gets the currently authenticated user ID or "Anonymous" if not authenticated
- `GetClientIp()`: Gets the client IP address (accounts for proxies)
- `GetCorrelationId()`: Gets or creates a correlation ID for request tracing
- `LogAuditAsync(string eventType, string message)`: Logs audit event for controller action
- `SetSecurityHeaders()`: Sets response security headers
- `SetCacheControl(int maxAgeSeconds)`: Sets response cache headers
- `SetNoCache()`: Prevents response caching

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