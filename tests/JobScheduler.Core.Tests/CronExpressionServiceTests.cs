#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Linq;
using Xunit;
using JobScheduler.Core.Exceptions;
using JobScheduler.Core.Services;

namespace JobScheduler.Core.Tests;

/// <summary>
/// Unit tests for CronExpressionService.
/// Validates cron expression parsing, validation, and execution time calculation.
/// </summary>
public sealed class CronExpressionServiceTests
{
    private readonly CronExpressionService _service = new();

    [Fact]
    public void IsValidCronExpression_WithValidExpression_ReturnsTrue()
    {
        // Arrange
        var validCrons = new[]
        {
            "0 0 * * *",      // Daily at midnight
            "0 * * * *",      // Every hour
            "* * * * *",      // Every minute
            "0 9 * * 1-5",    // Weekdays at 9 AM
            "30 2 * * 0",     // Sunday at 2:30 AM
        };

        // Act & Assert
        foreach (var cron in validCrons)
        {
            Assert.True(_service.IsValidCronExpression(cron), $"Expression '{cron}' should be valid");
        }
    }

    [Fact]
    public void IsValidCronExpression_WithInvalidExpression_ReturnsFalse()
    {
        // Arrange
        var invalidCrons = new[]
        {
            "",
            "   ",
            "invalid",
            "60 * * * *",     // Invalid minute
            "* * * * 7",      // Invalid day of week
            "* * 32 * *"      // Invalid day of month
        };

        // Act & Assert
        foreach (var cron in invalidCrons)
        {
            Assert.False(_service.IsValidCronExpression(cron), $"Expression '{cron}' should be invalid");
        }
    }

    [Fact]
    public void ParseCronExpression_WithValidExpression_ReturnsSchedule()
    {
        // Arrange
        var validCron = "0 9 * * 1-5";

        // Act
        var schedule = _service.ParseCronExpression(validCron);

        // Assert
        Assert.NotNull(schedule);
    }

    [Fact]
    public void ParseCronExpression_WithInvalidExpression_ThrowsException()
    {
        // Arrange
        var invalidCron = "invalid cron";

        // Act & Assert
        Assert.Throws<CronExpressionException>(() => _service.ParseCronExpression(invalidCron));
    }

    [Fact]
    public void GetNextExecutionTime_ReturnsTimeInFuture()
    {
        // Arrange
        var cron = "0 0 * * *"; // Daily at midnight
        var now = DateTime.UtcNow;

        // Act
        var nextTime = _service.GetNextExecutionTime(cron, now);

        // Assert
        Assert.True(nextTime > now, "Next execution should be in the future");
        Assert.Equal(0, nextTime.Hour);
        Assert.Equal(0, nextTime.Minute);
        Assert.Equal(0, nextTime.Second);
    }

    [Fact]
    public void GetNextExecutionTimes_ReturnsMultipleTimes()
    {
        // Arrange
        var cron = "0 * * * *"; // Every hour
        var count = 5;

        // Act
        var times = _service.GetNextExecutionTimes(cron, count).ToList();

        // Assert
        Assert.Equal(count, times.Count);
        for (int i = 0; i < times.Count - 1; i++)
        {
            Assert.True(times[i] < times[i + 1], "Times should be in ascending order");
        }
    }

    [Fact]
    public void ShouldExecuteAt_WithMatchingTime_ReturnsTrue()
    {
        // Arrange
        var cron = "0 12 * * *"; // Daily at noon
        var noon = DateTime.UtcNow.Date.AddHours(12);

        // Act
        var shouldExecute = _service.ShouldExecuteAt(cron, noon);

        // Assert
        Assert.True(shouldExecute, "Should execute at noon");
    }

