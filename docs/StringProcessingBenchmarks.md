# StringProcessingBenchmarks

The `StringProcessingBenchmarks` class serves as a dedicated harness for performance testing and validation of common string manipulation operations within the `dotnet-job-scheduler` project. It encapsulates a set of predefined input scenarios and expected outcomes for operations such as slugification, JSON escaping, truncation, and sensitive data masking, allowing benchmarking frameworks to measure execution time and allocation rates under consistent conditions without requiring external data setup.

## API

The following public members represent specific test cases or data fixtures used during benchmark execution. These members expose string values rather than methods, providing the input data or expected results for the associated benchmark tests.

### `ToSlug_Short`
*   **Purpose**: Provides a short, simple string input intended for testing the performance of slugification algorithms on minimal data.
*   **Parameters**: None (Property).
*   **Return Value**: A `string` containing a short text sample (e.g., a single word or short phrase).
*   **Throws**: None.

### `ToSlug_Complex`
*   **Purpose**: Provides a complex string input containing special characters, whitespace, and mixed casing to test the robustness and performance of slugification under non-ideal conditions.
*   **Parameters**: None (Property).
*   **Return Value**: A `string` containing text with symbols, accents, or irregular spacing.
*   **Throws**: None.

### `ToSlug_Long`
*   **Purpose**: Provides a lengthy string input to evaluate the scalability and memory allocation behavior of slugification routines when processing large payloads.
*   **Parameters**: None (Property).
*   **Return Value**: A `string` containing a significantly long sequence of characters.
*   **Throws**: None.

### `JsonEscape_Clean`
*   **Purpose**: Supplies a standard string devoid of special JSON characters to baseline the performance of JSON escaping logic when no transformations are theoretically required.
*   **Parameters**: None (Property).
*   **Return Value**: A `string` containing plain text safe for JSON inclusion without modification.
*   **Throws**: None.

### `JsonEscape_Special`
*   **Purpose**: Supplies a string containing characters that require escaping in JSON (e.g., quotes, backslashes, control characters) to test the overhead of escape sequence processing.
*   **Parameters**: None (Property).
*   **Return Value**: A `string` containing characters such as `"`, `\`, `\n`, or `\t`.
*   **Throws**: None.

### `Truncate_Needed`
*   **Purpose**: Provides a string that exceeds a defined maximum length threshold, used to benchmark the performance of truncation logic when the operation must actively shorten the content.
*   **Parameters**: None (Property).
*   **Return Value**: A `string` with a length greater than the standard truncation limit used in the associated tests.
*   **Throws**: None.

### `Truncate_NoOp`
*   **Purpose**: Provides a string that falls within the allowed length limits, used to verify the efficiency of truncation logic when no modification is necessary.
*   **Parameters**: None (Property).
*   **Return Value**: A `string` with a length less than or equal to the standard truncation limit.
*   **Throws**: None.

### `Mask_ApiKey`
*   **Purpose**: Provides a sample API key or sensitive token string used to benchmark the performance of masking or redaction algorithms designed to obscure sensitive information in logs or output.
*   **Parameters**: None (Property).
*   **Return Value**: A `string` representing a formatted API key or secret.
*   **Throws**: None.

## Usage

The `StringProcessingBenchmarks` class is typically instantiated by a benchmarking framework (such as BenchmarkDotNet) to supply consistent data to benchmark methods. The properties are accessed to retrieve the specific input data for a test scenario.

```csharp
using BenchmarkDotNet.Attributes;

[MemoryDiagnoser]
public class StringOperationsBenchmark
{
    private readonly StringProcessingBenchmarks _data;

    public StringOperationsBenchmark()
    {
        _data = new StringProcessingBenchmarks();
    }

    [Benchmark]
    public string Benchmark_SlugifyComplex()
    {
        // Uses the complex input string defined in the benchmarks class
        return SlugifyUtility.Convert(_data.ToSlug_Complex);
    }

    [Benchmark]
    public string Benchmark_MaskSensitiveData()
    {
        // Uses the sample API key to test masking performance
        return SecurityMasker.Mask(_data.Mask_ApiKey);
    }
}
```

In scenarios where validation of expected outputs is required alongside performance measurement, the properties can be used to assert correctness before timing the operation.

```csharp
public class ValidationTest
{
    public void ValidateTruncationLogic()
    {
        var benchmarks = new StringProcessingBenchmarks();
        var maxLength = 50;

        // Test case where truncation is required
        var longInput = benchmarks.Truncate_Needed;
        var resultNeeded = StringTruncator.Truncate(longInput, maxLength);
        
        // Test case where truncation is a no-op
        var shortInput = benchmarks.Truncate_NoOp;
        var resultNoOp = StringTruncator.Truncate(shortInput, maxLength);

        // Assertions would follow to ensure resultNeeded.Length <= maxLength
        // and resultNoOp equals shortInput
    }
}
```

## Notes

*   **Immutability**: As this class exposes data via `string` properties, the returned values are immutable. However, the class instance itself should be treated as stateful regarding its initialization; ensure the constructor has executed before accessing properties.
*   **Thread Safety**: The properties return standard .NET `string` instances. Reading these properties is thread-safe provided the `StringProcessingBenchmarks` instance is fully constructed before being shared across threads. No internal locking is required for read-only access to these members.
*   **Null Handling**: Based on the nature of benchmark data fixtures, these properties are expected to return valid string instances and should not return `null`. Consumers should not implement null-checking logic for these specific members unless the initialization logic is modified in future versions.
*   **Data Sensitivity**: While `Mask_ApiKey` contains a string resembling a secret, it is a dummy value intended for performance testing. It should not be used as an actual credential in production environments.
