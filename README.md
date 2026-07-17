// ... existing content ...

## DistributedJobLockServiceTests

The `DistributedJobLockServiceTests` class provides unit tests for the `DistributedJobLockService` class, focusing on distributed lock acquisition, renewal, and release functionality. These tests ensure that the service behaves correctly under various scenarios.

The following example demonstrates how to use some of the public members of `DistributedJobLockServiceTests` in your application:

```csharp
using DotnetJobScheduler.Tests;
using JobScheduler.Core.Data;
using JobScheduler.Core.Services;

// Create an instance of DistributedJobLockServiceTests
var tests = new DistributedJobLockServiceTests();

// Create a test lock
var jobId = Guid.NewGuid();
var service = DistributedJobLockServiceTests.CreateService();

// Try acquiring a lock
var acquired = await service.TryAcquireLockAsync(jobId, "node-1", TimeSpan.FromMinutes(5));

// Check if the lock is active
var locked = await service.IsLockedAsync(jobId);

// Release the lock
await service.ReleaseLockAsync(jobId, "node-1");

// Try renewing a lock
var renewed = await service.RenewLockAsync(jobId, "node-1", TimeSpan.FromMinutes(10));

// Clean expired locks
await service.CleanExpiredLocksAsync();

// Get active locks
var activeLocks = await service.GetActiveLocksAsync();
``` 