    [Fact]
    public void GetCronDescription_WithCommonExpression_ReturnsDescription()
    {
        // Arrange
        var cron = "0 0 * * *"; // Daily

        // Act
        var description = _service.GetCronDescription(cron);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(description));
        Assert.NotEqual("Invalid cron expression", description);
    }

    [Fact]
    public void GetNextExecutionTime_LeapYearExpressionInNonLeapYear_FindsNextLeapYear()
    {
        // Arrange
        var cron = "0 0 29 2 *"; // Feb 29th
        var nonLeapYear = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc); // Start of a non-leap year

        // Act
        var nextLeapYearOccurrence = _service.GetNextExecutionTime(cron, nonLeapYear);

        // Assert
        Assert.Equal(new DateTime(2024, 2, 29, 0, 0, 0, DateTimeKind.Utc), nextLeapYearOccurrence);
    }

    [Fact]
    public void GetNextExecutionTime_LeapYearExpressionInLeapYear_ReturnsSameYear()
    {
        // Arrange
        var cron = "0 0 29 2 *"; // Feb 29th
        var leapYear = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var next = _service.GetNextExecutionTime(cron, leapYear);

        // Assert
        Assert.Equal(new DateTime(2024, 2, 29, 0, 0, 0, DateTimeKind.Utc), next);
    }

    [Fact]
    public void GetNextExecutionTimeInZone_ReturnsCorrectUtcTime()
    {
        // "0 9 * * *" in Eastern Standard Time (UTC-5) should return 14:00 UTC.
        var cron = "0 9 * * *";
        // Use a known EST date (no DST) to keep the test deterministic.
        var baseUtc = new DateTime(2024, 1, 10, 10, 0, 0, DateTimeKind.Utc); // 05:00 EST

        var nextUtc = _service.GetNextExecutionTimeInZone(cron, "Eastern Standard Time", baseUtc);

        // 09:00 EST on 2024-01-10 = 14:00 UTC
        Assert.Equal(new DateTime(2024, 1, 10, 14, 0, 0, DateTimeKind.Utc), nextUtc);
    }

    [Fact]
    public void GetNextExecutionTimeInZone_WithNullTimezone_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            _service.GetNextExecutionTimeInZone("0 9 * * *", null!));
    }

    [Fact]
    public void GetNextExecutionTimeInZone_WithUnknownTimezone_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            _service.GetNextExecutionTimeInZone("0 9 * * *", "Unknown/NotReal_TZ"));
    }

    // ------------------------------------------------------------------------
    // Additional tests requested in the task description
    // ------------------------------------------------------------------------

    [Fact]
    public void GetNextExecutionTime_Day31Expression_SkipsMonthsWithout31Days()
    {
        // Arrange
        var cron = "0 0 31 * *"; // At midnight on the 31st day of any month
        var start = new DateTime(2024, 4, 30, 0, 0, 0, DateTimeKind.Utc); // April has 30 days

        // Act
        var next = _service.GetNextExecutionTime(cron, start);
        var following = _service.GetNextExecutionTime(cron, next.AddSeconds(1));

        // Assert
        // First occurrence should be May 31
        Assert.Equal(new DateTime(2024, 5, 31, 0, 0, 0, DateTimeKind.Utc), next);
        // Second occurrence should skip June (30 days) and be July 31
        Assert.Equal(new DateTime(2024, 7, 31, 0, 0, 0, DateTimeKind.Utc), following);
    }

    [Fact]
    public void GetNextExecutionTimes_StepEvery15Minutes_ReturnsCorrectIntervals()
    {
        // Arrange
        var cron = "*/15 * * * *"; // Every 15 minutes
        var now = new DateTime(2024, 1, 1, 0, 7, 0, DateTimeKind.Utc); // 07 minutes past the hour

        // Act
        var times = _service.GetNextExecutionTimes(cron, 3).ToList();

        // Assert
        Assert.Equal(3, times.Count);
        // Expected times: 00:15, 00:30, 00:45 on the same day
        Assert.Equal(new DateTime(2024, 1, 1, 0, 15, 0, DateTimeKind.Utc), times[0]);
        Assert.Equal(new DateTime(2024, 1, 1, 0, 30, 0, DateTimeKind.Utc), times[1]);
        Assert.Equal(new DateTime(2024, 1, 1, 0, 45, 0, DateTimeKind.Utc), times[2]);
    }

    [Fact]
    public void ParseCronExpression_InvalidExpression_ThrowsCronExpressionException_WithMessage()
    {
        // Arrange
        var invalidCron = "61 * * * *"; // Invalid minute value

        // Act
        var exception = Assert.Throws<CronExpressionException>(() => _service.ParseCronExpression(invalidCron));

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(exception.Message));
        // The message should contain some indication of why it failed.
        Assert.Contains("minute", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
