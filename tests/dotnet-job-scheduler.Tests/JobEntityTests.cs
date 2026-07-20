#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using JobScheduler.Core.Constants;
using JobScheduler.Core.Domain.Entities;
using Xunit;

/// <summary>
/// Provides unit tests for the <see cref="Job"/> entity, verifying validation logic,
/// execution metrics updates, success rate calculations, and concurrency checks.
/// </summary>
namespace DotnetJobScheduler.Tests;

public sealed class JobEntityTests
{
    private static Job CreateValidJob() => new()
    {
        Name = "report-generator",
        CronExpression = "0 6 * * *",
        HandlerType = "MyApp.Jobs.ReportJob, MyApp",
        MaxRetries = 3,
        ExecutionTimeoutSeconds = 300,
        MaxConcurrentExecutions = 1
    };

    [Fact]
    public void IsValidForScheduling_WithCompleteConfiguration_ReturnsTrue()
    {
        // Arrange
        var job = CreateValidJob();

        // Act
        var result = job.IsValidForScheduling();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidForScheduling_WithMissingHandlerType_ReturnsFalse()
    {
        // Arrange
        var job = CreateValidJob();
        job.HandlerType = string.Empty;

        // Act
        var result = job.IsValidForScheduling();

        // Assert
        result.Should().BeFalse("the scheduler cannot dispatch a job without a handler");
    }

    [Fact]
    public void IsValidForScheduling_WithZeroExecutionTimeout_ReturnsFalse()
    {
        // Arrange
        var job = CreateValidJob();
        job.ExecutionTimeoutSeconds = 0;

        // Act
        var result = job.IsValidForScheduling();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidForScheduling_WithNegativeMaxRetries_ReturnsFalse()
    {
        // Arrange
        var job = CreateValidJob();
        job.MaxRetries = -1;

        // Act
        var result = job.IsValidForScheduling();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void UpdateExecutionMetrics_WhenExecutionSucceeds_OnlySuccessCounterIncrements()
    {
        // Arrange
        var job = CreateValidJob();

        // Act
        job.UpdateExecutionMetrics(success: true);

        // Assert
        job.TotalExecutions.Should().Be(1);
        job.SuccessfulExecutions.Should().Be(1);
        job.FailedExecutions.Should().Be(0);
    }

    [Fact]
    public void UpdateExecutionMetrics_WhenExecutionFails_OnlyFailureCounterIncrements()
    {
        // Arrange
        var job = CreateValidJob();

        // Act
        job.UpdateExecutionMetrics(success: false);

        // Assert
        job.TotalExecutions.Should().Be(1);
        job.SuccessfulExecutions.Should().Be(0);
        job.FailedExecutions.Should().Be(1);
    }

    [Fact]
    public void GetSuccessRate_WithNoExecutionsYet_ReturnsZero()
    {
        // Arrange
        var job = CreateValidJob();

        // Act
        var rate = job.GetSuccessRate();

        // Assert
        rate.Should().Be(0);
    }

    [Fact]
    public void GetSuccessRate_WithTwoSuccessesAndOneFailure_Returns66Point67Percent()
    {
        // Arrange
        var job = CreateValidJob();
        job.UpdateExecutionMetrics(success: true);
        job.UpdateExecutionMetrics(success: true);
        job.UpdateExecutionMetrics(success: false);

        // Act
        var rate = job.GetSuccessRate();

        // Assert
        rate.Should().BeApproximately(66.67, 0.01);
    }

    [Fact]
    public void CanExecuteNow_WhenJobIsSuspended_ReturnsFalseRegardlessOfConcurrency()
    {
        // Arrange
        var job = CreateValidJob();
        job.Status = JobStatus.Suspended;

        // Act
        var result = job.CanExecuteNow(currentConcurrentCount: 0);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanExecuteNow_WhenConcurrencyLimitSaturated_ReturnsFalse()
    {
        // Arrange
        var job = CreateValidJob();
        job.MaxConcurrentExecutions = 2;

        // Act
        var result = job.CanExecuteNow(currentConcurrentCount: 2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanExecuteNow_WhenActiveWithSlotAvailable_ReturnsTrue()
    {
        // Arrange
        var job = CreateValidJob();
        job.Status = JobStatus.Scheduled;
        job.MaxConcurrentExecutions = 3;

        // Act
        var result = job.CanExecuteNow(currentConcurrentCount: 1);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ExecutionTimeoutSeconds_WithValidValue_ReturnsTrueInValidation()
    {
        // Arrange
        var job = CreateValidJob();
        job.ExecutionTimeoutSeconds = 60;

        // Act
        var result = job.IsValidForScheduling();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ExecutionTimeoutSeconds_WithZero_ReturnsFalseInValidation()
    {
        // Arrange
        var job = CreateValidJob();
        job.ExecutionTimeoutSeconds = 0;

        // Act
        var result = job.IsValidForScheduling();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ExecutionTimeoutSeconds_WithNegative_ReturnsFalseInValidation()
    {
        // Arrange
        var job = CreateValidJob();
        job.ExecutionTimeoutSeconds = -1;

        // Act
        var result = job.IsValidForScheduling();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ExecutionTimeoutSeconds_WithLargeValue_ReturnsTrueInValidation()
    {
        // Arrange
        var job = CreateValidJob();
        job.ExecutionTimeoutSeconds = 86400; // Max allowed value

        // Act
        var result = job.IsValidForScheduling();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ExecutionTimeoutSeconds_WithExceedingMaxValue_ReturnsFalseInValidation()
    {
        // Arrange
        var job = CreateValidJob();
        job.ExecutionTimeoutSeconds = 86401; // Exceeds max allowed value

        // Act
        var result = job.IsValidForScheduling();

        // Assert
        result.Should().BeFalse();
    }
}
