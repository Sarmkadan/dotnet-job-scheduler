# JobPipelineExtensions

This document provides detailed information about the extension methods available for the `JobPipeline` class in the `JobScheduler.Core.Domain.Entities` namespace.

## Methods

### IsValidForExecution

Determines whether a job pipeline is active and has at least one step.

#### Signature

```csharp
public static bool IsValidForExecution(this JobPipeline pipeline)
```

#### Parameters

| Name | Type | Description |
|------|------|-------------|
| pipeline | JobPipeline | The job pipeline to check. |

#### Returns

| Type | Description |
|------|-------------|
| bool | `true` if the pipeline is active and has at least one step; otherwise, `false`. |

#### Exceptions

| Exception | Condition |
|-----------|-----------|
| ArgumentNullException | Thrown when `pipeline` is `null`. |

#### Example

```csharp
var pipeline = new JobPipeline { IsActive = true };
pipeline.Steps.Add(new JobPipelineStep { Name = "Step1" });

bool isValid = pipeline.IsValidForExecution(); // Returns true
```

---

### GetSteps

Gets a read-only list of steps in the job pipeline.

#### Signature

```csharp
public static IReadOnlyList<JobPipelineStep> GetSteps(this JobPipeline pipeline)
```

#### Parameters

| Name | Type | Description |
|------|------|-------------|
| pipeline | JobPipeline | The job pipeline. |

#### Returns

| Type | Description |
|------|-------------|
| IReadOnlyList<JobPipelineStep> | A read-only list of steps in the pipeline. |

#### Exceptions

| Exception | Condition |
|-----------|-----------|
| ArgumentNullException | Thrown when `pipeline` is `null`. |

#### Example

```csharp
var pipeline = new JobPipeline();
pipeline.Steps.Add(new JobPipelineStep { Name = "Step1" });
pipeline.Steps.Add(new JobPipelineStep { Name = "Step2" });

IReadOnlyList<JobPipelineStep> steps = pipeline.GetSteps();
// steps contains two elements: Step1 and Step2
```

---

### HasStopOnFailureStep

Determines whether a job pipeline has any steps with `JobPipelineStep.StopOnFailure` set to `true`.

#### Signature

```csharp
public static bool HasStopOnFailureStep(this JobPipeline pipeline)
```

#### Parameters

| Name | Type | Description |
|------|------|-------------|
| pipeline | JobPipeline | The job pipeline to check. |

#### Returns

| Type | Description |
|------|-------------|
| bool | `true` if the pipeline has at least one step with `StopOnFailure` set to `true`; otherwise, `false`. |

#### Exceptions

| Exception | Condition |
|-----------|-----------|
| ArgumentNullException | Thrown when `pipeline` is `null`. |

#### Example

```csharp
var pipeline = new JobPipeline();
pipeline.Steps.Add(new JobPipelineStep { Name = "Step1", StopOnFailure = false });
pipeline.Steps.Add(new JobPipelineStep { Name = "Step2", StopOnFailure = true });

bool hasStopOnFailure = pipeline.HasStopOnFailureStep(); // Returns true
```