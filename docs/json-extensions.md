# Json Extensions Overview

This document provides an overview of all `*JsonExtensions.cs` classes in the `src/JobScheduler.Core` namespace. These extension methods provide JSON serialization and deserialization functionality using `System.Text.Json`.

## JobJsonExtensions

Provides JSON serialization and deserialization extensions for the `Job` entity.

### Methods
- `ToJson(this Job value, bool indented = false)` - Serializes a Job instance to JSON
- `FromJson(string json)` - Deserializes JSON to a Job instance (returns null on failure)
- `TryFromJson(string json, out Job? value)` - Attempts to deserialize JSON to a Job instance

### Example
```csharp
var job = new Job { Id = 1, Name = "Test Job" };
string json = job.ToJson(); // {"id":1,"name":"Test Job"}
Job? deserializedJob = JobJsonExtensions.FromJson(json);
```

## JobDependencyJsonExtensions

Provides extension methods for serializing and deserializing `JobDependency` instances to and from JSON.

### Methods
- `ToJson(this JobDependency value, bool indented = false)` - Serializes a JobDependency to JSON
- `FromJson(string json)` - Deserializes JSON to a JobDependency instance
- `TryFromJson(string json, out JobDependency? value)` - Attempts to deserialize JSON to a JobDependency instance

### Example
```csharp
var dependency = new JobDependency { JobId = 1, DependsOnJobId = 2 };
string json = dependency.ToJson(); // {"jobId":1,"dependsOnJobId":2}
JobDependency? deserialized = JobDependencyJsonExtensions.FromJson(json);
```

## CreateJobRequestJsonExtensions

Provides JSON serialization and deserialization extensions for `CreateJobRequest`.

### Methods
- `ToJson(this CreateJobRequest value, bool indented = false)` - Serializes request to JSON
- `FromJson(string json)` - Deserializes JSON to CreateJobRequest instance
- `TryFromJson(string json, out CreateJobRequest? value)` - Attempts to deserialize JSON to CreateJobRequest

### Example
```csharp
var request = new CreateJobRequest { Name = "New Job", CronExpression = "0 0 * * *" };
string json = request.ToJson(); // {"name":"New Job","cronExpression":"0 0 * * *"}
CreateJobRequest? deserialized = CreateJobRequestJsonExtensions.FromJson(json);
```

## ExecutionResponseJsonExtensions

Provides JSON serialization and deserialization extensions for `ExecutionResponse`.

### Methods
- `ToJson(this ExecutionResponse response, bool indented = false)` - Serializes execution response to JSON

### Example
```csharp
var response = new ExecutionResponse { Success = true, Output = "Job completed" };
string json = response.ToJson(); // {"success":true,"output":"Job completed"}
```

## ExecutionStatsResponseJsonExtensions

Provides JSON serialization and deserialization extensions for `ExecutionStatsResponse`.

### Methods
- `ToJson(this ExecutionStatsResponse value, bool indented = false)` - Serializes execution stats to JSON
- `FromJson(string json)` - Deserializes JSON to ExecutionStatsResponse instance
- `TryFromJson(string json, out ExecutionStatsResponse? value)` - Attempts to deserialize JSON to ExecutionStatsResponse

### Example
```csharp
var stats = new ExecutionStatsResponse { TotalExecutions = 10, SuccessCount = 8 };
string json = stats.ToJson(); // {"totalExecutions":10,"successCount":8}
ExecutionStatsResponse? deserialized = ExecutionStatsResponseJsonExtensions.FromJson(json);
```

## PerformanceAnalysisResponseJsonExtensions

Provides JSON serialization extensions for `PerformanceAnalysisResponse`.

### Methods
- `ToJson(this PerformanceAnalysisResponse response, bool indented = false)` - Serializes performance analysis to JSON

