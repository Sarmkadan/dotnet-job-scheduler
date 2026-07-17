# JobSchedulerServiceTests

The `JobSchedulerServiceTests` class provides unit tests for the `JobSchedulerService` class, ensuring correct job scheduling, management, and execution logic. These tests cover various scenarios, including job creation, suspension, resumption, deletion, and retry processing.

The following example demonstrates how to use some of the public members of `JobSchedulerServiceTests`:

```csharp
using DotnetJobScheduler.Tests;
using JobScheduler.Core.Data.Repositories;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Services;
using Moq;
using Xunit;

// Create a test instance
var tests = new JobSchedulerServiceTests();

// Arrange test dependencies
var jobRepoMock = tests._jobRepoMock;
var job = tests.CreateValidJob();

// Test creating a job
await tests.CreateJobAsync_WithValidJob_PersistsAndReturnsJob();

// Test creating a job with null
try
{
    await tests.CreateJobAsync_WithNullJob_ThrowsArgumentNullException();
    Assert.Fail("Expected ArgumentNullException");
}
catch (ArgumentNullException)
{
}

// Test suspending and resuming a job
await tests.SuspendJobAsync_WithActiveJob_SuspendsProperly();
await tests.ResumeJobAsync_WithSuspendedJob_ResumesScheduling();

// Test deleting a job
await tests.DeleteJobAsync_RemovesJobFromRepository();

// Process retries
await tests.ProcessRetriesAsync_RetriesFailedExecutions();
```

## RetryPolicyTests

The `RetryPolicyTests` class provides unit tests for the `RetryPolicy` class, ensuring correct backoff calculations, retry decisions, and budget enforcement. These tests cover various scenarios for job retry decisions.

The following example demonstrates how to use some of the public members of `RetryPolicyTests`:

```csharp
using DotnetJobScheduler.Tests;
using JobScheduler.Core.Domain.Entities;

// Create a test instance
var retryPolicyTests = new RetryPolicyTests();

// Arrange test data
var policy = new RetryPolicy
{
    Strategy = BackoffStrategy.Fixed,
    InitialBackoffSeconds = 10,
    MaxBackoffSeconds = 300
};

// Test fixed strategy backoff delay
retryPolicyTests.CalculateBackoffDelay_WithFixedStrategy_ReturnsConstantDelayAcrossAttempts(policy);

// Test linear strategy backoff delay
policy.Strategy = BackoffStrategy.Linear;
retryPolicyTests.CalculateBackoffDelay_WithLinearStrategy_IncrementsProportionallyToAttempt(policy);

// Test exponential strategy backoff delay
policy.Strategy = BackoffStrategy.Exponential;
retryPolicyTests.CalculateBackoffDelay_WithExponentialStrategy_DoublesOnEachAttempt(policy);

// Test retry decisions
var shouldRetry = retryPolicyTests.ShouldRetryOnException_WhenRetryableExceptionsIsEmpty_AllowsAnyException();
Assert.True(shouldRetry);

// Test retry decisions with allowlist
policy.RetryableExceptions = "TimeoutException, HttpRequestException";
shouldRetry = retryPolicyTests.ShouldRetryOnException_WhenExceptionMatchesAllowlist_ReturnsTrue("TimeoutException");
Assert.True(shouldRetry);

// Test validation
var isValid = retryPolicyTests.IsValid_WithWellFormedConfiguration_ReturnsTrue(policy);
Assert.True(isValid);
```

## JobEntityTests

The `JobEntityTests` class provides unit tests for the `Job` entity, verifying validation logic, execution metrics updates, success rate calculations, and concurrency checks.

The following example demonstrates how to use some of the public members of `JobEntityTests`:

```csharp
using DotnetJobScheduler.Tests;
using JobScheduler.Core.Domain.Entities;

// Create a test instance
var jobTests = new JobEntityTests();

// Create a valid job
var job = JobEntityTests.CreateValidJob();

// Test job validation
var isValid = job.IsValidForScheduling();
Assert.True(isValid);

// Update execution metrics
job.UpdateExecutionMetrics(success: true);
Assert.Equal(1, job.TotalExecutions);
Assert.Equal(1, job.SuccessfulExecutions);

// Test success rate calculation
var successRate = job.GetSuccessRate();
Assert.Equal(100, successRate);

// Test concurrency checks
var canExecuteNow = job.CanExecuteNow(currentConcurrentCount: 0);
Assert.True(canExecuteNow);
```

