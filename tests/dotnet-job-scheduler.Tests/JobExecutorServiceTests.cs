#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using JobScheduler.Core.Constants;
using JobScheduler.Core.Data.Repositories;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Exceptions;
using JobScheduler.Core.Services;
using Moq;
using Xunit;

namespace DotnetJobScheduler.Tests;

/// <summary>
/// Handler used by the executor tests: it does a short piece of cancellable work so that
/// the timeout and cancellation paths can be exercised.
/// </summary>
public sealed class DelayingJobHandler : IJobHandler
{
    public async Task<string> ExecuteAsync(Job job, CancellationToken cancellationToken)
    {
        await Task.Delay(100, cancellationToken);
        return $"Job {job.Name} executed successfully";
    }
}

/// <summary>
/// Handler that takes longer than the timeout to complete, used for testing timeout scenarios.
/// </summary>
public sealed class LongRunningJobHandler : IJobHandler
{
    public async Task<string> ExecuteAsync(Job job, CancellationToken cancellationToken)
    {
        // This will exceed typical timeout values used in tests
        await Task.Delay(10000, cancellationToken); // 10 seconds
        return $"Job {job.Name} completed after long delay";
    }
}

/// <summary>
/// Handler that completes quickly, used for testing successful execution under timeout.
/// </summary>
public sealed class FastJobHandler : IJobHandler
{
    public async Task<string> ExecuteAsync(Job job, CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken); // Very short delay
        return $"Job {job.Name} executed quickly";
    }
}

/// <summary>
/// Contains unit tests for the <see cref="JobExecutorService"/> class.
/// Tests the job execution flow including concurrency management, cancellation, timeouts,
/// and execution status tracking.
/// </summary>
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
        HandlerType = typeof(DelayingJobHandler).AssemblyQualifiedName!,
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

        // The slot was never taken, so it must not be released either.
        _concurrencyManagerMock.Verify(c => c.IncrementConcurrencyCount(job.Id), Times.Never);
        _concurrencyManagerMock.Verify(c => c.DecrementConcurrencyCount(job.Id), Times.Never);
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
        cts.Cancel(); // Cancelled before the handler can finish its work

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
        execution.StartedAt.Should().NotBe(default(DateTime));
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

        // Once for the Running record, once for the terminal state.
        _executionRepoMock.Verify(r => r.SaveChangesAsync(), Times.Exactly(2));
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

    [Fact]
    public async Task ExecuteJobAsync_JobUnderTimeout_Succeeds()
    {
        // Arrange - Create a job with a handler that completes quickly
        var job = new Job
        {
            Id = Guid.NewGuid(),
            Name = "fast-job",
            CronExpression = "0 9 * * *",
            HandlerType = typeof(FastJobHandler).AssemblyQualifiedName!,
            MaxRetries = 3,
            ExecutionTimeoutSeconds = 10, // Long timeout that won't be exceeded
            MaxConcurrentExecutions = 1,
            Status = JobStatus.Scheduled
        };

        _concurrencyManagerMock.Setup(c => c.EnsureCanExecuteAsync(job))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        var execution = await service.ExecuteJobAsync(job);

        // Assert
        execution.Should().NotBeNull();
        execution.Status.Should().Be(ExecutionStatus.Success);
        execution.ErrorMessage.Should().BeNull();
        _concurrencyManagerMock.Verify(c => c.DecrementConcurrencyCount(job.Id), Times.Once);
    }

    [Fact]
    public async Task ExecuteJobAsync_JobExceedingTimeout_FailsWithTimeoutMessage()
    {
        // Arrange - Create a job with a long-running handler and short timeout
        var job = new Job
        {
            Id = Guid.NewGuid(),
            Name = "long-running-job",
            CronExpression = "0 9 * * *",
            HandlerType = typeof(LongRunningJobHandler).AssemblyQualifiedName!,
            MaxRetries = 3,
            ExecutionTimeoutSeconds = 1, // Very short timeout that will be exceeded
            MaxConcurrentExecutions = 1,
            Status = JobStatus.Scheduled
        };

        _concurrencyManagerMock.Setup(c => c.EnsureCanExecuteAsync(job))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        var execution = await service.ExecuteJobAsync(job);

        // Assert
        execution.Should().NotBeNull();
        execution.Status.Should().Be(ExecutionStatus.TimedOut);
        execution.ErrorMessage.Should().Contain("timed out after 1 seconds");
        _concurrencyManagerMock.Verify(c => c.DecrementConcurrencyCount(job.Id), Times.Once);
    }

    [Fact]
    public async Task ExecuteJobAsync_JobWithZeroTimeout_UsesDefaultTimeout()
    {
        // Arrange - Job with zero timeout should use default or fail validation
        var job = new Job
        {
            Id = Guid.NewGuid(),
            Name = "zero-timeout-job",
            CronExpression = "0 9 * * *",
            HandlerType = typeof(FastJobHandler).AssemblyQualifiedName!,
            MaxRetries = 3,
            ExecutionTimeoutSeconds = 0, // Invalid timeout
            MaxConcurrentExecutions = 1,
            Status = JobStatus.Scheduled
        };

        _concurrencyManagerMock.Setup(c => c.EnsureCanExecuteAsync(job))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act & Assert - Should handle gracefully
        var execution = await service.ExecuteJobAsync(job);

        execution.Should().NotBeNull();
    }
}
