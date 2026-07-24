// tests/JobScheduler.Core.Tests/RetryPolicyExtensionsTests.cs
using System;
using JobScheduler.Core.Domain.Entities;
using Xunit;

namespace JobScheduler.Core.Tests;

public class RetryPolicyExtensionsTests
{
    [Fact]
    public void ShouldRetry_ReturnsTrue_WhenAttemptNumberIsWithinMaxRetries()
    {
        // Arrange
        var policy = new RetryPolicy
        {
            MaxRetries = 3
        };

        // Act
        var result = policy.ShouldRetry(3);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldRetry_ReturnsFalse_WhenAttemptNumberExceedsMaxRetries()
    {
        // Arrange
        var policy = new RetryPolicy
        {
            MaxRetries = 3
        };

        // Act
        var result = policy.ShouldRetry(4);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldRetry_ThrowsArgumentNullException_WhenPolicyIsNull()
    {
        // Arrange
        RetryPolicy policy = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => policy.ShouldRetry(1));
    }

    [Fact]
    public void GetTotalAccumulatedDelay_ReturnsZero_WhenMaxRetriesIsZero()
    {
        // Arrange
        var policy = new RetryPolicy
        {
            MaxRetries = 0,
            InitialBackoffSeconds = 10,
            Strategy = BackoffStrategy.Fixed
        };

        // Act
        var totalDelay = policy.GetTotalAccumulatedDelay();

        // Assert
        Assert.Equal(0, totalDelay);
    }

    [Fact]
    public void GetTotalAccumulatedDelay_CalculatesCorrectTotalForFixedStrategy()
    {
        // Arrange
        var policy = new RetryPolicy
        {
            MaxRetries = 3,
            InitialBackoffSeconds = 10,
            Strategy = BackoffStrategy.Fixed
        };

        // Act
        var totalDelay = policy.GetTotalAccumulatedDelay();

        // Assert
        Assert.Equal(30, totalDelay); // 10s * 3 retries
    }

    [Fact]
    public void GetTotalAccumulatedDelay_CalculatesCorrectTotalForLinearStrategy()
    {
        // Arrange
        var policy = new RetryPolicy
        {
            MaxRetries = 3,
            InitialBackoffSeconds = 10,
            Strategy = BackoffStrategy.Linear
        };

        // Act
        var totalDelay = policy.GetTotalAccumulatedDelay();

        // Assert
        Assert.Equal(60, totalDelay); // 10 + 20 + 30 = 60s
    }

    [Fact]
    public void GetTotalAccumulatedDelay_CalculatesCorrectTotalForExponentialStrategy()
    {
        // Arrange
        var policy = new RetryPolicy
        {
            MaxRetries = 3,
            InitialBackoffSeconds = 10,
            Strategy = BackoffStrategy.Exponential,
            BackoffMultiplier = 2.0
        };

        // Act
        var totalDelay = policy.GetTotalAccumulatedDelay();

        // Assert
        Assert.Equal(70, totalDelay); // 10 + 20 + 40 = 70s
    }

    [Fact]
    public void GetTotalAccumulatedDelay_ThrowsArgumentNullException_WhenPolicyIsNull()
    {
        // Arrange
        RetryPolicy policy = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => policy.GetTotalAccumulatedDelay());
    }

    [Fact]
    public void GetRetrySummary_ReturnsCorrectFormat()
    {
        // Arrange
        var policy = new RetryPolicy
        {
            MaxRetries = 5,
            InitialBackoffSeconds = 15,
            MaxBackoffSeconds = 300,
            Strategy = BackoffStrategy.Exponential,
            BackoffMultiplier = 2.5,
            RetryOnTimeout = true,
            RetryOnCancellation = false
        };

        // Act
        var summary = policy.GetRetrySummary();

        // Assert
        Assert.StartsWith("RetryPolicy: Exponential - MaxRetries: 5", summary);
        Assert.Contains("InitialBackoff: 15s", summary);
        Assert.Contains("MaxBackoff: 300s", summary);
        Assert.Contains("Multiplier: 2.5x", summary);
    }