## JobExecutorServiceTests

The `JobExecutorServiceTests` class provides unit tests for the `JobExecutorService` class, ensuring correct job execution, concurrency management, and execution status tracking. These tests cover various scenarios, including job execution, concurrency, and cancellation.

The following example demonstrates how to use some of the public members of `JobExecutorServiceTests`:

```csharp
using DotnetJobScheduler.Tests;
using JobScheduler.Core.Data.Repositories;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Services;
using Moq;
using Xunit;

// Create a test instance
var jobExecutorServiceTests = new JobExecutorServiceTests();

// Arrange test dependencies
var jobRepoMock = jobExecutorServiceTests._jobRepoMock;
var job = jobExecutorServiceTests.CreateValidJob();

// Test executing a job
await jobExecutorServiceTests.ExecuteJobAsync_WithValidJob_CreatesExecution();

// Test executing a null job
await jobExecutorServiceTests.ExecuteJobAsync_WithNullJob_ThrowsArgumentNullException();

// Test concurrency exceeded
await jobExecutorServiceTests.ExecuteJobAsync_WhenConcurrencyExceeded_ThrowsConcurrencyException();

// Test decrementing counter on completion
await jobExecutorServiceTests.ExecuteJobAsync_DecrementsCounterOnCompletion();

// Test setting running status on cancellation
await jobExecutorServiceTests.ExecuteJobAsync_WithCancellation_SetsRunningStatus();

// Test recording started and completed times
await jobExecutorServiceTests.ExecuteJobAsync_RecordsStartedAndCompletedTimes();

// Test handling concurrency
await jobExecutorServiceTests.ExecuteJobAsync_MultipleConcurrentExecutions_HandlesConcurrency();

// Test handling short timeout
await jobExecutorServiceTests.ExecuteJobAsync_WithShortTimeout_HandlesTimeoutScenario();

// Test saving execution to repository
await jobExecutorServiceTests.ExecuteJobAsync_SavesExecutionToRepository();
```

## CacheServiceTests

The `CacheServiceTests` class provides unit tests for the `CacheService` class, ensuring correct caching behavior, TTL expiration, pattern invalidation, and statistics reporting. These tests cover various scenarios, including basic key-value operations, cache invalidation, and factory-based value retrieval.

The following example demonstrates how to use some of the public members of `CacheServiceTests`:

```csharp
using DotnetJobScheduler.Tests;
using Xunit;

// Create a test instance
var tests = new CacheServiceTests();

// Test basic Get and Set operations
await tests.SetAsync_WithDefaultExpiration_CachesValue();
await tests.GetAsync_WithExistingKey_ReturnsValue();

// Test removing items
await tests.RemoveAsync_DeletesKey();

// Test GetOrSet logic
await tests.GetOrSetAsync_WithCachedValue_ReturnsCached();

// Test statistics
await tests.GetStatistics_ReturnsAccurateCount();

// Test key generation and operation consistency
await tests.CacheKeyGenerator_GeneratesConsistentKeys();
await tests.MultipleOperations_MaintainsConsistency();
```

## JobHistoryServiceTests

The `JobHistoryServiceTests` class provides unit tests for the `JobHistoryService` class, ensuring correct job history retrieval and summary functionality. These tests cover various scenarios, including job history retrieval, filtering, and pagination. 

The following example demonstrates how to use some of the public members of `JobHistoryServiceTests`:

```csharp
using DotnetJobScheduler.Tests;
using JobScheduler.Core.Data.Repositories;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Domain.Models;
using JobScheduler.Core.Exceptions;
using JobScheduler.Core.Services;
using Moq;
using Xunit;

// Create a test instance
var jobHistoryServiceTests = new JobHistoryServiceTests();

// Test getting job history with valid job and executions
await jobHistoryServiceTests.GetJobHistoryAsync_WithValidJobAndExecutions_ReturnsPaginatedHistory();

// Test getting job history with status filter
await jobHistoryServiceTests.GetJobHistoryAsync_WithStatusFilter_ReturnsFilteredRecords();

// Test getting job summary with executions
await jobHistoryServiceTests.GetJobSummaryAsync_WithExecutions_ReturnsAccurateStatistics();
```

