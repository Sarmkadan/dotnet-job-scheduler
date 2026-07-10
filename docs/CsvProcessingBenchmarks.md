# CsvProcessingBenchmarks
The `CsvProcessingBenchmarks` type provides a set of predefined CSV processing benchmarks, allowing developers to test and compare the performance of different CSV parsing and escaping strategies. These benchmarks cover various scenarios, including simple, quoted, and wide CSV lines, as well as escaping plain, comma-separated, and quoted fields.

## API
The following public members are available:
* `ParseCsvLine_Simple`: A list of strings representing a simple CSV line.
* `ParseCsvLine_Quoted`: A list of strings representing a quoted CSV line.
* `ParseCsvLine_Wide`: A list of strings representing a wide CSV line.
* `EscapeCsvField_Plain`: A string representing an escaped plain CSV field.
* `EscapeCsvField_Comma`: A string representing an escaped comma-separated CSV field.
* `EscapeCsvField_Quotes`: A string representing an escaped quoted CSV field.
* `ParsePriority_ByName`: An integer representing the parse priority by name.
* `ParsePriority_Default`: An integer representing the default parse priority.

## Usage
Here are two examples of using the `CsvProcessingBenchmarks` type:
```csharp
// Example 1: Using predefined CSV lines
var simpleLine = CsvProcessingBenchmarks.ParseCsvLine_Simple;
var quotedLine = CsvProcessingBenchmarks.ParseCsvLine_Quoted;
Console.WriteLine($"Simple line: {string.Join(", ", simpleLine)}");
Console.WriteLine($"Quoted line: {string.Join(", ", quotedLine)}");

// Example 2: Using escaped CSV fields
var plainField = CsvProcessingBenchmarks.EscapeCsvField_Plain;
var commaField = CsvProcessingBenchmarks.EscapeCsvField_Comma;
Console.WriteLine($"Escaped plain field: {plainField}");
Console.WriteLine($"Escaped comma field: {commaField}");
```

## Notes
When using the `CsvProcessingBenchmarks` type, note that the predefined CSV lines and escaped fields are static and do not throw exceptions. However, developers should be aware of potential edge cases, such as:
* Empty or null CSV lines and fields, which may require special handling.
* CSV lines and fields with special characters, such as newline characters or quotes, which may require additional escaping or parsing logic.
* Thread-safety considerations, as the `CsvProcessingBenchmarks` type is not designed to be thread-safe. If concurrent access is required, developers should implement appropriate synchronization mechanisms to ensure data integrity.
