// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using JobScheduler.Core.Constants;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Services;

/// <summary>
/// Advanced Usage Example
///
/// Demonstrates custom configuration, priority settings, retry policies,
/// and error handling.
/// </summary>

public class AdvancedUsage
{
    public async Task CreateAdvancedJob(JobSchedulerService schedulerService)
    {
        var complexJob = new Job
        {
            Name = "DataSyncTask",
            Description = "Critical data synchronization task with retries",
            CronExpression = "0 */4 * * *", // Every 4 hours
            HandlerType = "MyApp.Jobs.SyncHandler, MyApp",
            
            // Priority for scheduling
            Priority = JobPriority.Critical,
            
            // Configure retries for reliability
            MaxRetries = 5,
            RetryBackoffSeconds = 30, // Linear backoff
            
            // Performance constraints
            ExecutionTimeoutSeconds = 600, // 10 minutes
            MaxConcurrentExecutions = 1    // Ensure exclusive execution
        };

        try
        {
            var job = await schedulerService.CreateJobAsync(complexJob, "admin");
            Console.WriteLine($"Advanced job created with ID: {job.Id}");
        }
        catch (Exception ex)
        {
            // Handle validation or database errors
            Console.WriteLine($"Failed to create job: {ex.Message}");
        }
    }
}
