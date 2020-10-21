# ScheduleService

The `ScheduleService` class provides a centralized API for querying and analyzing job schedules in the `dotnet-job-scheduler` system. It exposes asynchronous methods to retrieve upcoming execution times, compute frequency and distribution statistics, describe cron expressions in human-readable form, and obtain the next jobs that are due to run. The service is designed to work with an internal collection of scheduled jobs and their associated cron expressions.

## API

### `public ScheduleService()`

Initializes a new instance of the `ScheduleService`. The service is ready to accept jobs and schedule definitions after construction.

### `public async Task<List<DateTime>> GetUpcomingExecutionTimesAsync()`

Returns a list of the next upcoming execution times for all registered schedules, computed from the current system time. The list is sorted in ascending order.

- **Returns**: A `List<DateTime>` containing the upcoming execution timestamps.
- **Throws**: `InvalidOperationException` if no schedules have been registered.

### `public async Task<double> GetExecutionFrequencyPerDayAsync()`

Calculates the average number of executions per day across all registered schedules.

- **Returns**: A `double` representing the average daily execution count.
- **Throws**: `InvalidOperationException` if no schedules are registered.

### `public async Task<string> GetCronExpressionDescriptionAsync()`

Generates a human-readable description of the cron expression associated with the default schedule. If multiple schedules exist, the description corresponds to the first registered schedule.

- **Returns**: A `string` describing the cron expression in plain English (e.g., "At 08:00 AM, Monday through Friday").
- **Throws**: `InvalidOperationException` if no schedule is registered.

### `public async Task<int> EstimateExecutionCountAsync()`

Estimates the total number of executions that will occur within the next 24 hours based on all registered schedules.

- **Returns**: An `int` representing the estimated execution count.
- **Throws**: `InvalidOperationException` if no schedules are registered.

### `public async Task<List<Job>> GetNextScheduledJobsAsync()`

Retrieves the next set of jobs that are scheduled to execute, based on the current time and the registered schedules. Each `Job` object contains metadata such as its identifier, cron expression, and next run time.

- **Returns**: A `List<Job>` of jobs that are due to run next.
- **Throws**: `InvalidOperationException` if no jobs are registered.

### `public async Task<Dictionary<int, int>> GetScheduleDistributionByHourAsync()`

Computes the distribution of scheduled executions across the 24 hours of the day. The returned dictionary maps each hour (0–23) to the number of executions that fall within that hour.

- **Returns**: A `Dictionary<int, int>` where keys are hours and values are execution counts.
- **Throws**: `InvalidOperationException` if no schedules are registered.

### `public async Task<List<Job>> GetJobsExecutingInNextMinutesAsync()`

Returns a list of jobs that are scheduled to execute within the next 60 minutes from the current time.

- **Returns**: A `List<Job>` of jobs that will execute in the next hour.
- **Throws**: `InvalidOperationException` if no jobs are registered.

## Usage

The following examples demonstrate typical usage of `ScheduleService`. In both examples, it is assumed that jobs and schedules have been registered with the service prior to calling these methods.

### Example 1: Retrieve upcoming execution times and describe the schedule

```csharp
using System;
using System.Threading.Tasks;
using DotNetJobScheduler;

public class ScheduleReporter
{
    private readonly ScheduleService _scheduleService;

    public ScheduleReporter(ScheduleService scheduleService)
    {
        _scheduleService = scheduleService;
    }

    public async Task ReportAsync()
    {
        // Get the next 10 execution times
        var upcomingTimes = await _scheduleService.GetUpcomingExecutionTimesAsync();
        Console.WriteLine("Upcoming execution times:");
        foreach (var time in upcomingTimes)
        {
            Console.WriteLine(time.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        // Describe the cron expression
        string description = await _scheduleService.GetCronExpressionDescriptionAsync();
        Console.WriteLine($"Schedule description: {description}");
    }
}
```

### Example 2: Analyze execution frequency and distribution

```csharp
using System;
using System.Threading.Tasks;
using DotNetJobScheduler;

public class ScheduleAnalyzer
{
    private readonly ScheduleService _scheduleService;

    public ScheduleAnalyzer(ScheduleService scheduleService)
    {
        _scheduleService = scheduleService;
    }

    public async Task AnalyzeAsync()
    {
        double freqPerDay = await _scheduleService.GetExecutionFrequencyPerDayAsync();
        Console.WriteLine($"Average executions per day: {freqPerDay:F2}");

        var distribution = await _scheduleService.GetScheduleDistributionByHourAsync();
        Console.WriteLine("Execution distribution by hour:");
        foreach (var kvp in distribution)
        {
            Console.WriteLine($"Hour {kvp.Key:D2}: {kvp.Value} executions");
        }

        int estimatedCount = await _scheduleService.EstimateExecutionCountAsync();
        Console.WriteLine($"Estimated executions in next 24 hours: {estimatedCount}");
    }
}
```

## Notes

- **Thread safety**: All public methods are asynchronous and do not modify the internal state of the service. The service is safe for concurrent read operations from multiple threads, provided that no writes (e.g., adding or removing schedules) occur simultaneously. If the service is used in a multi-threaded environment where schedules are modified concurrently, external synchronization is required.
- **Empty schedules**: Every method throws `InvalidOperationException` if no schedules or jobs have been registered. Callers should check for the presence of schedules before invoking these methods, or handle the exception appropriately.
- **Time zone**: All returned `DateTime` values are in the local time zone of the system where the service is running. The service does not perform time zone conversions.
- **Precision**: The methods `GetUpcomingExecutionTimesAsync` and `GetJobsExecutingInNextMinutesAsync` compute times based on the system clock at the moment of invocation. Delays in asynchronous execution may cause slight inaccuracies; these methods are intended for informational purposes, not for real-time scheduling decisions.
- **Cron expression description**: The `GetCronExpressionDescriptionAsync` method returns a description only for the first registered schedule. If multiple schedules exist, use the individual job’s description property instead.
