#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Exceptions;
using Xunit;

namespace JobScheduler.Core.Tests;

/// <summary>
/// Tests for the construction-time rules enforced by <see cref="RetryPolicy.EnsureValid"/>
/// and the agreeing <see cref="RetryPolicy.IsValid"/> check.
/// </summary>
public sealed class RetryPolicyValidationTests
{
    private static RetryPolicy CreateValidPolicy() => new()
    {
        MaxRetries = 3,
        InitialBackoffSeconds = 5,
        MaxBackoffSeconds = 300,
        BackoffMultiplier = 2.0
    };

    /// <summary>
    /// Verifies that a well-formed retry policy passes <see cref="RetryPolicy.EnsureValid"/>.
    /// </summary>
    [Fact]
    public void EnsureValid_WithValidPolicy_DoesNotThrow()
    {
        var policy = CreateValidPolicy();

        var exception = Record.Exception(() => policy.EnsureValid());

        Assert.Null(exception);
    }

    /// <summary>
    /// Verifies that the boundary values 0 (retries, delays) and 1.0 (multiplier) are accepted.
    /// </summary>
    [Fact]
    public void EnsureValid_AtLowerBoundaries_DoesNotThrow()
    {
        var policy = new RetryPolicy
        {
            MaxRetries = 0,
            InitialBackoffSeconds = 0,
            MaxBackoffSeconds = 0,
            BackoffMultiplier = 1.0
        };

        var exception = Record.Exception(() => policy.EnsureValid());

        Assert.Null(exception);
    }

    /// <summary>
    /// Verifies that an initial backoff equal to the maximum backoff is accepted.
    /// </summary>
    [Fact]
    public void EnsureValid_WhenInitialEqualsMax_DoesNotThrow()
    {
        var policy = CreateValidPolicy();
        policy.InitialBackoffSeconds = 60;
        policy.MaxBackoffSeconds = 60;

        var exception = Record.Exception(() => policy.EnsureValid());

        Assert.Null(exception);
    }

    /// <summary>
    /// Verifies that a negative MaxRetries is rejected, and the exception names the property and the offending value.
    /// </summary>
    /// <param name="maxRetries">The invalid number of retries.</param>
    [Theory]
    [InlineData(-1)]
    [InlineData(-50)]
    public void EnsureValid_WhenMaxRetriesIsNegative_ThrowsNamingPropertyAndValue(int maxRetries)
    {
        var policy = CreateValidPolicy();
        policy.MaxRetries = maxRetries;

        var exception = Assert.Throws<JobValidationException>(() => policy.EnsureValid());

        Assert.Equal(nameof(RetryPolicy.MaxRetries), exception.PropertyName);
        Assert.Equal($"MaxRetries must be >= 0. Current value: {maxRetries}", exception.Message);
    }

    /// <summary>
    /// Verifies that a negative initial backoff is rejected, and the exception names the property and the offending value.
    /// </summary>
    [Fact]
    public void EnsureValid_WhenInitialBackoffIsNegative_ThrowsNamingPropertyAndValue()
    {
        var policy = CreateValidPolicy();
        policy.InitialBackoffSeconds = -5;

        var exception = Assert.Throws<JobValidationException>(() => policy.EnsureValid());

        Assert.Equal(nameof(RetryPolicy.InitialBackoffSeconds), exception.PropertyName);
        Assert.Equal("InitialBackoffSeconds must be >= 0. Current value: -5", exception.Message);
    }

    /// <summary>
    /// Verifies that a negative maximum backoff is rejected, and the exception names the property and the offending value.
    /// </summary>
    [Fact]
    public void EnsureValid_WhenMaxBackoffIsNegative_ThrowsNamingPropertyAndValue()
    {
        var policy = CreateValidPolicy();
        policy.InitialBackoffSeconds = 0;
        policy.MaxBackoffSeconds = -1;

        var exception = Assert.Throws<JobValidationException>(() => policy.EnsureValid());

        Assert.Equal(nameof(RetryPolicy.MaxBackoffSeconds), exception.PropertyName);
        Assert.Equal("MaxBackoffSeconds must be >= 0. Current value: -1", exception.Message);
    }

    /// <summary>
    /// Verifies that an initial backoff greater than the maximum backoff is rejected, and the message shows both values.
    /// </summary>
    [Fact]
    public void EnsureValid_WhenInitialBackoffExceedsMax_ThrowsWithBothValues()
    {
        var policy = CreateValidPolicy();
        policy.InitialBackoffSeconds = 400;
        policy.MaxBackoffSeconds = 300;

        var exception = Assert.Throws<JobValidationException>(() => policy.EnsureValid());

        Assert.Equal(nameof(RetryPolicy.InitialBackoffSeconds), exception.PropertyName);
        Assert.Equal("InitialBackoffSeconds must be <= MaxBackoffSeconds. Current values: Initial=400, Max=300", exception.Message);
    }

    /// <summary>
    /// Verifies that a backoff multiplier below 1 is rejected, and the exception names the property and the offending value.
    /// </summary>
    /// <param name="multiplier">The invalid multiplier.</param>
    [Theory]
    [InlineData(0.5)]
    [InlineData(0.0)]
    [InlineData(-2.0)]
    public void EnsureValid_WhenBackoffMultiplierBelowOne_ThrowsNamingPropertyAndValue(double multiplier)
    {
        var policy = CreateValidPolicy();
        policy.BackoffMultiplier = multiplier;

        var exception = Assert.Throws<JobValidationException>(() => policy.EnsureValid());

        Assert.Equal(nameof(RetryPolicy.BackoffMultiplier), exception.PropertyName);
        Assert.Equal($"BackoffMultiplier must be >= 1. Current value: {multiplier}", exception.Message);
    }

    /// <summary>
    /// Verifies that <see cref="RetryPolicy.IsValid"/> agrees with <see cref="RetryPolicy.EnsureValid"/> on a zero initial backoff.
    /// WHY: A registered policy must not be silently replaced by defaults at runtime because the two checks disagree.
    /// </summary>
    [Fact]
    public void IsValid_WhenInitialBackoffIsZero_ReturnsTrue()
    {
        var policy = CreateValidPolicy();
        policy.InitialBackoffSeconds = 0;

        Assert.True(policy.IsValid());
        Assert.Null(Record.Exception(() => policy.EnsureValid()));
    }

    /// <summary>
    /// Verifies that <see cref="RetryPolicy.IsValid"/> returns false for a negative MaxRetries.
    /// </summary>
    [Fact]
    public void IsValid_WhenMaxRetriesIsNegative_ReturnsFalse()
    {
        var policy = CreateValidPolicy();
        policy.MaxRetries = -1;

        Assert.False(policy.IsValid());
    }
}