### Example
```csharp
var analysis = new PerformanceAnalysisResponse { AverageDuration = 1500, PeakDuration = 3000 };
string json = analysis.ToJson(); // {"averageDuration":1500,"peakDuration":3000}
```

## CleanupResponseJsonExtensions

Provides JSON serialization extensions for `CleanupResponse`.

### Methods
- `ToJson(this CleanupResponse response, bool indented = false)` - Serializes cleanup response to JSON

### Example
```csharp
var response = new CleanupResponse { DeletedJobs = 5, FreedSpaceMb = 10.5 };
string json = response.ToJson(); // {"deletedJobs":5,"freedSpaceMb":10.5}
```

## JobResponseJsonExtensions

Provides JSON serialization and deserialization extensions for `JobResponse`.

### Methods
- `ToJson(this JobResponse response, bool indented = false)` - Serializes job response to JSON

### Example
```csharp
var response = new JobResponse { Id = 1, Name = "Test Job", Status = JobStatus.Running };
string json = response.ToJson(); // {"id":1,"name":"Test Job","status":1}
```

## PipelineResponseJsonExtensions

Provides JSON serialization extensions for `PipelineResponse`.

### Methods
- `ToJson(this PipelineResponse response, bool indented = false)` - Serializes pipeline response to JSON

### Example
```csharp
var response = new PipelineResponse { Success = true, Steps = new List<PipelineStepResponse>() };
string json = response.ToJson(); // {"success":true,"steps":[]}
```

## AuditLoggerJsonExtensions

Provides JSON serialization and deserialization extensions for audit logging entities.

### Methods
- `ToJson(this AuditLogEntry value, bool indented = false)` - Serializes audit log entry to JSON
- `FromJsonToAuditLogEntry(string json)` - Deserializes JSON to AuditLogEntry instance
- `TryFromJsonToAuditLogEntry(string json, out AuditLogEntry? value)` - Attempts to deserialize JSON to AuditLogEntry
- `ToJson(this ApiCallAudit value, bool indented = false)` - Serializes API call audit to JSON
- `ToJson(this AuditStatistics value, bool indented = false)` - Serializes audit statistics to JSON

### Example
```csharp
var auditEntry = new AuditLogEntry { Action = "JobCreated", Timestamp = DateTime.UtcNow };
string json = AuditLoggerJsonExtensions.ToJson(auditEntry);
// {"action":"JobCreated","timestamp":"2026-09-11T10:00:00Z"}
AuditLogEntry? deserialized = AuditLoggerJsonExtensions.FromJsonToAuditLogEntry(json);
```

## CacheStatisticsJsonExtensions

Provides JSON serialization extensions for `CacheStatistics`.

### Methods
- `ToJson(this CacheStatistics statistics, bool indented = false)` - Serializes cache statistics to JSON

### Example
```csharp
var stats = new CacheStatistics { Hits = 100, Misses = 25 };
string json = stats.ToJson(); // {"hits":100,"misses":25}
```

## ExecutionStatisticsServiceJsonExtensions

Provides JSON serialization and deserialization extensions for `ExecutionStatisticsService`.

### Methods
- `ToJson(this ExecutionStatisticsService value, bool indented = false)` - Serializes service instance to JSON
- `FromJson(string json)` - Deserializes JSON to ExecutionStatisticsService instance
- `TryFromJson(string json, out ExecutionStatisticsService? value)` - Attempts to deserialize JSON to ExecutionStatisticsService
- `internal static bool TryFromJson(string json, out ExecutionStatisticsService? value, JsonSerializerOptions options)` - Internal method for deserialization with custom options

### Example
```csharp
var service = new ExecutionStatisticsService();
string json = service.ToJson(); // Serialized service state
ExecutionStatisticsService? deserialized = ExecutionStatisticsServiceJsonExtensions.FromJson(json);
```

## JobPipelineServiceJsonExtensions

Provides JSON serialization extensions for `JobPipelineService`.

