using Xunit;
using JobScheduler.Core.Domain.Entities;

namespace JobScheduler.Core.Tests;

public class ExecutionMetricsExtensionsTests
{
    [Fact]
    public void IsConsistentlyReliable_HappyPath_ReturnsTrue()
    {
        // Arrange
        var metrics = new ExecutionMetrics { SuccessRate = 0.95, TotalExecutions = 15 };

        // Act
        var result = metrics.IsConsistentlyReliable();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsConsistentlyReliable_NullInput_ThrowsArgumentNullException()
    {
        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => ((ExecutionMetrics?)null).IsConsistentlyReliable());
    }

    [Fact]
    public void GetAverageExecutionDurationInSeconds_HappyPath_ReturnsAverageDuration()
    {
        // Arrange
        var metrics = new ExecutionMetrics { AverageDurationMs = 1000 };

        // Act
        var result = metrics.GetAverageExecutionDurationInSeconds();

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public void GetAverageExecutionDurationInSeconds_NullInput_ThrowsArgumentNullException()
    {
        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => ((ExecutionMetrics?)null).GetAverageExecutionDurationInSeconds());
    }

    [Fact]
    public void HasHighFailureRate_HappyPath_ReturnsTrue()
    {
        // Arrange
        var metrics = new ExecutionMetrics { SuccessRate = 0.8 };

        // Act
        var result = metrics.HasHighFailureRate();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasHighFailureRate_NullInput_ThrowsArgumentNullException()
    {
        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => ((ExecutionMetrics?)null).HasHighFailureRate());
    }
}
