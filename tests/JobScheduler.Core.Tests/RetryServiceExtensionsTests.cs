// =============================================================================
// Author: Automated Generation
// =============================================================================

using System;
using System.Threading.Tasks;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Services;
using JobScheduler.Core.Data.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JobScheduler.Core.Tests;

public class RetryServiceExtensionsTests
{
    private readonly Mock<IJobRepository> _jobRepositoryMock;
    private readonly Mock<IExecutionRepository> _executionRepositoryMock;
    private readonly RetryService _retryService;

    public RetryServiceExtensionsTests()
    {
        _jobRepositoryMock = new Mock<IJobRepository>();
        _executionRepositoryMock = new Mock<IExecutionRepository>();
        _retryService = new RetryService(_jobRepositoryMock.Object, _executionRepositoryMock.Object);
    }

    #region CreateRetryExecution

    [Fact]
    public void CreateRetryExecution_ShouldReturnExecutionWithCustomExecutorName()
    {
        // Arrange
        var job = new Job { Id = Guid.NewGuid(), MaxRetries = 3, RetryBackoffSeconds = 5, ExecutionTimeoutSeconds = 60 };
        var failedExecution = new JobExecution
        {
            Id = Guid.NewGuid(),
            JobId = job.Id,
            AttemptNumber = 1,
            ExecutorName = "original-executor",
            CompletedAt = DateTime.UtcNow,
            IsRetryable = true
        };
        const string customExecutorName = "custom-executor";

        // Act
        var retryExecution = _retryService.CreateRetryExecution(job, failedExecution, customExecutorName);

        // Assert
        Assert.NotNull(retryExecution);
        Assert.Equal(customExecutorName, retryExecution.ExecutorName);
        Assert.Equal(failedExecution.AttemptNumber + 1, retryExecution.AttemptNumber);
        Assert.Equal(job.Id, retryExecution.JobId);
    }

    [Fact]
    public void CreateRetryExecution_NullRetryService_ThrowsArgumentNullException()
    {
        // Arrange
        var job = new Job();
        var failedExecution = new JobExecution();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            RetryServiceExtensions.CreateRetryExecution(null!, job, failedExecution, "executor"));
    }

    [Fact]
    public void CreateRetryExecution_NullJob_ThrowsArgumentNullException()
    {
        // Arrange
        var failedExecution = new JobExecution();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _retryService.CreateRetryExecution(null!, failedExecution, "executor"));
    }

    [Fact]
    public void CreateRetryExecution_NullFailedExecution_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _retryService.CreateRetryExecution(new Job(), null!, "executor"));
    }

    [Fact]
    public void CreateRetryExecution_EmptyExecutorName_ThrowsArgumentException()
    {
        // Arrange
        var job = new Job();
        var failedExecution = new JobExecution();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _retryService.CreateRetryExecution(job, failedExecution, "   "));
    }

    #endregion

    #region CalculateNextRetryTime

    [Fact]
    public void CalculateNextRetryTime_WithMinimumDelay_ShouldEnforceMinimum()
    {
        // Arrange
        var job = new Job
        {
            Id = Guid.NewGuid(),
            RetryBackoffSeconds = 1, // very small base delay
            ExecutionTimeoutSeconds = 30
        };
        var completedAt = DateTime.UtcNow;
        var failedExecution = new JobExecution
        {
            Id = Guid.NewGuid(),
            JobId = job.Id,
            AttemptNumber = 0,
            CompletedAt = completedAt,
            IsRetryable = true
        };

        // Act
        var nextRetry = _retryService.CalculateNextRetryTime(job, failedExecution, minimumDelaySeconds: 5);

        // Assert
        var expected = completedAt.AddSeconds(5);
        Assert.Equal(expected, nextRetry);
    }

    [Fact]
    public void CalculateNextRetryTime_NullRetryService_ThrowsArgumentNullException()
    {
        // Arrange
        var job = new Job();
        var exec = new JobExecution();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            RetryServiceExtensions.CalculateNextRetryTime(null!, job, exec));
    }

    [Fact]
    public void CalculateNextRetryTime_NullJob_ThrowsArgumentNullException()
    {
        // Arrange
        var exec = new JobExecution { CompletedAt = DateTime.UtcNow };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _retryService.CalculateNextRetryTime(null!, exec));
    }

    [Fact]
    public void CalculateNextRetryTime_NullFailedExecution_ThrowsArgumentNullException()
    {
        // Arrange
        var job = new Job();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _retryService.CalculateNextRetryTime(job, null!));
    }

    [Fact]
    public void CalculateNextRetryTime_MinimumDelayLessThanOne_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var job = new Job { RetryBackoffSeconds = 1, ExecutionTimeoutSeconds = 10 };
        var exec = new JobExecution { CompletedAt = DateTime.UtcNow };

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _retryService.CalculateNextRetryTime(job, exec, minimumDelaySeconds: 0));
    }

    #endregion

    #region IsRetryBudgetExceededAsync

    [Fact]
    public async Task IsRetryBudgetExceededAsync_ForwardsToService_ReturnsTrue()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var serviceMock = new Mock<RetryService>(_jobRepositoryMock.Object, _executionRepositoryMock.Object)
        {
            CallBase = true
        };
        serviceMock
            .Setup(s => s.IsRetryBudgetExceededAsync(jobId, It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(true);

        // Act
        var result = await serviceMock.Object.IsRetryBudgetExceededAsync(jobId);

        // Assert
        Assert.True(result);
        serviceMock.Verify(s => s.IsRetryBudgetExceededAsync(jobId, 5, 5), Times.Once);
    }

    [Fact]
    public async Task IsRetryBudgetExceededAsync_NullRetryService_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await RetryServiceExtensions.IsRetryBudgetExceededAsync(null!, Guid.NewGuid()));
    }

    [Fact]
    public async Task IsRetryBudgetExceededAsync_TimeWindowLessThanOne_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var jobId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await _retryService.IsRetryBudgetExceededAsync(jobId, timeWindowMinutes: 0));
    }

    #endregion

    #region FormatRetryMessage

    [Fact]
    public void FormatRetryMessage_ReturnsExpectedString()
    {
        // Arrange
        var attempt = 3;
        var delay = TimeSpan.FromSeconds(12);
        var serverName = "server-01";
        var jobId = Guid.NewGuid();

        // Act
        var message = _retryService.FormatRetryMessage(attempt, delay, serverName, jobId);

        // Assert
        var expected = $"Job {jobId} - Retry attempt {attempt} scheduled in {delay.TotalSeconds:F0}s on server '{serverName}'.";
        Assert.Equal(expected, message);
    }

    [Fact]
    public void FormatRetryMessage_NullRetryService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            RetryServiceExtensions.FormatRetryMessage(null!, 1, TimeSpan.Zero, "srv", Guid.NewGuid()));
    }

    [Fact]
    public void FormatRetryMessage_EmptyServerName_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _retryService.FormatRetryMessage(1, TimeSpan.Zero, "   ", Guid.NewGuid()));
    }

    #endregion
}
