<!-- ... existing content ... -->

## JobHelper

The `JobHelper` class provides utility methods for job operations including parameter serialization, status descriptions, execution frequency analysis, and reliability scoring. It's used throughout the job scheduler to standardize job data handling and provide human-readable job information.

### Usage

```csharp
using JobScheduler.Core.Utilities;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Constants;

// Serialize job handler parameters to JSON
var parameters = new { Source = "api", BatchSize = 100, Timeout = 30 };
var serialized = JobHelper.SerializeParameters(parameters);
Console.WriteLine($"Serialized parameters: {serialized}");

// Deserialize job handler parameters from JSON
var jsonParameters = "{\"Source\":\"database\",\"BatchSize\":50}";
var deserialized = JobHelper.DeserializeParameters<JobParameters>(jsonParameters);
Console.WriteLine($"Deserialized parameters: Source={deserialized?.Source}, BatchSize={deserialized?.BatchSize}");

// Get human-readable job status description
var job = new Job
{
    Status = JobStatus.Scheduled,
    NextExecutionAt = DateTime.UtcNow.AddHours(1)
};
var statusDescription = JobHelper.GetJobStatusDescription(job);
Console.WriteLine($"Job status: {statusDescription}");

// Validate handler type format ("Namespace.ClassName, AssemblyName")
var validHandler = "JobScheduler.Core.Jobs.ReportGeneratorJob, JobScheduler.Core";
var isValid = JobHelper.IsValidHandlerType(validHandler);
Console.WriteLine($"Is handler type valid? {isValid}");

// Get execution frequency description from cron expression
var cronExpression = "0 0 * * *";
var frequencyDescription = JobHelper.GetExecutionFrequencyDescription(cronExpression);
Console.WriteLine($"Execution frequency: {frequencyDescription}");

// Calculate job reliability score (0-100)
var jobWithHistory = new Job
{
    TotalExecutions = 100,
    SuccessfulExecutions = 85
};
var reliabilityScore = JobHelper.CalculateReliabilityScore(jobWithHistory);
Console.WriteLine($"Reliability score: {reliabilityScore}/100");

// Get recommended action based on job state
var failedJob = new Job
{
    Status = JobStatus.Failed,
    TotalExecutions = 15,
    SuccessfulExecutions = 5
};
var recommendedAction = JobHelper.GetRecommendedAction(failedJob);
Console.WriteLine($"Recommended action: {recommendedAction}");

// Format duration in milliseconds to human-readable format
var durationMs = 125000L; // 2 minutes and 5 seconds
var formattedDuration = JobHelper.FormatDuration(durationMs);
Console.WriteLine($"Job duration: {formattedDuration}");

// Check if job behavior is concerning
var concerningJob = new Job
{
    Status = JobStatus.Failed,
    TotalExecutions = 15,
    SuccessfulExecutions = 3
};
var isConcerning = JobHelper.IsConcerning(concerningJob);
Console.WriteLine($"Is job concerning? {isConcerning}");
```

<!-- ... rest of README content -->
