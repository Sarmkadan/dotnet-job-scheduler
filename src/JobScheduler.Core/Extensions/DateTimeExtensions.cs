#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

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
    /// <param name="dateTime">The DateTime to check.</param>
    /// <returns><see langword="true"/> if the DateTime is earlier than the current UTC time; otherwise, <see langword="false"/>.</returns>
    public static bool IsInThePast(this DateTime dateTime) => dateTime < DateTime.UtcNow;

    /// <summary>
    /// Checks if a DateTime is in the future.
    /// </summary>
    /// <param name="dateTime">The DateTime to check.</param>
    /// <returns><see langword="true"/> if the DateTime is later than the current UTC time; otherwise, <see langword="false"/>.</returns>
    public static bool IsInTheFuture(this DateTime dateTime) => dateTime > DateTime.UtcNow;

    /// <summary>
    /// Gets the time remaining until a DateTime.
    /// </summary>
    /// <param name="dateTime">The target DateTime.</param>
    /// <returns>A <see cref="TimeSpan"/> representing the time remaining. If the DateTime is in the past, the result will be negative.</returns>
    public static TimeSpan TimeUntil(this DateTime dateTime) => dateTime - DateTime.UtcNow;

    /// <summary>
    /// Gets the time elapsed since a DateTime.
    /// </summary>
    /// <param name="dateTime">The starting DateTime.</param>
    /// <returns>A <see cref="TimeSpan"/> representing the elapsed time. If the DateTime is in the future, the result will be negative.</returns>
    public static TimeSpan TimeSince(this DateTime dateTime) => DateTime.UtcNow - dateTime;

    /// <summary>
    /// Checks if two DateTime values are on the same day (UTC).
    /// </summary>
    /// <param name="dateTime">The first DateTime.</param>
    /// <param name="other">The second DateTime to compare with.</param>
    /// <returns><see langword="true"/> if both DateTime values represent the same day; otherwise, <see langword="false"/>.</returns>
    public static bool IsSameDay(this DateTime dateTime, DateTime other) => dateTime.Date == other.Date;

    /// <summary>
    /// Rounds a DateTime to the nearest minute.
    /// </summary>
    /// <param name="dateTime">The DateTime to round.</param>
    /// <returns>A new DateTime with seconds and milliseconds set to zero.</returns>
    public static DateTime RoundToNearestMinute(this DateTime dateTime) =>
        new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, 0, dateTime.Kind);

    /// <summary>
    /// Rounds a DateTime to the nearest hour.
    /// </summary>
    /// <param name="dateTime">The DateTime to round.</param>
    /// <returns>A new DateTime with minutes, seconds, and milliseconds set to zero.</returns>
    public static DateTime RoundToNearestHour(this DateTime dateTime) =>
        new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, 0, 0, dateTime.Kind);

    /// <summary>
    /// Gets the start of the day (midnight).
    /// </summary>
    /// <param name="dateTime">The DateTime.</param>
    /// <returns>A DateTime representing midnight of the same day in the local time zone.</returns>
    public static DateTime StartOfDay(this DateTime dateTime) => dateTime.Date;

    /// <summary>
    /// Gets the end of the day (23:59:59).
    /// </summary>
    /// <param name="dateTime">The DateTime.</param>
    /// <returns>A DateTime representing 23:59:59 of the same day in the local time zone.</returns>
    public static DateTime EndOfDay(this DateTime dateTime) => dateTime.Date.AddDays(1).AddSeconds(-1);

    /// <summary>
    /// Gets the start of the week (Monday).
    /// </summary>
    /// <param name="dateTime">The DateTime.</param>
    /// <returns>A DateTime representing midnight of the Monday of the current week.</returns>
    public static DateTime StartOfWeek(this DateTime dateTime)
    {
        var daysToSubtract = (int)dateTime.DayOfWeek;
        return daysToSubtract == 0
            ? dateTime.AddDays(-6).StartOfDay()
            : dateTime.AddDays(-daysToSubtract + 1).StartOfDay();
    }

    /// <summary>
    /// Gets the start of the month.
    /// </summary>
    /// <param name="dateTime">The DateTime.</param>
    /// <returns>A DateTime representing midnight of the first day of the current month.</returns>
    public static DateTime StartOfMonth(this DateTime dateTime) =>
        new DateTime(dateTime.Year, dateTime.Month, 1);

    /// <summary>
    /// Gets the end of the month.
    /// </summary>
    /// <param name="dateTime">The DateTime.</param>
    /// <returns>A DateTime representing 23:59:59 of the last day of the current month.</returns>
    public static DateTime EndOfMonth(this DateTime dateTime)
    {
        var startOfNextMonth = dateTime.StartOfMonth().AddMonths(1);
        return startOfNextMonth.AddSeconds(-1);
    }
}