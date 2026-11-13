#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using JobScheduler.Core.Constants;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Exceptions;
using Xunit;

namespace JobScheduler.Core.Tests;

/// <summary>
/// Tests for the Job class.
/// </summary>
public class JobTests
{
    /// <summary>
    /// Verifies that IsValidForScheduling returns true when all job properties are valid.
    /// </summary>
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

    /// <summary>
    /// Verifies that IsValidForScheduling returns false when the job name exceeds the maximum allowed length.
    /// </summary>
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

    /// <summary>
    /// Verifies that UpdateExecutionMetrics increments the appropriate counters and sets the last execution timestamp.
    /// </summary>
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

    /// <summary>
    /// Verifies that GetSuccessRate returns zero when no executions have been recorded.
    /// </summary>
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

    /// <summary>
    /// Verifies that CanExecuteNow returns false when concurrent execution is disallowed and the job is already running.
    /// </summary>
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

    /// <summary>
    /// Verifies that GetEffectiveRetryPolicy returns a default retry policy when the job's retry policy is null.
    /// </summary>
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

    /// <summary>
    /// Verifies that CalculateEffectivePriority applies aging bonus and caps at Critical priority level.
    /// </summary>
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

    /// <summary>
    /// Verifies that EnsureValid throws JobValidationException when job ID is empty.
    /// </summary>
    [Fact]
    public void EnsureValid_ThrowsJobValidationException_WhenIdIsEmpty()
    {
        // Arrange
        var job = new Job
        {
            Id = Guid.Empty,
            Name = "Test Job",
            CronExpression = "0 * * * *",
            HandlerType = "TestHandler",
            ExecutionTimeoutSeconds = 300
        };

        // Act & Assert
        var exception = Assert.Throws<JobValidationException>(() => job.EnsureValid());
        Assert.Equal("Job Id cannot be empty", exception.Message);
        Assert.Equal(nameof(job.Id), exception.PropertyName);
    }

    /// <summary>
    /// Verifies that EnsureValid throws JobValidationException when job name is empty or whitespace.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EnsureValid_ThrowsJobValidationException_WhenNameIsEmptyOrWhitespace(string name)
    {
        // Arrange
        var job = new Job
        {
            Id = Guid.NewGuid(),
            Name = name,
            CronExpression = "0 * * * *",
            HandlerType = "TestHandler",
            ExecutionTimeoutSeconds = 300
        };

        // Act & Assert
        var exception = Assert.Throws<JobValidationException>(() => job.EnsureValid());
        Assert.Equal("Job Name cannot be empty or whitespace", exception.Message);
        Assert.Equal(nameof(job.Name), exception.PropertyName);
    }

    /// <summary>
    /// Verifies that EnsureValid throws JobValidationException when cron expression is empty or whitespace.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EnsureValid_ThrowsJobValidationException_WhenCronExpressionIsEmptyOrWhitespace(string cronExpression)
    {
        // Arrange
        var job = new Job
        {
            Id = Guid.NewGuid(),
            Name = "Test Job",
            CronExpression = cronExpression,
            HandlerType = "TestHandler",
            ExecutionTimeoutSeconds = 300
        };

        // Act & Assert
        var exception = Assert.Throws<JobValidationException>(() => job.EnsureValid());
        Assert.Equal("CronExpression cannot be empty or whitespace", exception.Message);
        Assert.Equal(nameof(job.CronExpression), exception.PropertyName);
    }

    /// <summary>
    /// Verifies that EnsureValid throws JobValidationException when handler type is empty or whitespace.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EnsureValid_ThrowsJobValidationException_WhenHandlerTypeIsEmptyOrWhitespace(string handlerType)
    {
        // Arrange
        var job = new Job
        {
            Id = Guid.NewGuid(),
            Name = "Test Job",
            CronExpression = "0 * * * *",
            HandlerType = handlerType,
            ExecutionTimeoutSeconds = 300
        };

        // Act & Assert
        var exception = Assert.Throws<JobValidationException>(() => job.EnsureValid());
        Assert.Equal("HandlerType cannot be empty or whitespace", exception.Message);
        Assert.Equal(nameof(job.HandlerType), exception.PropertyName);
    }

    /// <summary>
    /// Verifies that EnsureValid throws JobValidationException when execution timeout is not positive.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void EnsureValid_ThrowsJobValidationException_WhenExecutionTimeoutIsNotPositive(int timeout)
    {
        // Arrange
        var job = new Job
        {
            Id = Guid.NewGuid(),
            Name = "Test Job",
            CronExpression = "0 * * * *",
            HandlerType = "TestHandler",
            ExecutionTimeoutSeconds = timeout
        };

        // Act & Assert
        var exception = Assert.Throws<JobValidationException>(() => job.EnsureValid());
        Assert.Equal($"ExecutionTimeoutSeconds must be > 0. Current value: {timeout}", exception.Message);
        Assert.Equal(nameof(job.ExecutionTimeoutSeconds), exception.PropertyName);
    }

    /// <summary>
    /// Verifies that EnsureValid throws JobValidationException when max concurrent executions is less than 1.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void EnsureValid_ThrowsJobValidationException_WhenMaxConcurrentExecutionsIsLessThan1(int maxConcurrent)
    {
        // Arrange
        var job = new Job
        {
            Id = Guid.NewGuid(),
            Name = "Test Job",
            CronExpression = "0 * * * *",
            HandlerType = "TestHandler",
            ExecutionTimeoutSeconds = 300,
            MaxConcurrentExecutions = maxConcurrent
        };

        // Act & Assert
        var exception = Assert.Throws<JobValidationException>(() => job.EnsureValid());
        Assert.Equal($"MaxConcurrentExecutions must be >= 1. Current value: {maxConcurrent}", exception.Message);
        Assert.Equal(nameof(job.MaxConcurrentExecutions), exception.PropertyName);
    }

    /// <summary>
    /// Verifies that EnsureValid throws JobValidationException when retry policy is invalid.
    /// </summary>
    [Fact]
    public void EnsureValid_ThrowsJobValidationException_WhenRetryPolicyIsInvalid()
    {
        // Arrange
        var job = new Job
        {
            Id = Guid.NewGuid(),
            Name = "Test Job",
            CronExpression = "0 * * * *",
            HandlerType = "TestHandler",
            ExecutionTimeoutSeconds = 300,
            RetryPolicy = new RetryPolicy
            {
                MaxRetries = -1,
                InitialBackoffSeconds = 5,
                MaxBackoffSeconds = 300,
                BackoffMultiplier = 2.0
            }
        };

        // Act & Assert
        var exception = Assert.Throws<JobValidationException>(() => job.EnsureValid());
        Assert.Equal($"MaxRetries must be >= 0. Current value: -1", exception.Message);
        Assert.Equal(nameof(job.RetryPolicy.MaxRetries), exception.PropertyName);
    }

    /// <summary>
    /// Verifies that EnsureValid does not throw when all job properties are valid.
    /// </summary>
    [Fact]
    public void EnsureValid_DoesNotThrow_WhenAllPropertiesAreValid()
    {
        // Arrange
        var job = new Job
        {
            Id = Guid.NewGuid(),
            Name = "Test Job",
            CronExpression = "0 * * * *",
            HandlerType = "TestHandler",
            ExecutionTimeoutSeconds = 300,
            MaxConcurrentExecutions = 2,
            RetryPolicy = new RetryPolicy
            {
                MaxRetries = 3,
                InitialBackoffSeconds = 5,
                MaxBackoffSeconds = 300,
                BackoffMultiplier = 2.0
            }
        };

        // Act & Assert
        job.EnsureValid(); // Should not throw
    }
}
