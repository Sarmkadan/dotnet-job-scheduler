// ... existing content ...

## DatabaseLeaderElectionServiceTests

The `DatabaseLeaderElectionServiceTests` class provides unit tests for the `DatabaseLeaderElectionService` class, focusing on distributed leadership election, lease management, and concurrency scenarios. These tests ensure that the service behaves correctly under various scenarios.

The following example demonstrates how to use some of the public members of `DatabaseLeaderElectionServiceTests` in your application:

```csharp
using DotnetJobScheduler.Tests;
using JobScheduler.Core.Data;
using JobScheduler.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

// Create an instance of DatabaseLeaderElectionServiceTests
var tests = new DatabaseLeaderElectionServiceTests();

// Initialize the test
await tests.InitializeAsync();

// Try acquiring leadership
var service = tests.CreateService("node-1");
var acquired = await service.TryAcquireLeadershipAsync();

// Check if the service is leader
var isLeader = service.IsLeader;

// Release leadership
await service.ReleaseLeadershipAsync();

// Dispose of the test
await tests.DisposeAsync();
``` 
