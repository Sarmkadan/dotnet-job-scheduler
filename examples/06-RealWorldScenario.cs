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
/// Example 6: Real-World Business Scenario
///
/// Demonstrates a realistic business scenario: e-commerce system with daily
/// reporting, inventory sync, and customer notifications.
/// </summary>

public class DailySalesReportJobHandler : IJobHandler
{
    private readonly ILogger<DailySalesReportJobHandler> _logger;

    public DailySalesReportJobHandler(ILogger<DailySalesReportJobHandler> logger)
    {
        _logger = logger;
    }

    public async Task<string> ExecuteAsync(Job job, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating daily sales report...");

        var stats = new
        {
            TotalOrders = 1250,
            TotalRevenue = 45300.50,
            AverageOrderValue = 36.24,
            TopProduct = "Widget Pro"
        };

        await Task.Delay(200, cancellationToken);

        var result = $"Daily Report: {stats.TotalOrders} orders, ${stats.TotalRevenue:F2} revenue, " +
                     $"Top: {stats.TopProduct}";
        _logger.LogInformation(result);

        return result;
    }
}

public class InventorySyncJobHandler : IJobHandler
{
    private readonly ILogger<InventorySyncJobHandler> _logger;

    public InventorySyncJobHandler(ILogger<InventorySyncJobHandler> logger)
    {
        _logger = logger;
    }

    public async Task<string> ExecuteAsync(Job job, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Syncing inventory with suppliers...");

        var itemsChecked = 5000;
        var itemsUpdated = 150;
        var lowStockAlerts = 23;

        await Task.Delay(300, cancellationToken);

        var result = $"Inventory sync: {itemsChecked} items checked, {itemsUpdated} updated, " +
                     $"{lowStockAlerts} low stock alerts";
        _logger.LogInformation(result);

        return result;
    }
}

public class CustomerNotificationJobHandler : IJobHandler
{
    private readonly ILogger<CustomerNotificationJobHandler> _logger;

    public CustomerNotificationJobHandler(ILogger<CustomerNotificationJobHandler> logger)
    {
        _logger = logger;
    }

    public async Task<string> ExecuteAsync(Job job, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sending customer notifications...");

        var emailsSent = 500;
        var smsSent = 150;
        var pushNotifications = 1200;

        for (int i = 0; i < 3; i++)
        {
            await Task.Delay(100, cancellationToken);
            _logger.LogInformation("Sent batch {Batch} of notifications", i + 1);
        }

        var result = $"Notifications sent: {emailsSent} emails, {smsSent} SMS, {pushNotifications} push";
        _logger.LogInformation(result);

        return result;
    }
}

