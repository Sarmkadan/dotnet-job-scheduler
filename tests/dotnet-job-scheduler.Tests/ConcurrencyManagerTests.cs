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

public sealed class ConcurrencyManagerTests
{
    private readonly Mock<IExecutionRepository> _executionRepoMock = new();

    private ConcurrencyManager CreateManager(int maxGlobalConcurrency = 10) =>
        new(_executionRepoMock.Object, maxGlobalConcurrency);

    private static Job CreateJob(Guid? id = null, int maxConcurrent = 1) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Name = "test-job",
        CronExpression = "0 9 * * *",
        HandlerType = "TestApp.Jobs.TestJob, TestApp",
        MaxRetries = 3,
        ExecutionTimeoutSeconds = 300,
        MaxConcurrentExecutions = maxConcurrent,
        Status = JobStatus.Scheduled
    };

    [Fact]
    public async Task CanExecuteAsync_WithAvailableCapacity_ReturnsTrue()
    {
        // Arrange
        var job = CreateJob();
        _executionRepoMock.Setup(r => r.GetConcurrentRunningCountAsync())
            .ReturnsAsync(2);
        _executionRepoMock.Setup(r => r.GetCurrentlyRunningCountAsync(job.Id))
            .ReturnsAsync(0);

        var manager = CreateManager(maxGlobalConcurrency: 10);

        // Act
        var result = await manager.CanExecuteAsync(job);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanExecuteAsync_WithExceededGlobalConcurrency_ReturnsFalse()
    {
        // Arrange
        var job = CreateJob();
        _executionRepoMock.Setup(r => r.GetConcurrentRunningCountAsync())
            .ReturnsAsync(10); // At global limit
        _executionRepoMock.Setup(r => r.GetCurrentlyRunningCountAsync(job.Id))
            .ReturnsAsync(0);

        var manager = CreateManager(maxGlobalConcurrency: 10);

        // Act
        var result = await manager.CanExecuteAsync(job);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanExecuteAsync_WithExceededJobConcurrency_ReturnsFalse()
    {
        // Arrange
        var job = CreateJob(maxConcurrent: 2);
        _executionRepoMock.Setup(r => r.GetConcurrentRunningCountAsync())
            .ReturnsAsync(5);
        _executionRepoMock.Setup(r => r.GetCurrentlyRunningCountAsync(job.Id))
            .ReturnsAsync(2); // At job's limit

        var manager = CreateManager(maxGlobalConcurrency: 10);

        // Act
        var result = await manager.CanExecuteAsync(job);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanExecuteAsync_WithNullJob_ThrowsArgumentNullException()
    {
        // Arrange
        var manager = CreateManager();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => manager.CanExecuteAsync(null!));
    }

    [Fact]
    public async Task EnsureCanExecuteAsync_WithAvailableCapacity_CompletesSuccessfully()
    {
        // Arrange
        var job = CreateJob();
        _executionRepoMock.Setup(r => r.GetConcurrentRunningCountAsync())
            .ReturnsAsync(1);
        _executionRepoMock.Setup(r => r.GetCurrentlyRunningCountAsync(job.Id))
            .ReturnsAsync(0);

        var manager = CreateManager(maxGlobalConcurrency: 10);

        // Act & Assert
        await manager.EnsureCanExecuteAsync(job);
        _executionRepoMock.Verify(r => r.GetConcurrentRunningCountAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task EnsureCanExecuteAsync_WithExceededCapacity_ThrowsConcurrencyException()
    {
        // Arrange
        var job = CreateJob(maxConcurrent: 1);
        _executionRepoMock.Setup(r => r.GetConcurrentRunningCountAsync())
            .ReturnsAsync(10);
        _executionRepoMock.Setup(r => r.GetCurrentlyRunningCountAsync(job.Id))
            .ReturnsAsync(1);

        var manager = CreateManager(maxGlobalConcurrency: 10);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConcurrencyException>(() => manager.EnsureCanExecuteAsync(job));
        ex.JobId.Should().Be(job.Id);
    }

    [Fact]
    public void IncrementConcurrencyCount_IncrementsJobCounter()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var manager = CreateManager();

        // Act
        manager.IncrementConcurrencyCount(jobId);
        manager.IncrementConcurrencyCount(jobId);

        // Assert
        var stats = manager.GetConcurrencyStats();
        stats.GlobalRunningCount.Should().Be(2);
    }

    [Fact]
    public void IncrementConcurrencyCount_MultipleJobs_TracksSeparately()
    {
        // Arrange
        var jobId1 = Guid.NewGuid();
        var jobId2 = Guid.NewGuid();
        var manager = CreateManager();

        // Act
        manager.IncrementConcurrencyCount(jobId1);
        manager.IncrementConcurrencyCount(jobId1);
        manager.IncrementConcurrencyCount(jobId2);

        // Assert
        var stats = manager.GetConcurrencyStats();
        stats.GlobalRunningCount.Should().Be(3);
    }

    [Fact]
    public void DecrementConcurrencyCount_DecrementsCounter()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var manager = CreateManager();

        // Act
        manager.IncrementConcurrencyCount(jobId);
        manager.IncrementConcurrencyCount(jobId);
        manager.DecrementConcurrencyCount(jobId);

        // Assert
        var stats = manager.GetConcurrencyStats();
        stats.GlobalRunningCount.Should().Be(1);
    }

    [Fact]
    public void DecrementConcurrencyCount_NeverGoesNegative()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var manager = CreateManager();

        // Act
        manager.DecrementConcurrencyCount(jobId); // Decrement without increment

        // Assert
        var stats = manager.GetConcurrencyStats();
        stats.GlobalRunningCount.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void GetConcurrencyStats_ReturnsCurrentStats()
    {
        // Arrange
        var jobId1 = Guid.NewGuid();
        var jobId2 = Guid.NewGuid();
        var manager = CreateManager(maxGlobalConcurrency: 20);

        // Act
        manager.IncrementConcurrencyCount(jobId1);
        manager.IncrementConcurrencyCount(jobId2);
        manager.IncrementConcurrencyCount(jobId2);

        var stats = manager.GetConcurrencyStats();

        // Assert
        stats.GlobalRunningCount.Should().Be(3);
        stats.MaxGlobalConcurrency.Should().Be(20);
    }

    [Fact]
    public async Task SynchronizeWithDatabaseAsync_UpdatesCacheFromDatabase()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        _executionRepoMock.Setup(r => r.GetConcurrentRunningCountAsync())
            .ReturnsAsync(5);
        _executionRepoMock.Setup(r => r.GetCurrentlyRunningCountAsync(jobId))
            .ReturnsAsync(2);

        var manager = CreateManager();

        // Act
        await manager.SynchronizeWithDatabaseAsync();

        // Assert
        _executionRepoMock.Verify(r => r.GetConcurrentRunningCountAsync(), Times.Once);
    }

    [Fact]
    public async Task ConcurrentOperations_HandlesRaceConditions()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var manager = CreateManager();

        _executionRepoMock.Setup(r => r.GetConcurrentRunningCountAsync())
            .ReturnsAsync(0);
        _executionRepoMock.Setup(r => r.GetCurrentlyRunningCountAsync(jobId))
            .ReturnsAsync(0);

        // Act - Multiple concurrent increments
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() => manager.IncrementConcurrencyCount(jobId)))
            .ToList();

        await Task.WhenAll(tasks);

        // Assert
        var stats = manager.GetConcurrencyStats();
        stats.GlobalRunningCount.Should().Be(10);
    }

    [Fact]
    public async Task MaxGlobalConcurrencyEnforced_AtCapacity_BlocksAdditionalJobs()
    {
        // Arrange
        var manager = CreateManager(maxGlobalConcurrency: 2);
        _executionRepoMock.Setup(r => r.GetConcurrentRunningCountAsync())
            .ReturnsAsync(2);

        var job = CreateJob();

        // Act
        var canExecute = await manager.CanExecuteAsync(job);

        // Assert
        canExecute.Should().BeFalse();
    }
}
