# DotnetJobSchedulerOptions

The `DotnetJobSchedulerOptions` class serves as the central configuration container for initializing and tuning the behavior of the .NET Job Scheduler. It encapsulates critical settings regarding database connectivity, concurrency limits, timeout policies, retry strategies, and background maintenance tasks, allowing developers to customize the scheduler's operational parameters to fit specific infrastructure and workload requirements.

## API

### ConnectionString
*   **Type**: `string`
*   **Purpose**: Specifies the database connection string used by the scheduler to persist job states, retrieve pending tasks, and manage locking mechanisms.
*   **Parameters**: None (Property setter accepts a valid ADO.NET connection string).
*   **Return Value**: None (Property getter returns the current connection string).
*   **Exceptions**: Throws an `ArgumentException` or provider-specific exception at runtime if the format is invalid or the database is unreachable during initialization.

### MaxConcurrentJobs
*   **Type**: `int`
*   **Purpose**: Defines the maximum number of jobs that the scheduler instance is permitted to execute simultaneously.
*   **Parameters**: None (Property setter accepts a positive integer).
*   **Return Value**: None (Property getter returns the configured limit).
*   **Exceptions**: May cause logical errors or deadlocks if set to a value less than 1; the scheduler typically validates this during startup.

### DefaultTimeoutSeconds
*   **Type**: `int`
*   **Purpose**: Sets the default duration, in seconds, a job is allowed to run before it is considered timed out and potentially marked as failed.
*   **Parameters**: None (Property setter accepts a positive integer).
*   **Return Value**: None (Property getter returns the timeout duration).
*   **Exceptions**: Throws an `ArgumentOutOfRangeException` if set to a non-positive value.

### DefaultMaxRetries
*   **Type**: `int`
*   **Purpose**: Configures the default maximum number of retry attempts for a job that fails due to a transient exception before it is moved to a failed state.
*   **Parameters**: None (Property setter accepts a non-negative integer).
*   **Return Value**: None (Property getter returns the retry limit).
*   **Exceptions**: None specific to the property itself, though negative values logically invalidate the retry policy.

### DefaultRetryBackoffSeconds
*   **Type**: `int`
*   **Purpose**: Determines the base wait time, in seconds, between retry attempts for failed jobs. This value is often used in exponential backoff calculations.
*   **Parameters**: None (Property setter accepts a positive integer).
*   **Return Value**: None (Property getter returns the backoff interval).
*   **Exceptions**: Throws an `ArgumentOutOfRangeException` if set to a value less than 1.

### QueuePollIntervalMs
*   **Type**: `int`
*   **Purpose**: Specifies the interval, in milliseconds, at which the scheduler polls the database queue for new pending jobs when the queue appears empty.
*   **Parameters**: None (Property setter accepts a positive integer).
*   **Return Value**: None (Property getter returns the poll interval).
*   **Exceptions**: Setting this too low may result in excessive database load; setting it too high increases job latency.

### EnableCleanup
*   **Type**: `bool`
*   **Purpose**: Toggles the automatic background cleanup process responsible for removing old job history records and expiring abandoned locks.
*   **Parameters**: None (Property setter accepts a boolean).
*   **Return Value**: None (Property getter returns the enabled state).
*   **Exceptions**: None.

### CleanupIntervalMs
*   **Type**: `int`
*   **Purpose**: Defines the frequency, in milliseconds, at which the background cleanup task runs when `EnableCleanup` is set to `true`.
*   **Parameters**: None (Property setter accepts a positive integer).
*   **Return Value**: None (Property getter returns the cleanup interval).
*   **Exceptions**: Throws an `ArgumentOutOfRangeException` if set to a non-positive value while `EnableCleanup` is true.

## Usage

### Example 1: Basic Configuration
The following example demonstrates initializing the options with standard production settings, defining a connection string, limiting concurrency, and setting a standard timeout.

```csharp
using DotnetJobScheduler;

var options = new DotnetJobSchedulerOptions
{
    ConnectionString = "Server=localhost;Database=JobsDb;Trusted_Connection=True;",
    MaxConcurrentJobs = 10,
    DefaultTimeoutSeconds = 300,
    DefaultMaxRetries = 3,
    DefaultRetryBackoffSeconds = 5,
    QueuePollIntervalMs = 500,
    EnableCleanup = true,
    CleanupIntervalMs = 3600000 // Run cleanup every hour
};

// Pass 'options' to the scheduler builder or host
// var scheduler = new JobSchedulerHost(options);
```

### Example 2: High-Throughput Development Setup
This example configures the scheduler for a local development environment with aggressive polling for immediate feedback, disabled cleanup to preserve history for debugging, and increased concurrency.

```csharp
using DotnetJobScheduler;

var devOptions = new DotnetJobSchedulerOptions
{
    ConnectionString = "Server=localhost;Database=JobsDev;User Id=dev;Password=dev;",
    MaxConcurrentJobs = 50,
    DefaultTimeoutSeconds = 60,
    DefaultMaxRetries = 0, // Fail immediately in dev
    DefaultRetryBackoffSeconds = 1,
    QueuePollIntervalMs = 100, // Poll frequently
    EnableCleanup = false,     // Preserve all history for debugging
    CleanupIntervalMs = 0      // Irrelevant when cleanup is disabled
};

// Initialize scheduler with development profile
// var scheduler = JobSchedulerFactory.Create(devOptions);
```

## Notes

*   **Thread Safety**: The `DotnetJobSchedulerOptions` class is designed as a Data Transfer Object (DTO) and is not inherently thread-safe for modification. It should be fully populated and configured before being passed to the scheduler constructor or dependency injection container. Once the scheduler is running, modifying these properties on the instance may lead to undefined behavior or race conditions.
*   **Validation Logic**: While the properties are simple types, the scheduler implementation typically validates constraints (e.g., ensuring `MaxConcurrentJobs` > 0) during the startup phase. Invalid configurations will usually result in an exception thrown during application initialization rather than at the point of property assignment.
*   **Resource Contention**: Setting `QueuePollIntervalMs` to a very low value in high-concurrency environments can lead to database connection pool exhaustion due to frequent polling requests. Conversely, a high value increases the latency between job submission and execution.
*   **Cleanup Dependencies**: If `EnableCleanup` is set to `true`, the `CleanupIntervalMs` must be a positive integer. If cleanup is disabled, the value of `CleanupIntervalMs` is ignored by the scheduler engine.
