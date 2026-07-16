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

## ValidationUtility

The `ValidationUtility` class provides validation methods for job scheduler configuration and runtime data. It ensures that job names, cron expressions, handler types, job configurations, JSON parameters, pagination parameters, and retry strategies conform to expected formats and business rules before jobs are created or executed.


### Usage

```csharp
using JobScheduler.Core.Utilities;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Domain.Enums;

// Validate a job name (alphanumeric with underscores and hyphens, 3-50 characters)
var jobName = "data_processing_job";
var nameValidation = ValidationUtility.ValidateJobName(jobName);
if (!nameValidation.IsValid)
{
    Console.WriteLine($"Invalid job name: {nameValidation.Message}");
}

// Validate a cron expression (standard cron format)
var cronExpression = "0 0 * * *";
var cronValidation = ValidationUtility.ValidateCronExpression(cronExpression);
if (!cronValidation.IsValid)
{
    Console.WriteLine($"Invalid cron expression: {cronValidation.Message}");
}

// Validate a handler type (format: "Namespace.ClassName, AssemblyName")
var handlerType = "JobScheduler.Core.Jobs.DataProcessingJob, JobScheduler.Core";
var handlerValidation = ValidationUtility.ValidateHandlerType(handlerType);
if (!handlerValidation.IsValid)
{
    Console.WriteLine($"Invalid handler type: {handlerValidation.Message}");
}

// Validate a complete job configuration
var jobConfig = new JobConfiguration
{
    Name = "report_generation_job",
    CronExpression = "0 12 * * *",
    HandlerType = "JobScheduler.Core.Jobs.ReportGeneratorJob, JobScheduler.Core",
    Parameters = "{\"ReportType\":\"Monthly\",\"Format\":\"PDF\"}",
    Enabled = true
};
var configValidation = ValidationUtility.ValidateJobConfiguration(jobConfig);
ValidationUtility.ThrowIfInvalid(configValidation);
Console.WriteLine("Job configuration is valid!");

// Validate JSON parameters
var jsonParameters = "{\"Source\":\"api\",\"BatchSize\":100}";
var jsonValidation = ValidationUtility.ValidateJsonParameters(jsonParameters);
if (!jsonValidation.IsValid)
{
    Console.WriteLine($"Invalid JSON parameters: {jsonValidation.Message}");
}

// Validate pagination parameters
var pagination = new PaginationParameters { PageNumber = 1, PageSize = 25 };
var paginationValidation = ValidationUtility.ValidatePagination(pagination);
if (!paginationValidation.IsValid)
{
    Console.WriteLine($"Invalid pagination: {paginationValidation.Message}");
}

// Validate retry strategy
var retryStrategy = new RetryStrategy
{
    MaxAttempts = 3,
    BackoffMultiplier = 2.0,
    InitialDelaySeconds = 5
};
var retryValidation = ValidationUtility.ValidateRetryStrategy(retryStrategy);
if (!retryValidation.IsValid)
{
    Console.WriteLine($"Invalid retry strategy: {retryValidation.Message}");
}
```

<!-- ... rest of README content -->
