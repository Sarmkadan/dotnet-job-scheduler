// tests/JobScheduler.Core.Tests/PerformanceAnalysisResponseTests.cs
using System;
using JobScheduler.Core.Domain.Models;
using Xunit;

namespace JobScheduler.Core.Tests;

public class PerformanceAnalysisResponseTests
{
    [Fact]
    public void DefaultValues_ReturnExpectedDefaults()
    {
        // Arrange & Act
        var response = new PerformanceAnalysisResponse();

        // Assert
        Assert.Equal(Guid.Empty, response.JobId);
        Assert.Equal(0, response.AverageExecutionTimeMs);
        Assert.Equal(0, response.MedianExecutionTimeMs);
        Assert.Equal(0, response.P95ExecutionTimeMs);
        Assert.Equal(0, response.P99ExecutionTimeMs);
        Assert.Equal(0, response.SlowestExecutionTimeMs);
        Assert.Equal(0, response.FastestExecutionTimeMs);
        Assert.Null(response.SlowestExecutionAt);
        Assert.Null(response.FastestExecutionAt);
    }

    [Fact]
    public void SetProperties_StoresValuesCorrectly()
    {
        // Arrange
        var expectedJobId = Guid.NewGuid();
        var expectedAverage = 1000L;
        var expectedMedian = 950L;
        var expectedP95 = 1500L;
        var expectedP99 = 2000L;
        var expectedSlowest = 5000L;
        var expectedFastest = 500L;
        var expectedSlowestAt = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var expectedFastestAt = new DateTime(2024, 1, 15, 10, 25, 0, DateTimeKind.Utc);

        // Act
        var response = new PerformanceAnalysisResponse
        {
            JobId = expectedJobId,
            AverageExecutionTimeMs = expectedAverage,
            MedianExecutionTimeMs = expectedMedian,
            P95ExecutionTimeMs = expectedP95,
            P99ExecutionTimeMs = expectedP99,
            SlowestExecutionTimeMs = expectedSlowest,
            FastestExecutionTimeMs = expectedFastest,
            SlowestExecutionAt = expectedSlowestAt,
            FastestExecutionAt = expectedFastestAt
        };

        // Assert
        Assert.Equal(expectedJobId, response.JobId);
        Assert.Equal(expectedAverage, response.AverageExecutionTimeMs);
        Assert.Equal(expectedMedian, response.MedianExecutionTimeMs);
        Assert.Equal(expectedP95, response.P95ExecutionTimeMs);
        Assert.Equal(expectedP99, response.P99ExecutionTimeMs);
        Assert.Equal(expectedSlowest, response.SlowestExecutionTimeMs);
        Assert.Equal(expectedFastest, response.FastestExecutionTimeMs);
        Assert.Equal(expectedSlowestAt, response.SlowestExecutionAt);
        Assert.Equal(expectedFastestAt, response.FastestExecutionAt);
    }

    [Fact]
    public void JobId_CanBeSetToAnyGuid()
    {
        // Arrange
        var expectedJobId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
        var response = new PerformanceAnalysisResponse();

        // Act
        response.JobId = expectedJobId;

        // Assert
        Assert.Equal(expectedJobId, response.JobId);
    }

    [Fact]
    public void ExecutionTimeProperties_CanBeSetToLargeValues()
    {
        // Arrange
        var response = new PerformanceAnalysisResponse();

        // Act
        response.AverageExecutionTimeMs = long.MaxValue;
        response.MedianExecutionTimeMs = long.MaxValue;
        response.P95ExecutionTimeMs = long.MaxValue;
        response.P99ExecutionTimeMs = long.MaxValue;
        response.SlowestExecutionTimeMs = long.MaxValue;
        response.FastestExecutionTimeMs = long.MaxValue;

        // Assert
        Assert.Equal(long.MaxValue, response.AverageExecutionTimeMs);
        Assert.Equal(long.MaxValue, response.MedianExecutionTimeMs);
        Assert.Equal(long.MaxValue, response.P95ExecutionTimeMs);
        Assert.Equal(long.MaxValue, response.P99ExecutionTimeMs);
        Assert.Equal(long.MaxValue, response.SlowestExecutionTimeMs);
        Assert.Equal(long.MaxValue, response.FastestExecutionTimeMs);
    }

