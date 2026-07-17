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

// Test recording error on exception
await jobExecutorServiceTests.ExecuteJobAsync_WithExceptionDuringExecution_RecordsError();
```
