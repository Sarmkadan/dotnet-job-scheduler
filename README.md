// ... (rest of README.md content)

## CsvProcessingBenchmarks

The `CsvProcessingBenchmarks` class measures CSV parsing and escaping performance used by the export formatter and audit log serializer. It benchmarks both simple and complex CSV scenarios, including lines with quoted fields, embedded commas, and wide rows with many columns.



Example usage:
```csharp
// Parse CSV lines with different formats
var simpleResult = CsvProcessingBenchmarks.ParseCsvLine_Simple();
var quotedResult = CsvProcessingBenchmarks.ParseCsvLine_Quoted();
var wideResult = CsvProcessingBenchmarks.ParseCsvLine_Wide();

// Escape fields for CSV output
var plainEscaped = CsvProcessingBenchmarks.EscapeCsvField_Plain();
var commaEscaped = CsvProcessingBenchmarks.EscapeCsvField_Comma();
var quotesEscaped = CsvProcessingBenchmarks.EscapeCsvField_Quotes();

// Parse priority values
var highPriority = CsvProcessingBenchmarks.ParsePriority_ByName();
var defaultPriority = CsvProcessingBenchmarks.ParsePriority_Default();
```

## JobSchedulerSettingsExtensions

The `JobSchedulerSettingsExtensions` class provides extension methods for validating, cloning, and analyzing `JobSchedulerSettings` configurations. It helps ensure that settings are properly configured and makes it easier to work with job scheduler settings.

Example usage:
```csharp
var settings = new JobSchedulerSettings
{
    ConnectionString = "ExampleConnectionString",
    MaxConcurrentJobs = 5,
    DefaultTimeoutSeconds = 300,
    DefaultMaxRetries = 3,
    DefaultRetryBackoffSeconds = 10,
    QueuePollIntervalMs = 1000,
    EnableCleanup = true,
    CleanupIntervalMs = 60000,
    MaxJobNameLength = 255,
    MaxCronExpressionLength = 255
};

// Validate settings
var errors = settings.Validate();
if (errors.Count > 0)
{
    Console.WriteLine("Validation errors:");
    foreach (var error in errors)
    {
        Console.WriteLine(error);
    }
}

// Clone settings
var clonedSettings = settings.Clone();

// Check if cleanup is enabled
bool isCleanupEnabled = settings.IsCleanupEnabled();

// Get effective timeout
int effectiveTimeoutMs = settings.GetEffectiveTimeoutMs(jobSpecificTimeoutSeconds: 600);

// Get maximum job name length
int maxJobNameLength = settings.GetMaxJobNameLength();

Console.WriteLine($"Is cleanup enabled: {isCleanupEnabled}");
Console.WriteLine($"Effective timeout: {effectiveTimeoutMs}ms");
Console.WriteLine($"Max job name length: {maxJobNameLength}");
```

## JobManagementBenchmarks

The `JobManagementBenchmarks` class measures performance of core job management operations including slug generation, JSON escaping, handler type masking, and status formatting. It benchmarks both simple and complex scenarios to provide realistic performance baselines for job identifier generation and string processing used throughout the scheduler.

Example usage:
```csharp
// Generate job slugs from different job names
var simpleSlug = JobManagementBenchmarks.GenerateJobSlug_Simple();
var complexSlug = JobManagementBenchmarks.GenerateJobSlug_Complex();
var longSlug = JobManagementBenchmarks.GenerateJobSlug_Long();

// Escape job descriptions for JSON serialization
var cleanEscaped = JobManagementBenchmarks.EscapeJobDescription_Clean();
var specialEscaped = JobManagementBenchmarks.EscapeJobDescription_Special();

// Process job descriptions
var truncatedDesc = JobManagementBenchmarks.TruncateJobDescription();

// Mask handler type names for security/logging
var maskedHandler = JobManagementBenchmarks.MaskHandlerType();

// Parse job priority values
var highPriority = JobManagementBenchmarks.ParseJobPriority_High();
var normalPriority = JobManagementBenchmarks.ParseJobPriority_Normal();
var lowPriority = JobManagementBenchmarks.ParseJobPriority_Low();
var defaultPriority = JobManagementBenchmarks.ParseJobPriority_Default();

// Create job identifiers
var jobId = JobManagementBenchmarks.CreateJobIdentifier();

// Format job status
var statusText = JobManagementBenchmarks.FormatJobStatus();

Console.WriteLine($"Job ID: {jobId}");
Console.WriteLine($"Status: {statusText}");
Console.WriteLine($"High priority: {highPriority}");
```
