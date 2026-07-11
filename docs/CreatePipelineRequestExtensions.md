# CreatePipelineRequestExtensions

The `CreatePipelineRequestExtensions` class provides a suite of static extension methods designed to enhance the `CreatePipelineRequest` type within the `dotnet-job-scheduler` framework. These utilities facilitate a fluent builder pattern for constructing pipeline requests, streamline metadata initialization, and offer safe cloning mechanisms to support pipeline configuration and reuse.

## API

### IsValid
`public static bool IsValid(this CreatePipelineRequest request)`

Checks whether the provided `CreatePipelineRequest` is correctly configured. 
- **Returns**: `true` if the request meets all mandatory configuration criteria; otherwise, `false`.

### AddStep
`public static CreatePipelineRequest AddStep(this CreatePipelineRequest request, JobStep step)`

Appends a single `JobStep` to the internal sequence of the pipeline request.
- **Parameters**: `step` – The `JobStep` instance to be added.
- **Returns**: The `CreatePipelineRequest` instance to allow for method chaining.
- **Throws**: `ArgumentNullException` if `request` or `step` is null.

### AddSteps
`public static CreatePipelineRequest AddSteps(this CreatePipelineRequest request, IEnumerable<JobStep> steps)`

Appends a collection of `JobStep` objects to the pipeline request.
- **Parameters**: `steps` – An enumerable collection of `JobStep` instances to add.
- **Returns**: The `CreatePipelineRequest` instance to allow for method chaining.
- **Throws**: `ArgumentNullException` if `request` or `steps` is null.

### SetDescriptionIfEmpty
`public static CreatePipelineRequest SetDescriptionIfEmpty(this CreatePipelineRequest request, string description)`

Assigns a description to the pipeline request only if the current description property is currently null or whitespace.
- **Parameters**: `description` – The string description to apply if the field is empty.
- **Returns**: The `CreatePipelineRequest` instance to allow for method chaining.
- **Throws**: `ArgumentNullException` if `request` is null.

### Clone
`public static CreatePipelineRequest Clone(this CreatePipelineRequest request)`

Generates a new `CreatePipelineRequest` instance that is a copy of the original request.
- **Returns**: A new instance of `CreatePipelineRequest` containing the same configuration as the original.
- **Throws**: `ArgumentNullException` if `request` is null.

## Usage

```csharp
// Fluent construction of a pipeline request
var pipelineRequest = new CreatePipelineRequest()
    .SetDescriptionIfEmpty("Daily Data Processing Pipeline")
    .AddStep(extractStep)
    .AddSteps(new[] { transformStep, loadStep });

// Validation and cloning for safe reuse
if (pipelineRequest.IsValid())
{
    var backupRequest = pipelineRequest.Clone();
    // Proceed with processing using the backup...
}
```

## Notes

- **Thread-Safety**: These extension methods are not inherently thread-safe. They modify the state of the underlying `CreatePipelineRequest` instance. Ensure that any `CreatePipelineRequest` instance is accessed exclusively by one thread if these methods are utilized to mutate its state.
- **Cloning Behavior**: The `Clone` method performs a shallow copy of the request object. If the `CreatePipelineRequest` holds references to mutable objects within its properties, those references will be shared between the original and the cloned instances.
- **Null Handling**: All methods perform null checks on the `request` parameter. Passing a null reference to any of these extensions will result in an `ArgumentNullException`.
