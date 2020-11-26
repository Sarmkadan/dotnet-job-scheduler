// ... (rest of README.md content)

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

// ... (rest of README.md content)
