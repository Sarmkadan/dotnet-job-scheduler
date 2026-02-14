// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;

namespace JobScheduler.Core.Extensions;

/// <summary>
/// Extension methods for DateTime operations related to job scheduling.
/// </summary>
public static class DateTimeExtensions
{
    /// <summary>
    /// Checks if a DateTime is in the past.
    /// </summary>
    public static bool IsInThePast(this DateTime dateTime)
    {
        return dateTime < DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if a DateTime is in the future.
    /// </summary>
    public static bool IsInTheFuture(this DateTime dateTime)
    {
        return dateTime > DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the time remaining until a DateTime.
    /// </summary>
    public static TimeSpan TimeUntil(this DateTime dateTime)
    {
        return dateTime - DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the time elapsed since a DateTime.
    /// </summary>
    public static TimeSpan TimeSince(this DateTime dateTime)
    {
        return DateTime.UtcNow - dateTime;
    }

    /// <summary>
    /// Checks if two DateTime values are on the same day (UTC).
    /// </summary>
    public static bool IsSameDay(this DateTime dateTime, DateTime other)
    {
        return dateTime.Date == other.Date;
    }

    /// <summary>
    /// Rounds a DateTime to the nearest minute.
    /// </summary>
    public static DateTime RoundToNearestMinute(this DateTime dateTime)
    {
        return new DateTime(
            dateTime.Year,
            dateTime.Month,
            dateTime.Day,
            dateTime.Hour,
            dateTime.Minute,
            0,
            dateTime.Kind);
    }

    /// <summary>
    /// Rounds a DateTime to the nearest hour.
    /// </summary>
    public static DateTime RoundToNearestHour(this DateTime dateTime)
    {
        return new DateTime(
            dateTime.Year,
            dateTime.Month,
            dateTime.Day,
            dateTime.Hour,
            0,
            0,
            dateTime.Kind);
    }

    /// <summary>
    /// Gets the start of the day (midnight).
    /// </summary>
    public static DateTime StartOfDay(this DateTime dateTime)
    {
        return dateTime.Date;
    }

    /// <summary>
    /// Gets the end of the day (23:59:59).
    /// </summary>
    public static DateTime EndOfDay(this DateTime dateTime)
    {
        return dateTime.Date.AddDays(1).AddSeconds(-1);
    }

    /// <summary>
    /// Gets the start of the week (Monday).
    /// </summary>
    public static DateTime StartOfWeek(this DateTime dateTime)
    {
        var daysToSubtract = (int)dateTime.DayOfWeek;
        if (daysToSubtract == 0)
            daysToSubtract = 7;

        return dateTime.AddDays(-daysToSubtract + 1).StartOfDay();
    }

    /// <summary>
    /// Gets the start of the month.
    /// </summary>
    public static DateTime StartOfMonth(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, 1);
    }

    /// <summary>
    /// Gets the end of the month.
    /// </summary>
    public static DateTime EndOfMonth(this DateTime dateTime)
    {
        var startOfNextMonth = dateTime.StartOfMonth().AddMonths(1);
        return startOfNextMonth.AddSeconds(-1);
    }
}
