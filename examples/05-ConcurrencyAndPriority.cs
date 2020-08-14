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
/// Example 5: Concurrency Control and Job Priority
///
/// Demonstrates how to manage concurrent job execution and use priority levels
/// to ensure critical jobs execute first.
/// </summary>

public sealed class LongRunningJobHandler : IJobHandler
{
    private readonly ILogger<LongRunningJobHandler> _logger;

    public LongRunningJobHandler(ILogger<LongRunningJobHandler> logger)
    {
        _logger = logger;
    }

    public async Task<string> ExecuteAsync(Job job, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Long running job started");

        // Simulate long-running operation
        for (int i = 0; i < 5; i++)
        {
            await Task.Delay(100, cancellationToken);
            _logger.LogInformation("Progress: {Percent}%", (i + 1) * 20);
        }

        _logger.LogInformation("Long running job completed");
        return "Completed long operation";
    }
}

public sealed class CriticalJobHandler : IJobHandler
{
    private readonly ILogger<CriticalJobHandler> _logger;

    public CriticalJobHandler(ILogger<CriticalJobHandler> logger)
    {
        _logger = logger;
    }

    public async Task<string> ExecuteAsync(Job job, CancellationToken cancellationToken)
    {
        _logger.LogInformation("CRITICAL: System check starting");
        await Task.Delay(100, cancellationToken);
        _logger.LogInformation("CRITICAL: System check completed - All systems operational");
        return "Critical job executed";
    }
}

public sealed class QuickTaskJobHandler : IJobHandler
{
    private readonly ILogger<QuickTaskJobHandler> _logger;

    public QuickTaskJobHandler(ILogger<QuickTaskJobHandler> logger)
    {
        _logger = logger;
    }

    public async Task<string> ExecuteAsync(Job job, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Quick task executed");
        await Task.Delay(50, cancellationToken);
        return "Quick task done";
    }
}