    [Fact]
    public void GetRetrySummary_ThrowsArgumentNullException_WhenPolicyIsNull()
    {
        // Arrange
        RetryPolicy policy = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => policy.GetRetrySummary());
    }

    [Fact]
    public void WithAdjustedParameters_ReturnsNewPolicy_WithAllParametersAdjusted()
    {
        // Arrange
        var originalPolicy = new RetryPolicy
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            JobId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            MaxRetries = 3,
            InitialBackoffSeconds = 10,
            MaxBackoffSeconds = 100,
            Strategy = BackoffStrategy.Linear,
            BackoffMultiplier = 2.0,
            RetryOnTimeout = true,
            RetryOnCancellation = false,
            RetryableExceptions = "TimeoutException"
        };

        // Act
        var adjustedPolicy = originalPolicy.WithAdjustedParameters(
            maxRetries: 5,
            initialBackoffSeconds: 20,
            maxBackoffSeconds: 200,
            backoffMultiplier: 3.0,
            strategy: BackoffStrategy.Exponential,
            retryOnTimeout: false,
            retryOnCancellation: true,
            retryableExceptions: "TimeoutException,HttpRequestException"
        );

        // Assert
        Assert.NotSame(originalPolicy, adjustedPolicy);
        Assert.Equal(5, adjustedPolicy.MaxRetries);
        Assert.Equal(20, adjustedPolicy.InitialBackoffSeconds);
        Assert.Equal(200, adjustedPolicy.MaxBackoffSeconds);
        Assert.Equal(BackoffStrategy.Exponential, adjustedPolicy.Strategy);
        Assert.Equal(3.0, adjustedPolicy.BackoffMultiplier);
        Assert.False(adjustedPolicy.RetryOnTimeout);
        Assert.True(adjustedPolicy.RetryOnCancellation);
        Assert.Equal("TimeoutException,HttpRequestException", adjustedPolicy.RetryableExceptions);
        Assert.NotEqual(originalPolicy.Id, adjustedPolicy.Id);
        Assert.Equal(originalPolicy.JobId, adjustedPolicy.JobId);
    }

    [Fact]
    public void WithAdjustedParameters_ReturnsNewPolicy_WithNoParametersAdjusted()
    {
        // Arrange
        var originalPolicy = new RetryPolicy
        {
            MaxRetries = 3,
            InitialBackoffSeconds = 10,
            MaxBackoffSeconds = 100,
            Strategy = BackoffStrategy.Linear,
            BackoffMultiplier = 2.0
        };

        var originalJobId = originalPolicy.JobId;

        // Act
        var adjustedPolicy = originalPolicy.WithAdjustedParameters();

        // Assert
        Assert.NotSame(originalPolicy, adjustedPolicy);
        Assert.Equal(originalPolicy.MaxRetries, adjustedPolicy.MaxRetries);
        Assert.Equal(originalPolicy.InitialBackoffSeconds, adjustedPolicy.InitialBackoffSeconds);
        Assert.Equal(originalPolicy.MaxBackoffSeconds, adjustedPolicy.MaxBackoffSeconds);
        Assert.Equal(originalPolicy.Strategy, adjustedPolicy.Strategy);
        Assert.Equal(originalPolicy.BackoffMultiplier, adjustedPolicy.BackoffMultiplier);
        Assert.NotEqual(originalPolicy.Id, adjustedPolicy.Id); // Always creates new ID
        Assert.Equal(originalJobId, adjustedPolicy.JobId);
        Assert.NotNull(adjustedPolicy.UpdatedAt);
        // Just verify UpdatedAt exists and is reasonable - exact timestamp comparison is fragile
        Assert.InRange(adjustedPolicy.UpdatedAt.Value, DateTime.UtcNow.AddSeconds(-1), DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public void WithAdjustedParameters_ReturnsNewPolicy_WithSomeParametersAdjusted()
    {
        // Arrange
        var originalPolicy = new RetryPolicy
        {
            MaxRetries = 3,
            InitialBackoffSeconds = 10,
            MaxBackoffSeconds = 100,
            Strategy = BackoffStrategy.Linear,
            BackoffMultiplier = 2.0
        };

        // Act - only adjust maxRetries and initialBackoffSeconds
        var adjustedPolicy = originalPolicy.WithAdjustedParameters(
            maxRetries: 7,
            initialBackoffSeconds: 15
        );

        // Assert
        Assert.Equal(7, adjustedPolicy.MaxRetries);
        Assert.Equal(15, adjustedPolicy.InitialBackoffSeconds);
        Assert.Equal(originalPolicy.MaxBackoffSeconds, adjustedPolicy.MaxBackoffSeconds); // unchanged
        Assert.Equal(originalPolicy.Strategy, adjustedPolicy.Strategy); // unchanged
        Assert.Equal(originalPolicy.BackoffMultiplier, adjustedPolicy.BackoffMultiplier); // unchanged
    }

    [Fact]
    public void WithAdjustedParameters_ThrowsArgumentNullException_WhenPolicyIsNull()
    {
        // Arrange
        RetryPolicy policy = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => policy.WithAdjustedParameters());
    }

    [Fact]
    public void WithAdjustedParameters_CreatesPolicyWithNewGuidAndUpdatedTimestamp()
    {
        // Arrange
        var originalPolicy = new RetryPolicy
        {
            MaxRetries = 3
        };

        // Act
        var adjustedPolicy = originalPolicy.WithAdjustedParameters(maxRetries: 5);

        // Assert
        Assert.NotEqual(originalPolicy.Id, adjustedPolicy.Id);
        Assert.Equal(originalPolicy.JobId, adjustedPolicy.JobId);
        Assert.Equal(5, adjustedPolicy.MaxRetries);
        Assert.NotNull(adjustedPolicy.UpdatedAt);
        // Just verify UpdatedAt exists and is reasonable - exact timestamp comparison is fragile
        Assert.InRange(adjustedPolicy.UpdatedAt.Value, DateTime.UtcNow.AddSeconds(-1), DateTime.UtcNow.AddSeconds(1));
    }
}
