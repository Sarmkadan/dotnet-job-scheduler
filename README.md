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

// ... existing content ...
