#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using JobScheduler.Core.Configuration;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Constants;
using JobScheduler.Core.Services;

/// <summary>
/// Example 3: Retry and Error Handling
///
/// Demonstrates different retry strategies and error handling patterns,
/// including exponential backoff and graceful failure scenarios.
/// </summary>

public sealed class UnstableExternalApiJobHandler : IJobHandler
{
    private readonly ILogger<UnstableExternalApiJobHandler> _logger;
    private static int _attemptCount = 0;

    public UnstableExternalApiJobHandler(ILogger<UnstableExternalApiJobHandler> logger)
    {
        _logger = logger;
    }

    public async Task<string> ExecuteAsync(Job job, CancellationToken cancellationToken)
    {
        _attemptCount++;
        _logger.LogInformation("Calling external API. Attempt: {Attempt}", _attemptCount);

        try
        {
            // Simulate API failure on first 2 attempts
            if (_attemptCount < 3)
            {
                throw new HttpRequestException("Service unavailable");
            }

            await Task.Delay(100, cancellationToken);
            _logger.LogInformation("API call successful on attempt {Attempt}", _attemptCount);
            return "Successfully called external API";
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Transient error calling API");
            throw; // Will trigger retry
        }
    }
}

public sealed class DatabaseQueryJobHandler : IJobHandler
{
    private readonly ILogger<DatabaseQueryJobHandler> _logger;

    public DatabaseQueryJobHandler(ILogger<DatabaseQueryJobHandler> logger)
    {
        _logger = logger;
    }

    public async Task<string> ExecuteAsync(Job job, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Executing database query...");
            await Task.Delay(150, cancellationToken);
            return "Query executed successfully";
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Database error");
            throw;
        }
    }
}

public sealed class GracefulFailureJobHandler : IJobHandler
{
    private readonly ILogger<GracefulFailureJobHandler> _logger;

    public GracefulFailureJobHandler(ILogger<GracefulFailureJobHandler> logger)
    {
        _logger = logger;
    }

    public async Task<string> ExecuteAsync(Job job, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing batch...");

            var successCount = 95;
            var failureCount = 5;

            await Task.Delay(100, cancellationToken);

            // Even with failures, report progress
            var message = $"Processed {successCount} successfully, {failureCount} failed";
            _logger.LogWarning(message);

            return message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical failure in batch processing");
            throw; // Will retry
        }
    }
}

public sealed class RetryAndErrorHandlingExample
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== Retry and Error Handling Example ===\n");

        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddConsole();
        });

        services.AddJobScheduler(options =>
        {
            options.ConnectionString = "Data Source=:memory:";
            options.MaxConcurrentJobs = 5;
            options.DefaultTimeoutSeconds = 60;
            options.DefaultMaxRetries = 3;
            options.DefaultRetryBackoffSeconds = 2;
        });

        services.AddScoped<UnstableExternalApiJobHandler>();
        services.AddScoped<DatabaseQueryJobHandler>();
        services.AddScoped<GracefulFailureJobHandler>();

        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var schedulerService = scope.ServiceProvider.GetRequiredService<JobSchedulerService>();
        var context = scope.ServiceProvider.GetRequiredService<JobSchedulerContext>();

        try
        {
            await context.Database.EnsureCreatedAsync();

            // Job with exponential backoff (2s, 4s, 8s)
            var unstableJob = new Job
            {
                Name = "UnstableApiCall",
                Description = "Calls external API with retry",
                CronExpression = "* * * * *",
                HandlerType = typeof(UnstableExternalApiJobHandler).FullName!,
                Priority = JobPriority.High,
                IsActive = true,
                MaxRetries = 3,
                RetryBackoffSeconds = 2,
                ExecutionTimeoutSeconds = 60
            };

            var createdJob1 = await schedulerService.CreateJobAsync(unstableJob, "example");
            Console.WriteLine($"Created job: {createdJob1.Name} (ID: {createdJob1.Id})");
            Console.WriteLine($"Retry Policy: Max {createdJob1.MaxRetries} retries, {createdJob1.RetryBackoffSeconds}s initial backoff\n");

            // Job with fixed backoff
            var dbJob = new Job
            {
                Name = "DatabaseQuery",
                Description = "Executes database query",
                CronExpression = "* * * * *",
                HandlerType = typeof(DatabaseQueryJobHandler).FullName!,
                Priority = JobPriority.Normal,
                IsActive = true,
                MaxRetries = 2,
                RetryBackoffSeconds = 5,
                ExecutionTimeoutSeconds = 30
            };

            var createdJob2 = await schedulerService.CreateJobAsync(dbJob, "example");
            Console.WriteLine($"Created job: {createdJob2.Name} (ID: {createdJob2.Id})");
            Console.WriteLine($"Retry Policy: Max {createdJob2.MaxRetries} retries, {createdJob2.RetryBackoffSeconds}s backoff\n");

            // Job that handles failures gracefully
            var batchJob = new Job
            {
                Name = "GracefulBatchProcess",
                Description = "Batch processing with error tolerance",
                CronExpression = "* * * * *",
                HandlerType = typeof(GracefulFailureJobHandler).FullName!,
                Priority = JobPriority.Low,
                IsActive = true,
                MaxRetries = 1,
                RetryBackoffSeconds = 10,
                ExecutionTimeoutSeconds = 120
            };

            var createdJob3 = await schedulerService.CreateJobAsync(batchJob, "example");
            Console.WriteLine($"Created job: {createdJob3.Name} (ID: {createdJob3.Id})\n");

            Console.WriteLine("Executing due jobs with retry demonstrations...\n");
            var executions = await schedulerService.ExecuteDueJobsAsync();

            Console.WriteLine($"\nInitial execution count: {executions.Count}\n");

            Console.WriteLine("=== Execution Details ===");
            foreach (var execution in executions)
            {
                Console.WriteLine($"Job ID: {execution.JobId}");
                Console.WriteLine($"Status: {execution.Status}");
                Console.WriteLine($"Attempt: {execution.RetryAttempt}");
                Console.WriteLine($"Result: {execution.Result}");
                if (!string.IsNullOrEmpty(execution.ErrorMessage))
                    Console.WriteLine($"Error: {execution.ErrorMessage}");
                Console.WriteLine();
            }

            var stats = await schedulerService.GetSchedulerStatisticsAsync();
            Console.WriteLine("=== Summary ===");
            Console.WriteLine($"Total Jobs: {stats.TotalJobs}");
            Console.WriteLine($"Success Rate: {stats.AverageSuccessRate:F1}%");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        Console.WriteLine("\nExample completed.");
    }
}
