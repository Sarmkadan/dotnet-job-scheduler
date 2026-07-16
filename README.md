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

<!-- ... rest of README content -->
