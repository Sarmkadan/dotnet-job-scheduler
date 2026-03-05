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
using System.Text;

/// <summary>
/// Example 7: Data Export and Reporting
///
/// Demonstrates how to export job execution data, generate reports,
/// and create audit trails from the scheduler.
/// </summary>

public sealed class DataExportJobHandler : IJobHandler
{
    private readonly ILogger<DataExportJobHandler> _logger;

    public DataExportJobHandler(ILogger<DataExportJobHandler> logger)
    {
        _logger = logger;
    }

    public async Task<string> ExecuteAsync(Job job, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Exporting job execution data...");

        // Simulate data export to CSV/JSON
        await Task.Delay(150, cancellationToken);

        var recordsExported = 2500;
        var fileName = $"execution_report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

        _logger.LogInformation("Exported {Count} records to {File}", recordsExported, fileName);
        return $"Exported {recordsExported} records to {fileName}";
    }
}

public sealed class DataExportAndReportingExample
{
    /// Demonstrates exporting and reporting on job execution data
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== Data Export and Reporting Example ===\n");

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

        services.AddScoped<DataExportJobHandler>();

        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var schedulerService = scope.ServiceProvider.GetRequiredService<JobSchedulerService>();
        var executionRepository = scope.ServiceProvider.GetRequiredService<IExecutionRepository>();
        var context = scope.ServiceProvider.GetRequiredService<JobSchedulerContext>();

        try
        {
            await context.Database.EnsureCreatedAsync();

            // Create test job
            var exportJob = new Job
            {
                Name = "DataExport",
                Description = "Exports execution data daily",
                CronExpression = "0 1 * * *",
                HandlerType = typeof(DataExportJobHandler).FullName!,
                Priority = JobPriority.Normal,
                IsActive = true,
                MaxRetries = 1,
                ExecutionTimeoutSeconds = 300
            };

            var createdJob = await schedulerService.CreateJobAsync(exportJob, "system");
            Console.WriteLine($"Created export job: {createdJob.Name} (ID: {createdJob.Id})\n");

            // Simulate some executions
            Console.WriteLine("Simulating job executions...\n");
            for (int i = 0; i < 5; i++)
            {
                var executions = await schedulerService.ExecuteDueJobsAsync();
                foreach (var execution in executions)
                {
                    Console.WriteLine($"Execution {i + 1}: {execution.Status}");
                }
                await Task.Delay(200);
            }

            Console.WriteLine("\n=== Execution Data Report ===\n");

            // Query all executions
            var allExecutions = await executionRepository
                .GetByJobIdAsync(createdJob.Id)
                .ToListAsync();

            if (allExecutions.Any())
            {
                // Generate CSV export
                var csvContent = GenerateCsvReport(createdJob, allExecutions);
                Console.WriteLine("CSV Report Preview:");
                Console.WriteLine(csvContent.Take(500));
                Console.WriteLine("...");
                Console.WriteLine($"\nTotal CSV size: {csvContent.Length} bytes");
            }

            Console.WriteLine("\n=== JSON Report ===\n");

            // Generate JSON report
            var jsonReport = GenerateJsonReport(createdJob, allExecutions);
            Console.WriteLine(jsonReport);

            Console.WriteLine("\n=== Summary Statistics ===\n");

            PrintExecutionSummary(allExecutions, createdJob.Name);

            Console.WriteLine("\n=== Audit Trail ===\n");

            var recentExecutions = allExecutions
                .OrderByDescending(e => e.ExecutedAt)
                .Take(10)
                .ToList();

            Console.WriteLine("Recent Executions (Last 10):");
            foreach (var execution in recentExecutions)
            {
                Console.WriteLine($"- {execution.ExecutedAt:yyyy-MM-dd HH:mm:ss} | Status: {execution.Status} | " +
                    $"Duration: {execution.Duration?.TotalMilliseconds:F0}ms");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        Console.WriteLine("\nExample completed.");
    }

    private static string GenerateCsvReport(Job job, List<JobExecution> executions)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("Execution ID,Job ID,Job Name,Status,Executed At,Completed At,Duration (ms),Retry Attempt,Result");

        // Data rows
        foreach (var execution in executions.OrderBy(e => e.ExecutedAt))
        {
            var duration = execution.Duration?.TotalMilliseconds ?? 0;
            sb.AppendLine($"\"{execution.Id}\",\"{execution.JobId}\",\"{job.Name}\",\"{execution.Status}\"," +
                $"\"{execution.ExecutedAt:yyyy-MM-dd HH:mm:ss}\",\"{execution.CompletedAt:yyyy-MM-dd HH:mm:ss}\"," +
                $"\"{duration:F2}\",\"{execution.RetryAttempt}\",\"{execution.Result}\"");
        }

        return sb.ToString();
    }

    private static string GenerateJsonReport(Job job, List<JobExecution> executions)
    {
        var sb = new StringBuilder();

        sb.AppendLine("{");
        sb.AppendLine($"  \"job\": {{");
        sb.AppendLine($"    \"id\": {job.Id},");
        sb.AppendLine($"    \"name\": \"{job.Name}\",");
        sb.AppendLine($"    \"cronExpression\": \"{job.CronExpression}\",");
        sb.AppendLine($"    \"priority\": \"{job.Priority}\"");
        sb.AppendLine($"  }},");
        sb.AppendLine($"  \"executionCount\": {executions.Count},");
        sb.AppendLine($"  \"executions\": [");

        for (int i = 0; i < executions.Count; i++)
        {
            var execution = executions[i];
            var duration = execution.Duration?.TotalMilliseconds ?? 0;

            sb.AppendLine($"    {{");
            sb.AppendLine($"      \"id\": {execution.Id},");
            sb.AppendLine($"      \"status\": \"{execution.Status}\",");
            sb.AppendLine($"      \"executedAt\": \"{execution.ExecutedAt:o}\",");
            sb.AppendLine($"      \"durationMs\": {duration:F0},");
            sb.AppendLine($"      \"retryAttempt\": {execution.RetryAttempt}");
            sb.Append($"    }}");

            if (i < executions.Count - 1)
                sb.AppendLine(",");
            else
                sb.AppendLine();
        }

        sb.AppendLine("  ]");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void PrintExecutionSummary(List<JobExecution> executions, string jobName)
    {
        Console.WriteLine($"Job: {jobName}");
        Console.WriteLine($"Total Executions: {executions.Count}");

        var completed = executions.Count(e => e.Status == ExecutionStatus.Completed);
        var failed = executions.Count(e => e.Status == ExecutionStatus.Failed);
        var successRate = executions.Count > 0 ? (completed * 100.0 / executions.Count) : 0;

        Console.WriteLine($"Successful: {completed}");
        Console.WriteLine($"Failed: {failed}");
        Console.WriteLine($"Success Rate: {successRate:F1}%");

        var executionsWithDuration = executions.Where(e => e.Duration.HasValue).ToList();
        if (executionsWithDuration.Any())
        {
            var avgDuration = executionsWithDuration.Average(e => e.Duration.Value.TotalMilliseconds);
            var minDuration = executionsWithDuration.Min(e => e.Duration.Value.TotalMilliseconds);
            var maxDuration = executionsWithDuration.Max(e => e.Duration.Value.TotalMilliseconds);

            Console.WriteLine($"Average Duration: {avgDuration:F0}ms");
            Console.WriteLine($"Min Duration: {minDuration:F0}ms");
            Console.WriteLine($"Max Duration: {maxDuration:F0}ms");
        }
    }
}
