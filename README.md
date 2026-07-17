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

<!-- ... rest of README content -->
```