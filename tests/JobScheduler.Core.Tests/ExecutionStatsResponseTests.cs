// tests/JobScheduler.Core.Tests/ExecutionStatsResponseTests.cs
using System;
using JobScheduler.Core.Domain.Models;
using Xunit;

namespace JobScheduler.Core.Tests;

public class ExecutionStatsResponseTests
{
    [Fact]
    public void DefaultValues_ShouldBeInitializedCorrectly()
    {
        // Arrange & Act
        var response = new ExecutionStatsResponse();

        // Assert
        Assert.Equal(Guid.Empty, response.JobId);
        Assert.Equal(0, response.TotalExecutions);
        Assert.Equal(0, response.SuccessfulExecutions);
        Assert.Equal(0, response.FailedExecutions);
        Assert.Equal(0.0, response.SuccessRate);
        Assert.Equal(0L, response.AverageExecutionTimeMs);
        Assert.Equal(0L, response.MinExecutionTimeMs);
        Assert.Equal(0L, response.MaxExecutionTimeMs);
        Assert.Null(response.LastExecutionAt);
    }

    [Fact]
    public void PropertySetAndGet_ShouldPersistValues()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var response = new ExecutionStatsResponse
        {
            JobId = guid,
            TotalExecutions = 100,
            SuccessfulExecutions = 80,
            FailedExecutions = 20,
            SuccessRate = 0.8,
            AverageExecutionTimeMs = 1500,
            MinExecutionTimeMs = 500,
            MaxExecutionTimeMs = 3000,
            LastExecutionAt = now
        };

        // Act & Assert
        Assert.Equal(guid, response.JobId);
        Assert.Equal(100, response.TotalExecutions);
        Assert.Equal(80, response.SuccessfulExecutions);
        Assert.Equal(20, response.FailedExecutions);
        Assert.Equal(0.8, response.SuccessRate);
        Assert.Equal(1500L, response.AverageExecutionTimeMs);
        Assert.Equal(500L, response.MinExecutionTimeMs);
        Assert.Equal(3000L, response.MaxExecutionTimeMs);
        Assert.Equal(now, response.LastExecutionAt);
    }

    [Fact]
    public void LastExecutionAt_CanBeSetToNull()
    {
        // Arrange
        var response = new ExecutionStatsResponse
        {
            LastExecutionAt = DateTime.UtcNow
        };

        // Act
        response.LastExecutionAt = null;

        // Assert
        Assert.Null(response.LastExecutionAt);
    }

    [Fact]
    public void SuccessRate_ShouldAllowExtremeValues()
    {
        // Arrange
        var response = new ExecutionStatsResponse();

        // Act
        response.SuccessRate = -1.0; // unrealistic but allowed by the model
        var negative = response.SuccessRate;

        response.SuccessRate = 2.5; // > 1 also allowed
        var overOne = response.SuccessRate;

        // Assert
        Assert.Equal(-1.0, negative);
        Assert.Equal(2.5, overOne);
    }

    [Fact]
    public void ExecutionTimeMetrics_ShouldHandleLargeValues()
    {
        // Arrange
        var response = new ExecutionStatsResponse
        {
            AverageExecutionTimeMs = long.MaxValue,
            MinExecutionTimeMs = long.MinValue,
            MaxExecutionTimeMs = long.MaxValue
        };

        // Act & Assert
        Assert.Equal(long.MaxValue, response.AverageExecutionTimeMs);
        Assert.Equal(long.MinValue, response.MinExecutionTimeMs);
        Assert.Equal(long.MaxValue, response.MaxExecutionTimeMs);
    }
}
