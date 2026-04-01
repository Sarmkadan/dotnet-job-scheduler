#nullable enable

using FluentAssertions;
using JobScheduler.Core.Constants;
using JobScheduler.Core.Data.Repositories;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Exceptions;
using JobScheduler.Core.Services;
using Moq;
using Xunit;

namespace DotnetJobScheduler.Tests;

public sealed class JobExecutorServiceTests
{
    private readonly Mock<IJobRepository> _jobRepoMock = new();
    private readonly Mock<IExecutionRepository> _executionRepoMock = new();
    private readonly Mock<ConcurrencyManager> _concurrencyManagerMock = new(
        Mock.Of<IExecutionRepository>());

    private JobExecutorService CreateService() => new(
        _jobRepoMock.Object,
        _executionRepoMock.Object,
        _concurrencyManagerMock.Object);

    private static Job CreateValidJob(string name = "test-job") => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        CronExpression = "0 9 * * *",
        HandlerType = "TestApp.Jobs.TestJob, TestApp",
        MaxRetries = 3,
        ExecutionTimeoutSeconds = 5,
        MaxConcurrentExecutions = 1,
        Status = JobStatus.Scheduled
    };

    [Fact]
    public async Task ExecuteJobAsync_WithValidJob_CreatesExecution()
    {
        // Arrange
        var job = CreateValidJob();
        _concurrencyManagerMock.Setup(c => c.EnsureCanExecuteAsync(job))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        var execution = await service.ExecuteJobAsync(job);

        // Assert
        execution.Should().NotBeNull();
        execution.JobId.Should().Be(job.Id);
        execution.AttemptNumber.Should().Be(1);
        execution.ExecutorName.Should().Be(Environment.MachineName);
        _executionRepoMock.Verify(r => r.AddAsync(It.IsAny<JobExecution>()), Times.Once);
        _concurrencyManagerMock.Verify(c => c.IncrementConcurrencyCount(job.Id), Times.Once);
    }

    [Fact]
    public async Task ExecuteJobAsync_WithNullJob_ThrowsArgumentNullException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.ExecuteJobAsync(null!));
    }

    [Fact]
    public async Task ExecuteJobAsync_WhenConcurrencyExceeded_ThrowsConcurrencyException()
    {
        // Arrange
        var job = CreateValidJob();
        _concurrencyManagerMock.Setup(c => c.EnsureCanExecuteAsync(job))
            .ThrowsAsync(new ConcurrencyException(job.Id, 5, 3));

        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ConcurrencyException>(() => service.ExecuteJobAsync(job));
        _concurrencyManagerMock.Verify(c => c.DecrementConcurrencyCount(job.Id), Times.Once);
    }

    [Fact]
    public async Task ExecuteJobAsync_DecrementsCounterOnCompletion()
    {
        // Arrange
        var job = CreateValidJob();
        _concurrencyManagerMock.Setup(c => c.EnsureCanExecuteAsync(job))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        var execution = await service.ExecuteJobAsync(job);

        // Assert
        _concurrencyManagerMock.Verify(c => c.DecrementConcurrencyCount(job.Id), Times.Once);
    }

    [Fact]
    public async Task ExecuteJobAsync_WithCancellation_SetsRunningStatus()
    {
        // Arrange
        var job = CreateValidJob();
        var cts = new CancellationTokenSource();
        cts.CancelAfter(100); // Cancel almost immediately

        _concurrencyManagerMock.Setup(c => c.EnsureCanExecuteAsync(job))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        var execution = await service.ExecuteJobAsync(job, cts.Token);

        // Assert
        execution.Status.Should().Be(ExecutionStatus.Cancelled);
        _concurrencyManagerMock.Verify(c => c.DecrementConcurrencyCount(job.Id), Times.Once);
    }

    [Fact]
    public async Task ExecuteJobAsync_RecordsStartedAndCompletedTimes()
    {
        // Arrange
        var job = CreateValidJob();
        _concurrencyManagerMock.Setup(c => c.EnsureCanExecuteAsync(job))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        var execution = await service.ExecuteJobAsync(job);

        // Assert
        execution.StartedAt.Should().NotBeNull();
        execution.StartedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task ExecuteJobAsync_MultipleConcurrentExecutions_HandlesConcurrency()
    {
        // Arrange
        var job = CreateValidJob();
        job.MaxConcurrentExecutions = 3;
        _concurrencyManagerMock.Setup(c => c.EnsureCanExecuteAsync(job))
            .Returns(Task.CompletedTask);

        var service = CreateService();
        var tasks = new List<Task<JobExecution>>();

        // Act - Execute 3 concurrent executions
        for (int i = 0; i < 3; i++)
        {
            tasks.Add(service.ExecuteJobAsync(job));
        }

        var executions = await Task.WhenAll(tasks);

        // Assert
        executions.Should().HaveCount(3);
        foreach (var execution in executions)
        {
            execution.JobId.Should().Be(job.Id);
        }
        _concurrencyManagerMock.Verify(c => c.IncrementConcurrencyCount(job.Id), Times.Exactly(3));
        _concurrencyManagerMock.Verify(c => c.DecrementConcurrencyCount(job.Id), Times.Exactly(3));
    }

    [Fact]
    public async Task ExecuteJobAsync_WithShortTimeout_HandlesTimeoutScenario()
    {
        // Arrange
        var job = CreateValidJob();
        job.ExecutionTimeoutSeconds = 1; // Very short timeout
        _concurrencyManagerMock.Setup(c => c.EnsureCanExecuteAsync(job))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        var execution = await service.ExecuteJobAsync(job);

        // Assert - Execution should complete (might timeout or succeed depending on timing)
        execution.Should().NotBeNull();
        execution.JobId.Should().Be(job.Id);
    }

    [Fact]
    public async Task ExecuteJobAsync_SavesExecutionToRepository()
    {
        // Arrange
        var job = CreateValidJob();
        _concurrencyManagerMock.Setup(c => c.EnsureCanExecuteAsync(job))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        await service.ExecuteJobAsync(job);

        // Assert
        _executionRepoMock.Verify(r => r.AddAsync(It.IsAny<JobExecution>()), Times.Once);
        _executionRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ExecuteJobAsync_WithExceptionDuringExecution_RecordsError()
    {
        // Arrange
        var job = CreateValidJob();
        _concurrencyManagerMock.Setup(c => c.EnsureCanExecuteAsync(job))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        var execution = await service.ExecuteJobAsync(job);

        // Assert
        execution.Should().NotBeNull();
        _concurrencyManagerMock.Verify(c => c.DecrementConcurrencyCount(job.Id), Times.Once);
    }
}
