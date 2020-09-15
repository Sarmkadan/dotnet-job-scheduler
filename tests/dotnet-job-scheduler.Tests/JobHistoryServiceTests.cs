#nullable enable

using FluentAssertions;
using JobScheduler.Core.Constants;
using JobScheduler.Core.Data.Repositories;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Domain.Models;
using JobScheduler.Core.Exceptions;
using JobScheduler.Core.Services;
using Moq;
using Xunit;

namespace DotnetJobScheduler.Tests;

public sealed class JobHistoryServiceTests
{
    private readonly Mock<IExecutionRepository> _executionRepoMock = new();
    private readonly Mock<IJobRepository> _jobRepoMock = new();

    private JobHistoryService CreateService() =>
        new(_executionRepoMock.Object, _jobRepoMock.Object);

    private static Job CreateJob(string name = "test-job") => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        CronExpression = "0 9 * * *",
        HandlerType = "TestApp.Jobs.TestJob, TestApp"
    };

    private static JobExecution CreateExecution(Guid jobId, ExecutionStatus status = ExecutionStatus.Success) => new()
    {
        Id = Guid.NewGuid(),
        JobId = jobId,
        Status = status,
        StartedAt = DateTime.UtcNow.AddMinutes(-5),
        CompletedAt = DateTime.UtcNow,
        DurationMilliseconds = 300
    };

    [Fact]
    public async Task GetJobHistoryAsync_WithValidJobAndExecutions_ReturnsPaginatedHistory()
    {
        // Arrange
        var job = CreateJob();
        var executions = new List<JobExecution>
        {
            CreateExecution(job.Id, ExecutionStatus.Success),
            CreateExecution(job.Id, ExecutionStatus.Failed),
            CreateExecution(job.Id, ExecutionStatus.Success)
        };

        _jobRepoMock.Setup(r => r.GetByIdAsync(job.Id)).ReturnsAsync(job);
        _executionRepoMock.Setup(r => r.GetExecutionsByJobAsync(job.Id))
            .ReturnsAsync(executions);

        var service = CreateService();

        // Act
        var result = await service.GetJobHistoryAsync(job.Id);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(3);
        result.Items.Should().HaveCount(3);
        result.PageNumber.Should().Be(1);
    }

    [Fact]
    public async Task GetJobHistoryAsync_WithStatusFilter_ReturnsFilteredRecords()
    {
        // Arrange
        var job = CreateJob();
        var executions = new List<JobExecution>
        {
            CreateExecution(job.Id, ExecutionStatus.Success),
            CreateExecution(job.Id, ExecutionStatus.Failed),
            CreateExecution(job.Id, ExecutionStatus.Success)
        };

        _jobRepoMock.Setup(r => r.GetByIdAsync(job.Id)).ReturnsAsync(job);
        _executionRepoMock.Setup(r => r.GetExecutionsByJobAsync(job.Id))
            .ReturnsAsync(executions);

        var service = CreateService();
        var query = new JobHistoryQuery { Status = ExecutionStatus.Failed };

        // Act
        var result = await service.GetJobHistoryAsync(job.Id, query);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Items.Should().AllSatisfy(e => e.Status.Should().Be(ExecutionStatus.Failed.ToString()));
    }

    [Fact]
    public async Task GetJobHistoryAsync_WithNonExistentJob_ThrowsJobNotFoundException()
    {
        // Arrange
        _jobRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Job?)null);
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<JobNotFoundException>(
            () => service.GetJobHistoryAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetJobHistoryAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var job = CreateJob();
        var executions = Enumerable.Range(0, 10)
            .Select(_ => CreateExecution(job.Id, ExecutionStatus.Success))
            .ToList();

        _jobRepoMock.Setup(r => r.GetByIdAsync(job.Id)).ReturnsAsync(job);
        _executionRepoMock.Setup(r => r.GetExecutionsByJobAsync(job.Id))
            .ReturnsAsync(executions);

        var service = CreateService();
        var query = new JobHistoryQuery { PageNumber = 2, PageSize = 3 };

        // Act
        var result = await service.GetJobHistoryAsync(job.Id, query);

        // Assert
        result.TotalCount.Should().Be(10);
        result.Items.Should().HaveCount(3);
        result.PageNumber.Should().Be(2);
        result.TotalPages.Should().Be(4);
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task GetJobSummaryAsync_WithExecutions_ReturnsAccurateStatistics()
    {
        // Arrange
        var job = CreateJob();
        var executions = new List<JobExecution>
        {
            new() { Id = Guid.NewGuid(), JobId = job.Id, Status = ExecutionStatus.Success, StartedAt = DateTime.UtcNow.AddHours(-2), DurationMilliseconds = 100 },
            new() { Id = Guid.NewGuid(), JobId = job.Id, Status = ExecutionStatus.Success, StartedAt = DateTime.UtcNow.AddHours(-1), DurationMilliseconds = 200 },
            new() { Id = Guid.NewGuid(), JobId = job.Id, Status = ExecutionStatus.Failed, StartedAt = DateTime.UtcNow.AddMinutes(-30), DurationMilliseconds = 50 }
        };

        _jobRepoMock.Setup(r => r.GetByIdAsync(job.Id)).ReturnsAsync(job);
        _executionRepoMock.Setup(r => r.GetExecutionsByJobAsync(job.Id))
            .ReturnsAsync(executions);

        var service = CreateService();

        // Act
        var summary = await service.GetJobSummaryAsync(job.Id);

        // Assert
        summary.Should().NotBeNull();
        summary.JobId.Should().Be(job.Id);
        summary.TotalExecutions.Should().Be(3);
        summary.SuccessCount.Should().Be(2);
        summary.FailureCount.Should().Be(1);
        summary.SuccessRate.Should().BeApproximately(66.67, 0.1);
        summary.AverageDurationMs.Should().Be(116); // (100+200+50)/3 ≈ 116
        summary.MinDurationMs.Should().Be(50);
        summary.MaxDurationMs.Should().Be(200);
    }

    [Fact]
    public async Task GetJobSummaryAsync_WithNoExecutions_ReturnsZeroStats()
    {
        // Arrange
        var job = CreateJob();
        _jobRepoMock.Setup(r => r.GetByIdAsync(job.Id)).ReturnsAsync(job);
        _executionRepoMock.Setup(r => r.GetExecutionsByJobAsync(job.Id))
            .ReturnsAsync(new List<JobExecution>());

        var service = CreateService();

        // Act
        var summary = await service.GetJobSummaryAsync(job.Id);

        // Assert
        summary.TotalExecutions.Should().Be(0);
        summary.SuccessRate.Should().Be(0);
    }

    [Fact]
    public async Task GetJobSummaryAsync_WithNonExistentJob_ThrowsJobNotFoundException()
    {
        // Arrange
        _jobRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Job?)null);
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<JobNotFoundException>(
            () => service.GetJobSummaryAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task PagedResult_Properties_AreComputedCorrectly()
    {
        // Arrange & Act
        var result = new PagedResult<string>(
            new[] { "a", "b", "c" }, totalCount: 25, pageNumber: 3, pageSize: 3);

        // Assert
        result.TotalPages.Should().Be(9);
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeTrue();
        result.Items.Should().HaveCount(3);
    }

    [Fact]
    public void JobHistoryQuery_Normalize_ClampsPageSize()
    {
        // Arrange
        var query = new JobHistoryQuery { PageSize = 500, PageNumber = 0 };

        // Act
        var normalized = query.Normalize();

        // Assert
        normalized.PageSize.Should().Be(200);
        normalized.PageNumber.Should().Be(1);
    }
}
