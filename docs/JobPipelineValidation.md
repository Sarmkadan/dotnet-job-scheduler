# JobPipelineValidation

Provides validation helpers for `JobPipeline` entities. Validates all public members according to business rules and data integrity constraints.

## Methods

### Validate(this JobPipeline? value)

```csharp
public static IReadOnlyList<string> Validate(this JobPipeline? value)
```

Validates the specified `JobPipeline` instance and returns a list of human-readable problems.

#### Parameters
- `value`: The pipeline instance to validate.

#### Returns
An empty list if valid; otherwise a list of validation error messages.

#### Exceptions
- `ArgumentNullException`: Thrown when `value` is null.

#### Validation Rules
- **Name**: Cannot be null, empty, or whitespace; maximum 255 characters
- **Description**: Maximum 1024 characters when provided
- **CreatedAt**: Must be non-default DateTime; cannot be in the future (beyond 5 minutes)
- **UpdatedAt**: When present, must be non-default DateTime; cannot be in the future (beyond 5 minutes); cannot be earlier than CreatedAt
- **CreatedBy**: When provided, cannot be empty or whitespace; maximum 128 characters
- **Steps**: Collection cannot be null; each step must have:
  - Non-default Guid for Id
  - Non-default Guid for PipelineId
  - Non-default Guid for JobId
  - StepOrder between 0 and 9999 inclusive
  - Step cannot be null

### IsValid(this JobPipeline? value)

```csharp
public static bool IsValid(this JobPipeline? value)
```

Determines whether the specified `JobPipeline` instance is valid.

#### Parameters
- `value`: The pipeline instance to check.

#### Returns
True if valid; otherwise false.

#### Exceptions
- `ArgumentNullException`: Thrown when `value` is null.

### EnsureValid(this JobPipeline? value)

```csharp
public static void EnsureValid(this JobPipeline? value)
```

Ensures that the specified `JobPipeline` instance is valid, throwing an `ArgumentException` with a detailed message listing all validation problems if it is not.

#### Parameters
- `value`: The pipeline instance to validate.

#### Exceptions
- `ArgumentNullException`: Thrown when `value` is null.
- `ArgumentException`: Thrown when `value` is invalid, with a message listing all problems.

## Usage Example

```csharp
using JobScheduler.Core.Domain.Entities;

// Create a pipeline to validate
var pipeline = new JobPipeline
{
    Name = "CI/CD Pipeline",
    Description = "Build, test, and deploy application",
    IsActive = true,
    CreatedAt = DateTime.UtcNow,
    Steps = new List<JobPipelineStep>
    {
        new JobPipelineStep
        {
            Id = Guid.NewGuid(),
            PipelineId = Guid.NewGuid(), // Should match pipeline.Id in real usage
            JobId = Guid.NewGuid(),
            StepOrder = 0
        }
    }
};

// Set the PipelineId to match the pipeline's Id for validity
pipeline.Steps[0].PipelineId = pipeline.Id;

// Validate and get detailed error messages
IReadOnlyList<string> errors = pipeline.Validate();
if (errors.Count > 0)
{
    foreach (string error in errors)
    {
        Console.WriteLine($"Validation error: {error}");
    }
}

// Check if valid
bool isValid = pipeline.IsValid();
Console.WriteLine($"Pipeline is valid: {isValid}");

// Throw exception if invalid (with detailed message)
pipeline.EnsureValid(); // Won't throw if valid
```