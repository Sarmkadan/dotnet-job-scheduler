# Pipeline Models

The pipeline DTOs are defined in `src/JobScheduler.Core/Domain/Models/PipelineModels.cs`. They are used by `PipelinesController` for pipeline creation, retrieval, listing, and status responses.

The JSON examples below use the camel-case property names produced by the default ASP.NET Core web JSON configuration. A host application can override its JSON serialization settings.

## CreatePipelineRequest

Request body accepted by `POST /api/pipelines`.

| Property | Type | Default | Validation attributes | Description |
| --- | --- | --- | --- | --- |
| `Name` | `string` | `string.Empty` | None | Human-readable pipeline name. |
| `Description` | `string` | `string.Empty` | None | Description of the pipeline's purpose. |
| `Steps` | `List<PipelineStepRequest>` | New empty list | None | Ordered steps; each job waits for the preceding job to succeed. |

Although the DTO has no validation attributes, `JobPipelineService.CreatePipelineAsync` rejects a null, empty, or whitespace-only `Name` and requires at least two entries in `Steps`. The model's XML documentation describes `Name` as having a maximum length of 256 characters, but this DTO does not declare a maximum-length validation attribute.

## PipelineStepRequest

Describes one requested step in a new pipeline.

| Property | Type | Default | Validation attributes | Description |
| --- | --- | --- | --- | --- |
| `JobId` | `Guid` | `Guid.Empty` | None | ID of the job to execute. The creation service verifies that the referenced job exists. |
| `StopOnFailure` | `bool` | `true` | None | Whether the pipeline should stop when this step fails. |

Steps are assigned zero-based order according to their position in the request array. The creation service also registers a dependency from each step after the first to its preceding step.

## PipelineResponse

Represents a pipeline returned by the API, including its ordered steps.

| Property | Type | Default | Validation attributes | Description |
| --- | --- | --- | --- | --- |
| `Id` | `Guid` | `Guid.Empty` | None | Pipeline ID. |
| `Name` | `string` | `string.Empty` | None | Pipeline name. |
| `Description` | `string` | `string.Empty` | None | Pipeline description. |
| `IsActive` | `bool` | `false` | None | Whether the pipeline is active. |
| `CreatedAt` | `DateTime` | `default(DateTime)` | None | Pipeline creation timestamp. |
| `CreatedBy` | `string?` | `null` | None | Identity that created the pipeline, when available. |
| `Steps` | `List<PipelineStepResponse>` | New empty list | None | Pipeline steps ordered by `StepOrder`. |

## PipelineStepResponse

Represents one persisted step in a pipeline response.

| Property | Type | Default | Validation attributes | Description |
| --- | --- | --- | --- | --- |
| `StepId` | `Guid` | `Guid.Empty` | None | Persisted pipeline-step ID. |
| `JobId` | `Guid` | `Guid.Empty` | None | Referenced job ID. |
| `JobName` | `string?` | `null` | None | Referenced job name, when loaded. |
| `StepOrder` | `int` | `0` | None | Zero-based position in the pipeline. |
| `StopOnFailure` | `bool` | `false` | None | Whether execution should stop if this step fails. |

## PipelineStatusResponse

Represents the current execution status of all steps in a pipeline.

| Property | Type | Default | Validation attributes | Description |
| --- | --- | --- | --- | --- |
| `PipelineId` | `Guid` | `Guid.Empty` | None | Pipeline ID. |
| `PipelineName` | `string` | `string.Empty` | None | Pipeline name. |
| `StepStatuses` | `List<PipelineStepStatus>` | New empty list | None | Current statuses in step order. |

## PipelineStepStatus

Represents the latest known execution state of one pipeline step.

| Property | Type | Default | Validation attributes | Description |
| --- | --- | --- | --- | --- |
| `StepOrder` | `int` | `0` | None | Zero-based position in the pipeline. |
| `JobId` | `Guid` | `Guid.Empty` | None | Referenced job ID. |
| `JobName` | `string?` | `null` | None | Referenced job name, when loaded. |
| `Status` | `string` | `string.Empty` | None | Latest execution status, or `NotStarted` when no execution exists. |
| `LastExecutedAt` | `DateTime?` | `null` | None | Start time of the latest execution, when one exists. |
| `IsReady` | `bool` | `false` | None | Whether the preceding step succeeded; the first step is ready by default. |

## POST /api/pipelines

`PipelinesController.CreatePipeline` accepts a `CreatePipelineRequest`. The referenced job IDs must already exist. On success, the controller returns `201 Created`, sets the `Location` header to the new pipeline's `GET /api/pipelines/{id}` route, and returns a `PipelineResponse` body.

### Example request

```json
{
  "name": "Nightly data pipeline",
  "description": "Imports, validates, and publishes the nightly data set.",
  "steps": [
    {
      "jobId": "11111111-1111-1111-1111-111111111111",
      "stopOnFailure": true
    },
    {
      "jobId": "22222222-2222-2222-2222-222222222222",
      "stopOnFailure": true
    },
    {
      "jobId": "33333333-3333-3333-3333-333333333333",
      "stopOnFailure": false
    }
  ]
}
```

### Example `201 Created` response

The generated IDs and timestamp below are illustrative. `createdBy` is populated from the authenticated user's name and can be `null`.

```json
{
  "id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  "name": "Nightly data pipeline",
  "description": "Imports, validates, and publishes the nightly data set.",
  "isActive": true,
  "createdAt": "2026-09-08T02:15:30.0000000Z",
  "createdBy": "scheduler-admin",
  "steps": [
    {
      "stepId": "aaaaaaaa-0000-0000-0000-000000000001",
      "jobId": "11111111-1111-1111-1111-111111111111",
      "jobName": "Import data",
      "stepOrder": 0,
      "stopOnFailure": true
    },
    {
      "stepId": "aaaaaaaa-0000-0000-0000-000000000002",
      "jobId": "22222222-2222-2222-2222-222222222222",
      "jobName": "Validate data",
      "stepOrder": 1,
      "stopOnFailure": true
    },
    {
      "stepId": "aaaaaaaa-0000-0000-0000-000000000003",
      "jobId": "33333333-3333-3333-3333-333333333333",
      "jobName": "Publish data",
      "stepOrder": 2,
      "stopOnFailure": false
    }
  ]
}
```

If the name is invalid or fewer than two steps are supplied, the controller returns `400 Bad Request` with an `error` property. A missing referenced job produces `404 Not Found` with the same error-body shape.
