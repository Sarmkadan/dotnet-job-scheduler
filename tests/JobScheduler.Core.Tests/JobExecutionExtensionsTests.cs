using System;
using JobScheduler.Core.Domain.Entities;
using Xunit;

namespace JobScheduler.Core.Tests;

public class JobExecutionExtensionsTests
{
    private static readonly DateTime Started = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void IsOverdue_CompletedAboveLimit_ReturnsTrue()
    {
        // Arrange
        var execution = new JobExecution { StartedAt = Started, CompletedAt = Started.AddMinutes(90) };

        // Act
        var result = execution.IsOverdue(60);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsOverdue_DurationExactlyAtLimit_ReturnsFalse()
    {
        // WHY: the comparison is strict (>), so reaching the limit is not overdue
        var execution = new JobExecution { StartedAt = Started, CompletedAt = Started.AddMinutes(60) };

        // Act
        var result = execution.IsOverdue(60);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsOverdue_RunningExecutionStartedLongAgo_ReturnsTrue()
    {
        // WHY: no CompletedAt, so the duration is measured against UtcNow
        var execution = new JobExecution { StartedAt = DateTime.UtcNow.AddHours(-2), CompletedAt = null };

        // Act & Assert
        Assert.True(execution.IsOverdue());
    }

    [Fact]
    public void IsOverdue_NullExecutionOrNonPositiveLimit_ThrowsExpectedExceptions()
    {
        JobExecution? nullExecution = null;
        var execution = new JobExecution { StartedAt = Started, CompletedAt = Started.AddMinutes(1) };

        Assert.Throws<ArgumentNullException>(() => nullExecution!.IsOverdue());
        Assert.Throws<ArgumentOutOfRangeException>(() => execution.IsOverdue(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => execution.IsOverdue(-1));
    }

    [Fact]
    public void ShouldRetry_RetryableBelowLimit_ReturnsTrueAndAtLimitOrNotRetryableReturnsFalse()
    {
        // WHY: call the extension explicitly; JobExecution.ShouldRetry(int) is an instance method
        // with a different (<=) comparison and would win overload resolution for an explicit argument.
        var belowLimit = new JobExecution { IsRetryable = true, AttemptNumber = 2 };
        var atLimit = new JobExecution { IsRetryable = true, AttemptNumber = 3 };
        var notRetryable = new JobExecution { IsRetryable = false, AttemptNumber = 1 };

        Assert.True(JobExecutionExtensions.ShouldRetry(belowLimit, 3));
        Assert.False(JobExecutionExtensions.ShouldRetry(atLimit, 3));
        Assert.False(notRetryable.ShouldRetry());
    }

    [Fact]
    public void ShouldRetry_NullExecutionOrNonPositiveLimit_ThrowsExpectedExceptions()
    {
        JobExecution? nullExecution = null;
        var execution = new JobExecution { IsRetryable = true };

        Assert.Throws<ArgumentNullException>(() => nullExecution!.ShouldRetry());
        Assert.Throws<ArgumentOutOfRangeException>(() => JobExecutionExtensions.ShouldRetry(execution, 0));
    }

    [Fact]
    public void GetExecutionRate_CompletedAfterFourSeconds_ReturnsQuarterPerSecond()
    {
        var execution = new JobExecution { StartedAt = Started, CompletedAt = Started.AddSeconds(4) };

        var result = execution.GetExecutionRate();

        Assert.Equal(0.25, result);
    }

    [Fact]
    public void GetExecutionRate_NotCompletedZeroDurationOrNull_ReturnsZeroOrThrows()
    {
        var running = new JobExecution { StartedAt = Started, CompletedAt = null };
        var instant = new JobExecution { StartedAt = Started, CompletedAt = Started };
        JobExecution? nullExecution = null;

        Assert.Equal(0, running.GetExecutionRate());
        Assert.Equal(0, instant.GetExecutionRate());
        Assert.Throws<ArgumentNullException>(() => nullExecution!.GetExecutionRate());
    }
}
