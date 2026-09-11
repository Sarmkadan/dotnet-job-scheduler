# Validation Helpers Overview

This document provides an overview of all `*Validation.cs` extension classes located in `src/JobScheduler.Core`. These helpers centralize validation logic, enforce business rules and data integrity constraints, and provide consistent error reporting across the application.

## 1. `GlobalExceptionMiddlewareValidation`
- **Validates:** `ErrorResponse`
- **Limits Enforced:**
  - `Message`: Must be a non-empty string.
  - `Timestamp`: Must be a valid `DateTime`, strictly UTC, not older than 1 year, and not more than 5 minutes in the future.
  - `ExceptionType` / `StackTrace`: Must be `null` or a non-empty string.
- **Return Types:** `IReadOnlyList<string>` (validation problems), `bool` (`IsValid`), throws `ArgumentException` (`EnsureValid`)
- **Example:**
  ```csharp
  var problems = errorResponse.Validate();
  if (!errorResponse.IsValid()) { /* handle */ }
  ```

## 2. `JobExecutionValidation`
- **Validates:** `JobExecution`
- **Limits Enforced:**
  - `JobId`: Must be a non-empty GUID.
  - `Status`: Must be a defined `ExecutionStatus` enum value.
  - `StartedAt` / `CompletedAt` / `CreatedAt`: Must be valid dates. `CompletedAt` >= `StartedAt`. `CreatedAt` within 5 minutes before `StartedAt`. Future tolerance of 5 minutes.
  - `DurationMilliseconds`: Non-negative. Must match calculated duration between `StartedAt` and `CompletedAt` within 1000ms tolerance.
  - `AttemptNumber`: 1 to 1000.
  - `ExecutorName` / `ExecutorInstance`: Max 255 characters.
  - `MemoryUsageMb`: 0 to 1,000,000.
  - `CpuUsagePercent`: 0 to 100.
  - Status-specific: `CompletedAt` required for Success/Failed/TimedOut/Cancelled/Skipped; must be null for Running. `ErrorMessage` required for Failed.
  - Lengths: `Output` <= 1,000,000, `ErrorMessage` <= 10,000, `StackTrace` <= 100,000.
- **Return Types:** `IReadOnlyList<string>`, `bool`, throws `ArgumentException`
- **Example:**
  ```csharp
  execution.EnsureValid();
  ```

## 3. `JobPipelineValidation`
- **Validates:** `JobPipeline`
- **Limits Enforced:**
  - `Name`: Max 255 characters.
  - `Description`: Max 1,024 characters.
  - `CreatedAt` / `UpdatedAt`: Valid dates. `UpdatedAt` >= `CreatedAt`. Future tolerance of 5 minutes.
  - `CreatedBy`: Max 128 characters.
  - `Steps`: Collection must not be null. Each step requires non-empty GUIDs for `Id`, `PipelineId`, `JobId`. `StepOrder` must be 0-9999.
- **Return Types:** `IReadOnlyList<string>`, `bool`, throws `ArgumentException`
- **Example:**
  ```csharp
  var isValid = pipeline.IsValid();
  ```

## 4. `JobHistoryQueryValidation`
- **Validates:** `JobHistoryQuery`
- **Limits Enforced:**
  - `Status`: Must be a valid `ExecutionStatus` enum value.
  - `From` / `To`: Must be valid dates. `From` must not be after `To`.
  - `PageNumber`: Must be >= 1.
  - `PageSize`: Must be between 1 and 200.
- **Return Types:** `IReadOnlyList<string>`, `bool`, throws `ArgumentException`
- **Example:**
  ```csharp
  query.EnsureValid();
  ```

