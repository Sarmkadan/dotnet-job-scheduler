#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using NCrontab;
using JobScheduler.Core.Exceptions;

namespace JobScheduler.Core.Services;

/// <summary>
/// Service for parsing, validating, and evaluating cron expressions.
/// Uses NCronTab library for POSIX-compliant cron parsing.
/// </summary>
public class CronExpressionService
{
    private static readonly ConcurrentDictionary<string, CrontabSchedule> _scheduleCache =
        new(StringComparer.Ordinal);

    private readonly ILogger<CronExpressionService>? _logger;

    /// <summary>
    /// Validates a cron expression syntax.
    /// </summary>
    public virtual bool IsValidCronExpression(string cronExpression)
    {
        if (string.IsNullOrWhiteSpace(cronExpression))
        {
            _logger?.LogDebug("Cron expression validation failed: expression is null or whitespace");
            return false;
        }

        try
        {
            ParseCronExpression(cronExpression);
            _logger?.LogDebug("Cron expression '{Expression}' validated successfully", cronExpression);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Cron expression '{Expression}' validation failed", cronExpression);
            return false;
        }
    }

    /// <summary>
    /// Parses a cron expression and throws if invalid.
    /// Result is cached so repeated calls for the same expression are allocation-free.
    /// </summary>
    public virtual CrontabSchedule ParseCronExpression(string cronExpression)
    {
        if (string.IsNullOrWhiteSpace(cronExpression))
        {
            _logger?.LogError("Cron expression parsing failed: expression is null or whitespace");
            throw new CronExpressionException(cronExpression, "Expression cannot be null or empty");
        }

        if (_scheduleCache.TryGetValue(cronExpression, out var cached))
        {
            _logger?.LogDebug("Cron expression '{Expression}' retrieved from cache", cronExpression);
            return cached;
        }

        CrontabSchedule parsed;
        try
        {
            parsed = CrontabSchedule.Parse(cronExpression);
            _logger?.LogDebug("Cron expression '{Expression}' parsed successfully", cronExpression);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Cron expression '{Expression}' parsing failed", cronExpression);
            throw new CronExpressionException(cronExpression, "Failed to parse expression", ex);
        }

        // GetOrAdd(key, value) is thread-safe: returns the winner if two threads race.
        return _scheduleCache.GetOrAdd(cronExpression, parsed);
    }

    /// <summary>
    /// Calculates the next execution time based on cron expression.
    /// Handles leap-year-specific expressions such as "0 0 29 2 *" (Feb 29) by
    /// advancing year-by-year until a valid date is found.
    /// </summary>
    public virtual DateTime GetNextExecutionTime(string cronExpression, DateTime? baseTime = null)
    {
        var schedule = ParseCronExpression(cronExpression);
        var reference = baseTime ?? DateTime.UtcNow;

        // Try up to 8 years ahead to accommodate leap-year expressions (Feb 29 repeats
        // every 4 years at most, 8 iterations is a safe upper bound).
        for (int attempt = 0; attempt < 8; attempt++)
        {
            DateTime next;
            try
            {
                next = schedule.GetNextOccurrence(reference);
            }
            catch (ArgumentOutOfRangeException)
            {
                // NCrontab tried to construct an invalid date (e.g. Feb 29 on a non-leap year).
                // Advance to the beginning of the next year and retry.
                reference = new DateTime(reference.Year + 1, 1, 1, 0, 0, 0, reference.Kind);
                continue;
            }

            if (next == DateTime.MaxValue)
                break;

            return next;
        }

        throw new CronExpressionException(cronExpression,
            "Could not calculate next occurrence. The expression may target a date that never exists " +
            "(e.g. Feb 29 with no upcoming leap year in range).");
    }

