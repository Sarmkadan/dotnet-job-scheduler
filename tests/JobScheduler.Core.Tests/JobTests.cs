// tests/JobScheduler.Core.Tests/JobTests.cs
using System;
using JobScheduler.Core.Constants;
using JobScheduler.Core.Domain.Entities;
using Xunit;

namespace JobScheduler.Core.Tests;

public class JobTests
{
    [Fact]
    public void IsValidForScheduling_ReturnsTrue_WhenAllPropertiesAreValid()
    {
        // Arrange
        var job = new Job
        {
            Name = "ValidJob",
            Description = "A valid job description",
            CronExpression = "0 * * * *", // every hour
            HandlerType = "MyNamespace.MyHandler",
            MaxRetries = 3,
            ExecutionTimeoutSeconds = 300,
            MaxConcurrentExecutions = 2,
            DisallowConcurrentExecution = false,
            IsActive = true,
            Status = JobStatus.Pending
        };

        // Act
        var result = job.IsValidForScheduling();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidForScheduling_ReturnsFalse_WhenNameIsTooLong()
    {
        // Arrange
        var job = new Job
        {
            Name = new string('a', SchedulerConstants.MaxJobNameLength + 1),
            CronExpression = "0 * * * *",
            HandlerType = "Handler",
            MaxRetries = 0,
            ExecutionTimeoutSeconds = 60,
            MaxConcurrentExecutions = 1
        };

        // Act
        var result = job.IsValidForScheduling();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void UpdateExecutionMetrics_IncrementsCounters_AndSetsLastExecutedAt()
    {
        // Arrange
        var job = new Job
        {
            TotalExecutions = 5,
            SuccessfulExecutions = 3,
            FailedExecutions = 2,
            LastExecutedAt = null
        };

        // Act
        job.UpdateExecutionMetrics(success: true);

        // Assert
        Assert.Equal(6, job.TotalExecutions);
        Assert.Equal(4, job.SuccessfulExecutions);
        Assert.Equal(2, job.FailedExecutions);
        Assert.NotNull(job.LastExecutedAt);
        Assert.True((DateTime.UtcNow - job.LastExecutedAt!.Value).TotalSeconds < 5);
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
    public void CanExecuteNow_ReturnsFalse_WhenDisallowConcurrentAndAlreadyRunning()
    {
        // Arrange
        var job = new Job
        {
            DisallowConcurrentExecution = true,
            IsActive = true,
            Status = JobStatus.Pending
        };

        // Act
        var canExecute = job.CanExecuteNow(currentConcurrentCount: 1);

        // Assert
        Assert.False(canExecute);
    }

    [Fact]
    public void GetEffectiveRetryPolicy_ReturnsDefault_WhenRetryPolicyIsNull()
    {
        // Arrange
        var job = new Job
        {
            MaxRetries = 2,
            RetryBackoffSeconds = 10,
            RetryPolicy = null
        };

        // Act
        var policy = job.GetEffectiveRetryPolicy();

        // Assert
        Assert.NotNull(policy);
        Assert.Equal(job.Id, policy.JobId);
        Assert.Equal(job.MaxRetries, policy.MaxRetries);
        Assert.Equal(job.RetryBackoffSeconds, policy.InitialBackoffSeconds);
        Assert.Equal(BackoffStrategy.Exponential, policy.Strategy);
    }

    [Fact]
    public void CalculateEffectivePriority_AppliesAgingBonus_AndCapsAtCritical()
    {
        // Arrange
        var now = new DateTime(2024, 01, 01, 12, 00, 00, DateTimeKind.Utc);
        var job = new Job
        {
            Priority = JobPriority.Low,
            NextExecutionAt = now.AddMinutes(-30) // overdue by 30 minutes
        };

        // Act
        var effective = job.CalculateEffectivePriority(now, agingRateMinutesPerLevel: 5.0);

        // The overdue minutes give a bonus of 6 levels, but the max allowed is Critical (4)
        var expected = (int)JobPriority.Critical; // capped

        // Assert
        Assert.Equal(expected, effective);
    }
}
