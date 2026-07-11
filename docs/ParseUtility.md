# ParseUtility
The `ParseUtility` class provides a set of static methods for parsing and formatting various data types, including integers, longs, doubles, booleans, dates, times, GUIDs, enums, JSON, time spans, and CSV lines. These methods can be used to simplify the process of converting between different data types and formats, making it easier to work with data in a .NET application.

## API
* `public static int ParseInt`: Parses a string as an integer. Parameters: the string to parse. Return value: the parsed integer. Throws: `FormatException` if the string cannot be parsed as an integer.
* `public static long ParseLong`: Parses a string as a long integer. Parameters: the string to parse. Return value: the parsed long integer. Throws: `FormatException` if the string cannot be parsed as a long integer.
* `public static double ParseDouble`: Parses a string as a double-precision floating-point number. Parameters: the string to parse. Return value: the parsed double. Throws: `FormatException` if the string cannot be parsed as a double.
* `public static bool ParseBool`: Parses a string as a boolean value. Parameters: the string to parse. Return value: the parsed boolean value. Throws: `FormatException` if the string cannot be parsed as a boolean.
* `public static DateTime ParseDateTime`: Parses a string as a date and time. Parameters: the string to parse. Return value: the parsed date and time. Throws: `FormatException` if the string cannot be parsed as a date and time.
* `public static Guid ParseGuid`: Parses a string as a GUID. Parameters: the string to parse. Return value: the parsed GUID. Throws: `FormatException` if the string cannot be parsed as a GUID.
* `public static T ParseEnum<T>`: Parses a string as an enum value of type `T`. Parameters: the string to parse. Return value: the parsed enum value. Throws: `FormatException` if the string cannot be parsed as an enum value of type `T`.
* `public static T? ParseJson<T>`: Parses a JSON string as an object of type `T`. Parameters: the JSON string to parse. Return value: the parsed object, or `null` if the string is `null` or empty. Throws: `JsonException` if the string cannot be parsed as JSON.
* `public static TimeSpan ParseTimeSpan`: Parses a string as a time span. Parameters: the string to parse. Return value: the parsed time span. Throws: `FormatException` if the string cannot be parsed as a time span.
* `public static int ParsePriority`: Parses a string as a priority level. Parameters: the string to parse. Return value: the parsed priority level. Throws: `FormatException` if the string cannot be parsed as a priority level.
* `public static string FormatFileSize`: Formats a file size as a human-readable string. Parameters: the file size in bytes. Return value: the formatted string.
* `public static string FormatDuration`: Formats a duration as a human-readable string. Parameters: the duration as a time span. Return value: the formatted string.
* `public static string FormatPercentage`: Formats a percentage as a human-readable string. Parameters: the percentage as a double. Return value: the formatted string.
* `public static List<string> ParseCsvLine`: Parses a CSV line as a list of strings. Parameters: the CSV line to parse. Return value: the list of parsed strings.
* `public static string EscapeCsvField`: Escapes a CSV field as a string. Parameters: the CSV field to escape. Return value: the escaped string.

## Usage
```csharp
// Example 1: Parsing a JSON string as an object
string json = "{\"name\":\"John\",\"age\":30}";
Person person = ParseUtility.ParseJson<Person>(json);
Console.WriteLine(person.Name); // Output: John
Console.WriteLine(person.Age); // Output: 30

// Example 2: Formatting a file size as a human-readable string
long fileSize = 1024 * 1024 * 1024; // 1 GB
string formattedSize = ParseUtility.FormatFileSize(fileSize);
Console.WriteLine(formattedSize); // Output: 1 GB
```

## Notes
The `ParseUtility` class is designed to be thread-safe, as all methods are static and do not rely on any shared state. However, the `ParseJson` method uses the `System.Text.Json` namespace, which may not be thread-safe in all scenarios. When using `ParseJson` in a multithreaded environment, ensure that the JSON string being parsed is not modified concurrently. Additionally, the `ParseEnum` method assumes that the enum type `T` has a `ToString` method that can be used to convert the enum value to a string. If this is not the case, the `ParseEnum` method may not work as expected. The `FormatFileSize`, `FormatDuration`, and `FormatPercentage` methods use locale-specific formatting, so the output may vary depending on the current culture. The `ParseCsvLine` and `EscapeCsvField` methods assume that the CSV line or field is properly formatted according to the CSV specification (RFC 4180). If the input is not properly formatted, the methods may not work as expected.
