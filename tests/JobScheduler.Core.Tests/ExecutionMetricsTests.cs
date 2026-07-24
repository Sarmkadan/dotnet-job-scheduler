using Xunit;
using JobScheduler.Core.Domain.Entities;
using System;

namespace JobScheduler.Core.Tests;

public class ExecutionMetricsTests
{
    [Fact]
    public void CalculateSuccessRate_ReturnsCorrectPercentage()
    {
        // Arrange
        var metrics = new ExecutionMetrics
        {
            TotalExecutions = 10,
            SuccessfulExecutions = 8
        };

        // Act
        var rate = metrics.CalculateSuccessRate();

        // Assert
        Assert.Equal(80.0, rate);
    }

    [Fact]
    public void CalculateSuccessRate_ReturnsZero_WhenTotalExecutionsIsZero()
    {
        // Arrange
        var metrics = new ExecutionMetrics { TotalExecutions = 0 };

        // Act
        var rate = metrics.CalculateSuccessRate();

        // Assert
        Assert.Equal(0.0, rate);
    }

    [Fact]
    public void IsReliable_ReturnsTrue_WhenSuccessRateMeetsThreshold()
    {
        // Arrange
        var metrics = new ExecutionMetrics
        {
            TotalExecutions = 100,
            SuccessfulExecutions = 90 // 90%
        };

        // Act & Assert
        Assert.True(metrics.IsReliable(90.0));
        Assert.False(metrics.IsReliable(91.0));
    }

    [Fact]
    public void GetActualFailureCount_SumsFailedAndTimedOut()
    {
        // Arrange
        var metrics = new ExecutionMetrics
        {
            FailedExecutions = 3,
            TimedOutExecutions = 2,
            CancelledExecutions = 5 // Should be ignored
        };

        // Act
        var count = metrics.GetActualFailureCount();

        // Assert
        Assert.Equal(5, count);
    }

    [Theory]
    [InlineData(50, "Excellent")]
    [InlineData(150, "Good")]
    [InlineData(1000, "Acceptable")]
    [InlineData(3000, "Slow")]
    [InlineData(6000, "Very Slow")]
    public void GetPerformanceClass_ReturnsCorrectClassification(long duration, string expectedClass)
    {
        // Arrange
        var metrics = new ExecutionMetrics { AverageDurationMs = duration };

        // Act
        var result = metrics.GetPerformanceClass();

        // Assert
        Assert.Equal(expectedClass, result);
    }

    [Fact]
    public void IsValid_ReturnsTrue_ForValidMetrics()
    {
        // Arrange
        var metrics = new ExecutionMetrics
        {
            JobId = Guid.NewGuid(),
            TotalExecutions = 10,
            SuccessfulExecutions = 8,
            FailedExecutions = 2,
            MinDurationMs = 100,
            MaxDurationMs = 200
        };

        // Act
        var isValid = metrics.IsValid();

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void IsValid_ReturnsFalse_WhenJobIdIsEmpty()
    {
        // Arrange
        var metrics = new ExecutionMetrics { JobId = Guid.Empty };

        // Act
        var isValid = metrics.IsValid();

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void IsValid_ReturnsFalse_WhenMinDurationGreaterThanMax()
    {
        // Arrange
        var metrics = new ExecutionMetrics
        {
            JobId = Guid.NewGuid(),
            MinDurationMs = 500,
            MaxDurationMs = 100
        };

        // Act
        var isValid = metrics.IsValid();

        // Assert
        Assert.False(isValid);
    }
}
