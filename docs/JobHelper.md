# JobHelper

`JobHelper` is a static utility class providing serialization, diagnostics, validation, and formatting helpers for the job scheduling infrastructure. It centralizes common operations such as parameter serialization, handler type validation, status and frequency descriptions, reliability scoring, and duration formatting, ensuring consistent behavior across the scheduler.

## API

### SerializeParameters

```csharp
public static string SerializeParameters(object? parameters)
```

Serializes an arbitrary parameters object into its JSON string representation. Accepts `null`, in which case it returns `null`. Used to persist job invocation arguments in storage.

- **Parameters**: `parameters` — the object to serialize; may be `null`.
- **Returns**: a JSON string, or `null` if the input is `null`.
- **Throws**: `Newtonsoft.Json.JsonSerializationException` (or equivalent from the configured serializer) when the object graph cannot be serialized.

### DeserializeParameters\<T\>

```csharp
public static T? DeserializeParameters<T>(string? json)
```

Deserializes a JSON string back into an instance of `T`. Returns `default(T)` when the input string is `null` or empty.

- **Parameters**: `json` — the JSON string to deserialize; may be `null`.
- **Returns**: an instance of `T`, or `default(T)` if the input is `null`/empty.
- **Throws**: `Newtonsoft.Json.JsonSerializationException` when the JSON is malformed or incompatible with `T`.

### GetJobStatusDescription

```csharp
public static string GetJobStatusDescription(JobStatus status)
```

Maps a `JobStatus` enum value to a human-readable, localized description string suitable for logs and user interfaces.

- **Parameters**: `status` — a member of the `JobStatus` enumeration.
- **Returns**: a non-null, non-empty description string.
- **Throws**: `ArgumentOutOfRangeException` when an unrecognized `JobStatus` value is supplied.

### IsValidHandlerType

```csharp
public static bool IsValidHandlerType(Type type)
```

Determines whether a given `Type` qualifies as a valid job handler. Checks that the type is a concrete class implementing the required handler contract (e.g., `IJobHandler`) and is not abstract or open generic.

- **Parameters**: `type` — the `Type` to inspect; must not be `null`.
- **Returns**: `true` if the type can be used as a job handler; otherwise `false`.
- **Throws**: `ArgumentNullException` when `type` is `null`.

### GetExecutionFrequencyDescription

```csharp
public static string GetExecutionFrequencyDescription(TimeSpan interval)
```

Produces a human-readable description of how often a job executes based on its recurrence interval. Examples include “every 30 seconds”, “every 2 hours”, or “once”.

- **Parameters**: `interval` — the `TimeSpan` representing the period between executions.
- **Returns**: a non-null, non-empty string describing the frequency.
- **Throws**: `ArgumentOutOfRangeException` when `interval` is negative or `TimeSpan.Zero` (implementation-dependent).

### CalculateReliabilityScore

```csharp
public static int CalculateReliabilityScore(int totalExecutions, int successfulExecutions, int failureCount, TimeSpan averageLatency)
```

Computes an integer reliability score (typically 0–100) for a job based on its execution history. Weighs success rate, failure count, and average latency to produce a single health indicator.

- **Parameters**:
  - `totalExecutions` — total number of execution attempts.
  - `successfulExecutions` — number of successful runs.
  - `failureCount` — number of failed runs.
  - `averageLatency` — average execution latency.
- **Returns**: an integer score; higher values indicate better reliability.
- **Throws**: `ArgumentOutOfRangeException` when any count is negative or `totalExecutions` is less than the sum of successful and failed runs.

### GetRecommendedAction

```csharp
public static string GetRecommendedAction(int reliabilityScore, bool isConcerning)
```

Returns a recommended operational action string (e.g., “No action required”, “Review job configuration”, “Disable and investigate”) based on the reliability score and a concerning-state flag.

- **Parameters**:
  - `reliabilityScore` — the score from `CalculateReliabilityScore`.
  - `isConcerning` — whether the job is currently flagged as concerning.
