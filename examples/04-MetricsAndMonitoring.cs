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
/// Example 4: Metrics and Monitoring
///
/// Demonstrates how to collect, analyze, and report on job execution metrics,
/// including success rates, performance statistics, and historical analysis.
/// </summary>

public class ReportGenerationJobHandler : IJobHandler
{
    private readonly ILogger<ReportGenerationJobHandler> _logger;

    public ReportGenerationJobHandler(ILogger<ReportGenerationJobHandler> logger)
    {
        _logger = logger;
    }

    public async Task<string> ExecuteAsync(Job job, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating report...");
        await Task.Delay(250, cancellationToken);
        return "Report generated: 10,500 records processed";
    }
}

public class MetricAnalysisJobHandler : IJobHandler
{
    private readonly ILogger<MetricAnalysisJobHandler> _logger;

    public MetricAnalysisJobHandler(ILogger<MetricAnalysisJobHandler> logger)
    {
        _logger = logger;
    }

    public async Task<string> ExecuteAsync(Job job, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Analyzing metrics...");
        await Task.Delay(150, cancellationToken);
        return "Metrics analyzed and stored";
    }
}

public class MetricsAndMonitoringExample
{
    /// Demonstrates metrics collection and analysis
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== Metrics and Monitoring Example ===\n");

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
            options.EnablePerformanceMonitoring = true;
        });

        services.AddScoped<ReportGenerationJobHandler>();
        services.AddScoped<MetricAnalysisJobHandler>();

        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var schedulerService = scope.ServiceProvider.GetRequiredService<JobSchedulerService>();
        var executionRepository = scope.ServiceProvider.GetRequiredService<IExecutionRepository>();
        var context = scope.ServiceProvider.GetRequiredService<JobSchedulerContext>();

        try
        {
            await context.Database.EnsureCreatedAsync();

            var reportJob = new Job
            {
                Name = "DailyReport",
                Description = "Generates daily report",
                CronExpression = "0 9 * * *",
                HandlerType = typeof(ReportGenerationJobHandler).FullName!,
                Priority = JobPriority.High,
                IsActive = true,
                MaxRetries = 2,
                ExecutionTimeoutSeconds = 300
            };

            var createdReportJob = await schedulerService.CreateJobAsync(reportJob, "example");

            var metricsJob = new Job
            {
                Name = "MetricsAnalysis",
                Description = "Analyzes system metrics",
                CronExpression = "0 */6 * * *",
                HandlerType = typeof(MetricAnalysisJobHandler).FullName!,
                Priority = JobPriority.Normal,
                IsActive = true,
                MaxRetries = 1,
                ExecutionTimeoutSeconds = 300
            };

            var createdMetricsJob = await schedulerService.CreateJobAsync(metricsJob, "example");

            Console.WriteLine("Created jobs:\n");
            Console.WriteLine($"1. {createdReportJob.Name} - Priority: {createdReportJob.Priority}");
            Console.WriteLine($"2. {createdMetricsJob.Name} - Priority: {createdMetricsJob.Priority}\n");

            Console.WriteLine("Simulating multiple job executions for metrics collection...\n");

            for (int i = 0; i < 3; i++)
            {
                var executions = await schedulerService.ExecuteDueJobsAsync();
                Console.WriteLine($"Batch {i + 1}: Executed {executions.Count} jobs");
                await Task.Delay(500);
            }

            Console.WriteLine("\n=== Scheduler Statistics ===\n");

            var stats = await schedulerService.GetSchedulerStatisticsAsync();
            PrintStatistics(stats);

            Console.WriteLine("\n=== Job-Specific Metrics ===\n");

            var reportJobExecutions = await executionRepository
                .GetByJobIdAsync(createdReportJob.Id)
                .ToListAsync();

            if (reportJobExecutions.Any())
            {
                Console.WriteLine($"Job: {createdReportJob.Name}");
                Console.WriteLine($"Total Executions: {reportJobExecutions.Count}");
                Console.WriteLine($"Successful: {reportJobExecutions.Count(e => e.Status == ExecutionStatus.Completed)}");
                Console.WriteLine($"Failed: {reportJobExecutions.Count(e => e.Status == ExecutionStatus.Failed)}");

                var avgDuration = reportJobExecutions
                    .Where(e => e.Duration.HasValue)
                    .Average(e => e.Duration.Value.TotalMilliseconds);

                Console.WriteLine($"Average Duration: {avgDuration:F0}ms");

                if (reportJobExecutions.Any(e => e.Duration.HasValue))
                {
                    var maxDuration = reportJobExecutions
                        .Where(e => e.Duration.HasValue)
                        .Max(e => e.Duration.Value.TotalMilliseconds);

                    var minDuration = reportJobExecutions
                        .Where(e => e.Duration.HasValue)
                        .Min(e => e.Duration.Value.TotalMilliseconds);

                    Console.WriteLine($"Duration Range: {minDuration:F0}ms - {maxDuration:F0}ms");
                }

                Console.WriteLine();
            }

            var metricsJobExecutions = await executionRepository
                .GetByJobIdAsync(createdMetricsJob.Id)
                .ToListAsync();

            if (metricsJobExecutions.Any())
            {
                Console.WriteLine($"Job: {createdMetricsJob.Name}");
                Console.WriteLine($"Total Executions: {metricsJobExecutions.Count}");
                Console.WriteLine($"Success Rate: {(metricsJobExecutions.Count(e => e.Status == ExecutionStatus.Completed) * 100.0 / metricsJobExecutions.Count):F1}%");
                Console.WriteLine();
            }

            Console.WriteLine("=== Execution History (Last 10) ===\n");

            var allExecutions = context.JobExecutions
                .OrderByDescending(e => e.ExecutedAt)
                .Take(10)
                .ToList();

            foreach (var execution in allExecutions)
            {
                var jobName = context.Jobs.FirstOrDefault(j => j.Id == execution.JobId)?.Name ?? "Unknown";
                Console.WriteLine($"Job: {jobName}");
                Console.WriteLine($"  Executed: {execution.ExecutedAt:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"  Status: {execution.Status}");
                Console.WriteLine($"  Duration: {execution.Duration?.TotalMilliseconds:F0}ms");
                Console.WriteLine($"  Result: {execution.Result}");
                Console.WriteLine();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        Console.WriteLine("Example completed.");
    }

    private static void PrintStatistics(SchedulerStatistics stats)
    {
        Console.WriteLine($"Total Jobs: {stats.TotalJobs}");
        Console.WriteLine($"Active Jobs: {stats.ActiveJobs}");
        Console.WriteLine($"Suspended Jobs: {stats.SuspendedJobs}");
        Console.WriteLine($"Running Executions: {stats.RunningExecutions}");
        Console.WriteLine($"Queued Jobs: {stats.QueuedJobs}");
        Console.WriteLine($"Average Success Rate: {stats.AverageSuccessRate:F1}%");
        Console.WriteLine($"Executions (Last 24h): {stats.ExecutionsLast24Hours}");
        Console.WriteLine($"Failed (Last 24h): {stats.FailedLast24Hours}");
        Console.WriteLine($"Average Execution Time: {stats.AverageExecutionTimeMs:F0}ms");
    }
}