### Methods
- `ToJson(this JobPipelineService value, bool indented = false)` - Serializes service instance to JSON
- `FromJson(string json)` - Deserializes JSON to JobPipelineService instance
- `TryFromJson(string json, out JobPipelineService? value)` - Attempts to deserialize JSON to JobPipelineService

### Example
```csharp
var service = new JobPipelineService();
string json = service.ToJson(); // Serialized service state
JobPipelineService? deserialized = JobPipelineServiceJsonExtensions.FromJson(json);
```

## WebhookNotificationServiceJsonExtensions

Provides JSON serialization extensions for `WebhookNotificationService`.

### Methods
- `ToJson(this WebhookNotificationService value, bool indented = false)` - Serializes service instance to JSON
- `FromJson(string json)` - Deserializes JSON to WebhookNotificationService instance
- `TryFromJson(string json, out WebhookNotificationService? value)` - Attempts to deserialize JSON to WebhookNotificationService

### Example
```csharp
var service = new WebhookNotificationService();
string json = service.ToJson(); // Serialized service state
WebhookNotificationService? deserialized = WebhookNotificationServiceJsonExtensions.FromJson(json);
```

## RetryStatisticsJsonExtensions

Provides JSON serialization extensions for `RetryStatistics`.

### Methods
- `ToJson(this RetryStatistics statistics, bool indented = false)` - Serializes retry statistics to JSON

### Example
```csharp
var stats = new RetryStatistics { TotalRetries = 5, SuccessfulRetries = 3 };
string json = stats.ToJson(); // {"totalRetries":5,"successfulRetries":3}
```

## ExecutionMetricsJsonExtensions

Provides JSON serialization and deserialization extensions for the `ExecutionMetrics` entity.

### Methods
- `ToJson(this ExecutionMetrics value, bool indented = false)` - Serializes execution metrics to JSON
- `FromJson(string json)` - Deserializes JSON to ExecutionMetrics instance
- `TryFromJson(string json, out ExecutionMetrics? value)` - Attempts to deserialize JSON to ExecutionMetrics instance

### Example
```csharp
var metrics = new ExecutionMetrics { JobId = 1, DurationMs = 1500 };
string json = metrics.ToJson(); // {"jobId":1,"durationMs":1500}
ExecutionMetrics? deserialized = ExecutionMetricsJsonExtensions.FromJson(json);
```

## LoggingMiddlewareJsonExtensions

Provides JSON serialization and deserialization extensions for `LoggingMiddleware`.

### Methods
- `ToJson(this LoggingMiddleware value, bool indented = false)` - Serializes middleware instance to JSON
- `FromJson(string json)` - Deserializes JSON to LoggingMiddleware instance
- `TryFromJson(string json, out LoggingMiddleware? value)` - Attempts to deserialize JSON to LoggingMiddleware instance

### Example
```csharp
var middleware = new LoggingMiddleware();
string json = middleware.ToJson(); // Serialized middleware state
LoggingMiddleware? deserialized = LoggingMiddlewareJsonExtensions.FromJson(json);
```

## StringExtensionsJsonExtensions

Provides JSON serialization and deserialization extensions for string values.

### Methods
- `ToJson(this string value, bool indented = false)` - Serializes string to JSON format
- `FromJson(string? json)` - Deserializes JSON string back to plain string value
- `TryFromJson(string? json, out string? value)` - Attempts to deserialize JSON string to plain string value

### Example
```csharp
string original = "Hello, World!";
string json = original.ToJson(); // "\"Hello, World!\""
string? deserialized = StringExtensionsJsonExtensions.FromJson(json); // "Hello, World!"
```

## JobSchedulerContextJsonExtensions

Provides JSON serialization and deserialization extensions for `JobSchedulerContext`.

### Methods
- `ToJson(this JobSchedulerContext value, bool indented = false)` - Serializes context instance to JSON
- `FromJson(string json)` - Deserializes JSON to JobSchedulerContext instance
- `TryFromJson(string json, out JobSchedulerContext? value)` - Attempts to deserialize JSON to JobSchedulerContext instance