    /// <summary>
    /// Calculates the next execution time in the specified IANA or Windows timezone.
    /// The cron expression is evaluated as if the clock were in that timezone, so
    /// a schedule of "0 9 * * *" with timezone "America/New_York" fires at 09:00 EST/EDT
    /// regardless of DST transitions.  The returned <see cref="DateTime"/> is UTC.
    /// </summary>
    /// <param name="cronExpression">Standard five-field cron expression.</param>
    /// <param name="timezoneId">
    /// IANA (e.g. "America/New_York") or Windows (e.g. "Eastern Standard Time") timezone ID.
    /// </param>
    /// <param name="baseTimeUtc">
    /// Reference point in UTC.  Defaults to <see cref="DateTime.UtcNow"/>.
    /// </param>
    public virtual DateTime GetNextExecutionTimeInZone(string cronExpression, string timezoneId, DateTime? baseTimeUtc = null)
    {
        if (string.IsNullOrWhiteSpace(timezoneId))
            throw new ArgumentException("Timezone ID cannot be null or empty.", nameof(timezoneId));

        TimeZoneInfo tz;
        try
        {
            tz = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
        }
        catch (TimeZoneNotFoundException ex)
        {
            throw new ArgumentException($"Unknown timezone '{timezoneId}'.", nameof(timezoneId), ex);
        }

        var schedule = ParseCronExpression(cronExpression);
        var referenceUtc = DateTime.SpecifyKind(baseTimeUtc ?? DateTime.UtcNow, DateTimeKind.Utc);

        // Convert the UTC reference to local wall-clock time so NCrontab evaluates
        // the expression in the target timezone.
        var referenceLocal = TimeZoneInfo.ConvertTimeFromUtc(referenceUtc, tz);

        for (int attempt = 0; attempt < 8; attempt++)
        {
            DateTime nextLocal;
            try
            {
                nextLocal = schedule.GetNextOccurrence(referenceLocal);
            }
            catch (ArgumentOutOfRangeException)
            {
                referenceLocal = new DateTime(referenceLocal.Year + 1, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
                continue;
            }

            if (nextLocal == DateTime.MaxValue)
                break;

            // Convert the local occurrence back to UTC.  If the local time falls in a
            // DST gap (a time that does not exist on the wall clock) advance by one
            // minute and retry so we always land in a valid instant.
            var nextLocalUnspecified = DateTime.SpecifyKind(nextLocal, DateTimeKind.Unspecified);
            if (tz.IsInvalidTime(nextLocalUnspecified))
            {
                referenceLocal = nextLocal.AddMinutes(1);
                continue;
            }

            return TimeZoneInfo.ConvertTimeToUtc(nextLocalUnspecified, tz);
        }

        throw new CronExpressionException(cronExpression,
            $"Could not calculate next occurrence in timezone '{timezoneId}'. " +
            "The expression may target a date that never exists (e.g. Feb 29 with no upcoming leap year in range).");
    }

    /// <summary>
    /// Calculates the next N execution times.
    /// </summary>
    public virtual IEnumerable<DateTime> GetNextExecutionTimes(string cronExpression, int count, DateTime? baseTime = null)
    {
        if (count <= 0)
            throw new ArgumentException("Count must be positive", nameof(count));

        var schedule = ParseCronExpression(cronExpression);
        var reference = baseTime ?? DateTime.UtcNow;
        var times = new List<DateTime>();

        for (int i = 0; i < count; i++)
        {
            reference = schedule.GetNextOccurrence(reference);
            if (reference == DateTime.MaxValue)
                break;

            times.Add(reference);
        }

        return times;
    }

    /// <summary>
    /// Checks if a job should execute at the given time based on its cron expression.
    /// </summary>
    public virtual bool ShouldExecuteAt(string cronExpression, DateTime checkTime)
    {
        try
        {
            var schedule = ParseCronExpression(cronExpression);
            _logger?.LogDebug("Checking if cron expression '{Expression}' should execute at {CheckTime}", cronExpression, checkTime);

            // NCrontab only exposes forward occurrence lookup, so the previous
            // occurrence relative to checkTime is found by scanning forward from
            // a day earlier until the next occurrence would land after checkTime.
            DateTime? previous = null;
            var cursor = checkTime.AddDays(-1);

            while (cursor < checkTime)
            {
                var next = schedule.GetNextOccurrence(cursor);
                if (next == DateTime.MaxValue || next > checkTime)
                    break;

                previous = next;
                cursor = next;
            }

            if (previous is null)
            {
                _logger?.LogDebug("No matching execution time found for cron '{Expression}' at {CheckTime}", cronExpression, checkTime);
                return false;
            }

            var difference = (checkTime - previous.Value).TotalSeconds;

            // Consider it a match if within 60 seconds (accounting for scheduling delays)
            var shouldExecute = difference >= 0 && difference < 60;
            _logger?.LogDebug("Cron '{Expression}' at {CheckTime}: difference={Difference}s, shouldExecute={ShouldExecute}",
                cronExpression, checkTime, difference, shouldExecute);
            return shouldExecute;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error checking if cron expression '{Expression}' should execute at {CheckTime}", cronExpression, checkTime);
            return false;
        }
    }

    /// <summary>
    /// Gets a human-readable description of a cron expression, for example
    /// "0 9 * * 1-5" becomes "At 09:00, Monday through Friday".
    /// Supports the 5 field (minute hour day month day-of-week) and the 6 field
    /// (second minute hour day month day-of-week) layouts.
    /// </summary>
    /// <returns>The description, or "Invalid cron expression" when the expression cannot be parsed.</returns>
    public virtual string GetCronDescription(string cronExpression)
    {
        try
        {
            // Parsing first guarantees the description never describes an expression the scheduler
            // would reject at run time.
            _ = ParseCronExpression(cronExpression);

            var parts = cronExpression.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            return parts.Length switch
            {
                5 => BuildDescription(second: null, minute: parts[0], hour: parts[1], dayOfMonth: parts[2], month: parts[3], dayOfWeek: parts[4]),
                6 => BuildDescription(second: parts[0], minute: parts[1], hour: parts[2], dayOfMonth: parts[3], month: parts[4], dayOfWeek: parts[5]),
                _ => "Invalid cron expression"
            };
        }
        catch
        {
            return "Invalid cron expression";
        }
    }

    private static readonly string[] _dayNames =
        ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];

