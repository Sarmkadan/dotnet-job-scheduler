#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Linq;
using Xunit;
using JobScheduler.Core.Services;

namespace JobScheduler.Core.Tests;

/// <summary>
/// Unit tests for <see cref="DatabaseLeaderElectionServiceValidation"/>.
/// </summary>
public sealed class DatabaseLeaderElectionServiceValidationTests
{
    private static SchedulerLeaderLock CreateValidLock()
    {
        return new SchedulerLeaderLock
        {
            Id = 1,
            LockName = SchedulerLeaderLock.DefaultLockName,
            LeaderInstanceId = "instance-a",
            AcquiredAt = DateTime.UtcNow,
            LeaseExpiresAt = DateTime.UtcNow.AddMinutes(1)
        };
    }

    /// <summary>
    /// A fully populated lock with UTC timestamps produces no validation problems.
    /// </summary>
    [Fact]
    public void Validate_ValidLock_ReturnsEmptyListAndIsValid()
    {
        SchedulerLeaderLock? entity = CreateValidLock();

        var problems = entity.Validate();

        Assert.Empty(problems);
        Assert.True(entity.IsValid());
        entity.EnsureValid();
    }

    /// <summary>
    /// Null input throws <see cref="ArgumentNullException"/> from every public member.
    /// </summary>
    [Fact]
    public void AllMembers_NullValue_ThrowArgumentNullException()
    {
        SchedulerLeaderLock? entity = null;

        Assert.Throws<ArgumentNullException>(() => entity.Validate());
        Assert.Throws<ArgumentNullException>(() => entity.IsValid());
        Assert.Throws<ArgumentNullException>(() => entity.EnsureValid());
    }

    /// <summary>
    /// Non-positive Id and blank text fields are each reported as a separate problem.
    /// </summary>
    [Fact]
    public void Validate_BlankAndNonPositiveFields_ReportsEachProblem()
    {
        var entity = CreateValidLock();
        entity.Id = 0;
        entity.LockName = "   ";
        entity.LeaderInstanceId = string.Empty;

        var problems = entity.Validate();

        Assert.Equal(3, problems.Count);
        Assert.Contains(problems, p => p.StartsWith("Id must be a positive integer"));
        Assert.Contains(problems, p => p == "LockName cannot be null or whitespace.");
        Assert.Contains(problems, p => p == "LeaderInstanceId cannot be null or whitespace.");
        Assert.False(entity.IsValid());
    }

    /// <summary>
    /// Text fields at exactly 100 characters are accepted; 101 characters are rejected.
    /// </summary>
    [Fact]
    public void Validate_TextLengthBoundary_AcceptsExactly100AndRejects101()
    {
        var atLimit = CreateValidLock();
        atLimit.LockName = new string('a', 100);
        atLimit.LeaderInstanceId = new string('b', 100);

        Assert.Empty(atLimit.Validate());

        var overLimit = CreateValidLock();
        overLimit.LockName = new string('a', 101);
        overLimit.LeaderInstanceId = new string('b', 101);

        var problems = overLimit.Validate();

        Assert.Equal(2, problems.Count);
        Assert.Contains("LockName cannot exceed 100 characters.", problems);
        Assert.Contains("LeaderInstanceId cannot exceed 100 characters.", problems);
    }

    /// <summary>
    /// Timestamps are checked for default value, UTC kind, staleness, future drift and ordering.
    /// </summary>
    [Fact]
    public void Validate_InvalidTimestamps_ReportsTimeRuleViolations()
    {
        var defaultTimes = CreateValidLock();
        defaultTimes.LeaseExpiresAt = default;
        defaultTimes.AcquiredAt = default;
        Assert.Contains("LeaseExpiresAt cannot be the default DateTime value.", defaultTimes.Validate());
        Assert.Contains("AcquiredAt cannot be the default DateTime value.", defaultTimes.Validate());

        var localKind = CreateValidLock();
        localKind.LeaseExpiresAt = DateTime.SpecifyKind(DateTime.UtcNow.AddMinutes(1), DateTimeKind.Local);
        Assert.Contains("LeaseExpiresAt must be in UTC format.", localKind.Validate());

        var expiredLease = CreateValidLock();
        expiredLease.LeaseExpiresAt = DateTime.UtcNow.AddMinutes(-6);
        expiredLease.AcquiredAt = DateTime.UtcNow.AddMinutes(-10);
        Assert.Contains("LeaseExpiresAt cannot be in the past (more than 5 minutes ago).", expiredLease.Validate());

        var futureAcquired = CreateValidLock();
        futureAcquired.AcquiredAt = DateTime.UtcNow.AddMinutes(6);
        futureAcquired.LeaseExpiresAt = DateTime.UtcNow.AddMinutes(10);
        Assert.Contains("AcquiredAt cannot be in the future (more than 5 minutes ahead).", futureAcquired.Validate());

        var inverted = CreateValidLock();
        inverted.AcquiredAt = DateTime.UtcNow.AddMinutes(2);
        inverted.LeaseExpiresAt = DateTime.UtcNow.AddMinutes(1);
        Assert.Contains("AcquiredAt cannot be after LeaseExpiresAt.", inverted.Validate());
    }

    /// <summary>
    /// EnsureValid throws <see cref="ArgumentException"/> whose message embeds every validation problem.
    /// </summary>
    [Fact]
    public void EnsureValid_InvalidLock_ThrowsArgumentExceptionWithProblems()
    {
        var entity = CreateValidLock();
        entity.Id = -5;

        var ex = Assert.Throws<ArgumentException>(() => entity.EnsureValid());

        Assert.StartsWith("SchedulerLeaderLock is not valid. Problems:", ex.Message);
        Assert.Contains("Id must be a positive integer, but was -5.", ex.Message);
    }
}
