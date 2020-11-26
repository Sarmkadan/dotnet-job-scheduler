## CreatePipelineRequestExtensions

The `CreatePipelineRequestExtensions` class provides utility methods for working with pipeline creation requests, including validation, step addition, description management, and cloning. It simplifies building and verifying pipeline configurations through fluent operations.

Example usage:
```csharp
var pipeline = new CreatePipelineRequest
{
    Name = "DataProcessingPipeline"
};

// Add individual job step
pipeline = pipeline.AddStep(Guid.Parse("12345678-0000-0000-0000-000000000001"));

// Add multiple job steps
var jobIds = new[]
{
    Guid.Parse("12345678-0000-0000-0000-000000000002"),
    Guid.Parse("12345678-0000-0000-0000-000000000003")
};
pipeline = pipeline.AddSteps(jobIds, stopOnFailure: false);

// Set description if empty
pipeline = pipeline.SetDescriptionIfEmpty("Processes daily data pipeline");

// Validate configuration
bool isValid = pipeline.IsValid();

// Create a copy of the pipeline request
var pipelineCopy = pipeline.Clone();

Console.WriteLine($"Pipeline has {pipeline.Steps.Count} steps and is valid: {isValid}");
```

## JobPipelineServiceExtensions

The `JobPipelineServiceExtensions` class provides extension methods for the `JobPipelineService` to enhance pipeline management capabilities. It includes methods for checking pipeline existence, retrieving pipelines by name, listing active pipelines, and fetching detailed status with execution statistics for pipeline steps.

Example usage:
```csharp
var pipelineService = new JobPipelineService(context);

// Check if pipeline exists
bool exists = await pipelineService.ExistsAsync(Guid.Parse("12345678-0000-0000-0000-000000000004"));

// Get active pipelines
var activePipelines = await pipelineService.GetActivePipelinesAsync();

// Get pipeline status with execution stats
var pipelineStatus = await pipelineService.GetPipelineStatusWithStatsAsync(Guid.Parse("12345678-0000-0000-0000-000000000004"));

if (pipelineStatus != null)
{
    Console.WriteLine($"Pipeline: {pipelineStatus.PipelineName} (ID: {pipelineStatus.PipelineId})");
    foreach (var step in pipelineStatus.ExecutionStats)
    {
        Console.WriteLine($"Step {step.StepOrder}: {step.JobName} (Job ID: {step.JobId})");
        Console.WriteLine($"  Status: {step.Status}");
        Console.WriteLine($"  Last Executed: {step.LastExecutedAt}");
        Console.WriteLine($"  Ready: {step.IsReady}");
        Console.WriteLine($"  Successes: {step.SuccessCount}, Failures: {step.FailureCount}");
        Console.WriteLine($"  Avg Duration: {step.AverageDuration}, Total: {step.TotalExecutions}");
    }
}
```

## DependencyGraphValidationResultExtensions

The `DependencyGraphValidationResultExtensions` class provides utility methods to analyze and combine dependency graph validation results. It helps detect cycles, validate graph integrity, and format cycle information for logging or display.

Example usage:
```csharp
var result1 = DependencyGraphValidationResult.Valid();
var result2 = new DependencyGraphValidationResult
{
    IsValid = false,
    Message = "Cycle detected",
    CycleNodes = new List<Guid>
    {
        Guid.Parse("12345678-0000-0000-0000-000000000001"),
        Guid.Parse("12345678-0000-0000-0000-000000000002")
    }
};

// Combine results
var combinedResult = result1.CombineWith(result2);

if (combinedResult.HasCycle())
{
    Console.WriteLine($"Cycle detected: {combinedResult.FormatCycle()}");
    Console.WriteLine($"Error: {combinedResult.GetErrorMessage()}");
    Console.WriteLine($"Cycle count: {combinedResult.CycleCount()}");
}
else
{
    Console.WriteLine("Graph is valid.");
}
```

// ... (rest of README.md content)
