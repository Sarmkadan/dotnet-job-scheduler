// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using JobScheduler.Core.Configuration;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Services;

/// <summary>
/// Basic Usage Example
/// 
/// This example provides the absolute minimum setup required to use the
/// dotnet-job-scheduler library.
/// </summary>

public sealed class BasicUsage
{
    public static async Task Main()
    {
        // 1. Setup DI and Scheduler
        var services = new ServiceCollection();
        
        services.AddJobScheduler(options =>
        {
            options.ConnectionString = "Data Source=:memory:"; // In-memory for example
        });

        var provider = services.BuildServiceProvider();
        var schedulerService = provider.GetRequiredService<JobSchedulerService>();
        
        // 2. Create a job (requires a handler type)
        var job = new Job
        {
            Name = "SimpleJob",
            CronExpression = "* * * * *", // Every minute
            HandlerType = "MyApp.Jobs.MyHandler, MyApp" 
        };

        var createdJob = await schedulerService.CreateJobAsync(job, "user");
        Console.WriteLine($"Created job: {createdJob.Id}");

        // 3. Execute due jobs (usually run in a background loop)
        var executions = await schedulerService.ExecuteDueJobsAsync();
        Console.WriteLine($"Executed {executions.Count} jobs.");
    }
}
