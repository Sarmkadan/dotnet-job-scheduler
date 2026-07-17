// ... existing content ...

## JobPipelineServiceTests

The `JobPipelineServiceTests` class provides unit tests for the `JobPipelineService` class, focusing on pipeline creation, retrieval, deletion, and mapping functionality. These tests ensure that the service behaves correctly under various scenarios.

The following example demonstrates how to use some of the public members of `JobPipelineServiceTests` in your application:

```csharp
using DotnetJobScheduler.Tests;
using JobScheduler.Core.Data;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Services;

// Create an instance of JobPipelineServiceTests
var tests = new JobPipelineServiceTests();

// Create a test job
var job = new Job { Id = Guid.NewGuid(), Name = "TestJob", CronExpression = "0 9 * * *" };

// Create a test pipeline request
var request = new CreatePipelineRequest
{
    Name = "TestPipeline",
    Steps = new List<PipelineStepRequest> { new() { JobId = job.Id } }
};

// Test creating a pipeline with a valid request
var (service, ctx) = JobPipelineServiceTests.CreateService();
var pipeline = await service.CreatePipelineAsync(request, "test-user");

// Test getting a pipeline by ID
var fetchedPipeline = await service.GetPipelineAsync(pipeline.Id);

// Test deleting a pipeline
var deleted = await service.DeletePipelineAsync(pipeline.Id);

// Test getting all pipelines
var allPipelines = await service.GetAllPipelinesAsync();

// Test mapping a pipeline to a response
var response = JobPipelineService.MapToResponse(pipeline);
``` 
