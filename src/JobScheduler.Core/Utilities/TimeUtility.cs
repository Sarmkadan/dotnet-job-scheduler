#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace JobScheduler.Core.Utilities;

/// <summary>
/// Utility methods for time and date operations.
/// Provides consistent timezone handling and time conversions across the scheduler.
/// WHY: Centralized time utilities prevent timezone bugs and inconsistent conversions.
/// </summary>
public static class TimeUtility
{
    /// <summary>
    /// Gets current UTC time (preferred over DateTime.Now for consistency).
    /// </summary>
    public static DateTime GetUtcNow()
    {
        return DateTime.UtcNow;
    }

    /// <summary>
    /// Converts Unix timestamp to DateTime.
    /// </summary>
    public static DateTime FromUnixTimestamp(long unixTimestamp)
    {
        return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(unixTimestamp);
    }

    /// <summary>
    /// Converts DateTime to Unix timestamp.
    /// </summary>
    public static long ToUnixTimestamp(DateTime dateTime)
    {
        return (long)(dateTime.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
    }

    /// <summary>
    /// Converts DateTime to ISO 8601 string format.
    /// </summary>
    public static string ToIso8601(DateTime dateTime)
    {
        return dateTime.ToUniversalTime().ToString("o");
    }

    /// <summary>
    /// Parses ISO 8601 string to DateTime.
    /// </summary>
    public static DateTime? ParseIso8601(string? isoString)
    {
        if (string.IsNullOrEmpty(isoString))
            return null;

        if (DateTime.TryParse(isoString, out var result))
            return result.ToUniversalTime();

        return null;
    }

    /// <summary>
    /// Rounds time down to nearest interval.
    /// Example: 14:37 rounded down to 15-minute interval = 14:30.
    /// </summary>
    public static DateTime RoundDown(DateTime dateTime, TimeSpan interval)
    {
        return new DateTime((dateTime.Ticks / interval.Ticks) * interval.Ticks);
    }

    /// <summary>
    /// Rounds time up to nearest interval.
    /// </summary>
    public static DateTime RoundUp(DateTime dateTime, TimeSpan interval)
    {
        return new DateTime(((dateTime.Ticks + interval.Ticks - 1) / interval.Ticks) * interval.Ticks);
    }

    /// <summary>
    /// Gets age in specified unit.
    /// </summary>
    public static int GetAge(DateTime birthDate, DateTime? referenceDate = null)
    {
        var reference = referenceDate ?? DateTime.UtcNow;
        var age = reference.Year - birthDate.Year;

        if (birthDate > reference.AddYears(-age))
            age--;

        return age;
    }

    /// <summary>
    /// Checks if a time falls within a time range on a given day.
    /// </summary>
    public static bool IsBetweenTimes(DateTime time, TimeSpan startTime, TimeSpan endTime)
    {
        var timeOfDay = time.TimeOfDay;
        return timeOfDay >= startTime && timeOfDay <= endTime;
    }

    /// <summary>
    /// Checks if a date falls within a date range.
    /// </summary>
    public static bool IsBetweenDates(DateTime date, DateTime startDate, DateTime endDate)
    {
        return date.Date >= startDate.Date && date.Date <= endDate.Date;
    }

    /// <summary>
    /// Gets the number of business days between two dates.
    /// </summary>
    public static int GetBusinessDaysBetween(DateTime startDate, DateTime endDate)
    {
        var count = 0;
        var current = startDate;

        while (current <= endDate)
        {
            if (current.DayOfWeek != DayOfWeek.Saturday && current.DayOfWeek != DayOfWeek.Sunday)
                count++;

            current = current.AddDays(1);
        }

        return count;
    }

    /// <summary>
    /// Gets the next business day.
    /// </summary>
    public static DateTime GetNextBusinessDay(DateTime date)
    {
        var next = date.AddDays(1);
        while (next.DayOfWeek == DayOfWeek.Saturday || next.DayOfWeek == DayOfWeek.Sunday)
            next = next.AddDays(1);

        return next;
    }

    /// <summary>
    /// Gets the previous business day.
    /// </summary>
    public static DateTime GetPreviousBusinessDay(DateTime date)
    {
        var previous = date.AddDays(-1);
        while (previous.DayOfWeek == DayOfWeek.Saturday || previous.DayOfWeek == DayOfWeek.Sunday)
            previous = previous.AddDays(-1);

        return previous;
    }

    /// <summary>
    /// Gets the start of the week (Monday).
    /// </summary>
    public static DateTime GetStartOfWeek(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-1 * diff).Date;
    }

    /// <summary>
    /// Gets the end of the week (Sunday).
    /// </summary>
    public static DateTime GetEndOfWeek(DateTime date)
    {
        return GetStartOfWeek(date).AddDays(6).AddHours(23).AddMinutes(59).AddSeconds(59);
    }

    /// <summary>
    /// Gets the start of the month.
    /// </summary>
    public static DateTime GetStartOfMonth(DateTime date)
    {
        return new DateTime(date.Year, date.Month, 1);
    }

    /// <summary>
    /// Gets the end of the month.
    /// </summary>
    public static DateTime GetEndOfMonth(DateTime date)
    {
        return GetStartOfMonth(date).AddMonths(1).AddSeconds(-1);
    }

    /// <summary>
    /// Formats duration in human-readable format.
    /// Example: 90 seconds = "1 minute 30 seconds"
    /// </summary>
    public static string FormatDuration(TimeSpan duration)
    {
        var parts = new List<string>();

        if (duration.Days > 0)
            parts.Add($"{duration.Days} day{(duration.Days > 1 ? "s" : "")}");

        if (duration.Hours > 0)
            parts.Add($"{duration.Hours} hour{(duration.Hours > 1 ? "s" : "")}");

        if (duration.Minutes > 0)
            parts.Add($"{duration.Minutes} minute{(duration.Minutes > 1 ? "s" : "")}");

        if (duration.Seconds > 0 && duration.Days == 0 && duration.Hours == 0)
            parts.Add($"{duration.Seconds} second{(duration.Seconds > 1 ? "s" : "")}");

        return string.Join(", ", parts.Count > 0 ? parts : new List<string> { "0 seconds" });
    }

    /// <summary>
    /// Checks if a year is a leap year.
    /// </summary>
    public static bool IsLeapYear(int year)
    {
        return (year % 4 == 0 && year % 100 != 0) || (year % 400 == 0);
    }
}
