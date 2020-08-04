// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using JobScheduler.Core.Constants;
using JobScheduler.Core.Data.Repositories;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Services;
using Moq;
using Xunit;

namespace DotnetJobScheduler.Tests;

public class RetryServiceTests
{
    private readonly Mock<IJobRepository> _jobRepoMock = new();
    private readonly Mock<IExecutionRepository> _executionRepoMock = new();
    private readonly RetryService _service;

    public RetryServiceTests()
    {
        _service = new RetryService(_jobRepoMock.Object, _executionRepoMock.Object);
    }

    private static Job BuildJob(int maxRetries = 3, int backoffSeconds = 5, int timeoutSeconds = 300) => new()
    {
        Id = Guid.NewGuid(),
        Name = "integration-sync",
        MaxRetries = maxRetries,
        RetryBackoffSeconds = backoffSeconds,
        ExecutionTimeoutSeconds = timeoutSeconds
    };

    private static JobExecution BuildFailedExecution(int attempt = 1, bool retryable = true) => new()
    {
        Id = Guid.NewGuid(),
        Status = ExecutionStatus.Failed,
        AttemptNumber = attempt,
        IsRetryable = retryable,
        CompletedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task ShouldRetryAsync_WhenAttemptNumberExceedsMaxRetries_ReturnsFalse()
    {
        // Arrange
        var job = BuildJob(maxRetries: 3);
        var execution = BuildFailedExecution(attempt: 4);

        // Act
        var result = await _service.ShouldRetryAsync(job, execution);

        // Assert
        result.Should().BeFalse("attempt 4 is beyond the 3-retry ceiling");
    }

    [Fact]
    public async Task ShouldRetryAsync_WhenExecutionMarkedNotRetryable_ReturnsFalse()
    {
        // Arrange
        var job = BuildJob(maxRetries: 3);
        var execution = BuildFailedExecution(attempt: 1, retryable: false);

        // Act
        var result = await _service.ShouldRetryAsync(job, execution);

        // Assert
        result.Should().BeFalse("execution explicitly opted out of retries");
    }

    [Fact]
    public async Task ShouldRetryAsync_WhenWithinLimitsAndRetryable_ReturnsTrue()
    {
        // Arrange
        var job = BuildJob(maxRetries: 3);
        var execution = BuildFailedExecution(attempt: 2, retryable: true);

        // Act
        var result = await _service.ShouldRetryAsync(job, execution);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CalculateBackoffDelay_WithAttemptZero_ReturnsInitialBackoff()
    {
        // Arrange
        var job = BuildJob(backoffSeconds: 5, timeoutSeconds: 300);

        // Act
        var delay = _service.CalculateBackoffDelay(job, attemptNumber: 0);

        // Assert
        delay.Should().Be(5);
    }

    [Fact]
    public void CalculateBackoffDelay_WithSecondAttempt_AppliesExponentialFormula()
    {
        // Arrange — exponential: initial * 2^(attempt-1)
        var job = BuildJob(backoffSeconds: 5, timeoutSeconds: 300);

        // Act
        var delay = _service.CalculateBackoffDelay(job, attemptNumber: 2); // 5 * 2^1 = 10

        // Assert
        delay.Should().Be(10);
    }

    [Fact]
    public void CalculateBackoffDelay_WhenDelayExceedsJobTimeout_CapsAtTimeoutSeconds()
    {
        // Arrange
        var job = BuildJob(backoffSeconds: 60, timeoutSeconds: 100);

        // Act — 60 * 2^(5-1) = 960, capped at 100
        var delay = _service.CalculateBackoffDelay(job, attemptNumber: 5);

        // Assert
        delay.Should().Be(100);
    }

    [Fact]
    public void CreateRetryExecution_WithFailedExecution_ProducesIncrementedAttemptAndRunningStatus()
    {
        // Arrange
        var job = BuildJob();
        var failed = BuildFailedExecution(attempt: 2);

        // Act
        var retry = _service.CreateRetryExecution(job, failed);

        // Assert
        retry.AttemptNumber.Should().Be(3);
        retry.JobId.Should().Be(job.Id);
        retry.Status.Should().Be(ExecutionStatus.Running);
        retry.IsRetryable.Should().BeTrue();
    }

    [Fact]
    public async Task IsRetryBudgetExceededAsync_WhenRecentFailuresExceedBudget_ReturnsTrue()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var recentFailures = Enumerable.Range(1, 6)
            .Select(_ => new JobExecution
            {
                JobId = jobId,
                Status = ExecutionStatus.Failed,
                StartedAt = DateTime.UtcNow.AddMinutes(-2)
            })
            .ToList<JobExecution>();

        _executionRepoMock
            .Setup(r => r.GetExecutionsByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(recentFailures);

        // Act
        var exceeded = await _service.IsRetryBudgetExceededAsync(jobId, retryBudgetCount: 5, timeWindowMinutes: 5);

        // Assert
        exceeded.Should().BeTrue("6 failures in a 5-minute window exceeds a budget of 5");
    }
}
