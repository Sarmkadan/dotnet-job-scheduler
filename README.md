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

// 1. Get a human‑readable description
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

## AuditLoggerJsonExtensions

`AuditLoggerJsonExtensions` provides JSON‑serialization helpers for audit‑logging entities such as `AuditLogEntry`, `ApiCallAudit` and `AuditStatistics`. The extensions use `System.Text.Json` with camel‑case naming and optional indentation, and they include safe deserialization helpers for `AuditLogEntry`.

### Usage Example

```csharp
using System;
using JobScheduler.Core.Services;

// Create an audit log entry (populate the properties you need)
var entry = new AuditLogEntry
{
    // Example property assignments
    // Id = Guid.NewGuid(),
    // Timestamp = DateTime.UtcNow,
    // Message = "Job executed successfully"
};

// Serialize to a compact JSON string
string json = entry.ToJson();               // {"id":"...","timestamp":"...","message":"..."}
Console.WriteLine(json);

// Serialize with indentation for readability
string indentedJson = entry.ToJson(indented: true);
Console.WriteLine(indentedJson);

// Deserialize back to an AuditLogEntry (throws on failure)
AuditLogEntry? deserialized = AuditLoggerJsonExtensions.FromJsonToAuditLogEntry(json);
Console.WriteLine(deserialized?.Message);

// Try‑deserialize without throwing
if (AuditLoggerJsonExtensions.TryFromJsonToAuditLogEntry(json, out var tryResult))
{
    Console.WriteLine($"Successfully parsed: {tryResult.Message}");
}
else
{
    Console.WriteLine("Failed to parse JSON.");
}

// Serialize other audit types
var apiCall = new ApiCallAudit
{
    // Populate properties as needed
};

string apiJson = apiCall.ToJson();
Console.WriteLine(apiJson);

var stats = new AuditStatistics
{
    // Populate properties as needed
};

string statsJson = stats.ToJson(indented: true);
Console.WriteLine(statsJson);
```

## CacheServiceValidation

The `CacheServiceValidation` class provides validation helpers for the `CacheService` and related cache entities. It ensures cache keys, values, patterns, and statistics are valid before operations to prevent runtime errors and data integrity issues.

### Usage Example

```csharp
using JobScheduler.Core.Services;
using System;

// Validate a CacheService instance
var cacheService = new CacheService();
var cacheServiceProblems = CacheServiceValidation.Validate(cacheService);
if (cacheServiceProblems.Any())
{
    Console.WriteLine("CacheService has validation issues:");
    foreach (var problem in cacheServiceProblems)
    {
        Console.WriteLine($"- {problem}");
    }
}
else
{
    Console.WriteLine("CacheService is valid!");
}

// Validate a cache key
string cacheKey = "job:123:results";
var keyProblems = CacheServiceValidation.ValidateKey(cacheKey);
if (!keyProblems.Any())
{
    Console.WriteLine("Cache key is valid!");
}
else
{
    Console.WriteLine("Invalid cache key:");
    foreach (var problem in keyProblems)
    {
        Console.WriteLine($"- {problem}");
    }
}

// Validate a cache key pattern
string cacheKeyPattern = "job:*:results";
var patternProblems = CacheServiceValidation.ValidateKeyPattern(cacheKeyPattern);
if (!patternProblems.Any())
{
    Console.WriteLine("Cache key pattern is valid!");
}
else
{
    Console.WriteLine("Invalid cache key pattern:");
    foreach (var problem in patternProblems)
    {
        Console.WriteLine($"- {problem}");
    }
}

// Use EnsureValid to throw exceptions on invalid input
try
{
    CacheServiceValidation.EnsureValidKey("invalid key with spaces");
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Validation failed: {ex.Message}");
}

// Validate cache statistics
var stats = new CacheStatistics
{
    TotalKeys = 100,
    Timestamp = DateTime.UtcNow
};
var statsProblems = CacheServiceValidation.Validate(stats);
if (!statsProblems.Any())
{
    Console.WriteLine("Cache statistics are valid!");
}
```

// ... existing content ...