- **Returns**: a non-null action recommendation string.
- **Throws**: does not throw under normal input ranges.

### FormatDuration

```csharp
public static string FormatDuration(TimeSpan duration)
```

Formats a `TimeSpan` into a compact, human-readable duration string (e.g., “2h 15m 30s”). Omits zero components unless the entire duration is zero.

- **Parameters**: `duration` — the `TimeSpan` to format.
- **Returns**: a non-null formatted string.
- **Throws**: `ArgumentOutOfRangeException` when `duration` is negative (implementation-dependent).

### IsConcerning

```csharp
public static bool IsConcerning(int reliabilityScore, int consecutiveFailures, TimeSpan timeSinceLastSuccess)
```

Evaluates whether a job’s current metrics warrant a “concerning” flag. Combines low reliability score, consecutive failure count, and elapsed time since the last successful execution against predefined thresholds.

- **Parameters**:
  - `reliabilityScore` — the score from `CalculateReliabilityScore`.
  - `consecutiveFailures` — number of consecutive failed executions.
  - `timeSinceLastSuccess` — time elapsed since the last successful run.
- **Returns**: `true` if the job meets concerning criteria; otherwise `false`.
- **Throws**: `ArgumentOutOfRangeException` when `consecutiveFailures` is negative.

## Usage

### Example 1: Serializing and deserializing job parameters

```csharp
public class MyJobParams
{
    public string Target { get; set; }
    public int Retries { get; set; }
}

// Serialize before persisting
var parameters = new MyJobParams { Target = "orders", Retries = 3 };
string json = JobHelper.SerializeParameters(parameters);
// json: {"Target":"orders","Retries":3}

// Deserialize when loading the job
MyJobParams? restored = JobHelper.DeserializeParameters<MyJobParams>(json);
Console.WriteLine(restored?.Target); // "orders"
```

### Example 2: Evaluating job health and getting a recommendation

```csharp
int score = JobHelper.CalculateReliabilityScore(
    totalExecutions: 100,
    successfulExecutions: 85,
    failureCount: 15,
    averageLatency: TimeSpan.FromMilliseconds(200));

bool concerning = JobHelper.IsConcerning(
    reliabilityScore: score,
    consecutiveFailures: 5,
    timeSinceLastSuccess: TimeSpan.FromHours(2));

string action = JobHelper.GetRecommendedAction(score, concerning);
Console.WriteLine(action); // e.g., "Review job configuration"
```

## Notes

- **Serialization dependency**: `SerializeParameters` and `DeserializeParameters<T>` rely on the configured JSON serializer (typically Newtonsoft.Json). Custom converter settings applied globally will affect their output. Ensure parameter types are serializable; circular references cause exceptions.
- **Null handling**: `SerializeParameters` returns `null` for `null` input, while `DeserializeParameters<T>` returns `default(T)` for `null` or empty JSON. Callers must distinguish between “no parameters” and “parameters that deserialize to null/default.”
- **Validation order**: `IsValidHandlerType` performs a shallow type check. It does not verify that the type’s dependencies can be resolved or that it can be instantiated. Use in conjunction with DI container validation.
- **Status descriptions**: `GetJobStatusDescription` is exhaustive for defined enum members. Passing an undefined integer cast to `JobStatus` throws `ArgumentOutOfRangeException`.
- **Reliability score boundaries**: `CalculateReliabilityScore` expects logically consistent inputs (`successfulExecutions + failureCount ≤ totalExecutions`). Violations throw `ArgumentOutOfRangeException`. The score formula may clamp results to a fixed range (e.g., 0–100).
- **Concerning thresholds**: `IsConcerning` uses internal thresholds that may change across versions. Callers should not hard-code expectations about the exact cutoff values.
- **Thread safety**: All methods are static and operate on immutable inputs or produce new output strings. No shared mutable state is maintained. The class is safe to call concurrently from multiple threads without external synchronization.
