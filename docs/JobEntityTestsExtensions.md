# JobEntityTestsExtensions

Utility class providing static factory methods and assertion helpers for creating and validating `Job` entities in unit tests. Designed to standardize test data generation for the `dotnet-job-scheduler` project, ensuring consistent coverage of edge cases and validation rules.

## API

### `CreateMinimalValidJob()`
Creates a minimal valid `Job` entity with required properties set to default valid values. The resulting job passes all validation checks and is suitable for testing basic job operations.

- **Parameters**: None
- **Return value**: A new `Job` instance with:
  - `Handler` set to `"TestHandler"`
  - `Schedule` set to `"@daily"`
  - `ConcurrencyPolicy` set to `"Allow"`
  - `ExecutionTimeout` set to `TimeSpan.FromMinutes(5)`
  - `CreatedAt` set to `DateTime.UtcNow`
  - `UpdatedAt` set to `DateTime.UtcNow`
- **Throws**: Never

---

### `CreateInvalidHandlerJob()`
Creates a `Job` entity with an intentionally invalid `Handler` property. Useful for testing validation logic and error handling when invalid handlers are provided.

- **Parameters**: None
- **Return value**: A new `Job` instance with:
  - `Handler` set to `null`
  - All other properties set to valid defaults (see `CreateMinimalValidJob`)
- **Throws**: Never

---

### `CreateJobWithMetrics()`
Creates a `Job` entity with execution metrics populated. Useful for testing metric aggregation, reporting, and serialization scenarios.

- **Parameters**: None
- **Return value**: A new `Job` instance with:
  - All properties set to valid defaults (see `CreateMinimalValidJob`)
  - `LastExecution` set to a completed execution with:
    - `Status` = `"Completed"`
    - `StartedAt` = `DateTime.UtcNow.AddMinutes(-10)`
    - `CompletedAt` = `DateTime.UtcNow.AddMinutes(-5)`
    - `Metrics` containing:
      - `Duration` = `TimeSpan.FromMinutes(5)`
      - `MemoryUsed` = `1024`
      - `CpuUsage` = `0.75`
- **Throws**: Never

---

### `CreateSuspendedJob()`
Creates a `Job` entity with the `Suspended` flag set to `true`. Useful for testing job suspension behavior, scheduling rules, and state transitions.

- **Parameters**: None
- **Return value**: A new `Job` instance with:
  - `Suspended` set to `true`
  - All other properties set to valid defaults (see `CreateMinimalValidJob`)
- **Throws**: Never

---
### `CreateConcurrentJob()`
Creates a `Job` entity with the `ConcurrencyPolicy` set to `"Forbid"`. Useful for testing concurrency control, lock acquisition, and execution scheduling behavior.

- **Parameters**: None
- **Return value**: A new `Job` instance with:
  - `ConcurrencyPolicy` set to `"Forbid"`
  - All other properties set to valid defaults (see `CreateMinimalValidJob`)
- **Throws**: Never

---
### `GetExecutionMetricsSummary(Job job)`
Extracts and formats a human-readable summary of execution metrics from a `Job` entity. Used in tests to assert metric values without parsing raw JSON or internal structures.

- **Parameters**:
  - `job` (`Job`): The job instance to extract metrics from. Must not be `null`.
- **Return value**: A string summary in the format:
  `"Duration: {duration}; Memory: {memory}MB; CPU: {cpu}%"`
  where `{duration}` is in `HH:mm:ss`, `{memory}` is in megabytes, and `{cpu}` is a percentage.
- **Throws**:
  - `ArgumentNullException`: If `job` is `null`.
  - `InvalidOperationException`: If the job has no execution history or metrics are missing.

---
### `GetValidationErrors(Job job)`
Collects all validation error messages for a given `Job` entity. Useful for testing validation logic and asserting multiple error conditions in a single assertion.

- **Parameters**:
  - `job` (`Job`): The job instance to validate. Must not be `null`.
- **Return value**: An `IEnumerable<string>` containing zero or more validation error messages. Each message describes a specific validation failure.
- **Throws**:
  - `ArgumentNullException`: If `job` is `null`.

## Usage
