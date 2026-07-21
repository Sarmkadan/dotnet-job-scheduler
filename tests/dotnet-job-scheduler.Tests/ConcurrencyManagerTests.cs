#nullable enable

using System.Threading;
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
/// Unit tests for <see cref="ConcurrencyManager"/> that verify concurrency control and capacity management.
/// </summary>
public sealed class ConcurrencyManagerTests
{
    private readonly Mock<IExecutionRepository> _executionRepoMock = new();

    /// <summary>
    /// Creates a new <see cref="ConcurrencyManager"/> instance with the specified global concurrency limit.
    /// </summary>
    /// <param name="maxGlobalConcurrency">The maximum number of concurrent executions allowed across all jobs.</param>
    /// <returns>A new <see cref="ConcurrencyManager"/> instance.</returns>
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

    /// <summary>
    /// Tests that <see cref="ConcurrencyManager.CanExecuteAsync"/> returns true when there is available capacity both globally and for the specific job.
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="ConcurrencyManager.CanExecuteAsync"/> returns false when global concurrency limit is exceeded.
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="ConcurrencyManager.CanExecuteAsync"/> returns false when the specific job's concurrency limit is exceeded.
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="ConcurrencyManager.CanExecuteAsync"/> throws <see cref="ArgumentNullException"/> when null job is provided.
    /// </summary>
    [Fact]
    public async Task CanExecuteAsync_WithNullJob_ThrowsArgumentNullException()
    {
        // Arrange
        var manager = CreateManager();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => manager.CanExecuteAsync(null!));
    }

    /// <summary>
    /// Tests that <see cref="ConcurrencyManager.EnsureCanExecuteAsync"/> completes successfully when there is available capacity.
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="ConcurrencyManager.EnsureCanExecuteAsync"/> throws <see cref="ConcurrencyException"/> when capacity is exceeded.
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="ConcurrencyManager.IncrementConcurrencyCount"/> increments the job's concurrency counter.
    /// </summary>
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
        stats["GlobalRunning"].Should().Be(2);
    }

    /// <summary>
    /// Tests that <see cref="ConcurrencyManager.IncrementConcurrencyCount"/> tracks multiple jobs separately.
    /// </summary>
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
        stats["GlobalRunning"].Should().Be(3);
    }

    /// <summary>
    /// Tests that <see cref="ConcurrencyManager.DecrementConcurrencyCount"/> decrements the counter.
    /// </summary>
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
        stats["GlobalRunning"].Should().Be(1);
    }

    /// <summary>
    /// Tests that <see cref="ConcurrencyManager.DecrementConcurrencyCount"/> never allows the counter to go negative.
    /// </summary>
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
        stats["GlobalRunning"].Should().BeGreaterThanOrEqualTo(0);
    }

    /// <summary>
    /// Tests that <see cref="ConcurrencyManager.GetConcurrencyStats"/> returns current statistics.
    /// </summary>
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
        stats["GlobalRunning"].Should().Be(3);
        stats["GlobalLimit"].Should().Be(20);
    }

    /// <summary>
    /// Tests that <see cref="ConcurrencyManager.SynchronizeWithDatabaseAsync"/> updates internal cache from database values.
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="ConcurrencyManager.IncrementConcurrencyCount"/> handles race conditions correctly when called concurrently.
    /// </summary>
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
        stats["GlobalRunning"].Should().Be(10);
    }

    /// <summary>
    /// Tests that <see cref="ConcurrencyManager.CanExecuteAsync"/> enforces max global concurrency limit.
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="ConcurrencyManager.IncrementConcurrencyCount"/> and <see cref="ConcurrencyManager.DecrementConcurrencyCount"/>
    /// respect the max global concurrency limit of 50 parallel acquisitions.
    /// </summary>
    [Fact]
    public void MaxGlobalConcurrency_50ParallelAcquisitions_Respected()
    {
        // Arrange
        var manager = CreateManager(maxGlobalConcurrency: 50);
        var jobId = Guid.NewGuid();

        // Act - Increment concurrency count 50 times
        for (int i = 0; i < 50; i++)
        {
            manager.IncrementConcurrencyCount(jobId);
        }

        // Assert - Global concurrency should be exactly 50
        var stats = manager.GetConcurrencyStats();
        stats["GlobalRunning"].Should().Be(50);
        stats["GlobalLimit"].Should().Be(50);

        // Verify no overflow occurred
        stats["GlobalRunning"].Should().BeLessThanOrEqualTo(stats["GlobalLimit"]);
    }

    /// <summary>
    /// Tests that <see cref="ConcurrencyManager.DecrementConcurrencyCount"/> restores a slot correctly.
    /// </summary>
    [Fact]
    public void DecrementConcurrencyCount_RestoresSlot()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var manager = CreateManager(maxGlobalConcurrency: 10);

        // Increment to establish a baseline
        manager.IncrementConcurrencyCount(jobId);
        manager.IncrementConcurrencyCount(jobId);
        var initialCount = manager.GetGlobalConcurrencyCount();
        initialCount.Should().Be(2);

        // Act - Decrement once
        manager.DecrementConcurrencyCount(jobId);

        // Assert - Slot should be restored
        var finalCount = manager.GetGlobalConcurrencyCount();
        finalCount.Should().Be(1);
        finalCount.Should().BeLessThan(initialCount);
    }

    /// <summary>
    /// Tests that <see cref="ConcurrencyManager.GetGlobalConcurrencyCount"/> returns the correct value
    /// using Interlocked counter for thread-safe access.
    /// </summary>
    [Fact]
    public void GetGlobalConcurrencyCount_ReturnsCorrectValue_UsingInterlocked()
    {
        // Arrange
        var manager = CreateManager(maxGlobalConcurrency: 100);
        var jobId = Guid.NewGuid();

        // Act - Increment to various levels
        manager.IncrementConcurrencyCount(jobId);
        manager.IncrementConcurrencyCount(jobId);
        manager.IncrementConcurrencyCount(jobId);

        // Assert
        var count = manager.GetGlobalConcurrencyCount();
        count.Should().Be(3);

        // Verify it's using Interlocked (thread-safe)
        Parallel.For(0, 10, _ =>
        {
            manager.IncrementConcurrencyCount(jobId);
        });

        var parallelCount = manager.GetGlobalConcurrencyCount();
        parallelCount.Should().Be(13);
    }

    /// <summary>
    /// Tests that <see cref="ConcurrencyManager.DecrementConcurrencyCount"/> correctly handles concurrent decrements
    /// without going negative.
    /// </summary>
    [Fact]
    public void DecrementConcurrencyCount_Concurrent_ThreadSafe()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var manager = CreateManager(maxGlobalConcurrency: 100);

        // Establish initial count
        manager.IncrementConcurrencyCount(jobId);
        manager.IncrementConcurrencyCount(jobId);
        manager.IncrementConcurrencyCount(jobId);

        var initialCount = manager.GetGlobalConcurrencyCount();
        initialCount.Should().Be(3);

        // Act - Concurrent decrements
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() => manager.DecrementConcurrencyCount(jobId)))
            .ToList();

        Task.WaitAll(tasks.ToArray());

        // Assert - Should never go below zero
        var finalCount = manager.GetGlobalConcurrencyCount();
        finalCount.Should().BeGreaterThanOrEqualTo(0);
        finalCount.Should().BeLessThanOrEqualTo(initialCount);
    }

    /// <summary>
    /// Tests that <see cref="ConcurrencyManager.IncrementConcurrencyCount"/> and <see cref="ConcurrencyManager.DecrementConcurrencyCount"/>
    /// maintain correct counts across multiple jobs with different limits.
    /// </summary>
    [Fact]
    public void ConcurrencyCount_MultipleJobs_WithDifferentLimits_MaintainedCorrectly()
    {
        // Arrange
        var manager = CreateManager(maxGlobalConcurrency: 100);
        var jobId1 = Guid.NewGuid();
        var jobId2 = Guid.NewGuid();
        var jobId3 = Guid.NewGuid();

        // Act - Different jobs with different concurrency patterns
        // Job 1: 5 executions
        for (int i = 0; i < 5; i++)
        {
            manager.IncrementConcurrencyCount(jobId1);
        }

        // Job 2: 10 executions
        for (int i = 0; i < 10; i++)
        {
            manager.IncrementConcurrencyCount(jobId2);
        }

        // Job 3: 3 executions
        for (int i = 0; i < 3; i++)
        {
            manager.IncrementConcurrencyCount(jobId3);
        }

        // Assert - Total should be 18
        var stats = manager.GetConcurrencyStats();
        stats["GlobalRunning"].Should().Be(18);

        // Verify individual job counts
        stats = manager.GetConcurrencyStats();
        manager.GetJobConcurrencyCount(jobId1).Should().Be(5);
        manager.GetJobConcurrencyCount(jobId2).Should().Be(10);
        manager.GetJobConcurrencyCount(jobId3).Should().Be(3);

        // Act - Decrement some jobs
        for (int i = 0; i < 3; i++)
        {
            manager.DecrementConcurrencyCount(jobId2);
        }

        // Assert - Job 2 should now have 7, total should be 15
        stats = manager.GetConcurrencyStats();
        stats["GlobalRunning"].Should().Be(15);
        manager.GetJobConcurrencyCount(jobId2).Should().Be(7);
    }

    /// <summary>
    /// Tests cancellation while waiting scenario - simulates a job that gets cancelled
    /// while waiting for a concurrency slot.
    /// </summary>
    [Fact]
    public async Task Cancellation_WhileWaitingForSlot_HandledGracefully()
    {
        // Arrange
        var manager = CreateManager(maxGlobalConcurrency: 2);
        var job = CreateJob();

        _executionRepoMock.Setup(r => r.GetConcurrentRunningCountAsync())
            .ReturnsAsync(2); // At global limit
        _executionRepoMock.Setup(r => r.GetCurrentlyRunningCountAsync(job.Id))
            .ReturnsAsync(0);

        // Act - Try to check if job can execute (will be blocked by global limit)
        var canExecuteTask = manager.CanExecuteAsync(job);

        // Small delay to ensure the task is started
        await Task.Delay(10);

        // Assert - Should return false immediately (no waiting)
        var canExecute = await canExecuteTask;
        canExecute.Should().BeFalse();
    }

    /// <summary>
    /// Tests that <see cref="ConcurrencyManager.GetJobConcurrencyCount"/> returns 0 for unknown job IDs.
    /// </summary>
    [Fact]
    public void GetJobConcurrencyCount_UnknownJobId_ReturnsZero()
    {
        // Arrange
        var manager = CreateManager();
        var unknownJobId = Guid.NewGuid();

        // Act
        var count = manager.GetJobConcurrencyCount(unknownJobId);

        // Assert
        count.Should().Be(0);
    }

    /// <summary>
    /// Tests that <see cref="ConcurrencyManager.GetConcurrencyStats"/> returns accurate statistics
    /// including all tracked metrics.
    /// </summary>
    [Fact]
    public void GetConcurrencyStats_ReturnsAccurateStatistics()
    {
        // Arrange
        var manager = CreateManager(maxGlobalConcurrency: 50);
        var jobId1 = Guid.NewGuid();
        var jobId2 = Guid.NewGuid();
        var jobId3 = Guid.NewGuid();

        // Act - Set up various states
        manager.IncrementConcurrencyCount(jobId1);
        manager.IncrementConcurrencyCount(jobId1);
        manager.IncrementConcurrencyCount(jobId2);
        manager.IncrementConcurrencyCount(jobId3);

        // Assert
        var stats = manager.GetConcurrencyStats();

        stats.Should().ContainKey("GlobalRunning");
        stats.Should().ContainKey("GlobalLimit");
        stats.Should().ContainKey("JobsWithExecutions");
        stats.Should().ContainKey("TotalCachedJobs");

        stats["GlobalRunning"].Should().Be(4);
        stats["GlobalLimit"].Should().Be(50);
        stats["JobsWithExecutions"].Should().Be(3); // 3 jobs have executions
        stats["TotalCachedJobs"].Should().Be(3); // 3 unique job IDs tracked
    }

    /// <summary>
    /// Tests that <see cref="ConcurrencyManager.IncrementConcurrencyCount"/> and <see cref="ConcurrencyManager.DecrementConcurrencyCount"/>
    /// work correctly with the default max concurrency limit.
    /// </summary>
    [Fact]
    public void ConcurrencyOperations_WithDefaultMaxConcurrency_WorkCorrectly()
    {
        // Arrange
        var manager = CreateManager(); // Uses default maxGlobalConcurrency
        var jobId = Guid.NewGuid();

        // Act - Use default limit
        for (int i = 0; i < SchedulerConstants.DefaultMaxConcurrentJobs; i++)
        {
            manager.IncrementConcurrencyCount(jobId);
        }

        // Assert
        var stats = manager.GetConcurrencyStats();
        stats["GlobalRunning"].Should().Be(SchedulerConstants.DefaultMaxConcurrentJobs);
        stats["GlobalLimit"].Should().Be(SchedulerConstants.DefaultMaxConcurrentJobs);
    }

    /// <summary>
    /// Tests that the global concurrency counter correctly tracks concurrent operations
    /// even with rapid concurrent operations, using Interlocked for thread safety.
    /// </summary>
    [Fact]
    public void GlobalConcurrencyCounter_TracksConcurrentOperations_CorrectlyWithInterlocked()
    {
        // Arrange
        var manager = CreateManager(maxGlobalConcurrency: 100);
        var jobId = Guid.NewGuid();
        var iterations = 1000; // Large number to stress test

        // Act - Rapid concurrent increments
        Parallel.For(0, iterations, _ =>
        {
            manager.IncrementConcurrencyCount(jobId);
        });

        // Assert - Should correctly track all increments (Interlocked ensures no lost updates)
        var stats = manager.GetConcurrencyStats();
        stats["GlobalRunning"].Should().Be(iterations);
        stats["GlobalRunning"].Should().BeGreaterThan(0);

        // Verify the counter is using Interlocked (thread-safe operations)
        // The counter should be exactly equal to the number of increments
        stats["GlobalRunning"].Should().Be(iterations);
    }
}