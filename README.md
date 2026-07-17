// ... existing content ...

## LongRunningJobHandler

The `LongRunningJobHandler` class implements the `IJobHandler` interface and simulates a long-running operation. It executes asynchronously and returns a completion message.

### Usage Example

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Register the handler in your DI container
var services = new ServiceCollection();
services.AddLogging(builder => builder.AddConsole());
services.AddScoped<LongRunningJobHandler>();

var provider = services.BuildServiceProvider();

// Create and execute the job
var handler = provider.GetRequiredService<LongRunningJobHandler>();
var job = new Job(); // Assuming a Job instance is created properly
var result = await handler.ExecuteAsync(job, CancellationToken.None);

// result will contain: "Completed long operation"
```

## UnstableExternalApiJobHandler

`UnstableExternalApiJobHandler` demonstrates how to work with transient failures.  
It logs each attempt, throws an exception on the first two calls to simulate an unstable external API, and succeeds on subsequent attempts. The class is useful for testing retry and error‑handling logic in the scheduler.

### Usage Example

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using JobScheduler.Core.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

// Register the handlers and logging
var services = new ServiceCollection();
services.AddLogging(builder => builder.AddConsole());

services.AddScoped<UnstableExternalApiJobHandler>();
services.AddScoped<DatabaseQueryJobHandler>();
services.AddScoped<GracefulFailureJobHandler>();

var provider = services.BuildServiceProvider();

// A job instance (populate as needed)
var job = new Job();

// Execute UnstableExternalApiJobHandler
var unstableHandler = provider.GetRequiredService<UnstableExternalApiJobHandler>();
string unstableResult = await unstableHandler.ExecuteAsync(job, CancellationToken.None);
Console.WriteLine(unstableResult);

// Execute DatabaseQueryJobHandler
var dbHandler = provider.GetRequiredService<DatabaseQueryJobHandler>();
string dbResult = await dbHandler.ExecuteAsync(job, CancellationToken.None);
Console.WriteLine(dbResult);

// Execute GracefulFailureJobHandler
var gracefulHandler = provider.GetRequiredService<GracefulFailureJobHandler>();
string gracefulResult = await gracefulHandler.ExecuteAsync(job, CancellationToken.None);
Console.WriteLine(gracefulResult);
```

## CyclicDependencyExceptionExtensions

The `CyclicDependencyExceptionExtensions` class provides utility methods to enhance error handling and diagnostics for cyclic dependency detection in job scheduling. It offers functionality to format error details, check for specific job involvement, and generate diagnostic summaries.

### Usage Example

```csharp
using JobScheduler.Core.Exceptions;
using System;
using System.Collections.Generic;

// Assuming CyclicDependencyException is defined in JobScheduler.Core.Exceptions
// and has the required properties (JobId, DependsOnJobId, ErrorCode, Message).

var jobId = Guid.Parse("11111111-1111-1111-1111-111111111111");
var dependsOnJobId = Guid.Parse("22222222-2222-2222-2222-222222222222");

// Create an instance of the exception
var exception = new CyclicDependencyException
{
    JobId = jobId,
    DependsOnJobId = dependsOnJobId,
    ErrorCode = "CYCLE_DETECTED",
    Message = "A cyclic dependency was found in the job graph."
};

// 1. Get a human-readable description
string description = exception.GetDescription();
Console.WriteLine($"Description: {description}");

// 2. Check if a specific job is involved in the cycle
bool isInvolved = exception.InvolvesJob(jobId);
Console.WriteLine($"Is Job {jobId} involved? {isInvolved}");

// 3. Get a detailed description including error code
string details = exception.FormatDetails();
Console.WriteLine($"Details: {details}");

// 4. Check if the exception matches a specific error code
bool isCycleError = exception.IsSpecificError("CYCLE_DETECTED");
Console.WriteLine($"Is specific error 'CYCLE_DETECTED'? {isCycleError}");

// 5. Get a summary dictionary for logging
IReadOnlyDictionary<string, object> summary = exception.GetSummary();
Console.WriteLine("Summary:");
foreach (var kvp in summary)
{
    Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
}
```

// ... existing content ...
