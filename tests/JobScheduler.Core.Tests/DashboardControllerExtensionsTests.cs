#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JobScheduler.Core.Constants;
using JobScheduler.Core.Controllers;
using JobScheduler.Core.Data.Repositories;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JobScheduler.Core.Tests;

/// <summary>
/// Contains unit tests for <see cref="DashboardControllerExtensions"/>, exercised through a real
/// <see cref="DashboardController"/> built on real services backed by mocked repositories.
/// </summary>
public sealed class DashboardControllerExtensionsTests
{
    private readonly Mock<IJobRepository> _jobRepoMock = new();
    private readonly Mock<IExecutionRepository> _executionRepoMock = new();
    private readonly List<Job> _jobs = new();
    private readonly List<JobExecution> _executions = new();

    private DashboardController CreateController()
    {
        _jobRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(_jobs);
        _executionRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(_executions);

        var concurrencyManager = new ConcurrencyManager(_executionRepoMock.Object);
        var executorService = new JobExecutorService(_jobRepoMock.Object, _executionRepoMock.Object, concurrencyManager);
        var schedulerService = new JobSchedulerService(
            _jobRepoMock.Object,
            _executionRepoMock.Object,
            executorService,
            new CronExpressionService(),
            new RetryService(_jobRepoMock.Object, _executionRepoMock.Object),
            concurrencyManager,
            null);
        return new DashboardController(
            schedulerService,
            new PerformanceMonitor(Mock.Of<ILogger<PerformanceMonitor>>()),
            new JobHistoryService(_executionRepoMock.Object, _jobRepoMock.Object),
            Mock.Of<ILogger<DashboardController>>());
    }

    private static Job CreateJob(string name = "Job", int total = 0, int successful = 0, int failed = 0, JobStatus status = JobStatus.Completed) => new()
    {
        Name = name, CronExpression = "0 * * * *", HandlerType = "Test.Handler", Status = status,
        TotalExecutions = total, SuccessfulExecutions = successful, FailedExecutions = failed
    };

    /// <summary>
    /// Verifies that the health score subtracts the failed-job penalty from the average success rate.
    /// One job at 80% success averages with one failed job at 0% to 40, minus 2 for the single failed job, giving 38.
    /// </summary>
    [Fact]
    public async Task CalculateHealthScoreAsync_SubtractsFailedJobPenalty()
    {
        _jobs.Add(CreateJob(total: 10, successful: 8));
        _jobs.Add(CreateJob(status: JobStatus.Failed));
        var controller = CreateController();

        var score = await controller.CalculateHealthScoreAsync();

        Assert.Equal(38, score);
    }

    /// <summary>
    /// Verifies that an empty system yields a health score of zero rather than throwing.
    /// </summary>
    [Fact]
    public async Task CalculateHealthScoreAsync_ReturnsZero_WhenNoJobsExist()
    {
        var controller = CreateController();

        var score = await controller.CalculateHealthScoreAsync();

        Assert.Equal(0, score);
    }

    /// <summary>
    /// Verifies that <see cref="DashboardControllerExtensions.GetHealthStatusAsync"/> maps the score to the correct band,
    /// including the boundary at 50 (Warning) and the all-success case (Good).
    /// </summary>
    [Theory]
    [InlineData(10, "Good", "green")]
    [InlineData(6, "Warning", "yellow")]
    [InlineData(5, "Warning", "yellow")]
    [InlineData(4, "Critical", "red")]
    public async Task GetHealthStatusAsync_MapsScoreToStatusBand(int successful, string expectedStatus, string expectedColor)
    {
        _jobs.Add(CreateJob(total: 10, successful: successful));
        var controller = CreateController();

        var (status, color) = await controller.GetHealthStatusAsync();

        Assert.Equal(expectedStatus, status);
        Assert.Equal(expectedColor, color);
    }

    /// <summary>
    /// Verifies that formatted statistics render counts, durations, and connectivity as display strings.
    /// </summary>
    [Fact]
    public async Task GetFormattedStatisticsAsync_FormatsDisplayValues()
    {
        _jobs.Add(CreateJob(total: 10, successful: 10));
        var controller = CreateController();

        var stats = await controller.GetFormattedStatisticsAsync();

        Assert.Equal("1", stats["Total Jobs"]);
        Assert.Equal("1", stats["Active Jobs"]);
        Assert.Equal("0", stats["Pending"]);
        Assert.Equal("0ms", stats["Avg Execution"]);
        Assert.Equal("0m", stats["Time to Empty"]);
        Assert.Equal("Connected", stats["Database"]);
        Assert.True(stats.ContainsKey("Last Updated"));
    }

    /// <summary>
    /// Verifies that the top-N analysis returns only the requested count, ordered by failures,
    /// and that <c>IsSlow</c> reflects membership in the top-10 slowest jobs.
    /// </summary>
    [Fact]
    public async Task GetTopFailingJobsWithAnalysisAsync_ReturnsTopCountWithSlowFlag()
    {
        // Job i fails (11 - i) times and runs for (i + 1) * 100 ms. Job 0 is the fastest of the 11, so it
        // falls outside the slowest-10 list (not slow), while job 4 is inside it (slow).
        for (var i = 0; i < 11; i++)
        {
            var job = CreateJob(name: $"Job{i}", failed: 11 - i);
            _jobs.Add(job);
            _executions.Add(new JobExecution
            {
                JobId = job.Id,
                Status = ExecutionStatus.Success,
                DurationMilliseconds = (i + 1) * 100
            });
        }
        var controller = CreateController();

        var result = await controller.GetTopFailingJobsWithAnalysisAsync(count: 5);

        Assert.Equal(5, result.Count);
        Assert.Equal(_jobs[0].Id, result[0].JobId);
        Assert.Equal(11, result[0].FailedCount);
        Assert.Equal(100, result[0].FailureRate);
        Assert.Equal(11, result[0].FailureImpactScore);
        Assert.Equal("Low Impact", result[0].Status);
        Assert.False(result[0].IsSlow);
        Assert.True(result[4].IsSlow);
    }

    /// <summary>
    /// Verifies that failure impact is classified by thresholds: impact greater than 20 is Medium and greater than 50 is High.
    /// A single job with zero executions has a 100% failure rate, so its impact equals its failed count.
    /// </summary>
    [Theory]
    [InlineData(20, "Low Impact")]
    [InlineData(21, "Medium Impact")]
    [InlineData(50, "Medium Impact")]
    [InlineData(51, "High Impact")]
    public async Task GetTopFailingJobsWithAnalysisAsync_ClassifiesImpactAtThresholds(int failedCount, string expectedStatus)
    {
        _jobs.Add(CreateJob(failed: failedCount));
        var controller = CreateController();

        var result = await controller.GetTopFailingJobsWithAnalysisAsync();

        Assert.Single(result);
        Assert.Equal(expectedStatus, result[0].Status);
    }

    /// <summary>
    /// Verifies that a null controller is rejected with <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public async Task CalculateHealthScoreAsync_ThrowsArgumentNullException_WhenControllerIsNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => DashboardControllerExtensions.CalculateHealthScoreAsync(null!));
    }

    /// <summary>
    /// Verifies that requesting fewer than one job is rejected with <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    [Fact]
    public async Task GetTopFailingJobsWithAnalysisAsync_ThrowsArgumentOutOfRange_WhenCountIsZero()
    {
        var controller = CreateController();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => controller.GetTopFailingJobsWithAnalysisAsync(0));
    }
}
