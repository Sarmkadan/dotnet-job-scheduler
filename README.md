// ... existing content ...

## DatabaseLeaderElectionServiceTestsExtensions

The `DatabaseLeaderElectionServiceTestsExtensions` class supplies a collection of extension methods that simplify writing unit‑tests for `DatabaseLeaderElectionService`.  
It lets you spin up isolated in‑memory databases, create paired services that share the same context, query the underlying `SchedulerLeaderLock` rows, and assert that leadership is correctly acquired or released.

**Usage example**

```csharp
using System;
using System.Threading.Tasks;
using JobScheduler.Core.Data;
using JobScheduler.Core.Services;
using DotnetJobScheduler.Tests; // contains DatabaseLeaderElectionServiceTests

public class LeaderElectionDemo
{
    public async Task RunAsync()
    {
        // 1️⃣ Create an isolated service with its own in‑memory DB
        var test = new DatabaseLeaderElectionServiceTests();
        var isolatedService = test.CreateIsolatedService();

        // 2️⃣ Create a pair of services that share the same DB (useful for coordination tests)
        var (service1, service2) = test.CreateServicePair();

        // 3️⃣ Acquire leadership with the first service and verify it became leader
        await test.AssertHasLeadershipAsync(service1, service1.InstanceId);

        // 4️⃣ The second service should not be able to acquire leadership while the first holds the lock
        var acquiredBySecond = await service2.TryAcquireLeadershipAsync();
        acquiredBySecond.Should().BeFalse("second instance must not acquire the lock while first is leader");

        // 5️⃣ Query the current leader lock directly from the DB
        var context = service1.GetContextForTesting();
        var currentLock = await test.GetCurrentLeaderLockAsync(context);
        Console.WriteLine($"Current leader: {currentLock?.LeaderInstanceId}");

        // 6️⃣ Release leadership from the first service and verify the lock disappears
        await test.AssertHasReleasedLeadershipAsync(service1, service1.InstanceId);

        // 7️⃣ Create a short‑lease service to test lease‑expiration scenarios
        var shortLeaseService = test.CreateShortLeaseService(leaseDurationSeconds: 1);
        await shortLeaseService.TryAcquireLeadershipAsync(); // acquire quickly
        await Task.Delay(TimeSpan.FromSeconds(2));           // let the lease expire

        // 8️⃣ Retrieve the full leadership acquisition history for the short‑lease instance
        var history = await test.GetLeadershipHistoryAsync(context, shortLeaseService.InstanceId);
        Console.WriteLine($"Acquisition attempts: {history.Count}");
    }
}
```

The example demonstrates how the extension methods can be combined to:

* spin up isolated or shared test environments,
* acquire and release leadership,
* inspect the underlying `SchedulerLeaderLock` rows,
* and verify the expected behavior with FluentAssertions.

## JobEntityTestsExtensions

The `JobEntityTestsExtensions` class provides extension methods for creating test job entities with various configurations and validating job configurations. It simplifies test setup by providing factory methods for common job scenarios and validation helpers.

**Usage example**

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using JobScheduler.Core.Domain.Entities;
using DotnetJobScheduler.Tests; // contains JobEntityTestsExtensions

public class JobEntityTestsDemo
{
    public void RunDemo()
    {
        var tests = new JobEntityTests(); // Instance needed for extension methods
        
        // Create a minimally valid job for testing
        var validJob = tests.CreateMinimalValidJob("test-job", "MyHandler, MyAssembly");
        
        // Create a job with execution metrics for testing success rate calculations
        var jobWithMetrics = tests.CreateJobWithMetrics(8, 2, "metrics-job");
        
        // Get formatted metrics summary
        var metricsSummary = tests.GetExecutionMetricsSummary(jobWithMetrics);
        Console.WriteLine(metricsSummary); // Output: Executions: 10, Success: 8, Failure: 2, Success Rate: 80.0%
        
        // Create a suspended job to test execution blocking
        var suspendedJob = tests.CreateSuspendedJob("suspended-job");
        
        // Create a job configured for concurrency testing
        var concurrentJob = tests.CreateConcurrentJob(5, 2, "concurrent-job");
        
        // Create an invalid job for negative testing
        var invalidJob = tests.CreateInvalidHandlerJob("invalid-job");
        
        // Validate job configuration and get detailed error messages
        var validationErrors = tests.GetValidationErrors(invalidJob).ToList();
        Console.WriteLine($"Validation errors: {string.Join(", ", validationErrors)}");
        // Output: Validation errors: HandlerType is required
    }
}
```

The example demonstrates how the extension methods can be combined to:

* create jobs with various configurations for different test scenarios,
* generate jobs with pre-populated execution metrics for testing calculations,
* create jobs with specific statuses (suspended, concurrent limits) for behavioral testing,
* generate invalid configurations for negative testing scenarios,
* and retrieve detailed validation error messages for job configuration validation.