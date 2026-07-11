# CsvExportFormatter

`CsvExportFormatter` provides static methods for serializing job definitions, execution records, and aggregated statistics into CSV format, as well as parsing CSV content back into structured `JobCsvRow` objects. It also defines the `JobCsvRow` type, which represents a single row in a jobs CSV file with fields covering identity, scheduling, status, handler configuration, retry policy, and execution metrics.

## API

### Static Methods

#### `ExportJobsToCsv`

```csharp
public static string ExportJobsToCsv(IEnumerable<JobCsvRow> jobs)
```

Generates a CSV string from a collection of `JobCsvRow` instances. Each row is serialized with all public fields in a consistent column order.

- **Parameters**: `jobs` — the collection of job rows to serialize.
- **Returns**: a `string` containing the full CSV content, including a header row.
- **Exceptions**: throws `ArgumentNullException` when `jobs` is `null`.

#### `ExportExecutionsToCsv`

```csharp
public static string ExportExecutionsToCsv(IEnumerable<ExecutionRecord> executions)
```

Generates a CSV string from a collection of execution records. The output includes execution-level details such as start time, end time, outcome, and associated job identifier.

- **Parameters**: `executions` — the collection of execution records to serialize.
- **Returns**: a `string` containing the full CSV content, including a header row.
- **Exceptions**: throws `ArgumentNullException` when `executions` is `null`.

#### `ExportStatisticsToCsv`

```csharp
public static string ExportStatisticsToCsv(Dictionary<Guid, JobStatistics> statistics)
```

Generates a CSV string from a dictionary mapping job identifiers to their aggregated statistics. Each row combines the job ID with computed metrics such as total executions and success rate.

- **Parameters**: `statistics` — a dictionary where keys are job GUIDs and values are `JobStatistics` objects.
- **Returns**: a `string` containing the full CSV content, including a header row.
- **Exceptions**: throws `ArgumentNullException` when `statistics` is `null`.

#### `ParseJobsCsv`

```csharp
public static List<JobCsvRow> ParseJobsCsv(string csvContent)
```

Parses a CSV string and returns a list of `JobCsvRow` instances. The first row is treated as a header and skipped during parsing. Fields are mapped by column position.

- **Parameters**: `csvContent` — the raw CSV string to parse.
- **Returns**: a `List<JobCsvRow>` containing one element per data row.
- **Exceptions**: throws `ArgumentNullException` when `csvContent` is `null`; throws `FormatException` when the CSV structure is malformed or a required field cannot be parsed.

### JobCsvRow Fields

| Field | Type | Description |
|---|---|---|
| `Id` | `Guid` | Unique identifier of the job. |
| `Name` | `string` | Display name of the job. |
| `Description` | `string` | Human-readable description of the job's purpose. |
| `CronExpression` | `string` | Cron expression defining the job's schedule. |
| `Priority` | `string` | Priority level of the job (e.g., Low, Normal, High, Critical). |
| `Status` | `string` | Current lifecycle status of the job. |
| `IsActive` | `bool` | Whether the job is enabled for scheduling and execution. |
| `HandlerType` | `string` | Fully qualified type name of the job handler. |
| `MaxRetries` | `int` | Maximum number of retry attempts on failure. |
| `ExecutionTimeoutSeconds` | `int` | Timeout in seconds for a single execution. |
| `NextExecution` | `string` | Formatted string representing the next scheduled execution time, or empty if not scheduled. |
| `LastExecution` | `string` | Formatted string representing the most recent execution time, or empty if never executed. |
| `TotalExecutions` | `int` | Total number of execution attempts, including retries. |
| `SuccessRate` | `double` | Percentage of successful executions, represented as a value between 0 and 100. |

## Usage

### Example 1: Exporting jobs to CSV and saving to disk

```csharp
var rows = new List<JobCsvRow>
{
    new JobCsvRow
    {
        Id = Guid.NewGuid(),
        Name = "DailyReport",
        Description = "Generates the end-of-day summary report",
        CronExpression = "0 0 18 * * ?",
        Priority = "High",
        Status = "Active",
        IsActive = true,
        HandlerType = "MyApp.Jobs.ReportJobHandler",
        MaxRetries = 3,
        ExecutionTimeoutSeconds = 300,
        NextExecution = "2025-03-21T18:00:00Z",
        LastExecution = "2025-03-20T18:00:02Z",
        TotalExecutions = 47,
        SuccessRate = 97.87
    },
    new JobCsvRow
    {
        Id = Guid.NewGuid(),
        Name = "CacheWarmup",
        Description = "Pre-warms distributed cache before peak hours",
        CronExpression = "0 0 7 * * ?",
        Priority = "Normal",
        Status = "Active",
        IsActive = true,
        HandlerType = "MyApp.Jobs.CacheWarmupHandler",
        MaxRetries = 1,
        ExecutionTimeoutSeconds = 120,
        NextExecution = "2025-03-21T07:00:00Z",
        LastExecution = "2025-03-20T07:00:01Z",
        TotalExecutions = 31,
        SuccessRate = 100.0
    }
};

string csv = CsvExportFormatter.ExportJobsToCsv(rows);
await File.WriteAllTextAsync("jobs_export.csv", csv);
```

### Example 2: Round-tripping jobs through CSV

```csharp
// Export current job definitions
string exported = CsvExportFormatter.ExportJobsToCsv(existingJobRows);
File.WriteAllText("backup.csv", exported);

// Later: parse the backup and inspect contents
string csvContent = File.ReadAllText("backup.csv");
List<JobCsvRow> parsed = CsvExportFormatter.ParseJobsCsv(csvContent);

foreach (var row in parsed)
{
    Console.WriteLine($"Job '{row.Name}' — Status: {row.Status}, " +
                      $"Success Rate: {row.SuccessRate:F1}%, " +
                      $"Next: {row.NextExecution}");
}
```

## Notes

- All static export methods include a header row as the first line of the CSV output. `ParseJobsCsv` expects this header row and skips it; CSVs generated by the export methods are therefore directly compatible with the parse method.
- `ParseJobsCsv` relies on positional column mapping. Reordering or adding columns in the CSV outside of the defined schema will cause `FormatException` or incorrect field assignment.
- The `NextExecution` and `LastExecution` fields are serialized as string representations. When a job has never been executed, `LastExecution` is an empty string. When no future execution is scheduled, `NextExecution` is an empty string.
- `SuccessRate` is expressed as a percentage (0–100). A value of `-1` or `double.NaN` in source data may appear in the CSV output; consumers should handle these sentinel values when parsing.
- The static methods are stateless and thread-safe. They can be called concurrently from multiple threads without external synchronization.
- `ExportStatisticsToCsv` accepts a dictionary with `Guid` keys. Duplicate keys are not possible by definition, but an empty dictionary produces a CSV with only a header row.
- CSV escaping follows standard conventions: fields containing commas, double quotes, or line breaks are wrapped in double quotes, and embedded double quotes are doubled.