## JobSchedulerIntegrationTests

`JobSchedulerIntegrationTests` is an integration test suite that validates the full job‑scheduler workflow using real services and an in‑memory database. It exercises job creation, scheduling, state transitions, concurrency limits, caching, retry handling, and other core features to ensure the system works end‑to‑end.

Below is a realistic usage example that demonstrates how the public members of the test class can be invoked in an async context:

```csharp
using System;
using System.Threading.Tasks;
using DotnetJobScheduler.Tests;

public static class IntegrationTestRunner
{
    public static async Task Main()
    {
        var integrationTests = new JobSchedulerIntegrationTests();

        // Set up the in‑memory test environment
        await integrationTests.InitializeAsync();

        // Run the individual integration scenarios
        await integrationTests.CreateJob_WithValidInput_SchedulesJobSuccessfully();
        await integrationTests.CreateMultipleJobs_WithDifferentSchedules_AllPersistCorrectly();
        await integrationTests.SuspendAndResumeJob_TransitionsStateCorrectly();
        await integrationTests.UpdateJobSchedule_ChangesExecutionTiming();
        await integrationTests.DeleteJob_RemovesJobFromSystem();
        await integrationTests.ConcurrencyControl_EnforcesJobConcurrencyLimits();
        await integrationTests.CacheService_OptimizesDataRetrieval();
        await integrationTests.ScheduleService_CalculatesUpcomingExecutions();
        await integrationTests.RetryService_HandlesFailedExecutions();
        await integrationTests.JobExecutor_ExecutesJobWithTimeout();
        await integrationTests.WorkflowComplete_CreateExecuteAndTrack();
        await integrationTests.MultipleJobTypes_WithVariedConfigurations();

        // Clean up the test environment
        await integrationTests.DisposeAsync();
    }
}
```

## ConcurrencyManagerTests

The `ConcurrencyManagerTests` class provides unit tests for the `ConcurrencyManager` class, ensuring correct concurrency control and capacity management. These tests cover various scenarios, including job execution, concurrency limits, and cache synchronization. The following example demonstrates how to use some of the public members of `ConcurrencyManagerTests`:

## EmailSendingJobHandler

The `EmailSendingJobHandler` class implements the `IJobHandler` interface and is responsible for sending emails. It executes asynchronously and returns a summary of the email sending process. Here's an example of how to use the `EmailSendingJobHandler`:

```csharp
var handler = new EmailSendingJobHandler(new LoggerFactory().CreateLogger<EmailSendingJobHandler>());
var result = await handler.ExecuteAsync(new Job(), CancellationToken.None);
Console.WriteLine(result);
```

## HelloWorldJobHandler

The `HelloWorldJobHandler` class implements the `IJobHandler` interface and provides a simple demonstration job handler that logs a message and returns a completion status. It's ideal for testing basic job scheduling functionality without complex dependencies.


Here's a realistic usage example:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Services;

// Register the handler in your DI container
var services = new ServiceCollection();
services.AddLogging(builder => builder.AddConsole());
services.AddScoped<HelloWorldJobHandler>();

var provider = services.BuildServiceProvider();

// Create a job that uses HelloWorldJobHandler
var helloWorldJob = new Job
{
    Name = "HelloWorldDemo",
    Description = "Simple hello world demonstration job",
    CronExpression = "*/5 * * * *",
    HandlerType = typeof(HelloWorldJobHandler).FullName!,
    Priority = JobPriority.Normal,
    IsActive = true,
    MaxRetries = 2,
    ExecutionTimeoutSeconds = 30
};

// Execute the job
using var scope = provider.CreateScope();
var handler = scope.ServiceProvider.GetRequiredService<HelloWorldJobHandler>();
var result = await handler.ExecuteAsync(helloWorldJob, CancellationToken.None);

// result will contain: "Hello World job completed at {timestamp}"
```

To run a complete example with job scheduling:

```csharp
using var scope = provider.CreateScope();
var schedulerService = scope.ServiceProvider.GetRequiredService<JobSchedulerService>();

