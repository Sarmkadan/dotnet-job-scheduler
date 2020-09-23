#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using JobScheduler.Core.Services;

namespace JobScheduler.Core.Tests;

/// <summary>
/// Extension methods for <see cref="CronExpressionServiceTests"/> that provide additional utility functionality
/// for testing cron expressions in various scenarios.
/// </summary>
public static class CronExpressionServiceTestsExtensions
{
    /// <summary>
    /// Validates a collection of cron expressions and returns detailed validation results.
    /// </summary>
    /// <param name="tests">The test instance.</param>
    /// <param name="expressions">The cron expressions to validate.</param>
    /// <returns>A dictionary mapping each expression to its validation result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="expressions"/> is null.</exception>
    public static IReadOnlyDictionary<string, bool> ValidateCronExpressions(
        this CronExpressionServiceTests tests,
        IEnumerable<string> expressions)
    {
        ArgumentNullException.ThrowIfNull(expressions);

        var results = new Dictionary<string, bool>();
        var service = new CronExpressionService();

        foreach (var expression in expressions)
        {
            results[expression] = service.IsValidCronExpression(expression);
        }

        return results.AsReadOnly();
    }

    /// <summary>
    /// Gets the next execution times for a cron expression starting from multiple base times.
    /// </summary>
    /// <param name="tests">The test instance.</param>
    /// <param name="cronExpression">The cron expression to evaluate.</param>
    /// <param name="baseTimes">The base times to calculate from.</param>
    /// <param name="count">The number of future executions to return for each base time.</param>
    /// <returns>A dictionary mapping each base time to its next execution times.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="cronExpression"/> or <paramref name="baseTimes"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="cronExpression"/> is invalid.</exception>
    public static IReadOnlyDictionary<DateTime, IReadOnlyList<DateTime>> GetNextExecutionTimesFromMultipleBases(
        this CronExpressionServiceTests tests,
        string cronExpression,
        IEnumerable<DateTime> baseTimes,
        int count = 5)
    {
        ArgumentNullException.ThrowIfNull(cronExpression);
        ArgumentNullException.ThrowIfNull(baseTimes);

        var results = new Dictionary<DateTime, IReadOnlyList<DateTime>>();
        var service = new CronExpressionService();

        foreach (var baseTime in baseTimes)
        {
            var times = service.GetNextExecutionTimes(cronExpression, count, baseTime).ToList();
            results[baseTime] = times.AsReadOnly();
        }

        return results.AsReadOnly();
    }

    /// <summary>
    /// Determines whether a specific cron expression should execute at any of the provided times.
    /// </summary>
    /// <param name="tests">The test instance.</param>
    /// <param name="cronExpression">The cron expression to evaluate.</param>
    /// <param name="times">The times to check.</param>
    /// <returns>A dictionary mapping each time to whether it matches the cron expression.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="cronExpression"/> or <paramref name="times"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="cronExpression"/> is invalid.</exception>
    public static IReadOnlyDictionary<DateTime, bool> ShouldExecuteAtAny(
        this CronExpressionServiceTests tests,
        string cronExpression,
        IEnumerable<DateTime> times)
    {
        ArgumentNullException.ThrowIfNull(cronExpression);
        ArgumentNullException.ThrowIfNull(times);

        var results = new Dictionary<DateTime, bool>();
        var service = new CronExpressionService();

        foreach (var time in times)
        {
            results[time] = service.ShouldExecuteAt(cronExpression, time);
        }

        return results.AsReadOnly();
    }

    /// <summary>
    /// Gets the time until next execution in a human-readable format.
    /// </summary>
    /// <param name="tests">The test instance.</param>
    /// <param name="cronExpression">The cron expression to evaluate.</param>
    /// <param name="currentTime">The current time to calculate from.</param>
    /// <returns>A formatted string representing the time until next execution.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="cronExpression"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="cronExpression"/> is invalid.</exception>
    public static string GetTimeUntilNextExecution(
        this CronExpressionServiceTests tests,
        string cronExpression,
        DateTime currentTime)
    {
        ArgumentNullException.ThrowIfNull(cronExpression);

        var service = new CronExpressionService();
        var nextExecution = service.GetNextExecutionTime(cronExpression, baseTime: currentTime);
        var timeSpan = nextExecution - currentTime;

        return FormatTimeSpan(timeSpan);
    }

    /// <summary>
    /// Gets the next execution times for a cron expression in a specific timezone.
    /// </summary>
    /// <param name="tests">The test instance.</param>
    /// <param name="cronExpression">The cron expression to evaluate.</param>
    /// <param name="timezoneId">The timezone identifier.</param>
    /// <param name="baseUtc">The base UTC time.</param>
    /// <param name="count">The number of executions to return.</param>
    /// <returns>A list of next execution times in UTC.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="cronExpression"/> or <paramref name="timezoneId"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="cronExpression"/> is invalid or timezone is unknown.</exception>
    public static IReadOnlyList<DateTime> GetNextExecutionTimesInZone(
        this CronExpressionServiceTests tests,
        string cronExpression,
        string timezoneId,
        DateTime baseUtc,
        int count = 5)
    {
        ArgumentNullException.ThrowIfNull(cronExpression);
        ArgumentNullException.ThrowIfNull(timezoneId);

        var results = new List<DateTime>();
        var service = new CronExpressionService();

        for (var i = 0; i < count; i++)
        {
            var nextTime = service.GetNextExecutionTimeInZone(cronExpression, timezoneId, baseUtc);
            results.Add(nextTime);
            baseUtc = nextTime.AddSeconds(1);
        }

        return results.AsReadOnly();
    }

    /// <summary>
    /// Validates that a cron expression is valid and can be parsed.
    /// </summary>
    /// <param name="tests">The test instance.</param>
    /// <param name="expression">The cron expression to validate.</param>
    /// <returns>True if the expression is valid and can be parsed; otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="expression"/> is null.</exception>
    public static bool IsValidAndParsable(
        this CronExpressionServiceTests tests,
        string expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        try
        {
            var service = new CronExpressionService();
            return service.IsValidCronExpression(expression)
                   && service.ParseCronExpression(expression) is not null;
        }
        catch
        {
            return false;
        }
    }

    private static string FormatTimeSpan(TimeSpan timeSpan)
    {
        var parts = new List<string>();

        if (timeSpan.TotalDays >= 1)
        {
            var days = (int)timeSpan.TotalDays;
            parts.Add($"{days} day{(days == 1 ? "" : "s")}");
            timeSpan = timeSpan.Subtract(TimeSpan.FromDays(days));
        }

        if (timeSpan.Hours > 0)
        {
            parts.Add($"{timeSpan.Hours} hour{(timeSpan.Hours == 1 ? "" : "s")}");
        }

        if (timeSpan.Minutes > 0)
        {
            parts.Add($"{timeSpan.Minutes} minute{(timeSpan.Minutes == 1 ? "" : "s")}");
        }

        if (timeSpan.Seconds > 0 || parts.Count == 0)
        {
            parts.Add($"{timeSpan.Seconds} second{(timeSpan.Seconds == 1 ? "" : "s")}");
        }

        return string.Join(", ", parts);
    }
}