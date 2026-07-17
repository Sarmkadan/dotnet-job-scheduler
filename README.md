// ... existing content ...

## DailySalesReportJobHandler

The `DailySalesReportJobHandler` class implements the `IJobHandler` interface and is responsible for generating a daily sales report. It executes asynchronously and returns a summary of the generated report.

### Usage Example

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Services;

// Register the handler in your DI container
var services = new ServiceCollection();
services.AddLogging(builder => builder.AddConsole());
services.AddScoped<DailySalesReportJobHandler>();

var provider = services.BuildServiceProvider();

// Create a job that uses DailySalesReportJobHandler
var dailySalesReportJob = new Job
{
    Name = "DailySalesReport",
    Description = "Generates daily sales metrics and sends to management",
    CronExpression = "0 9 * * *",  // 9 AM daily
    HandlerType = typeof(DailySalesReportJobHandler).FullName!,
    Priority = JobPriority.High,
    IsActive = true,
    MaxRetries = 2,
    ExecutionTimeoutSeconds = 300,
    MaxConcurrentExecutions = 1
};

// Execute the job directly
using var scope = provider.CreateScope();
var handler = scope.ServiceProvider.GetRequiredService<DailySalesReportJobHandler>();
var result = await handler.ExecuteAsync(dailySalesReportJob, CancellationToken.None);

// result will contain: "Daily Report: 1250 orders, $45300.50 revenue, Top: Widget Pro"
```

// ... existing content ...
