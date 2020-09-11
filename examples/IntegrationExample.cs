// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using JobScheduler.Core.Services;

/// <summary>
/// Integration Example
///
/// Shows how to integrate the scheduler into an ASP.NET Core application
/// using a HostedService to process jobs in the background.
/// </summary>

public class IntegrationExample : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public IntegrationExample(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Poll for due jobs every 10 seconds
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            using var scope = _serviceProvider.CreateScope();
            var scheduler = scope.ServiceProvider.GetRequiredService<JobSchedulerService>();
            
            await scheduler.ExecuteDueJobsAsync();
        }
    }
}
