# V2 Basic Usage Example

This example demonstrates the basic usage of dotnet-job-scheduler v2.0 features.

## Features Demonstrated

- ✅ v2.0 package reference
- ✅ New configuration options
- ✅ Basic job creation and execution
- ✅ Background service integration


## Prerequisites


- .NET 10.0 SDK
- SQLite (included in .NET)


## Running the Example


```bash
# Navigate to example directory
cd examples/v2-basic-usage

# Restore packages
dotnet restore

# Run the application
dotnet run
```

## Expected Output


```
info: JobScheduler.Examples.V2BasicUsage.BasicJobHandler[0]
      Executing basic job: V2BasicUsage
Created job with ID: 1
```

## Key v2.0 Features Used


1. **Package Version**: Uses v2.0.0 of the scheduler
2. **Configuration**: Uses the updated JobSchedulerSettings
3. **Background Service**: Uses the built-in JobSchedulerBackgroundService

## Code Highlights


```csharp
// Configure scheduler with v2.0 settings
services.AddJobScheduler(options =>
{
    options.ConnectionString = "Data Source=scheduler.db";
    options.MaxConcurrentJobs = 5;
    options.DefaultTimeoutSeconds = 300;
    options.DefaultMaxRetries = 2;
});

// Create job with standard v2.0 API
var job = new Job
{
    Name = "V2BasicUsage",
    CronExpression = "*/5 * * * *",
    HandlerType = typeof(BasicJobHandler).FullName,
    Priority = JobPriority.Normal,
    IsActive = true
};
```

## Next Steps


- Review the [Migration Guide](../docs/migration-guide-v2.md) for v2.0 changes
- Check the [Docker Guide](../docs/docker-guide.md) for container deployment
- See the [Getting Started Guide](../docs/getting-started.md) for more examples
