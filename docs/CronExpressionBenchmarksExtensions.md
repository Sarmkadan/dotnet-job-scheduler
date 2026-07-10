# CronExpressionBenchmarksExtensions

The `CronExpressionBenchmarksExtensions` static class provides a suite of benchmarking-focused extension methods designed to evaluate the performance and correctness of various cron expression processing operations. These methods facilitate the testing of cron parsing, next execution time calculation, execution validation, and description generation under different scheduling scenarios, ensuring that cron expression handling components maintain efficiency and reliability within the `dotnet-job-scheduler` ecosystem.

## API

*   **`bool IsValidCronExpression_Complex(string cronExpression)`**: Validates a complex cron expression for structural correctness and compliance with expected scheduling rules. Returns `true` if valid, `false` otherwise.
*   **`bool IsValidCronExpression_Invalid(string cronExpression)`**: Attempts validation of an intentionally invalid or malformed cron expression to verify that validation logic correctly identifies and rejects it. Returns `true` if the expression is correctly identified as invalid.
*   **`DateTime GetNextExecutionTime_Complex(string cronExpression, DateTime fromTime)`**: Calculates the next `DateTime` execution point based on a provided complex cron expression, relative to the specified `fromTime`.
*   **`DateTime GetNextExecutionTime_Hourly(string cronExpression, DateTime fromTime)`**: Calculates the next `DateTime` execution point for a standard hourly cron schedule, relative to the specified `fromTime`.
*   **`DateTime GetNextExecutionTime_Monthly(string cronExpression, DateTime fromTime)`**: Calculates the next `DateTime` execution point for a standard monthly cron schedule, relative to the specified `fromTime`.
*   **`IEnumerable<DateTime> GetNextExecutionTimes_100(string cronExpression, DateTime fromTime)`**: Retrieves a collection of the next 100 valid `DateTime` execution times for the provided cron expression, starting from `fromTime`.
*   **`IEnumerable<DateTime> GetNextExecutionTimes_1000(string cronExpression, DateTime fromTime)`**: Retrieves a collection of the next 1000 valid `DateTime` execution times for the provided cron expression, starting from `fromTime`.
*   **`bool ShouldExecuteAt_Hit(string cronExpression, DateTime targetTime)`**: Validates that an execution signal is correctly triggered (a "hit") at a specific, expected time for the given cron expression.
*   **`bool ShouldExecuteAt_Future(string cronExpression, DateTime targetTime)`**: Validates that an execution signal is not incorrectly triggered for a future, non-matching time for the given cron expression.
*   **`string GetCronDescription_Complex(string cronExpression)`**: Generates a human-readable string description for a complex cron expression.
*   **`string GetCronDescription_WithSeconds(string cronExpression)`**: Generates a human-readable string description for a cron expression that specifically includes second-level precision.

## Usage

```csharp
// Example 1: Measuring performance of execution time calculation
var cron = "0 0 * * * *"; // Every hour at the top of the hour
var startTime = DateTime.UtcNow;

// Validate next 100 occurrences for benchmark purposes
var nextExecutions = CronExpressionBenchmarksExtensions.GetNextExecutionTimes_100(cron, startTime);

foreach (var time in nextExecutions)
{
    Console.WriteLine($"Next run scheduled at: {time}");
}
```

```csharp
// Example 2: Verifying complex cron expression parsing
var complexCron = "0 0/5 14 * * ? *";

if (CronExpressionBenchmarksExtensions.IsValidCronExpression_Complex(complexCron))
{
    var description = CronExpressionBenchmarksExtensions.GetCronDescription_Complex(complexCron);
    Console.WriteLine($"Expression '{complexCron}' parsed: {description}");
}
else
{
    Console.Error.WriteLine("Failed to parse complex expression.");
}
```

## Notes

*   **Thread Safety**: These extension methods are static and perform read-only operations on their input parameters. They are thread-safe, assuming that the provided `cronExpression` strings and `DateTime` instances are not modified concurrently by other threads during execution.
*   **Performance Considerations**: Methods calculating large sets of execution times (`GetNextExecutionTimes_100`, `GetNextExecutionTimes_1000`) should be used cautiously in high-frequency paths, as they may incur significant memory allocation and computation costs depending on the complexity of the cron expression.
*   **Exception Handling**: While some methods are designed to test invalid inputs (e.g., `IsValidCronExpression_Invalid`), other methods may throw exceptions if provided with syntactically malformed cron expressions that violate the underlying cron parser's expectations. Callers should handle potential parsing exceptions appropriately.
