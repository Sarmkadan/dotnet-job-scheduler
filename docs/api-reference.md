# API Reference

Complete API documentation for dotnet-job-scheduler service layer and REST endpoints.

## REST API Endpoints

### Base URL
```
http://localhost:5000/api
```

---

## Jobs Endpoints

### List All Jobs

```http
GET /api/jobs
```

**Query Parameters**:
- `status` (string, optional): Filter by status (Scheduled, Running, Failed, etc.)
- `priority` (integer, optional): Filter by priority (0=Low, 1=Normal, 2=High, 3=Critical)
- `isActive` (boolean, optional): Filter by active status
- `pageNumber` (integer, default=1): Page number
- `pageSize` (integer, default=20): Items per page
- `sortBy` (string, default="CreatedAt"): Sort field
- `sortDescending` (boolean, default=true): Sort order

**Response** (200 OK):
```json
{
  "items": [
    {
      "id": 1,
      "name": "DailyReport",
      "description": "Daily business report",
      "cronExpression": "0 9 * * *",
      "status": "Scheduled",
      "priority": 2,
      "isActive": true,
      "nextExecutionTime": "2026-05-05T09:00:00Z",
      "lastExecutionTime": "2026-05-04T09:00:00Z",
      "maxRetries": 3,
      "executionTimeoutSeconds": 600,
      "createdAt": "2026-04-15T10:30:00Z",
      "createdBy": "admin@example.com"
    }
  ],
  "total": 45,
  "pageNumber": 1,
  "pageSize": 20,
  "totalPages": 3
}
```

**Error** (400 Bad Request):
```json
{
  "error": "Invalid status value",
  "message": "Status must be one of: Scheduled, Running, Failed, etc."
}
```

---

### Get Job by ID

```http
GET /api/jobs/{id}
```

**Path Parameters**:
- `id` (integer, required): Job ID

**Response** (200 OK):
```json
{
  "id": 1,
  "name": "DailyReport",
  "description": "Daily business report",
  "cronExpression": "0 9 * * *",
  "handlerType": "MyApp.Jobs.ReportHandler, MyApp",
  "status": "Scheduled",
  "priority": 2,
  "isActive": true,
  "maxRetries": 3,
  "retryBackoffSeconds": 5,
  "executionTimeoutSeconds": 600,
  "maxConcurrentExecutions": 1,
  "nextExecutionTime": "2026-05-05T09:00:00Z",
  "lastExecutionTime": "2026-05-04T09:00:00Z",
  "createdAt": "2026-04-15T10:30:00Z",
  "createdBy": "admin@example.com",
  "modifiedAt": "2026-04-20T14:15:00Z",
  "modifiedBy": "admin@example.com"
}
```

**Error** (404 Not Found):
```json
{
  "error": "Job not found",
  "message": "No job found with ID: 999"
}
```

---

### Create Job

```http
POST /api/jobs
Content-Type: application/json
```

**Request Body**:
```json
{
  "name": "DailyReport",
  "description": "Daily business report",
  "cronExpression": "0 9 * * *",
  "handlerType": "MyApp.Jobs.ReportHandler, MyApp",
  "priority": 2,
  "isActive": true,
  "maxRetries": 3,
  "retryBackoffSeconds": 5,
  "executionTimeoutSeconds": 600,
  "maxConcurrentExecutions": 1
}
```

**Field Validation**:
- `name`: Required, 1-256 characters, must be unique
- `cronExpression`: Required, valid POSIX format
- `handlerType`: Required, fully qualified type name
- `priority`: 0-3 (Low, Normal, High, Critical)
- `maxRetries`: 0-10
- `executionTimeoutSeconds`: 1-3600
- `maxConcurrentExecutions`: >= 1

**Response** (201 Created):
```json
{
  "id": 1,
  "name": "DailyReport",
  "description": "Daily business report",
  "cronExpression": "0 9 * * *",
  "handlerType": "MyApp.Jobs.ReportHandler, MyApp",
  "status": "Scheduled",
  "priority": 2,
  "isActive": true,
  "nextExecutionTime": "2026-05-05T09:00:00Z",
  "createdAt": "2026-05-04T10:30:00Z",
  "createdBy": "admin@example.com"
}
```