public class RealWorldScenarioExample
{
    /// Demonstrates a realistic business scenario with multiple scheduled jobs
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== Real-World Business Scenario: E-Commerce System ===\n");

        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddConsole();
        });

        services.AddJobScheduler(options =>
        {
            options.ConnectionString = "Data Source=ecommerce.db";
            options.MaxConcurrentJobs = 5;
            options.DefaultTimeoutSeconds = 600;
            options.DefaultMaxRetries = 3;
            options.QueuePollIntervalMs = 5000;
            options.EnableCleanup = true;
            options.CleanupIntervalMs = 300000;
            options.ExecutionHistoryRetentionDays = 90;
        });

        services.AddScoped<DailySalesReportJobHandler>();
        services.AddScoped<InventorySyncJobHandler>();
        services.AddScoped<CustomerNotificationJobHandler>();

        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var schedulerService = scope.ServiceProvider.GetRequiredService<JobSchedulerService>();
        var context = scope.ServiceProvider.GetRequiredService<JobSchedulerContext>();

        try
        {
            await context.Database.EnsureCreatedAsync();

            Console.WriteLine("=== Setting up E-Commerce Scheduler ===\n");
            Console.WriteLine("System: Multi-channel e-commerce platform");
            Console.WriteLine("Requirements:");
            Console.WriteLine("- Daily sales reporting");
            Console.WriteLine("- Real-time inventory sync");
            Console.WriteLine("- Customer notification system\n");

            // 1. Daily Sales Report - 9 AM every day
            var salesReportJob = new Job
            {
                Name = "DailySalesReport",
                Description = "Generates daily sales metrics and sends to management",
                CronExpression = "0 9 * * *",  // 9 AM daily
                HandlerType = typeof(DailySalesReportJobHandler).FullName!,
                Priority = JobPriority.High,
                IsActive = true,
                MaxRetries = 2,
                RetryBackoffSeconds = 30,
                ExecutionTimeoutSeconds = 300,
                MaxConcurrentExecutions = 1
            };

            var createdSalesJob = await schedulerService.CreateJobAsync(salesReportJob, "system");
            Console.WriteLine("✓ Daily Sales Report Job Created");
            Console.WriteLine($"  ID: {createdSalesJob.Id} | Schedule: 9 AM Daily | Priority: High\n");

            // 2. Inventory Sync - Every 2 hours
            var inventorySyncJob = new Job
            {
                Name = "InventorySync",
                Description = "Synchronizes inventory levels with supplier systems",
                CronExpression = "0 */2 * * *",  // Every 2 hours
                HandlerType = typeof(InventorySyncJobHandler).FullName!,
                Priority = JobPriority.High,
                IsActive = true,
                MaxRetries = 3,
                RetryBackoffSeconds = 60,
                ExecutionTimeoutSeconds = 600,
                MaxConcurrentExecutions = 1
            };

            var createdInventoryJob = await schedulerService.CreateJobAsync(inventorySyncJob, "system");
            Console.WriteLine("✓ Inventory Sync Job Created");
            Console.WriteLine($"  ID: {createdInventoryJob.Id} | Schedule: Every 2 Hours | Priority: High\n");

            // 3. Customer Notifications - 3 PM and 7 PM
            var notificationJob = new Job
            {
                Name = "CustomerNotifications",
                Description = "Sends promotional and transactional notifications to customers",
                CronExpression = "0 15,19 * * *",  // 3 PM and 7 PM daily
                HandlerType = typeof(CustomerNotificationJobHandler).FullName!,
                Priority = JobPriority.Normal,
                IsActive = true,
                MaxRetries = 1,
                RetryBackoffSeconds = 120,
                ExecutionTimeoutSeconds = 600,
                MaxConcurrentExecutions = 2  // Allow 2 parallel notification batches
            };

            var createdNotificationJob = await schedulerService.CreateJobAsync(notificationJob, "system");
            Console.WriteLine("✓ Customer Notification Job Created");
            Console.WriteLine($"  ID: {createdNotificationJob.Id} | Schedule: 3 PM & 7 PM Daily | Priority: Normal\n");

            Console.WriteLine("=== System Status ===\n");
            var stats = await schedulerService.GetSchedulerStatisticsAsync();
            PrintSystemStatus(stats);

            Console.WriteLine("\n=== Simulating Job Executions ===\n");

            // Simulate multiple execution cycles
            for (int cycle = 1; cycle <= 2; cycle++)
            {
                Console.WriteLine($"--- Execution Cycle {cycle} ---");

                var executions = await schedulerService.ExecuteDueJobsAsync();

                if (executions.Count > 0)
                {
                    foreach (var execution in executions)
                    {
                        var jobName = context.Jobs
                            .FirstOrDefault(j => j.Id == execution.JobId)?.Name ?? "Unknown";

                        Console.WriteLine($"✓ {jobName} executed in {execution.Duration?.TotalMilliseconds:F0}ms");
                        Console.WriteLine($"  Status: {execution.Status}");
                        Console.WriteLine($"  Result: {execution.Result}");
                    }
                }
                else
                {
                    Console.WriteLine("No jobs due for execution");
                }

                Console.WriteLine();
                await Task.Delay(1000);
            }

            Console.WriteLine("=== Execution History ===\n");

            var allExecutions = context.JobExecutions
                .OrderByDescending(e => e.ExecutedAt)
                .Take(5)
                .ToList();

            foreach (var execution in allExecutions)
            {
                var job = context.Jobs.FirstOrDefault(j => j.Id == execution.JobId);
                var statusEmoji = execution.Status == ExecutionStatus.Completed ? "✓" : "✗";

                Console.WriteLine($"{statusEmoji} {job?.Name}");
                Console.WriteLine($"   Executed: {execution.ExecutedAt:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"   Duration: {execution.Duration?.TotalMilliseconds:F0}ms");
                Console.WriteLine();
            }

            Console.WriteLine("=== Business Metrics ===\n");

            var finalStats = await schedulerService.GetSchedulerStatisticsAsync();
            Console.WriteLine($"Total Job Runs: {finalStats.ExecutionsLast24Hours}");
            Console.WriteLine($"Success Rate: {finalStats.AverageSuccessRate:F1}%");
            Console.WriteLine($"Failed Jobs: {finalStats.FailedLast24Hours}");
            Console.WriteLine($"Avg Execution Time: {finalStats.AverageExecutionTimeMs:F0}ms");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        Console.WriteLine("\nExample completed.");
    }

    private static void PrintSystemStatus(SchedulerStatistics stats)
    {
        Console.WriteLine($"Total Jobs: {stats.TotalJobs}");
        Console.WriteLine($"Active: {stats.ActiveJobs}");
        Console.WriteLine($"Running: {stats.RunningExecutions}");
        Console.WriteLine($"Queued: {stats.QueuedJobs}");
        Console.WriteLine($"Success Rate: {stats.AverageSuccessRate:F1}%");
    }
}
