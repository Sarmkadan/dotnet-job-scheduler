#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
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
}
