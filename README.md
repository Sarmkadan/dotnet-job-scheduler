// ... existing content ...

## HelloWorldJobHandlerExtensions

`HelloWorldJobHandlerExtensions` offers a collection of extension methods for `JobSchedulerService` that make it easy to work with the built‑in `HelloWorldJobHandler`.  
With these helpers you can create single or batched hello‑world jobs, retrieve active jobs, search by name pattern, validate a job's configuration, format its next execution time, and create recurring jobs using a simple interval.

**Usage example**

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Services;

public class HelloWorldDemo
{
    public static async Task Main()
    {
        // Assume an already configured JobSchedulerService instance
        var scheduler = new JobSchedulerService(/* dependencies */);

        // 1️⃣ Create a single hello‑world job
        var job = await scheduler.CreateHelloWorldJobAsync(
            name: "DemoHello",
            cronExpression: "*/5 * * * *", // every 5 minutes
            priority: JobPriority.Normal,
            isActive: true,
            maxRetries: 2,
            timeoutSeconds: 30,
            createdBy: "demo");

        // 2️⃣ Create a batch of jobs
        var batch = await scheduler.CreateHelloWorldJobsBatchAsync(
            baseName: "BatchJob",
            count: 3,
            startIndex: 1,
            cronExpression: "0 * * * *", // hourly
            createdBy: "demo");

        // 3️⃣ Retrieve all active hello‑world jobs
        IReadOnlyList<Job> activeJobs = await scheduler.GetActiveHelloWorldJobsAsync();

        // 4️⃣ Find jobs whose name contains "Demo"
        IReadOnlyList<Job> found = await scheduler.FindHelloWorldJobsByNameAsync("Demo");

        // 5️⃣ Validate a job configuration
        bool isValid = job.ValidateHelloWorldJobConfiguration();

        // 6️⃣ Get a human‑readable next execution time
        string next = job.GetNextExecutionTime();
        Console.WriteLine($"Next execution: {next}");

        // 7️⃣ Create a recurring hello‑world job that runs every 10 minutes
        var recurring = await scheduler.CreateRecurringHelloWorldJobAsync(
            name: "RecurringHello",
            intervalMinutes: 10,
            createdBy: "demo");

        Console.WriteLine("Demo completed.");
    }
}

## EmailSendingJobHandlerExtensions

`EmailSendingJobHandlerExtensions` provides a set of extension methods for `JobSchedulerService` to simplify the creation and management of email sending jobs. These extensions enable you to create single or batched email sending jobs, retrieve active jobs, find jobs by name pattern, validate job configurations, and get the next execution time in a human-readable format.

**Usage example**

```csharp
using System;
using System.Threading.Tasks;
using JobScheduler.Core.Services;

public class EmailSendingDemo
{
    public static async Task Main()
    {
        // Assume an already configured JobSchedulerService instance
        var scheduler = new JobSchedulerService(/* dependencies */);

        // Create a single email sending job
        var emailJob = await scheduler.CreateEmailSendingJobAsync(
            name: "DemoEmail",
            emailConfig: "{\"to\":\"example@example.com\",\"subject\":\"Hello\"}",
            cronExpression: "*/5 * * * *", // every 5 minutes
            priority: JobPriority.Normal,
            isActive: true,
            maxRetries: 2,
            timeoutSeconds: 30,
            createdBy: "demo");

        // Create a batch of email sending jobs
        var emailBatch = await scheduler.CreateEmailSendingJobsBatchAsync(
            baseName: "BatchEmail",
            emailConfig: "{\"to\":\"example@example.com\",\"subject\":\"Hello\"}",
            cronExpression: "0 * * * *", // hourly
            count: 3,
            startIndex: 1,
            createdBy: "demo");

        // Retrieve all active email sending jobs
        var activeEmailJobs = await scheduler.GetActiveEmailSendingJobsAsync();

        // Find email sending jobs by name pattern
        var foundEmailJobs = await scheduler.FindEmailSendingJobsByNameAsync("Demo");

        // Validate an email job configuration
        bool isValidEmailJob = emailJob.ValidateEmailJobConfiguration();

        // Get the next execution time for an email job
        string nextExecutionTime = emailJob.GetNextExecutionTime();
        Console.WriteLine($"Next execution: {nextExecutionTime}");

        Console.WriteLine("Demo completed.");
    }
}

These extensions streamline common email sending job scenarios while keeping the core scheduler logic untouched.

## JobValidationExceptionTests

The `JobValidationExceptionTests` class verifies the behavior of the `JobValidationException` exception, including constructor overloads and property mutability.

**Usage example**

```csharp
using JobScheduler.Core.Exceptions;

// Creating an exception with only a message
var ex1 = new JobValidationException("Job validation failed");
// ex1.Message == "Job validation failed"
// ex1.PropertyName == null