## 5. `DatabaseLeaderElectionServiceValidation`
- **Validates:** `SchedulerLeaderLock`
- **Limits Enforced:**
  - `Id`: Must be a positive integer.
  - `LockName` / `LeaderInstanceId`: Max 100 characters.
  - `LeaseExpiresAt` / `AcquiredAt`: Must be valid UTC dates. `AcquiredAt` <= `LeaseExpiresAt`. `LeaseExpiresAt` cannot be > 5 minutes in the past. `AcquiredAt` cannot be > 5 minutes in the future.
- **Return Types:** `IReadOnlyList<string>`, `bool`, throws `ArgumentException`
- **Example:**
  ```csharp
  lockEntity.Validate();
  ```

## 6. `JobHistoryServiceValidation`
- **Validates:** `PagedResult<ExecutionResponse>`, `PagedResult<JobExecutionSummary>`, `JobExecutionSummary`, `ExecutionResponse`
- **Limits Enforced:**
  - **Paged Results:** `Items` not null, count <= `TotalCount`, `TotalCount` >= 0, `PageSize` > 0 if `TotalCount` > 0, `PageNumber` >= 1, `TotalPages` >= 0, `PageNumber` <= `TotalPages` + 1.
  - **Summaries/Responses:** IDs non-empty GUIDs, dates valid/future-tolerant, durations non-negative, counts non-negative, logical consistency (e.g., `MinDuration` <= `MaxDuration`, `AverageDuration` within bounds).
- **Return Types:** `IReadOnlyList<string>`, `bool`, throws `ArgumentException`
- **Example:**
  ```csharp
  pagedResult.EnsureValid();
  ```

## 7. `DependencyGraphValidationResultValidation`
- **Validates:** `DependencyGraphValidationResult`
- **Limits Enforced:**
  - `IsValid` consistency: `true` implies empty `CycleNodes`; `false` implies non-empty `CycleNodes`.
  - `CycleNodes`: Cannot contain `Guid.Empty` or duplicate IDs.
  - `Message`: Must not be null or whitespace when `IsValid` is `false`.
- **Return Types:** `IReadOnlyList<string>`, throws `ArgumentException`
- **Example:**
  ```csharp
  result.EnsureValid();
  ```

## 8. `JobExecutionSummaryValidation`
- **Validates:** `JobExecutionSummary`
- **Limits Enforced:**
  - Counts: Non-negative. Sum of sub-counts <= `TotalExecutions`. Individual counts <= `TotalExecutions`. If `TotalExecutions` == 0, all sub-counts must be 0.
  - Durations: Non-negative. `MinDurationMs` <= `MaxDurationMs`. `AverageDurationMs` must fall within `Min`/`Max` bounds when > 0.
  - `LastExecutedAt`: Must be valid and not in the future if `LastStatus` is set. Cannot be default.
- **Return Types:** `IReadOnlyList<string>`, `bool`, throws `ArgumentException`
- **Example:**
  ```csharp
  summary.Validate();
  ```

## 9. `HttpContextExtensionsValidation`
- **Validates:** `HttpContext`
- **Limits Enforced:**
  - `GetUserId()`: Must not be empty or whitespace.
  - `GetClientIpAddress()`: Must be a valid IP address.
  - `GetCorrelationId()`: Must be a valid GUID.
  - `GetRequestScheme()`: Must be a valid URI scheme (`http` or `https`).
  - `GetFullRequestUrl()`: Must be a valid absolute URI.
- **Return Types:** `IReadOnlyList<string>`, `bool`, throws `ArgumentException`
- **Example:**
  ```csharp
  context.EnsureValid();
  ```

## 10. `JobScheduleHistoryValidation`
- **Validates:** `JobScheduleHistory`
- **Limits Enforced:**
  - `Id` / `JobId`: Must be non-empty GUIDs.
  - `PropertyName` / `ChangeReason`: Must not be null or whitespace.
  - `ChangedAt`: Must not be the default `DateTime` value.
- **Return Types:** `IReadOnlyList<string>`, `bool`, throws `ArgumentException`
- **Example:**
  ```csharp
  history.EnsureValid();
  ```
