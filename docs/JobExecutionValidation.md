# JobExecutionValidation

`JobExecutionValidation` provides three public extension methods for validating `JobExecution` entities. The source file currently contains three public methods, not four.

## Methods

### `Validate(this JobExecution value)`

```csharp
public static IReadOnlyList<string> Validate(this JobExecution value)
```

Checks every validation rule below and returns a read-only list containing all problems found. The list is empty when the execution is valid.

Rules checked:

- `JobId` must not be `Guid.Empty`.
- `Status` must be a defined `ExecutionStatus` value.
- `StartedAt` must not be the default `DateTime` and must not be more than five minutes ahead of `DateTime.UtcNow`.
- When present, `CompletedAt` must not be earlier than `StartedAt` or more than five minutes ahead of `DateTime.UtcNow`.
- `DurationMilliseconds` must not be negative. When `CompletedAt` is present and the duration is positive, it must be within 1,000 milliseconds of `CompletedAt - StartedAt`.
- `AttemptNumber` must be from 1 through 1,000.
- `ExecutorName` must not be null, empty, or whitespace and must be at most 255 characters.
- When `ExecutorInstance` is not null, empty, or whitespace, it must be at most 255 characters.
- `CreatedAt` must not be the default `DateTime`, must not be more than five minutes ahead of `DateTime.UtcNow`, and must not be more than five minutes before `StartedAt`.
- `MemoryUsageMb` must be from 0 through 1,000,000.
- `CpuUsagePercent` must be from 0 through 100, inclusive.
- `Success`, `TimedOut`, `Cancelled`, and `Skipped` executions must have `CompletedAt` set.
- A `Failed` execution must have `CompletedAt` set and a non-blank `ErrorMessage`.
- A `Running` execution must not have `CompletedAt` set.
- When `Output` is not null, empty, or whitespace, it must be at most 1,000,000 characters.
- When `ErrorMessage` is not null, empty, or whitespace, it must be at most 10,000 characters.
- When `StackTrace` is not null, empty, or whitespace, it must be at most 100,000 characters.

Exceptions:

- `ArgumentNullException` when `value` is null.

### `IsValid(this JobExecution value)`

```csharp
public static bool IsValid(this JobExecution value)
```

Runs the same rules as `Validate` and returns `true` when no validation errors are found; otherwise, it returns `false`.

Exceptions:

- `ArgumentNullException` when `value` is null, propagated from `Validate`.

> `JobExecution` also defines an instance method named `IsValid()`. To invoke this extension method unambiguously, call `JobExecutionValidation.IsValid(execution)`.

### `EnsureValid(this JobExecution value)`

```csharp
public static void EnsureValid(this JobExecution value)
```

Runs the same rules as `Validate`. It returns normally when the execution is valid.

Exceptions:

- `ArgumentNullException` when `value` is null.
- `ArgumentException` when one or more rules fail. Its message contains every validation problem, separated by semicolons, and its parameter name is `value`.

## Usage example

```csharp
using JobScheduler.Core.Constants;
using JobScheduler.Core.Domain.Entities;

var startedAt = DateTime.UtcNow;
var execution = new JobExecution
{
    JobId = Guid.NewGuid(),
    Status = ExecutionStatus.Running,
    StartedAt = startedAt,
    CreatedAt = startedAt,
    AttemptNumber = 1,
    ExecutorName = "worker-01",
    MemoryUsageMb = 256,
    CpuUsagePercent = 18.5
};

IReadOnlyList<string> errors = execution.Validate();
foreach (string error in errors)
{
    Console.WriteLine(error);
}

// Use the static form because JobExecution has its own IsValid() instance method.
bool isValid = JobExecutionValidation.IsValid(execution);

// Throws ArgumentException if any validation rule fails.
execution.EnsureValid();
```
