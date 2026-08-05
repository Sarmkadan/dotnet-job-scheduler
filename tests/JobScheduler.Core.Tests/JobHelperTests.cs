#nullable enable
using System;
using JobScheduler.Core.Constants;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Utilities;
using Xunit;

namespace JobScheduler.Core.Tests;

public sealed class JobHelperTests
{
    [Fact]
    public void SerializeParameters_WithNull_ReturnsEmptyString()
    {
        var result = JobHelper.SerializeParameters(null);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void SerializeParameters_ThenDeserializeParameters_RoundTripsValue()
    {
        var payload = new { Name = "test", Count = 3 };
        var json = JobHelper.SerializeParameters(payload);

        Assert.Contains("test", json);

        var deserialized = JobHelper.DeserializeParameters<TestParameters>(json);

        Assert.NotNull(deserialized);
        Assert.Equal("test", deserialized!.Name);
        Assert.Equal(3, deserialized.Count);
    }

    [Fact]
    public void DeserializeParameters_WithNullOrWhitespace_ReturnsDefault()
    {
        Assert.Null(JobHelper.DeserializeParameters<TestParameters>(null));
        Assert.Null(JobHelper.DeserializeParameters<TestParameters>(string.Empty));
        Assert.Null(JobHelper.DeserializeParameters<TestParameters>("   "));
    }

    [Fact]
    public void DeserializeParameters_WithInvalidJson_ReturnsDefault()
    {
        var result = JobHelper.DeserializeParameters<TestParameters>("not valid json");
        Assert.Null(result);
    }

    [Fact]
    public void GetJobStatusDescription_WithNullJob_ReturnsUnknown()
    {
        var result = JobHelper.GetJobStatusDescription(null!);
        Assert.Equal("Unknown", result);
    }

    [Theory]
    [InlineData(JobStatus.Pending, "Awaiting scheduling")]
    [InlineData(JobStatus.Running, "Currently executing")]
    [InlineData(JobStatus.Cancelled, "Cancelled")]
    [InlineData(JobStatus.FailedPermanently, "Failed permanently - manual intervention needed")]
    public void GetJobStatusDescription_WithVariousStatuses_ReturnsExpectedDescription(JobStatus status, string expected)
    {
        var job = new Job { Status = status };
        Assert.Equal(expected, JobHelper.GetJobStatusDescription(job));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("NoComma", false)]
    [InlineData("Namespace.ClassName", false)]
    [InlineData(",AssemblyName", false)]
    [InlineData("Namespace.ClassName,", false)]
    [InlineData("Namespace.ClassName, AssemblyName", true)]
    public void IsValidHandlerType_WithVariousInputs_ReturnsExpectedResult(string? handlerType, bool expected)
    {
        Assert.Equal(expected, JobHelper.IsValidHandlerType(handlerType));
    }

    [Theory]
    [InlineData("", "Never")]
    [InlineData(null, "Never")]
    [InlineData("* * * * *", "Every minute")]
    [InlineData("0 * * * *", "Every hour")]
    [InlineData("0 0 * * *", "Daily at midnight")]
    [InlineData("*/15 * * * *", "Custom schedule")]
    public void GetExecutionFrequencyDescription_WithVariousExpressions_ReturnsExpectedDescription(string? cronExpression, string expected)
    {
        Assert.Equal(expected, JobHelper.GetExecutionFrequencyDescription(cronExpression!));
    }

    [Fact]
    public void CalculateReliabilityScore_WithNoExecutions_ReturnsFifty()
    {
        var job = new Job { TotalExecutions = 0 };
        Assert.Equal(50, JobHelper.CalculateReliabilityScore(job));
    }

    [Fact]
    public void CalculateReliabilityScore_WithAllSuccessfulExecutions_ReturnsHighScore()
    {
        var job = new Job { TotalExecutions = 10, SuccessfulExecutions = 10, FailedExecutions = 0 };
        var score = JobHelper.CalculateReliabilityScore(job);

        Assert.InRange(score, 0, 100);
        Assert.Equal(70, score);
    }

    [Fact]
    public void CalculateReliabilityScore_WithHighFailureRate_ReturnsLowerScoreAndStaysWithinBounds()
    {
        var job = new Job { TotalExecutions = 10, SuccessfulExecutions = 1, FailedExecutions = 9 };
        var score = JobHelper.CalculateReliabilityScore(job);

        Assert.InRange(score, 0, 100);
    }

    [Fact]
    public void GetRecommendedAction_WithFailedPermanently_ReturnsReviewMessage()
    {
        var job = new Job { Status = JobStatus.FailedPermanently };
        Assert.Equal(
            "Review job configuration and error details. Fix and reactivate if needed.",
            JobHelper.GetRecommendedAction(job));
    }

    [Fact]
    public void GetRecommendedAction_WithHealthyJob_ReturnsNormalMessage()
    {
        var job = new Job
        {
            Status = JobStatus.Completed,
            TotalExecutions = 10,
            SuccessfulExecutions = 10,
            FailedExecutions = 0,
            ExecutionTimeoutSeconds = SchedulerConstants.DefaultExecutionTimeoutSeconds
        };

        Assert.Equal("Job is operating normally.", JobHelper.GetRecommendedAction(job));
    }

    [Theory]
    [InlineData(-1, "Invalid")]
    [InlineData(0, "0ms")]
    [InlineData(500, "500ms")]
    [InlineData(1500, "1.50s")]
    [InlineData(90000, "1.50m")]
    [InlineData(7200000, "2.00h")]
    public void FormatDuration_WithVariousValues_ReturnsExpectedFormat(long milliseconds, string expected)
    {
        Assert.Equal(expected, JobHelper.FormatDuration(milliseconds));
    }

    [Fact]
    public void IsConcerning_WithFailedPermanently_ReturnsTrue()
    {
        var job = new Job { Status = JobStatus.FailedPermanently };
        Assert.True(JobHelper.IsConcerning(job));
    }

    [Fact]
    public void IsConcerning_WithHealthyJob_ReturnsFalse()
    {
        var job = new Job
        {
            Status = JobStatus.Completed,
            TotalExecutions = 10,
            SuccessfulExecutions = 10,
            FailedExecutions = 0
        };

        Assert.False(JobHelper.IsConcerning(job));
    }

    [Fact]
    public void IsConcerning_WithLowSuccessRateAndEnoughExecutions_ReturnsTrue()
    {
        var job = new Job
        {
            Status = JobStatus.Completed,
            TotalExecutions = 10,
            SuccessfulExecutions = 2,
            FailedExecutions = 8
        };

        Assert.True(JobHelper.IsConcerning(job));
    }

    private sealed class TestParameters
    {
        public string? Name { get; set; }
        public int Count { get; set; }
    }
}
