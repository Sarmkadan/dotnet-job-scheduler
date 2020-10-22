# CronExpressionService

The `CronExpressionService` provides core functionality for parsing, validating, and evaluating Cron expressions within the `dotnet-job-scheduler` framework. It serves as the primary utility for determining job execution schedules, calculating future run times across different time zones, and generating human-readable descriptions of complex scheduling patterns.

## API

### `IsValidCronExpression`
Validates whether a provided string conforms to a standard Cron expression format.
*   **Parameters**: None (operates on the current instance context or input string depending on implementation signature; assumes input is provided via constructor or method argument in typical usage).
*   **Return Value**: `bool` – Returns `true` if the expression is syntactically correct; otherwise `false`.
*   **Exceptions**: Does not throw exceptions for invalid formats; returns `false` instead.

### `ParseCronExpression`
Parses a string representation of a Cron expression into a strongly-typed `CrontabSchedule` object.
*   **Parameters**: `string expression` – The Cron string to parse.
*   **Return Value**: `CrontabSchedule` – The parsed schedule object used for subsequent calculations.
*   **Exceptions**: Throws `FormatException` or a specific parsing exception if the input string is invalid.

### `GetNextExecutionTime`
Calculates the immediate next occurrence of the schedule based on the current system time or a specified base time.
*   **Parameters**: `DateTime baseTime` (optional) – The reference time from which to calculate the next occurrence. Defaults to `DateTime.Now` if omitted.
*   **Return Value**: `DateTime` – The calculated next execution time in the local system time zone.
*   **Exceptions**: May throw if the schedule cannot resolve a future time within a reasonable threshold (e.g., malformed internal state).

### `GetNextExecutionTimeInZone`
Calculates the next execution time adjusted for a specific time zone, ensuring accuracy for jobs targeting regions different from the server's local time.
*   **Parameters**: 
    *   `DateTime baseTime` – The reference time.
    *   `TimeZoneInfo timeZone` – The target time zone for the calculation.
*   **Return Value**: `DateTime` – The next execution time converted to the specified time zone.
*   **Exceptions**: Throws `ArgumentNullException` if `timeZone` is null; throws `TimeZoneNotFoundException` if the provided time zone ID is invalid.

### `GetNextExecutionTimes`
Generates a sequence of upcoming execution times for forecasting or preview purposes.
*   **Parameters**: 
    *   `DateTime baseTime` – The starting reference time.
    *   `int count` – The number of future occurrences to retrieve.
*   **Return Value**: `IEnumerable<DateTime>` – An enumerable collection of the next `count` execution times.
*   **Exceptions**: Throws `ArgumentOutOfRangeException` if `count` is less than or equal to zero.

### `ShouldExecuteAt`
Determines if a job should trigger immediately based on a specific timestamp.
*   **Parameters**: `DateTime checkTime` – The specific timestamp to evaluate against the schedule.
*   **Return Value**: `bool` – Returns `true` if the `checkTime` matches a scheduled execution moment; otherwise `false`.
*   **Exceptions**: None typically expected for valid date inputs.

### `GetCronDescription`
Generates a human-readable string describing the frequency and timing of the Cron expression.
*   **Parameters**: None (uses the currently loaded expression).
*   **Return Value**: `string` – A descriptive sentence (e.g., "Every day at 14:00").
*   **Exceptions**: Throws `InvalidOperationException` if called before a valid expression has been parsed or loaded.

## Usage

### Example 1: Validating and Calculating Next Run Time
This example demonstrates validating a user-provided string and calculating the next run time in a specific time zone.

```csharp
using System;
using DotNetJobScheduler;

public class SchedulerSetup
{
    public void ConfigureJob()
    {
        var service = new CronExpressionService();
        string userExpression = "0 0/5 * * * ?"; // Every 5 minutes

        if (service.IsValidCronExpression(userExpression))
        {
            var schedule = service.ParseCronExpression(userExpression);
            
            // Calculate next run in Eastern Standard Time
            var estZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            DateTime nextRun = service.GetNextExecutionTimeInZone(DateTime.UtcNow, estZone);
            
            Console.WriteLine($"Next execution: {nextRun}");
        }
        else
        {
            Console.WriteLine("Invalid Cron expression provided.");
        }
    }
}
```

### Example 2: Forecasting and Description
This example retrieves the next five execution times and generates a description for logging or UI display.

```csharp
using System;
using System.Linq;
using DotNetJobScheduler;

public class JobPreview
{
    public void ShowSchedule()
    {
        var service = new CronExpressionService();
        service.ParseCronExpression("0 15 10 ? * MON-FRI");

        string description = service.GetCronDescription();
        Console.WriteLine($"Schedule: {description}");

        var upcomingRuns = service.GetNextExecutionTimes(DateTime.Now, 5);
        
        Console.WriteLine("Upcoming 5 runs:");
        foreach (var run in upcomingRuns)
        {
            Console.WriteLine($"- {run:yyyy-MM-dd HH:mm:ss}");
        }
    }
}
```

## Notes

*   **Thread Safety**: The `CronExpressionService` instance methods are not guaranteed to be thread-safe when mutating internal state (such as parsing a new expression). It is recommended to treat instances as immutable after initialization or to synchronize access if parsing new expressions concurrently on the same instance. Read-only operations like `GetNextExecutionTime` are generally safe for concurrent access once the expression is parsed.
*   **Time Zone Handling**: When using `GetNextExecutionTimeInZone`, ensure the `TimeZoneInfo` object is valid and handles Daylight Saving Time transitions correctly. The method performs conversions relative to the provided `baseTime`, so mixing `DateTimeKind.Utc` and `DateTimeKind.Local` without explicit care may lead to off-by-one-hour errors during DST shifts.
*   **Edge Cases**: 
    *   Expressions containing non-standard characters or incorrect field counts will cause `ParseCronExpression` to throw; always use `IsValidCronExpression` as a pre-check if handling untrusted input.
    *   `GetNextExecutionTimes` with a large `count` value on complex intervals (e.g., "Last Friday of the month") may incur higher computational costs.
    *   `ShouldExecuteAt` performs exact second matching; ensure the calling logic accounts for potential execution delays or drift if used for strict triggering logic.
