# DependencyGraphValidationResultExtensions

DependencyGraphValidationResultExtensions provides a set of static extension methods designed to simplify the analysis and manipulation of DependencyGraphValidationResult objects within the dotnet-job-scheduler library. These utilities enable efficient inspection of graph validity, comprehensive cycle detection, and the merging of multiple validation results to streamline complex job scheduling dependency management.

## API

### IsGraphValid
`public static bool IsGraphValid(this DependencyGraphValidationResult result)`
Returns a boolean value indicating whether the graph validation process completed without identifying any structural issues or dependencies violations.

### GetErrorMessage
`public static string GetErrorMessage(this DependencyGraphValidationResult result)`
Retrieves the descriptive error message associated with the validation failure. Returns an empty string or null if the graph is valid.

### HasCycle
`public static bool HasCycle(this DependencyGraphValidationResult result)`
Indicates whether the dependency graph contains one or more cycles, which would prevent valid job scheduling.

### CycleCount
`public static int CycleCount(this DependencyGraphValidationResult result)`
Returns the total number of distinct cycles detected within the dependency graph.

### FormatCycle
`public static string FormatCycle(this DependencyGraphValidationResult result, int index)`
Returns a human-readable string representation of a specific cycle identified during the validation process. The `index` parameter specifies which detected cycle to format, ranging from 0 to `CycleCount - 1`.

### CombineWith
`public static DependencyGraphValidationResult CombineWith(this DependencyGraphValidationResult result, DependencyGraphValidationResult other)`
Merges the current validation result with another `DependencyGraphValidationResult` instance. This operation aggregates all errors and detected cycles into a new, combined result instance.

## Usage

### Basic Validation Check
```csharp
var result = graphValidator.Validate(jobGraph);

if (!result.IsGraphValid())
{
    Console.WriteLine($"Graph validation failed: {result.GetErrorMessage()}");
}
```

### Combining Results and Cycle Inspection
```csharp
var resultA = graphValidator.Validate(jobGraphA);
var resultB = graphValidator.Validate(jobGraphB);

var combined = resultA.CombineWith(resultB);

if (combined.HasCycle())
{
    Console.WriteLine($"Validation detected {combined.CycleCount()} cycles.");
    // Log the first cycle found
    Console.WriteLine($"Detailed cycle report: {combined.FormatCycle(0)}");
}
```

## Notes

*   **Null Handling**: If the `DependencyGraphValidationResult` instance passed to these extension methods is null, a `NullReferenceException` will be thrown. Ensure the result object is validated prior to calling these extensions.
*   **Index Bounds**: When utilizing `FormatCycle`, ensure the provided `index` parameter is within the valid range `[0, CycleCount - 1]`. Providing an index outside this range may result in an `ArgumentOutOfRangeException` or similar runtime error, depending on the internal implementation of the result object.
*   **Thread Safety**: These methods are designed as pure functions that operate on the state of the provided `DependencyGraphValidationResult` instance. They do not maintain or modify internal global state and are considered thread-safe, provided the `DependencyGraphValidationResult` objects themselves are treated as immutable or are handled appropriately within a multi-threaded context.
