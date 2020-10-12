# CreatePipelineRequest

Represents a request to create a new pipeline in the job scheduler system, encapsulating pipeline metadata, configuration, and associated steps.

## API

### `Name` (string)
The name of the pipeline. Must be unique within the system. Used for identification and display purposes.

### `Description` (string)
A human-readable description of the pipeline's purpose or functionality. Optional field.

### `Steps` (List<PipelineStepRequest>)
A collection of pipeline steps to be executed in sequence. Each step defines a unit of work within the pipeline.

### `JobId` (Guid)
The unique identifier of the job to which this pipeline belongs. Required field.

### `StopOnFailure` (bool)
Determines whether the pipeline execution should halt if any step fails. Defaults to `true`.

### `Id` (Guid)
The unique identifier for the pipeline. Assigned by the system upon creation.

### `IsActive` (bool)
Indicates whether the pipeline is currently active and eligible for execution. Defaults to `true`.

### `CreatedAt` (DateTime)
The timestamp when the pipeline was created. Set by the system.

### `CreatedBy` (string?)
The identifier of the user or system entity that created the pipeline. Optional field.

## Usage

### Example 1: Creating a Basic Pipeline
