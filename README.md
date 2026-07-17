<!-- ... existing content ... -->

## CsvExportFormatter

The `CsvExportFormatter` class provides functionality for exporting job and execution data to CSV format. It offers methods for exporting jobs, executions, and statistics, as well as parsing CSV data back into objects. The following example demonstrates how to use `CsvExportFormatter` to export a list of jobs to CSV:

```csharp
using JobScheduler.Core.Formatters;
using JobScheduler.Core.Domain.Entities;
using System.Collections.Generic;

// Sample list of jobs
var jobs = new List<Job>
{
    new Job { Id = Guid.NewGuid(), Name = "Job1", Description = "Description1" },
    new Job { Id = Guid.NewGuid(), Name = "Job2", Description = "Description2" }
};

// Export jobs to CSV
var csv = CsvExportFormatter.ExportJobsToCsv(jobs);

// Print the CSV data
Console.WriteLine(csv);

// Alternatively, export executions or statistics
var executions = new List<JobExecution>
{
    new JobExecution { Id = Guid.NewGuid(), JobId = Guid.NewGuid(), Status = "Success" },
    new JobExecution { Id = Guid.NewGuid(), JobId = Guid.NewGuid(), Status = "Failed" }
};
var executionsCsv = CsvExportFormatter.ExportExecutionsToCsv(executions);
Console.WriteLine(executionsCsv);

var stats = new Dictionary<Guid, (int Total, int Successful, long AvgTime)>
{
    { Guid.NewGuid(), (10, 8, 1000) },
    { Guid.NewGuid(), (20, 15, 2000) }
};
var statsCsv = CsvExportFormatter.ExportStatisticsToCsv(stats);
Console.WriteLine(statsCsv);
```

## CronExpressionServiceTests

The `CronExpressionServiceTests` class contains unit tests for the `CronExpressionService` that validate cron expression parsing, validation, and execution time calculation. It tests various scenarios including valid and invalid cron expressions, timezone handling, leap year calculations, and multiple execution time scenarios.

The following example demonstrates how to use the `CronExpressionService` methods in your application:

```csharp
using JobScheduler.Core.Services;
using JobScheduler.Core.Exceptions;

// Create service instance
var cronService = new CronExpressionService();

// Validate a cron expression
bool isValid = cronService.IsValidCronExpression("0 9 * * 1-5"); // Weekdays at 9 AM
Console.WriteLine($"Is valid: {isValid}");

// Parse a cron expression
try
{
    var schedule = cronService.ParseCronExpression("0 0 * * *"); // Daily at midnight
    Console.WriteLine("Successfully parsed cron expression");
}
catch (CronExpressionException ex)
{
    Console.WriteLine($"Failed to parse: {ex.Message}");
}

// Get next execution time from now
var nextExecution = cronService.GetNextExecutionTime("0 * * * *", DateTime.UtcNow);
Console.WriteLine($"Next execution at: {nextExecution}");

// Get multiple next execution times
var nextFiveExecutions = cronService.GetNextExecutionTimes("0 0 * * *", 5).ToList();
Console.WriteLine($"Next 5 executions: {string.Join(", ", nextFiveExecutions)}");

// Check if a specific time should execute
bool shouldExecute = cronService.ShouldExecuteAt("0 12 * * *", DateTime.UtcNow.Date.AddHours(12));
Console.WriteLine($"Should execute at noon: {shouldExecute}");

// Get human-readable description
string description = cronService.GetCronDescription("0 9 * * 1-5");
Console.WriteLine($"Description: {description}");

// Get next execution time in a specific timezone
var nextInZone = cronService.GetNextExecutionTimeInZone(
    "0 9 * * *",
    "Eastern Standard Time",
    DateTime.UtcNow
);
Console.WriteLine($"Next execution in EST: {nextInZone}");
```

<!-- ... rest of README content -->
```