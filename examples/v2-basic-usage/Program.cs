#nullable enable

using JobScheduler.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace JobScheduler.Examples.V2BasicUsage
{
    /// <summary>
    /// Demonstrates basic usage of dotnet-job-scheduler v2.0 features
    /// </summary>
    public sealed class BasicJobHandler : IJobHandler
    {
        private readonly ILogger<BasicJobHandler> _logger;

        public BasicJobHandler(ILogger<BasicJobHandler> logger)
        {
            _logger = logger;
        }

        public async Task<string> ExecuteAsync(Job job, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Executing basic job: {JobName}", job.Name);

            // Simulate some work
            await Task.Delay(1000, cancellationToken);

            return "Basic job completed successfully";
        }
    }

    public sealed class Program
    {
        public static async Task Main(string[] args)
        {
            // Create host
            var builder = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddJobScheduler(options =>
                    {
                        options.ConnectionString = "Data Source=scheduler.db";
                        options.MaxConcurrentJobs = 5;
                        options.DefaultTimeoutSeconds = 300;
                        options.DefaultMaxRetries = 2;
                    });

                    services.AddScoped<BasicJobHandler>();
                })
                .Build();

            await host.StartAsync();

            // Register job handlers
            var schedulerService = host.Services.GetRequiredService<JobSchedulerService>();

            // Create a job using the new v2.0 API
            var job = new Job
            {
                Name = "V2BasicUsage",
                Description = "Basic example of v2.0 features",
                CronExpression = "*/5 * * * *", // Every 5 minutes
                HandlerType = typeof(BasicJobHandler).FullName,
                Priority = JobPriority.Normal,
                IsActive = true
            };

            var createdJob = await schedulerService.CreateJobAsync(job, "system");
            Console.WriteLine($"Created job with ID: {createdJob.Id}");

            // Start background service to process jobs
            var backgroundService = new JobSchedulerBackgroundService(
                host.Services,
                host.Services.GetRequiredService<ILogger<JobSchedulerBackgroundService>>());

            await host.RunAsync();
        }
    }
}