    [Fact]
    public void ExecutionTimeProperties_CanBeSetToZero()
    {
        // Arrange
        var response = new PerformanceAnalysisResponse
        {
            AverageExecutionTimeMs = 100,
            MedianExecutionTimeMs = 100,
            P95ExecutionTimeMs = 100,
            P99ExecutionTimeMs = 100,
            SlowestExecutionTimeMs = 100,
            FastestExecutionTimeMs = 100
        };

        // Act
        response.AverageExecutionTimeMs = 0;
        response.MedianExecutionTimeMs = 0;
        response.P95ExecutionTimeMs = 0;
        response.P99ExecutionTimeMs = 0;
        response.SlowestExecutionTimeMs = 0;
        response.FastestExecutionTimeMs = 0;

        // Assert
        Assert.Equal(0, response.AverageExecutionTimeMs);
        Assert.Equal(0, response.MedianExecutionTimeMs);
        Assert.Equal(0, response.P95ExecutionTimeMs);
        Assert.Equal(0, response.P99ExecutionTimeMs);
        Assert.Equal(0, response.SlowestExecutionTimeMs);
        Assert.Equal(0, response.FastestExecutionTimeMs);
    }

    [Fact]
    public void ExecutionTimeProperties_CanBeNegative()
    {
        // Arrange
        var response = new PerformanceAnalysisResponse();

        // Act
        response.AverageExecutionTimeMs = -1;
        response.MedianExecutionTimeMs = -100;
        response.P95ExecutionTimeMs = -50;
        response.P99ExecutionTimeMs = -1000;
        response.SlowestExecutionTimeMs = -10;
        response.FastestExecutionTimeMs = -1;

        // Assert
        Assert.Equal(-1, response.AverageExecutionTimeMs);
        Assert.Equal(-100, response.MedianExecutionTimeMs);
        Assert.Equal(-50, response.P95ExecutionTimeMs);
        Assert.Equal(-1000, response.P99ExecutionTimeMs);
        Assert.Equal(-10, response.SlowestExecutionTimeMs);
        Assert.Equal(-1, response.FastestExecutionTimeMs);
    }

    [Fact]
    public void DateTimeProperties_CanBeMinValue()
    {
        // Arrange
        var response = new PerformanceAnalysisResponse();

        // Act
        response.SlowestExecutionAt = DateTime.MinValue;
        response.FastestExecutionAt = DateTime.MinValue;

        // Assert
        Assert.Equal(DateTime.MinValue, response.SlowestExecutionAt);
        Assert.Equal(DateTime.MinValue, response.FastestExecutionAt);
    }

    [Fact]
    public void DateTimeProperties_CanBeMaxValue()
    {
        // Arrange
        var response = new PerformanceAnalysisResponse();

        // Act
        response.SlowestExecutionAt = DateTime.MaxValue;
        response.FastestExecutionAt = DateTime.MaxValue;

        // Assert
        Assert.Equal(DateTime.MaxValue, response.SlowestExecutionAt);
        Assert.Equal(DateTime.MaxValue, response.FastestExecutionAt);
    }

    [Fact]
    public void DateTimeProperties_CanBeNull()
    {
        // Arrange
        var response = new PerformanceAnalysisResponse
        {
            SlowestExecutionAt = DateTime.Now,
            FastestExecutionAt = DateTime.Now
        };

        // Act
        response.SlowestExecutionAt = null;
        response.FastestExecutionAt = null;

        // Assert
        Assert.Null(response.SlowestExecutionAt);
        Assert.Null(response.FastestExecutionAt);
    }

    [Fact]
    public void AllProperties_WorkIndependently()
    {
        // Arrange
        var response1 = new PerformanceAnalysisResponse();
        var response2 = new PerformanceAnalysisResponse();

        // Act - modify different properties on each instance
        response1.AverageExecutionTimeMs = 100;
        response1.MedianExecutionTimeMs = 90;
        response2.AverageExecutionTimeMs = 200;
        response2.MedianExecutionTimeMs = 180;

        // Assert
        Assert.Equal(100, response1.AverageExecutionTimeMs);
        Assert.Equal(90, response1.MedianExecutionTimeMs);
        Assert.Equal(200, response2.AverageExecutionTimeMs);
        Assert.Equal(180, response2.MedianExecutionTimeMs);
    }
}
