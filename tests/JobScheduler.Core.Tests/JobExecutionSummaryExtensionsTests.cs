// tests/JobScheduler.Core.Tests/JobExecutionSummaryExtensionsTests.cs
using System;
using JobScheduler.Core.Domain.Models;
using Xunit;

namespace JobScheduler.Core.Tests;

public class JobExecutionSummaryExtensionsTests
{
    #region GetFailureRate

    [Fact]
    public void GetFailureRate_ReturnsCorrectPercentage()
    {
        // Arrange
        var summary = new JobExecutionSummary
        {
            TotalExecutions = 20,
            FailureCount = 5
        };

        // Act
        var rate = summary.GetFailureRate();

        // Assert
        Assert.Equal(25.0, rate);
    }

    [Fact]
    public void GetFailureRate_ReturnsZero_WhenNoExecutions()
    {
        var summary = new JobExecutionSummary
        {
            TotalExecutions = 0,
            FailureCount = 0
        };

        var rate = summary.GetFailureRate();

        Assert.Equal(0.0, rate);
    }

    [Fact]
    public void GetFailureRate_ThrowsArgumentNullException_WhenSummaryIsNull()
    {
        JobExecutionSummary? summary = null;
        Assert.Throws<ArgumentNullException>(() => summary!.GetFailureRate());
    }

    #endregion

    #region GetTimeoutCancelledRate

    [Fact]
    public void GetTimeoutCancelledRate_ReturnsCorrectPercentage()
    {
        var summary = new JobExecutionSummary
        {
            TotalExecutions = 40,
            TimedOutCount = 4,
            CancelledCount = 2
        };

        var rate = summary.GetTimeoutCancelledRate();

        // (4 + 2) / 40 * 100 = 15%
        Assert.Equal(15.0, rate);
    }

    [Fact]
    public void GetTimeoutCancelledRate_ReturnsZero_WhenNoExecutions()
    {
        var summary = new JobExecutionSummary
        {
            TotalExecutions = 0,
            TimedOutCount = 10,
            CancelledCount = 5
        };

        var rate = summary.GetTimeoutCancelledRate();

        Assert.Equal(0.0, rate);
    }

    [Fact]
    public void GetTimeoutCancelledRate_ThrowsArgumentNullException_WhenSummaryIsNull()
    {
        JobExecutionSummary? summary = null;
        Assert.Throws<ArgumentNullException>(() => summary!.GetTimeoutCancelledRate());
    }

    #endregion

    #region HasFailures

    [Fact]
    public void HasFailures_ReturnsTrue_WhenAnyFailureExists()
    {
        var summary = new JobExecutionSummary
        {
            FailureCount = 0,
            TimedOutCount = 1,
            CancelledCount = 0
        };

        Assert.True(summary.HasFailures());
    }

    [Fact]
    public void HasFailures_ReturnsFalse_WhenNoFailures()
    {
        var summary = new JobExecutionSummary
        {
            FailureCount = 0,
            TimedOutCount = 0,
            CancelledCount = 0
        };

        Assert.False(summary.HasFailures());
    }

    [Fact]
    public void HasFailures_ThrowsArgumentNullException_WhenSummaryIsNull()
    {
        JobExecutionSummary? summary = null;
        Assert.Throws<ArgumentNullException>(() => summary!.HasFailures());
    }

    #endregion

    #region GetDurationRange

    [Fact]
    public void GetDurationRange_ReturnsCorrectTuple()
    {
        var summary = new JobExecutionSummary
        {
            MinDurationMs = 120,
            MaxDurationMs = 980
        };

        var (min, max) = summary.GetDurationRange();

        Assert.Equal(120, min);
        Assert.Equal(980, max);
    }

    [Fact]
    public void GetDurationRange_ThrowsArgumentNullException_WhenSummaryIsNull()
    {
        JobExecutionSummary? summary = null;
        Assert.Throws<ArgumentNullException>(() => summary!.GetDurationRange());
    }

    #endregion

    #region GetDurationStandardDeviation

    [Fact]
    public void GetDurationStandardDeviation_ReturnsZero_WhenNoExecutions()
    {
        var summary = new JobExecutionSummary
        {
            TotalExecutions = 0,
            MinDurationMs = 100,
            MaxDurationMs = 500
        };

        var stdDev = summary.GetDurationStandardDeviation();

        Assert.Equal(0.0, stdDev);
    }

    [Fact]
    public void GetDurationStandardDeviation_ReturnsZero_WhenMinEqualsMax()
    {
        var summary = new JobExecutionSummary
        {
            TotalExecutions = 10,
            MinDurationMs = 300,
            MaxDurationMs = 300
        };

        var stdDev = summary.GetDurationStandardDeviation();

        Assert.Equal(0.0, stdDev);
    }

    [Fact]
    public void GetDurationStandardDeviation_ReturnsApproximation()
    {
        var summary = new JobExecutionSummary
        {
            TotalExecutions = 15,
            MinDurationMs = 100,
            MaxDurationMs = 700 // range = 600
        };

        var stdDev = summary.GetDurationStandardDeviation();

        // Expected approx = range / 6 = 100
        Assert.Equal(100.0, stdDev);
    }

    [Fact]
    public void GetDurationStandardDeviation_ThrowsArgumentNullException_WhenSummaryIsNull()
    {
        JobExecutionSummary? summary = null;
        Assert.Throws<ArgumentNullException>(() => summary!.GetDurationStandardDeviation());
    }

    #endregion
}
