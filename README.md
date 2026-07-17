## JobSchedulerServiceTests

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

## RetryServiceTests

The `RetryServiceTests` class provides comprehensive unit tests for the `RetryService` class, ensuring correct retry policy application, backoff calculations, and budget enforcement. These tests cover various scenarios for job retry decisions.

The following example demonstrates how to use some of the public members of `RetryServiceTests`:

```csharp
using tests;
using JobScheduler.Core.Services;
using JobScheduler.Core.Domain.Entities;

// Create a test instance
var retryTests = new RetryServiceTests();

// Build test job and execution
var job = RetryServiceTests.BuildJob(maxRetries: 3);
var execution = RetryServiceTests.BuildFailedExecution(attempt: 2);

// Test retry decisions
var shouldRetry = await retryTests.ShouldRetryAsync(job, execution);
Assert.False(shouldRetry);

// Calculate backoff delay
var delay = retryTests.CalculateBackoffDelay(job, attemptNumber: 2);
Assert.Equal(10, delay);

// Create a new retry execution
var retryExecution = retryTests.CreateRetryExecution(job, execution);
Assert.Equal(3, retryExecution.AttemptNumber);
Assert.Equal(ExecutionStatus.Running, retryExecution.Status);

// Check retry budget
var exceeded = await retryTests.IsRetryBudgetExceededAsync(job.Id, retryBudgetCount: 5, timeWindowMinutes: 5);
Assert.False(exceeded);
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