// Create and schedule the job
var createdJob = await schedulerService.CreateJobAsync(helloWorldJob, "demo");

// Execute due jobs
var executions = await schedulerService.ExecuteDueJobsAsync();
```

## ReportGenerationJobHandler

The `ReportGenerationJobHandler` class implements the `IJobHandler` interface and is responsible for generating scheduled reports by processing job data and producing formatted output. It executes asynchronously and returns a summary of the generated report content.

The following example demonstrates how to use the public members of `ReportGenerationJobHandler`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Services;

// Register the handler in your DI container
var services = new ServiceCollection();
services.AddLogging(builder => builder.AddConsole());
services.AddScoped<ReportGenerationJobHandler>();

var provider = services.BuildServiceProvider();

// Create a job that uses ReportGenerationJobHandler
var reportJob = new Job
{
    Name = "DailyReport",
    Description = "Generates daily report",
    CronExpression = "0 9 * * *",
    HandlerType = typeof(ReportGenerationJobHandler).FullName!,
    Priority = JobPriority.High,
    IsActive = true,
    MaxRetries = 2,
    ExecutionTimeoutSeconds = 300
};

// Execute the job
using var scope = provider.CreateScope();
var handler = scope.ServiceProvider.GetRequiredService<ReportGenerationJobHandler>();
var result = await handler.ExecuteAsync(reportJob, CancellationToken.None);

// result will contain: "Report generated: 10,500 records processed"
```

To run a complete example with job scheduling:

```csharp
public sealed class ReportGenerationJobHandler : IJobHandler
{
    private readonly ILogger<ReportGenerationJobHandler> _logger;

    public ReportGenerationJobHandler(ILogger<ReportGenerationJobHandler> logger)
    {
        _logger = logger;
    }

    public async Task<string> ExecuteAsync(Job job, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating report...");
        await Task.Delay(250, cancellationToken);
        return "Report generated: 10,500 records processed";
    }
}
```

```csharp
using DotnetJobScheduler.Tests;
using JobScheduler.Core.Data.Repositories;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Services;
using Moq;
using Xunit;

// Create a test instance
var tests = new ConcurrencyManagerTests();

// Test CanExecuteAsync with available capacity
await tests.CanExecuteAsync_WithAvailableCapacity_ReturnsTrue();

// Test CanExecuteAsync with exceeded global concurrency
await tests.CanExecuteAsync_WithExceededGlobalConcurrency_ReturnsFalse();

// Test EnsureCanExecuteAsync with available capacity
await tests.EnsureCanExecuteAsync_WithAvailableCapacity_CompletesSuccessfully();

// Test IncrementConcurrencyCount
tests.IncrementConcurrencyCount_IncrementsJobCounter();

// Test DecrementConcurrencyCount
tests.DecrementConcurrencyCount_DecrementsCounter();
```

## ScheduleServiceTests

The `ScheduleServiceTests` class provides unit tests for the `ScheduleService` class, ensuring correct job scheduling and execution time calculations. These tests cover various scenarios, including job scheduling, cron expression processing, and execution frequency calculations. The following example demonstrates how to use some of the public members of `ScheduleServiceTests`:

```csharp
using DotnetJobScheduler.Tests;
using JobScheduler.Core.Data.Repositories;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Services;
using Moq;
using Xunit;

// Create a test instance
var tests = new ScheduleServiceTests();

// Test getting upcoming execution times for a valid job
await tests.GetUpcomingExecutionTimesAsync_WithValidJob_ReturnsMultipleTimes();

// Test getting upcoming execution times for an inactive job
await tests.GetUpcomingExecutionTimesAsync_WithInactiveJob_ReturnsEmpty();

// Test getting upcoming execution times for a nonexistent job
await tests.GetUpcomingExecutionTimesAsync_WithNonexistentJob_ReturnsEmpty();

// Test getting execution frequency per day for a valid cron expression
await tests.GetExecutionFrequencyPerDayAsync_CalculatesCorrectly();
```

This example shows how to programmatically drive the scheduling tests, which can be useful for custom test harnesses or debugging complex scheduling scenarios.