**Error** (400 Bad Request):
```json
{
  "errors": [
    "name: A job with this name already exists",
    "cronExpression: Invalid cron expression"
  ]
}
```

---

### Update Job

```http
PUT /api/jobs/{id}
Content-Type: application/json
```

**Request Body**:
```json
{
  "name": "DailyReport",
  "description": "Updated daily report",
  "cronExpression": "0 10 * * *",
  "priority": 3,
  "maxRetries": 5,
  "executionTimeoutSeconds": 900
}
```

**Response** (200 OK):
Same as Create Job response

**Error** (404 Not Found):
```json
{
  "error": "Job not found",
  "message": "No job found with ID: 999"
}
```

---

### Delete Job

```http
DELETE /api/jobs/{id}
```

**Response** (204 No Content):
Empty response body

**Error** (404 Not Found):
```json
{
  "error": "Job not found"
}
```

---

### Suspend Job

```http
POST /api/jobs/{id}/suspend
Content-Type: application/json
```

**Request Body**:
```json
{
  "reason": "Investigating high failure rate"
}
```

**Response** (200 OK):
```json
{
  "id": 1,
  "status": "Suspended",
  "suspensionReason": "Investigating high failure rate",
  "suspendedAt": "2026-05-04T14:30:00Z"
}
```

---

### Resume Job

```http
POST /api/jobs/{id}/resume
```

**Response** (200 OK):
```json
{
  "id": 1,
  "status": "Scheduled",
  "resumedAt": "2026-05-04T14:35:00Z"
}
```

---

## Executions Endpoints

### List Job Executions

```http
GET /api/jobs/{jobId}/executions
```

**Query Parameters**:
- `status` (string, optional): Filter by execution status
- `pageNumber` (integer, default=1): Page number
- `pageSize` (integer, default=20): Items per page

**Response** (200 OK):
```json
{
  "items": [
    {
      "id": 1,
      "jobId": 1,
      "status": "Completed",
      "executedAt": "2026-05-04T09:00:00Z",
      "completedAt": "2026-05-04T09:05:32Z",
      "duration": "00:05:32",
      "result": "Report generated and sent",
      "retryAttempt": 0,
      "serverName": "scheduler-01"
    }
  ],
  "total": 95,
  "pageNumber": 1,
  "pageSize": 20,
  "totalPages": 5
}
```

---

### Get Execution Details

```http
GET /api/jobs/{jobId}/executions/{executionId}
```

**Response** (200 OK):
```json
{
  "id": 1,
  "jobId": 1,
  "status": "Completed",
  "executedAt": "2026-05-04T09:00:00Z",
  "completedAt": "2026-05-04T09:05:32Z",
  "duration": "00:05:32",
  "result": "Report generated and sent successfully",
  "errorMessage": null,
  "stackTrace": null,
  "retryAttempt": 0,
  "nextRetryTime": null,
  "serverName": "scheduler-01",
  "processId": 1234,
  "memoryUsageMb": 256,
  "cpuUsagePercent": 45.5
}
```

---

### Get Last Execution

```http
GET /api/jobs/{jobId}/executions/latest
```

**Response** (200 OK):
Same structure as Get Execution Details

---

## Dashboard Endpoints

### Get Dashboard Statistics

```http
GET /api/dashboard/statistics
```

**Response** (200 OK):
```json
{
  "totalJobs": 45,
  "activeJobs": 42,
  "suspendedJobs": 3,
  "runningExecutions": 2,
  "queuedJobs": 5,
  "averageSuccessRate": 98.5,
  "executionsLast24Hours": 156,
  "failedLast24Hours": 3,
  "averageExecutionTimeMs": 2450,
  "uptime": "18 days 5 hours 30 minutes"
}
```

---

### Get Job Metrics

```http
GET /api/dashboard/metrics
```

**Query Parameters**:
- `jobId` (integer, optional): Specific job, or all if omitted
- `startDate` (datetime, required): ISO 8601 format
- `endDate` (datetime, required): ISO 8601 format
- `interval` (string, optional): "hour", "day", "week"