    private static readonly string[] _monthNames =
        ["January", "February", "March", "April", "May", "June",
         "July", "August", "September", "October", "November", "December"];

    /// <summary>
    /// Composes the description from the individual cron fields.
    /// </summary>
    private static string BuildDescription(string? second, string minute, string hour, string dayOfMonth, string month, string dayOfWeek)
    {
        var description = new StringBuilder(DescribeTime(second, minute, hour));

        if (dayOfWeek != "*" && dayOfWeek != "?")
            description.Append(", ").Append(DescribeSet(dayOfWeek, DayName));

        if (dayOfMonth != "*" && dayOfMonth != "?")
            description.Append(", on day ").Append(DescribeSet(dayOfMonth, static v => v.ToString(CultureInfo.InvariantCulture))).Append(" of the month");

        if (month != "*")
            description.Append(", in ").Append(DescribeSet(month, MonthName));

        return description.ToString();
    }

    /// <summary>
    /// Describes the second/minute/hour portion of the expression.
    /// </summary>
    private static string DescribeTime(string? second, string minute, string hour)
    {
        var secondsSuffix = second is null or "0" or "*"
            ? string.Empty
            : $" at second {DescribeSet(second, static v => v.ToString(CultureInfo.InvariantCulture))}";

        if (second is "*" && minute == "*" && hour == "*")
            return "Every second";

        if (minute == "*" && hour == "*")
            return "Every minute" + secondsSuffix;

        if (TryGetStep(minute, out var minuteStep) && hour == "*")
            return $"Every {minuteStep} minutes" + secondsSuffix;

        if (TryGetStep(hour, out var hourStep))
            return $"Every {hourStep} hours at minute {DescribeSet(minute, static v => v.ToString(CultureInfo.InvariantCulture))}" + secondsSuffix;

        if (hour == "*")
            return $"Hourly at minute {DescribeSet(minute, static v => v.ToString(CultureInfo.InvariantCulture))}" + secondsSuffix;

        if (int.TryParse(hour, NumberStyles.Integer, CultureInfo.InvariantCulture, out var singleHour) &&
            int.TryParse(minute, NumberStyles.Integer, CultureInfo.InvariantCulture, out var singleMinute))
        {
            return string.Create(CultureInfo.InvariantCulture, $"At {singleHour:D2}:{singleMinute:D2}") + secondsSuffix;
        }

        return $"At minute {DescribeSet(minute, static v => v.ToString(CultureInfo.InvariantCulture))} " +
               $"past hour {DescribeSet(hour, static v => v.ToString(CultureInfo.InvariantCulture))}" + secondsSuffix;
    }

    /// <summary>
    /// Renders a cron field ("1", "1-5", "1,3", "*/10") using the supplied value formatter.
    /// </summary>
    private static string DescribeSet(string field, Func<int, string> format)
    {
        if (field == "*")
            return "every value";

        if (TryGetStep(field, out var step))
            return $"every {step}";

        var segments = field.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var rendered = new List<string>(segments.Length);

        foreach (var segment in segments)
        {
            var bounds = segment.Split('-', 2);

            if (bounds.Length == 2 &&
                int.TryParse(bounds[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var from) &&
                int.TryParse(bounds[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var to))
            {
                rendered.Add($"{format(from)} through {format(to)}");
            }
            else if (int.TryParse(segment, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            {
                rendered.Add(format(value));
            }
            else
            {
                rendered.Add(segment);
            }
        }

        return rendered.Count switch
        {
            0 => field,
            1 => rendered[0],
            _ => string.Join(" and ", string.Join(", ", rendered.Take(rendered.Count - 1)), rendered[^1])
        };
    }

    /// <summary>
    /// Extracts the step of a "*/n" or "a-b/n" field.
    /// </summary>
    private static bool TryGetStep(string field, out int step)
    {
        step = 0;
        var slash = field.IndexOf('/');

        return slash >= 0 &&
               int.TryParse(field[(slash + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out step) &&
               step > 0;
    }

    private static string DayName(int value) =>
        value is >= 0 and <= 7 ? _dayNames[value % 7] : value.ToString(CultureInfo.InvariantCulture);

    private static string MonthName(int value) =>
        value is >= 1 and <= 12 ? _monthNames[value - 1] : value.ToString(CultureInfo.InvariantCulture);
}
