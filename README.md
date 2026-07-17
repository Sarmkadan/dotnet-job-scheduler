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
