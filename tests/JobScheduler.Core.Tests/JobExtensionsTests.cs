// tests/JobScheduler.Core.Tests/JobExtensionsTests.cs
using System;
using System.Globalization;
using JobScheduler.Core.Constants;
using JobScheduler.Core.Domain.Entities;
using Xunit;

namespace JobScheduler.Core.Tests;

public class JobExtensionsTests
{
    [Fact]
    public void IsActiveAndDueForExecution_ReturnsTrue_WhenJobIsActiveAndDueForExecution()
    {
        // Arrange
        var job = new Job
        {
            IsActive = true,
            NextExecutionAt = DateTime.UtcNow.AddMinutes(-1)
        };

        // Act
        var result = job.IsActiveAndDueForExecution();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsActiveAndDueForExecution_ReturnsFalse_WhenJobIsInactive()
    {
        // Arrange
        var job = new Job
        {
            IsActive = false,
            NextExecutionAt = DateTime.UtcNow.AddMinutes(-1)
        };

        // Act
        var result = job.IsActiveAndDueForExecution();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsActiveAndDueForExecution_ReturnsFalse_WhenJobIsNotDueForExecution()
    {
        // Arrange
        var job = new Job
        {
            IsActive = true,
            NextExecutionAt = DateTime.UtcNow.AddMinutes(1)
        };

        // Act
        var result = job.IsActiveAndDueForExecution();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetSuccessRate_ReturnsZero_WhenNoExecutions()
    {
        // Arrange
        var job = new Job();

        // Act
        var rate = job.GetSuccessRate();

        // Assert
        Assert.Equal(0.0, rate);
    }

    [Fact]
    public void GetSuccessRate_ReturnsCorrectRate_WhenExecutions()
    {
        // Arrange
        var job = new Job
        {
            TotalExecutions = 10,
            SuccessfulExecutions = 8
        };

        // Act
        var rate = job.GetSuccessRate();

        // Assert
        Assert.Equal(0.8, rate);
    }

    [Fact]
    public void GetTimeZoneInfo_ReturnsUtc_WhenTimeZoneIdIsNull()
    {
        // Arrange
        var job = new Job { TimeZoneId = null };

        // Act
        var timeZoneInfo = job.GetTimeZoneInfo();

        // Assert
        Assert.Equal(TimeZoneInfo.Utc, timeZoneInfo);
    }

    [Fact]
    public void GetTimeZoneInfo_ReturnsUtc_WhenTimeZoneIdIsEmpty()
    {
        // Arrange
        var job = new Job { TimeZoneId = string.Empty };

        // Act
        var timeZoneInfo = job.GetTimeZoneInfo();

        // Assert
        Assert.Equal(TimeZoneInfo.Utc, timeZoneInfo);
    }

    [Fact]
    public void GetTimeZoneInfo_ReturnsCorrectTimeZoneInfo_WhenTimeZoneIdIsValid()
    {
        // Arrange
        var job = new Job { TimeZoneId = "Eastern Standard Time" };

        // Act
        var timeZoneInfo = job.GetTimeZoneInfo();

        // Assert
        Assert.Equal(TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"), timeZoneInfo);
    }

    [Fact]
    public void GetSummary_ReturnsCorrectSummary_WhenJobHasAllProperties()
    {
        // Arrange
        var job = new Job
        {
            Name = "Test Job",
            Description = "This is a test job",
            CronExpression = "0 * * * *",
            HandlerType = "MyHandler",
            MaxRetries = 3,
            ExecutionTimeoutSeconds = 300,
            MaxConcurrentExecutions = 2,
            DisallowConcurrentExecution = false,
            IsActive = true,
            Status = JobStatus.Pending,
            TotalExecutions = 10,
            SuccessfulExecutions = 8,
            LastExecutedAt = DateTime.UtcNow
        };

        // Act
        var summary = job.GetSummary();

        // Assert
        Assert.Equal("Job 'Test Job' [1234567890-abcdef] - Status: Pending, Priority: Low, Executions: 10 (Success: 8) (0.8%)", summary);
    }

    [Fact]
    public void GetSummary_ReturnsCorrectSummary_WhenJobHasMissingProperties()
    {
        // Arrange
        var job = new Job
        {
            Name = "Test Job",
            Description = "This is a test job",
            CronExpression = "0 * * * *",
            HandlerType = "MyHandler",
            MaxRetries = 3,
            ExecutionTimeoutSeconds = 300,
            MaxConcurrentExecutions = 2,
            DisallowConcurrentExecution = false,
            IsActive = true,
            Status = JobStatus.Pending
        };

        // Act
        var summary = job.GetSummary();

        // Assert
        Assert.Equal("Job 'Test Job' [1234567890-abcdef] - Status: Pending, Priority: Low, Executions: 0 (Success: 0) (0.0%)", summary);
    }
}
