using System;
using System.Collections.Generic;
using JobScheduler.Core.Domain.Entities;
using Xunit;

namespace JobScheduler.Core.Tests;

public class JobScheduleHistoryValidationTests
{
    private JobScheduleHistory CreateValidHistory()
    {
        return new JobScheduleHistory
        {
            Id = Guid.NewGuid(),
            JobId = Guid.NewGuid(),
            PropertyName = "TestProperty",
            ChangeReason = "TestReason",
            ChangedAt = DateTime.UtcNow
        };
    }

    [Fact]
    public void Validate_ValidInstance_ReturnsEmptyList()
    {
        var history = CreateValidHistory();

        var result = history.Validate();

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void Validate_InvalidInstance_ReturnsErrors()
    {
        var history = new JobScheduleHistory
        {
            Id = Guid.Empty,
            JobId = Guid.Empty,
            PropertyName = "   ",
            ChangeReason = null,
            ChangedAt = default
        };

        var errors = history.Validate();

        Assert.NotNull(errors);
        Assert.Equal(5, errors.Count);
        Assert.Contains("Id cannot be empty.", errors);
        Assert.Contains("JobId cannot be empty.", errors);
        Assert.Contains("PropertyName cannot be null or whitespace.", errors);
        Assert.Contains("ChangeReason cannot be null or whitespace.", errors);
        Assert.Contains("ChangedAt cannot be the default value.", errors);
    }

    [Fact]
    public void Validate_NullInstance_ThrowsArgumentNullException()
    {
        JobScheduleHistory? history = null;

        Assert.Throws<ArgumentNullException>(() => history!.Validate());
    }

    [Fact]
    public void IsValid_ValidInstance_ReturnsTrue()
    {
        var history = CreateValidHistory();

        var result = history.IsValid();

        Assert.True(result);
    }

    [Fact]
    public void IsValid_InvalidInstance_ReturnsFalse()
    {
        var history = new JobScheduleHistory
        {
            Id = Guid.NewGuid(),
            JobId = Guid.Empty,
            PropertyName = "",
            ChangeReason = "Reason",
            ChangedAt = DateTime.UtcNow
        };

        var result = history.IsValid();

        Assert.False(result);
    }

    [Fact]
    public void IsValid_NullInstance_ThrowsArgumentNullException()
    {
        JobScheduleHistory? history = null;

        Assert.Throws<ArgumentNullException>(() => history!.IsValid());
    }

    [Fact]
    public void EnsureValid_ValidInstance_DoesNotThrow()
    {
        var history = CreateValidHistory();

        var exception = Record.Exception(() => history.EnsureValid());

        Assert.Null(exception);
    }

    [Fact]
    public void EnsureValid_InvalidInstance_ThrowsArgumentException()
    {
        var history = new JobScheduleHistory
        {
            Id = Guid.Empty,
            JobId = Guid.Empty,
            PropertyName = null,
            ChangeReason = "",
            ChangedAt = default
        };

        var ex = Assert.Throws<ArgumentException>(() => history.EnsureValid());

        Assert.Contains("JobScheduleHistory validation failed", ex.Message);
        Assert.Contains("Id cannot be empty.", ex.Message);
        Assert.Contains("JobId cannot be empty.", ex.Message);
        Assert.Contains("PropertyName cannot be null or whitespace.", ex.Message);
        Assert.Contains("ChangeReason cannot be null or whitespace.", ex.Message);
        Assert.Contains("ChangedAt cannot be the default value.", ex.Message);
    }
}