// Creating an exception with message and property name
var ex2 = new JobValidationException("Invalid priority", "Priority");
// ex2.Message == "Invalid priority"
// ex2.PropertyName == "Priority"

// Creating an exception with message and inner exception
var inner = new InvalidOperationException("Inner failure");
var ex3 = new JobValidationException("Validation error occurred", inner);
// ex3.Message == "Validation error occurred"
// ex3.InnerException == inner

// Modifying the PropertyName after construction
ex1.PropertyName = "JobId";
// ex1.PropertyName == "JobId"
```
```
```

## JobSchedulerExceptionExtensionsTests

The `JobSchedulerExceptionExtensionsTests` class verifies the behavior of the extension methods for `JobSchedulerException`,
including formatting details, checking specific error codes, and generating a summary dictionary.

**Usage example**

```csharp
using JobScheduler.Core.Exceptions;

// Create an exception with an error code
var exception = new JobSchedulerException("Something went wrong", "JOB-123");

// Get formatted details (includes error code)
string details = exception.FormatDetails();
// details == "Something went wrong (Error Code: JOB-123)"

// Check if the exception matches a specific error code
bool isMatch = exception.IsSpecificError("JOB-123");
// isMatch == true

// Get a summary of the exception as a dictionary
var summary = exception.GetSummary();
// summary contains Type, Message, and ErrorCode
```

## JobSchedulerExceptionTests

The `JobSchedulerExceptionTests` class verifies the behavior of the `JobSchedulerException` exception,
including constructor overloads, property behavior, and validation of null arguments.

**Usage example**

```csharp
using JobScheduler.Core.Exceptions;

// Creating an exception with only a message
var ex1 = new JobSchedulerException("Job scheduler failed");
// ex1.Message == "Job scheduler failed"
// ex1.ErrorCode == null

// Creating an exception with message and error code
var ex2 = new JobSchedulerException("Job scheduler failed", "SCH-001");
// ex2.Message == "Job scheduler failed"
// ex2.ErrorCode == "SCH-001"

// Creating an exception with message and inner exception
var inner = new InvalidOperationException("Inner failure");
var ex3 = new JobSchedulerException("Job scheduler failed", inner);
// ex3.Message == "Job scheduler failed"
// ex3.InnerException == inner
```
```

## ConcurrencyExceptionTests

The `ConcurrencyExceptionTests` class verifies the behavior of the `ConcurrencyException` exception,
which is thrown when job execution exceeds concurrency limits. It tests constructor behavior,
message formatting, property mutability, and inheritance from `JobSchedulerException`.

**Usage example**

```csharp
using JobScheduler.Core.Exceptions;

// Creating a concurrency exception with job ID and counts
var jobId = Guid.Parse("12345678-1234-1234-1234-123456789012");
var ex = new ConcurrencyException(jobId, 5, 3);
// ex.Message == "Job 12345678-1234-1234-1234-123456789012 cannot execute: current concurrent executions (5) exceed maximum allowed (3)."
// ex.JobId == jobId
// ex.CurrentConcurrentExecutions == 5
// ex.MaxAllowed == 3
// ex.ErrorCode == "CONCURRENCY_LIMIT_EXCEEDED"

// Properties can be modified after construction
ex.JobId = Guid.NewGuid();
ex.CurrentConcurrentExecutions = 99;
ex.MaxAllowed = 100;
```

## ExecutionExceptionTests

The `ExecutionExceptionTests` class verifies the behavior of the `ExecutionException` exception,
including constructor overloads for setting execution ID, job ID, attempt number, and inner exception,
as well as property mutability and inheritance from `JobSchedulerException`.

**Usage example**

```csharp
using JobScheduler.Core.Exceptions;

// Creating an exception with execution ID and job ID (defaults attempt number to 0)
var executionId = Guid.Parse("11111111-1111-1111-1111-111111111111");
var jobId = Guid.Parse("22222222-2222-2222-2222-222222222222");
var ex1 = new ExecutionException("Job failed", executionId, jobId);
// ex1.ExecutionId == executionId
// ex1.JobId == jobId
// ex1.AttemptNumber == 0

// Creating an exception with attempt number specified
var ex2 = new ExecutionException("Job failed on retry", executionId, jobId, 3);
// ex2.AttemptNumber == 3

// Creating an exception with inner exception
var inner = new InvalidOperationException("Database connection failed");
var ex3 = new ExecutionException("Job execution failed", executionId, jobId, inner);
// ex3.InnerException == inner

// Properties can be modified after construction
ex1.ExecutionId = Guid.NewGuid();
ex1.JobId = Guid.NewGuid();
ex1.AttemptNumber = 5;
```
```