### Example
```csharp
var context = new JobSchedulerContext();
string json = context.ToJson(); // Serialized context state
JobSchedulerContext? deserialized = JobSchedulerContextJsonExtensions.FromJson(json);
```

## ConcurrencyExceptionJsonExtensions

Provides JSON serialization and deserialization extensions for `ConcurrencyException`.

### Methods
- `ToJson(this ConcurrencyException value, bool indented = false)` - Serializes exception to JSON
- `FromJson(string json)` - Deserializes JSON to ConcurrencyException instance
- `TryFromJson(string json, out ConcurrencyException? value)` - Attempts to deserialize JSON to ConcurrencyException instance

### Example
```csharp
var exception = new ConcurrencyException("Resource is locked");
string json = exception.ToJson(); // Serialized exception
ConcurrencyException? deserialized = ConcurrencyExceptionJsonExtensions.FromJson(json);
```

## CyclicDependencyExceptionJsonExtensions

Provides JSON serialization and deserialization extensions for `CyclicDependencyException`.

### Methods
- `ToJson(this CyclicDependencyException value, bool indented = false)` - Serializes exception to JSON
- `FromJson(string json)` - Deserializes JSON to CyclicDependencyException instance
- `TryFromJson(string json, out CyclicDependencyException? value)` - Attempts to deserialize JSON to CyclicDependencyException instance

### Example
```csharp
var exception = new CyclicDependencyException("Circular dependency detected");
string json = exception.ToJson(); // Serialized exception
CyclicDependencyException? deserialized = CyclicDependencyExceptionJsonExtensions.FromJson(json);
```

## CronExpressionExceptionJsonExtensions

Provides JSON serialization extensions for `CronExpressionException`.

### Methods
- `ToJson(this CronExpressionException exception, bool indented = false)` - Serializes exception to JSON with specific properties

### Example
```csharp
var exception = new CronExpressionException("Invalid cron expression", "* * * * *");
string json = exception.ToJson();
// {
//   "type": "CronExpressionException",
//   "message": "Invalid cron expression",
//   "cronExpression": "* * * * *",
//   "errorCode": 0,
//   "innerMessage": null
// }
```

## JobRepositoryJsonExtensions

Provides JSON serialization and deserialization extensions for `JobRepository`.

### Methods
- `ToJson(this JobRepository value, bool indented = false)` - Serializes repository instance to JSON
- `FromJson(string json)` - Deserializes JSON to JobRepository instance
- `TryFromJson(string json, out JobRepository? value)` - Attempts to deserialize JSON to JobRepository instance

### Example
```csharp
var repository = new JobRepository();
string json = repository.ToJson(); // Serialized repository state
JobRepository? deserialized = JobRepositoryJsonExtensions.FromJson(json);
```

## RepositoryJsonExtensions

Provides JSON serialization and deserialization extensions for repository types (generic).

### Methods
- `ToJson(this object value, bool indented = false)` - Serializes repository instance to JSON
- `FromJson<T>(string json)` - Deserializes JSON to repository instance of type T
- `TryFromJson<T>(string json, out T? value)` - Attempts to deserialize JSON to repository instance of type T

### Example
```csharp
var repository = new JobRepository();
string json = repository.ToJson(); // Serialized repository state
JobRepository? deserialized = RepositoryJsonExtensions.FromJson<JobRepository>(json);
```

## ValidationResultJsonExtensions

Provides JSON serialization extensions for `ValidationResult` instances.

### Methods
- `ToJson(this ValidationResult result, bool indented = false)` - Serializes validation result to JSON

### Example
```csharp
var result = new ValidationResult { IsValid = false, Errors = new List<string> { "Invalid input" } };
string json = result.ToJson(); // {"isValid":false,"errors":["Invalid input"]}
```