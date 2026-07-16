<!-- ... existing content ... -->

## PipelinesController

The `PipelinesController` class provides RESTful API endpoints for managing job pipelines. It allows creating, retrieving, listing, deleting, and checking the status of pipelines.

### Usage

```csharp
using JobScheduler.Core.Controllers;
using JobScheduler.Core.Domain.Models;

// Create a new pipeline
var pipelineService = new JobPipelineService();
var pipeline = await pipelineService.CreatePipelineAsync(new CreatePipelineRequest
{
  Jobs = new[] { "job1", "job2", "job3" },
  Name = "My Pipeline"
});

// Get a pipeline by ID
var pipelineResponse = await new PipelinesController(pipelineService).GetPipelineAsync(pipeline.Id);

// List all pipelines
var pipelines = await new PipelinesController(pipelineService).ListPipelinesAsync();

// Delete a pipeline
await new PipelinesController(pipelineService).DeletePipelineAsync(pipeline.Id);

// Get the status of a pipeline
var pipelineStatus = await new PipelinesController(pipelineService).GetPipelineStatusAsync(pipeline.Id);
```

## DashboardController

The `DashboardController` class provides RESTful API endpoints for monitoring and analyzing job scheduler performance metrics. It exposes endpoints for retrieving overview statistics, queue status, performance timelines, health reports, and various job analytics.

### Usage

```csharp
using JobScheduler.Core.Controllers;
using JobScheduler.Core.Domain.Models;

// Create dashboard controller with required services
var jobService = new JobService();
var queueService = new QueueService();
var performanceService = new PerformanceService();
var healthService = new HealthService();

var controller = new DashboardController(
    jobService,
    queueService,
    performanceService,
    healthService
);

// Get dashboard overview with key metrics
var overview = await controller.GetOverview();
Console.WriteLine($"Total Jobs: {controller.TotalJobs}");
Console.WriteLine($"Active Jobs: {controller.ActiveJobs}");
Console.WriteLine($"Average Success Rate: {controller.AverageSuccessRate:P}");
Console.WriteLine($"Last Updated: {controller.LastUpdatedAt}");

// Get queue status
var queueStatus = await controller.GetQueueStatus();
Console.WriteLine($"Pending Jobs: {queueStatus.PendingJobs}");
Console.WriteLine($"Running Jobs: {queueStatus.RunningJobs}");
Console.WriteLine($"Failed Jobs: {queueStatus.FailedJobs}");

// Get performance timeline data
var timeline = await controller.GetPerformanceTimeline();
foreach (var point in timeline)
{
    Console.WriteLine($"{point.Timestamp}: {point.SuccessRate:P} success rate");
}

// Get health report
var health = await controller.GetHealthReport();
Console.WriteLine($"System Status: {health.Status}");
Console.WriteLine($"Uptime: {health.Uptime}");

// Get slowest jobs
var slowestJobs = await controller.GetSlowestJobs();
foreach (var job in slowestJobs.Take(5))
{
    Console.WriteLine($"{job.JobId}: {job.AverageExecutionTimeMs}ms avg");
}

// Get most failing jobs
var failingJobs = await controller.GetMostFailingJobs();
foreach (var job in failingJobs.Take(5))
{
    Console.WriteLine($"{job.JobId}: {job.FailureCount} failures");
}
```

<!-- ... rest of README content -->