**Response** (200 OK):
```json
{
  "jobId": 1,
  "startDate": "2026-04-04T00:00:00Z",
  "endDate": "2026-05-04T00:00:00Z",
  "metrics": [
    {
      "timestamp": "2026-05-04T00:00:00Z",
      "totalExecutions": 4,
      "successfulCount": 4,
      "failedCount": 0,
      "successRate": 100.0,
      "averageDurationMs": 2100,
      "minDurationMs": 1800,
      "maxDurationMs": 2500
    }
  ],
  "summary": {
    "totalExecutions": 120,
    "successfulCount": 119,
    "failedCount": 1,
    "overallSuccessRate": 99.17,
    "totalDurationHours": 4.2,
    "averageDurationMs": 2150,
    "minDurationMs": 900,
    "maxDurationMs": 8500
  }
}
```

---

### Get System Health

```http
GET /api/dashboard/health
```

**Response** (200 OK):
```json
{
  "status": "Healthy",
  "checks": {
    "database": {
      "status": "Healthy",
      "responseTimeMs": 45
    },
    "scheduler": {
      "status": "Healthy",
      "lastPollTime": "2026-05-04T14:30:15Z",
      "pollIntervalMs": 5000
    },
    "queueProcessor": {
      "status": "Healthy",
      "queuedJobs": 2,
      "processingJobs": 1
    }
  },
  "timestamp": "2026-05-04T14:31:00Z"
}
```

**Error** (503 Service Unavailable):
```json
{
  "status": "Unhealthy",
  "checks": {
    "database": {
      "status": "Unhealthy",
      "message": "Connection timeout"
    }
  }
}
```

---

## Service Layer API

### JobSchedulerService

```csharp
public class JobSchedulerService
{
    // Job Management
    public Task<Job> CreateJobAsync(Job job, string createdBy)
    public Task<Job> UpdateJobAsync(Job job, string modifiedBy)
    public Task DeleteJobAsync(int jobId, string deletedBy)
    public Task<Job?> GetJobByIdAsync(int jobId)
    public Task<List<Job>> GetActiveJobsAsync()
    public Task<List<Job>> GetAllJobsAsync()

    // Execution
    public Task<List<JobExecution>> ExecuteDueJobsAsync()
    public Task<JobExecution> ExecuteJobNowAsync(int jobId)
    
    // Status Management
    public Task SuspendJobAsync(int jobId, string reason)
    public Task ResumeJobAsync(int jobId)
    
    // Metrics
    public Task<SchedulerStatistics> GetSchedulerStatisticsAsync()
    public Task<Page<Job>> QueryJobsAsync(JobQuery query)
}
```

---

### JobExecutorService

```csharp
public class JobExecutorService
{
    // Execute a single job
    public Task<JobExecution> ExecuteAsync(
        Job job, 
        CancellationToken cancellationToken = default)

    // Get execution context
    public JobExecutionContext GetExecutionContext(Job job)
}
```

---

### CronExpressionService

```csharp
public class CronExpressionService
{
    // Validate cron expression
    public bool IsValidExpression(string expression)
    
    // Get next occurrence
    public DateTime? GetNextOccurrence(
        string cronExpression, 
        DateTime fromTime,
        TimeZoneInfo? timeZone = null)

    // Get next N occurrences
    public List<DateTime> GetNextOccurrences(
        string cronExpression,
        DateTime fromTime,
        int count,
        TimeZoneInfo? timeZone = null)

    // Get previous occurrence
    public DateTime? GetPreviousOccurrence(
        string cronExpression,
        DateTime fromTime,
        TimeZoneInfo? timeZone = null)
}
```

---

### RetryService

```csharp
public class RetryService
{
    // Determine if job should retry
    public bool ShouldRetry(Job job, int attemptNumber)
    
    // Calculate backoff delay
    public TimeSpan CalculateBackoff(
        Job job,
        int attemptNumber,
        BackoffStrategy strategy = BackoffStrategy.Exponential)

    // Schedule next retry
    public Task ScheduleRetryAsync(
        Job job,
        JobExecution execution,
        int attemptNumber)
}
```

---

### ConcurrencyManager

```csharp
public class ConcurrencyManager
{
    // Check if job can execute
    public Task<bool> CanExecuteAsync(Job job)
    
    // Get running job count
    public Task<int> GetRunningCountAsync(int? jobId = null)
    
    // Get queue size
    public Task<int> GetQueuedCountAsync()

    // Acquire execution slot
    public Task<bool> AcquireSlotAsync(Job job)
    
    // Release execution slot
    public Task ReleaseSlotAsync(Job job)
}
```

---

