#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using JobScheduler.Core.Domain.Entities;
using Xunit;

namespace DotnetJobScheduler.Tests;

/// <summary>
/// Tests for the <see cref="RetryPolicy"/> class.
/// </summary>
public sealed class RetryPolicyTests
{
    /// <summary>
    /// Verifies that the backoff delay remains constant for the Fixed strategy across attempts.
    /// </summary>
    [Fact]
    public void CalculateBackoffDelay_WithFixedStrategy_ReturnsConstantDelayAcrossAttempts()
    {
        // Arrange
        var policy = new RetryPolicy
        {
            Strategy = BackoffStrategy.Fixed,
            InitialBackoffSeconds = 10,
            MaxBackoffSeconds = 300
        };

        // Act
        var delayAttempt1 = policy.CalculateBackoffDelay(1);
        var delayAttempt5 = policy.CalculateBackoffDelay(5);

        // Assert
        delayAttempt1.Should().Be(10);
        delayAttempt5.Should().Be(10, "fixed strategy never changes the delay");
    }

    /// <summary>
    /// Verifies that the backoff delay increments proportionally to the attempt number for the Linear strategy.
    /// </summary>
    [Fact]
    public void CalculateBackoffDelay_WithLinearStrategy_IncrementsProportionallyToAttempt()
    {
        // Arrange
        var policy = new RetryPolicy
        {
            Strategy = BackoffStrategy.Linear,
            InitialBackoffSeconds = 5,
            MaxBackoffSeconds = 300
        };

        // Act
        var delayAttempt1 = policy.CalculateBackoffDelay(1); // 5 * 1
        var delayAttempt3 = policy.CalculateBackoffDelay(3); // 5 * 3

        // Assert
        delayAttempt1.Should().Be(5);
        delayAttempt3.Should().Be(15);
    }

    /// <summary>
    /// Verifies that the backoff delay doubles on each attempt for the Exponential strategy.
    /// </summary>
    [Fact]
    public void CalculateBackoffDelay_WithExponentialStrategy_DoublesOnEachAttempt()
    {
        // Arrange
        var policy = new RetryPolicy
        {
            Strategy = BackoffStrategy.Exponential,
            InitialBackoffSeconds = 5,
            BackoffMultiplier = 2.0,
            MaxBackoffSeconds = 300
        };

        // Act
        var delayAttempt1 = policy.CalculateBackoffDelay(1); // 5 * 2^0 = 5
        var delayAttempt2 = policy.CalculateBackoffDelay(2); // 5 * 2^1 = 10
        var delayAttempt3 = policy.CalculateBackoffDelay(3); // 5 * 2^2 = 20

        // Assert
        delayAttempt1.Should().Be(5);
        delayAttempt2.Should().Be(10);
        delayAttempt3.Should().Be(20);
    }

    /// <summary>
    /// Verifies that the calculated delay is capped at <see cref="RetryPolicy.MaxBackoffSeconds"/> when it would otherwise exceed the maximum.
    /// </summary>
    [Fact]
    public void CalculateBackoffDelay_WhenCalculatedDelayExceedsMax_CapsAtMaxBackoff()
    {
        // Arrange
        var policy = new RetryPolicy
        {
            Strategy = BackoffStrategy.Exponential,
            InitialBackoffSeconds = 60,
            BackoffMultiplier = 2.0,
            MaxBackoffSeconds = 100
        };

        // Act — 60 * 2^9 = 30720 would overflow without the cap
        var delay = policy.CalculateBackoffDelay(10);

        // Assert
        delay.Should().Be(100);
    }

    /// <summary>
    /// Verifies that when <see cref="RetryPolicy.RetryableExceptions"/> is null, any exception type is considered retryable.
    /// </summary>
    [Fact]
    public void ShouldRetryOnException_WhenRetryableExceptionsIsEmpty_AllowsAnyException()
    {
        // Arrange
        var policy = new RetryPolicy { RetryableExceptions = null };

        // Act
        var result = policy.ShouldRetryOnException("System.Net.Http.HttpRequestException");

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that an exception type present in the allowlist is considered retryable.
    /// </summary>
    [Fact]
    public void ShouldRetryOnException_WhenExceptionMatchesAllowlist_ReturnsTrue()
    {
        // Arrange
        var policy = new RetryPolicy { RetryableExceptions = "TimeoutException, HttpRequestException" };

        // Act
        var result = policy.ShouldRetryOnException("System.Net.Http.HttpRequestException");

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that an exception type not present in the allowlist is not considered retryable.
    /// </summary>
    [Fact]
    public void ShouldRetryOnException_WhenExceptionNotInAllowlist_ReturnsFalse()
    {
        // Arrange
        var policy = new RetryPolicy { RetryableExceptions = "TimeoutException" };

        // Act
        var result = policy.ShouldRetryOnException("System.InvalidOperationException");

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that a well‑formed configuration passes validation.
    /// </summary>
    [Fact]
    public void IsValid_WithWellFormedConfiguration_ReturnsTrue()
    {
        // Arrange
        var policy = new RetryPolicy
        {
            MaxRetries = 3,
            InitialBackoffSeconds = 5,
            MaxBackoffSeconds = 300,
            BackoffMultiplier = 2.0
        };

        // Act
        var result = policy.IsValid();

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that a configuration where the initial backoff exceeds the maximum backoff fails validation.
    /// </summary>
    [Fact]
    public void IsValid_WhenInitialBackoffExceedsMaxBackoff_ReturnsFalse()
    {
        // Arrange
        var policy = new RetryPolicy
        {
            MaxRetries = 3,
            InitialBackoffSeconds = 400,
            MaxBackoffSeconds = 300,
            BackoffMultiplier = 2.0
        };

        // Act
        var result = policy.IsValid();

        // Assert
        result.Should().BeFalse("initial backoff cannot exceed the maximum allowed backoff");
    }
}
