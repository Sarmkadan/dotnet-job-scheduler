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
/// Example 1: Basic Console Application
///
/// This example demonstrates how to integrate dotnet-job-scheduler into a simple
/// console application. It creates a few basic scheduled jobs and executes them
/// in a controlled manner.
/// </summary>

public sealed class HelloWorldJobHandler : IJobHandler
{
    private readonly ILogger<HelloWorldJobHandler> _logger;

    public HelloWorldJobHandler(ILogger<HelloWorldJobHandler> logger)
    {
        _logger = logger;
    }

    public async Task<string> ExecuteAsync(Job job, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Hello World job executed at {Time}", DateTime.UtcNow);
        await Task.Delay(100, cancellationToken);
        return $"Hello World job completed at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
    }
}

public sealed class CounterJobHandler : IJobHandler
{
    private readonly ILogger<CounterJobHandler> _logger;

    public CounterJobHandler(ILogger<CounterJobHandler> logger)
    {
        _logger = logger;
    }

    public async Task<string> ExecuteAsync(Job job, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Counter job started");

        for (int i = 1; i <= 5; i++)
        {
            _logger.LogInformation("Count: {Count}", i);
            await Task.Delay(100, cancellationToken);
        }

        return "Counted to 5";
    }
}

public sealed class BasicConsoleExample
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== Basic Console Application Example ===\n");

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
            options.DefaultMaxRetries = 1;
        });

        services.AddScoped<HelloWorldJobHandler>();
        services.AddScoped<CounterJobHandler>();

        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var schedulerService = scope.ServiceProvider.GetRequiredService<JobSchedulerService>();
        var context = scope.ServiceProvider.GetRequiredService<JobSchedulerContext>();

        try
        {
            await context.Database.EnsureCreatedAsync();

            var job1 = new Job
            {
                Name = "HelloWorld",
                Description = "Simple hello world job",
                CronExpression = "* * * * *",
                HandlerType = typeof(HelloWorldJobHandler).FullName!,
                Priority = JobPriority.Normal,
                IsActive = true,
                MaxRetries = 1,
                ExecutionTimeoutSeconds = 30
            };

            var createdJob1 = await schedulerService.CreateJobAsync(job1, "console");
            Console.WriteLine($"Created job 1: {createdJob1.Name} (ID: {createdJob1.Id})\n");

            var job2 = new Job
            {
                Name = "Counter",
                Description = "Counts to 5",
                CronExpression = "* * * * *",
                HandlerType = typeof(CounterJobHandler).FullName!,
                Priority = JobPriority.Normal,
                IsActive = true,
                MaxRetries = 0,
                ExecutionTimeoutSeconds = 30
            };

            var createdJob2 = await schedulerService.CreateJobAsync(job2, "console");
            Console.WriteLine($"Created job 2: {createdJob2.Name} (ID: {createdJob2.Id})\n");

            Console.WriteLine("Executing due jobs...\n");
            var executions = await schedulerService.ExecuteDueJobsAsync();

            Console.WriteLine($"\nExecuted {executions.Count} jobs.\n");

            Console.WriteLine("=== Execution Summary ===");
            foreach (var execution in executions)
            {
                Console.WriteLine($"Job ID: {execution.JobId}");
                Console.WriteLine($"Status: {execution.Status}");
                Console.WriteLine($"Duration: {execution.Duration?.TotalSeconds:F2}s");
                if (!string.IsNullOrEmpty(execution.ErrorMessage))
                    Console.WriteLine($"Error: {execution.ErrorMessage}");
                Console.WriteLine();
            }

            var stats = await schedulerService.GetSchedulerStatisticsAsync();
            Console.WriteLine("=== Scheduler Statistics ===");
            Console.WriteLine($"Total Jobs: {stats.TotalJobs}");
            Console.WriteLine($"Active Jobs: {stats.ActiveJobs}");
            Console.WriteLine($"Success Rate: {stats.AverageSuccessRate:F1}%");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        Console.WriteLine("\nExample completed.");
    }
}
