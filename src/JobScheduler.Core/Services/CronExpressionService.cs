#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using NCrontab;
using JobScheduler.Core.Exceptions;

namespace JobScheduler.Core.Services;

/// <summary>
/// Service for parsing, validating, and evaluating cron expressions.
/// Uses NCronTab library for POSIX-compliant cron parsing.
/// </summary>
public sealed class CronExpressionService
{
    // Parsed schedules are immutable and cheap to share; cache by expression string
    // to avoid paying NCronTab parse cost on every evaluation cycle.
    private static readonly ConcurrentDictionary<string, CrontabSchedule> _scheduleCache =
        new(StringComparer.Ordinal);

    /// <summary>
    /// Validates a cron expression syntax.
    /// </summary>
    public bool IsValidCronExpression(string cronExpression)
    {
        if (string.IsNullOrWhiteSpace(cronExpression))
            return false;

        try
        {
            ParseCronExpression(cronExpression);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Parses a cron expression and throws if invalid.
    /// Result is cached so repeated calls for the same expression are allocation-free.
    /// </summary>
    public CrontabSchedule ParseCronExpression(string cronExpression)
    {
        if (string.IsNullOrWhiteSpace(cronExpression))
            throw new CronExpressionException(cronExpression, "Expression cannot be null or empty");

        if (_scheduleCache.TryGetValue(cronExpression, out var cached))
            return cached;

        CrontabSchedule parsed;
        try
        {
            parsed = CrontabSchedule.Parse(cronExpression);
        }
        catch (Exception ex)
        {
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
    public DateTime GetNextExecutionTime(string cronExpression, DateTime? baseTime = null)
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
    /// Calculates the next N execution times.
    /// </summary>
    public IEnumerable<DateTime> GetNextExecutionTimes(string cronExpression, int count, DateTime? baseTime = null)
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
    public bool ShouldExecuteAt(string cronExpression, DateTime checkTime)
    {
        try
        {
            var schedule = ParseCronExpression(cronExpression);
            var previous = schedule.GetPreviousOccurrence(checkTime.AddSeconds(1));
            var difference = (checkTime - previous).TotalSeconds;

            // Consider it a match if within 60 seconds (accounting for scheduling delays)
            return difference >= 0 && difference < 60;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets a human-readable description of a cron expression.
    /// </summary>
    public string GetCronDescription(string cronExpression)
    {
        try
        {
            // This is a simplified description - could be enhanced with CronExpressionDescriptor
            var schedule = ParseCronExpression(cronExpression);
            var parts = cronExpression.Split(' ');

            return parts.Length switch
            {
                5 => GetSimpleCronDescription(parts),
                6 => GetQuartzCronDescription(parts),
                _ => "Complex schedule"
            };
        }
        catch
        {
            return "Invalid cron expression";
        }
    }

    private string GetSimpleCronDescription(string[] parts)
    {
        // minute hour day month dayofweek
        if (parts[0] == "*" && parts[1] == "*" && parts[2] == "*" && parts[3] == "*" && parts[4] == "*")
            return "Every minute";

        if (parts[0] == "0" && parts[1] == "*")
            return "Hourly";

        if (parts[0] == "0" && parts[1] == "0")
            return "Daily";

        return "Custom schedule";
    }

    private string GetQuartzCronDescription(string[] parts)
    {
        // second minute hour day month dayofweek year
        return "Custom schedule";
    }
}