### ExecutionStatisticsService

```csharp
public class ExecutionStatisticsService
{
    // Get execution metrics
    public Task<ExecutionMetrics> GetMetricsAsync(
        int? jobId = null,
        DateTime? startDate = null,
        DateTime? endDate = null)

    // Get success rate
    public Task<double> GetSuccessRateAsync(
        int? jobId = null,
        TimeSpan? timeWindow = null)

    // Get performance metrics
    public Task<PerformanceMetrics> GetPerformanceAsync(int? jobId = null)

    // Get error statistics
    public Task<List<ErrorStatistic>> GetErrorStatisticsAsync(
        int? jobId = null,
        int topCount = 10)
}
```

---

## Data Models

### JobStatus Enum

```csharp
public enum JobStatus
{
    Pending = 0,           // Created but not scheduled
    Scheduled = 1,         // Awaiting next execution
    Running = 2,           // Currently executing
    Completed = 3,         // Last execution successful
    Failed = 4,            // Failed, eligible for retry
    FailedPermanently = 5, // Max retries exceeded
    Suspended = 6,         // Paused by user
    Cancelled = 7          // Manually cancelled
}
```

### JobPriority Enum

```csharp
public enum JobPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3
}
```

### ExecutionStatus Enum

```csharp
public enum ExecutionStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    FailedPermanently = 4,
    Timeout = 5,
    Cancelled = 6
}
```

### BackoffStrategy Enum

```csharp
public enum BackoffStrategy
{
    Exponential = 0,  // delay * 2^attempt
    Linear = 1,       // delay * attempt
    Fixed = 2         // delay (constant)
}
```

---

## Error Response Format

All error responses follow this format:

```json
{
  "error": "Error code or title",
  "message": "Detailed error message",
  "statusCode": 400,
  "timestamp": "2026-05-04T14:30:00Z",
  "traceId": "0HN3G4H8D7K2L9M5:0000001"
}
```

### Common Error Codes

| Code | Status | Description |
|------|--------|-------------|
| `JobNotFound` | 404 | Job with specified ID doesn't exist |
| `DuplicateJobName` | 409 | Job name already exists |
| `InvalidCronExpression` | 400 | Cron expression is invalid |
| `InvalidJobStatus` | 400 | Job status transition not allowed |
| `ConcurrencyLimitExceeded` | 429 | Too many concurrent executions |
| `ExecutionTimeout` | 408 | Job execution exceeded timeout |
| `HandlerNotFound` | 404 | Job handler type cannot be resolved |
| `DatabaseError` | 500 | Database operation failed |

---

## Rate Limiting

API endpoints are rate-limited:

**Rate Limits**:
- 1000 requests per minute per IP address
- 5000 requests per minute per authenticated user

**Headers**:
```
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 999
X-RateLimit-Reset: 1609459200
```

**When limit exceeded** (429 Too Many Requests):
```json
{
  "error": "RateLimitExceeded",
  "message": "Too many requests. Limit: 1000/minute",
  "retryAfter": 60
}
```

---

## Pagination

List endpoints support pagination:

**Query Parameters**:
- `pageNumber` (default: 1): Page number (1-indexed)
- `pageSize` (default: 20): Items per page (1-1000)

**Response**:
```json
{
  "items": [...],
  "pageNumber": 1,
  "pageSize": 20,
  "total": 150,
  "totalPages": 8
}
```

---

## Filtering & Sorting

**Filtering**:
- Multiple filters supported via query parameters
- Filters are AND'ed together
- Example: `?status=Scheduled&priority=3&isActive=true`

**Sorting**:
- `sortBy`: Field name (CamelCase)
- `sortDescending`: true/false
- Example: `?sortBy=CreatedAt&sortDescending=true`

Valid sort fields: `Name`, `Status`, `Priority`, `NextExecutionTime`, `LastExecutionTime`, `CreatedAt`

---

## API Versioning

Current API version: **v1**

**Version Header**:
```
X-Api-Version: 1.0
```

Versions are backward compatible. Major breaking changes will create v2 endpoint.

---

## Authentication

(Optional) If API authentication is configured:

```http
Authorization: Bearer <jwt-token>
```

All endpoints except `/health` require authentication.

---

This API reference covers all available endpoints and service methods. For code examples, see the [examples/](../examples/) directory.