public sealed class ConcurrencyAndPriorityExample
{
    /// Demonstrates concurrency limits and job priority execution
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== Concurrency Control and Job Priority Example ===\n");

        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddConsole();
        });

        services.AddJobScheduler(options =>
        {
            options.ConnectionString = "Data Source=:memory:";
            options.MaxConcurrentJobs = 3;  // Limit to 3 concurrent jobs
            options.DefaultTimeoutSeconds = 120;
            options.DefaultMaxRetries = 1;
        });

        services.AddScoped<CriticalJobHandler>();
        services.AddScoped<LongRunningJobHandler>();
        services.AddScoped<QuickTaskJobHandler>();

        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var schedulerService = scope.ServiceProvider.GetRequiredService<JobSchedulerService>();
        var context = scope.ServiceProvider.GetRequiredService<JobSchedulerContext>();

        try
        {
            await context.Database.EnsureCreatedAsync();

            Console.WriteLine("=== Creating Jobs with Different Priorities ===\n");

            // Critical job - must execute first
            var criticalJob = new Job
            {
                Name = "CriticalSystemCheck",
                Description = "Critical system health check",
                CronExpression = "* * * * *",
                HandlerType = typeof(CriticalJobHandler).FullName!,
                Priority = JobPriority.Critical,  // Highest priority
                IsActive = true,
                MaxRetries = 2,
                MaxConcurrentExecutions = 1,  // Never run in parallel
                ExecutionTimeoutSeconds = 60
            };

            var createdCritical = await schedulerService.CreateJobAsync(criticalJob, "example");
            Console.WriteLine($"1. {createdCritical.Name}");
            Console.WriteLine($"   Priority: {createdCritical.Priority} (CRITICAL)");
            Console.WriteLine($"   Max Concurrent: {createdCritical.MaxConcurrentExecutions}");
            Console.WriteLine();

            // High priority job
            var highPriorityJob = new Job
            {
                Name = "DataBackupJob",
                Description = "High priority backup task",
                CronExpression = "* * * * *",
                HandlerType = typeof(LongRunningJobHandler).FullName!,
                Priority = JobPriority.High,
                IsActive = true,
                MaxRetries = 1,
                MaxConcurrentExecutions = 1,  // Only one backup at a time
                ExecutionTimeoutSeconds = 120
            };

            var createdHighPriority = await schedulerService.CreateJobAsync(highPriorityJob, "example");
            Console.WriteLine($"2. {createdHighPriority.Name}");
            Console.WriteLine($"   Priority: {createdHighPriority.Priority} (HIGH)");
            Console.WriteLine($"   Max Concurrent: {createdHighPriority.MaxConcurrentExecutions}");
            Console.WriteLine();

            // Normal priority job
            var normalJob = new Job
            {
                Name = "RegularCleanup",
                Description = "Normal priority cleanup",
                CronExpression = "* * * * *",
                HandlerType = typeof(QuickTaskJobHandler).FullName!,
                Priority = JobPriority.Normal,
                IsActive = true,
                MaxRetries = 0,
                MaxConcurrentExecutions = 3,  // Allow multiple concurrent
                ExecutionTimeoutSeconds = 60
            };

            var createdNormal = await schedulerService.CreateJobAsync(normalJob, "example");
            Console.WriteLine($"3. {createdNormal.Name}");
            Console.WriteLine($"   Priority: {createdNormal.Priority} (NORMAL)");
            Console.WriteLine($"   Max Concurrent: {createdNormal.MaxConcurrentExecutions}");
            Console.WriteLine();

            // Low priority job
            var lowPriorityJob = new Job
            {
                Name = "LowPriorityAnalysis",
                Description = "Low priority background analysis",
                CronExpression = "* * * * *",
                HandlerType = typeof(QuickTaskJobHandler).FullName!,
                Priority = JobPriority.Low,
                IsActive = true,
                MaxRetries = 0,
                MaxConcurrentExecutions = 2,
                ExecutionTimeoutSeconds = 60
            };

            var createdLow = await schedulerService.CreateJobAsync(lowPriorityJob, "example");
            Console.WriteLine($"4. {createdLow.Name}");
            Console.WriteLine($"   Priority: {createdLow.Priority} (LOW)");
            Console.WriteLine($"   Max Concurrent: {createdLow.MaxConcurrentExecutions}");
            Console.WriteLine();

            Console.WriteLine("=== Scheduler Configuration ===\n");
            Console.WriteLine("Global Max Concurrent Jobs: 3");
            Console.WriteLine("(Critical: 1, High: 1, Normal: 3, Low: 2)\n");

            Console.WriteLine("Execution Order (by priority):");
            Console.WriteLine("1. CriticalSystemCheck (Priority: 3)");
            Console.WriteLine("2. DataBackupJob (Priority: 2)");
            Console.WriteLine("3. RegularCleanup (Priority: 1)");
            Console.WriteLine("4. LowPriorityAnalysis (Priority: 0)\n");

            Console.WriteLine("=== Executing Due Jobs ===\n");

            var executions = await schedulerService.ExecuteDueJobsAsync();
            Console.WriteLine($"Executed {executions.Count} jobs\n");

            Console.WriteLine("=== Execution Summary ===\n");

            var executionsByPriority = executions
                .GroupBy(e => context.Jobs.FirstOrDefault(j => j.Id == e.JobId)?.Priority)
                .OrderByDescending(g => g.Key)
                .ToList();

            foreach (var group in executionsByPriority)
            {
                var priorityName = group.Key switch
                {
                    JobPriority.Critical => "CRITICAL",
                    JobPriority.High => "HIGH",
                    JobPriority.Normal => "NORMAL",
                    JobPriority.Low => "LOW",
                    _ => "UNKNOWN"
                };

                Console.WriteLine($"Priority {priorityName}:");
                foreach (var execution in group)
                {
                    var job = context.Jobs.FirstOrDefault(j => j.Id == execution.JobId);
                    Console.WriteLine($"  - {job?.Name}: {execution.Status}");
                    if (execution.Duration.HasValue)
                        Console.WriteLine($"    Duration: {execution.Duration.Value.TotalMilliseconds:F0}ms");
                }
                Console.WriteLine();
            }

            var stats = await schedulerService.GetSchedulerStatisticsAsync();
            Console.WriteLine("=== Scheduler Statistics ===");
            Console.WriteLine($"Total Jobs: {stats.TotalJobs}");
            Console.WriteLine($"Running Executions: {stats.RunningExecutions}");
            Console.WriteLine($"Success Rate: {stats.AverageSuccessRate:F1}%");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        Console.WriteLine("\nExample completed.");
    }
}
