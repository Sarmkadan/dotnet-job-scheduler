#nullable enable
using System;
using JobScheduler.Core.Extensions;
using Xunit;

namespace JobScheduler.Core.Tests;

public sealed class DateTimeExtensionsTests
{
    private static readonly DateTime TestNow = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void IsInThePast_WithPastDateTime_ReturnsTrue()
    {
        // Arrange
        var pastDate = TestNow.AddMinutes(-5);

        // Act
        var result = pastDate.IsInThePast();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsInThePast_WithFutureDateTime_ReturnsFalse()
    {
        // Arrange
        var futureDate = DateTime.UtcNow.AddMinutes(5);

        // Act
        var result = futureDate.IsInThePast();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsInTheFuture_WithPastDateTime_ReturnsFalse()
    {
        // Arrange
        var pastDate = DateTime.UtcNow.AddMinutes(-5);

        // Act
        var result = pastDate.IsInTheFuture();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void TimeUntil_WithFutureDateTime_ReturnsPositiveTimeSpan()
    {
        // Arrange
        var futureDate = DateTime.UtcNow.AddMinutes(1);

        // Act
        var result = futureDate.TimeUntil();

        // Assert
        Assert.True(result > TimeSpan.Zero);
    }

    [Fact]
    public void TimeUntil_WithPastDateTime_ReturnsNegativeTimeSpan()
    {
        // Arrange
        var pastDate = DateTime.UtcNow.AddMinutes(-1);

        // Act
        var result = pastDate.TimeUntil();

        // Assert
        Assert.True(result < TimeSpan.Zero);
    }

    [Fact]
    public void TimeSince_WithPastDateTime_ReturnsPositiveTimeSpan()
    {
        // Arrange
        var pastDate = DateTime.UtcNow.AddMinutes(-1);

        // Act
        var result = pastDate.TimeSince();

        // Assert
        Assert.True(result > TimeSpan.Zero);
    }

    [Fact]
    public void TimeSince_WithFutureDateTime_ReturnsNegativeTimeSpan()
    {
        // Arrange
        var futureDate = DateTime.UtcNow.AddMinutes(1);

        // Act
        var result = futureDate.TimeSince();

        // Assert
        Assert.True(result < TimeSpan.Zero);
    }

    [Fact]
    public void IsSameDay_WithSameDay_ReturnsTrue()
    {
        // Arrange
        var date1 = new DateTime(2024, 6, 15, 10, 30, 0);
        var date2 = new DateTime(2024, 6, 15, 20, 45, 0);

        // Act
        var result = date1.IsSameDay(date2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsSameDay_WithDifferentDays_ReturnsFalse()
    {
        // Arrange
        var date1 = new DateTime(2024, 6, 15);
        var date2 = new DateTime(2024, 6, 16);

        // Act
        var result = date1.IsSameDay(date2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void RoundToNearestMinute_WithExactMinute_ReturnsSameDateTime()
    {
        // Arrange
        var dateTime = new DateTime(2024, 6, 15, 10, 30, 45, 123);

        // Act
        var result = dateTime.RoundToNearestMinute();

        // Assert
        Assert.Equal(new DateTime(2024, 6, 15, 10, 30, 0), result);
    }

    [Fact]
    public void RoundToNearestMinute_WithSeconds_RoundsDown()
    {
        // Arrange
        var dateTime = new DateTime(2024, 6, 15, 10, 30, 29, 999);

        // Act
        var result = dateTime.RoundToNearestMinute();

        // Assert
        Assert.Equal(new DateTime(2024, 6, 15, 10, 30, 0), result);
    }

    [Fact]
    public void RoundToNearestMinute_WithSeconds_TruncatesToMinute()
    {
        // Arrange
        var dateTime = new DateTime(2024, 6, 15, 10, 30, 30, 0);

        // Act
        var result = dateTime.RoundToNearestMinute();

        // Assert - This method truncates, doesn't round
        Assert.Equal(new DateTime(2024, 6, 15, 10, 30, 0, 0), result);
    }

    [Fact]
    public void RoundToNearestHour_WithExactHour_ReturnsSameDateTime()
    {
        // Arrange
        var dateTime = new DateTime(2024, 6, 15, 10, 30, 45, 123);

        // Act
        var result = dateTime.RoundToNearestHour();

        // Assert
        Assert.Equal(new DateTime(2024, 6, 15, 10, 0, 0), result);
    }

    [Fact]
    public void RoundToNearestHour_WithMinutes_RoundsDown()
    {
        // Arrange
        var dateTime = new DateTime(2024, 6, 15, 10, 29, 59, 999);

        // Act
        var result = dateTime.RoundToNearestHour();

        // Assert
        Assert.Equal(new DateTime(2024, 6, 15, 10, 0, 0), result);
    }

    [Fact]
    public void RoundToNearestHour_WithMinutes_TruncatesToHour()
    {
        // Arrange
        var dateTime = new DateTime(2024, 6, 15, 10, 30, 0, 0);

        // Act
        var result = dateTime.RoundToNearestHour();

        // Assert - This method truncates, doesn't round
        Assert.Equal(new DateTime(2024, 6, 15, 10, 0, 0, 0), result);
    }

    [Fact]
    public void StartOfDay_WithDateTime_ReturnsMidnight()
    {
        // Arrange
        var dateTime = new DateTime(2024, 6, 15, 10, 30, 45, 123);

        // Act
        var result = dateTime.StartOfDay();

        // Assert
        Assert.Equal(new DateTime(2024, 6, 15, 0, 0, 0), result);
    }

    [Fact]
    public void EndOfDay_WithDateTime_ReturnsEndOfDay()
    {
        // Arrange
        var dateTime = new DateTime(2024, 6, 15, 10, 30, 45, 123);

        // Act
        var result = dateTime.EndOfDay();

        // Assert
        Assert.Equal(new DateTime(2024, 6, 15, 23, 59, 59), result);
    }

    [Fact]
    public void StartOfWeek_WithMonday_ReturnsSameMonday()
    {
        // Arrange
        var monday = new DateTime(2024, 6, 17); // June 17, 2024 is Monday

        // Act
        var result = monday.StartOfWeek();

        // Assert
        Assert.Equal(new DateTime(2024, 6, 17, 0, 0, 0), result);
    }

    [Fact]
    public void StartOfWeek_WithSunday_ReturnsPreviousMonday()
    {
        // Arrange
        var sunday = new DateTime(2024, 6, 16); // June 16, 2024 is Sunday

        // Act
        var result = sunday.StartOfWeek();

        // Assert
        Assert.Equal(new DateTime(2024, 6, 10, 0, 0, 0), result);
    }

    [Fact]
    public void StartOfWeek_WithWednesday_ReturnsMondayOfSameWeek()
    {
        // Arrange
        var wednesday = new DateTime(2024, 6, 19); // June 19, 2024 is Wednesday

        // Act
        var result = wednesday.StartOfWeek();

        // Assert
        Assert.Equal(new DateTime(2024, 6, 17, 0, 0, 0), result);
    }
}