using System;
using System.Collections.Generic;
using System.Linq;
using JobScheduler.Core.Domain.Models;
using JobScheduler.Core.Constants;
using Xunit;

namespace JobScheduler.Core.Tests;

public class JobHistoryQueryValidationTests
{
    private static JobHistoryQuery CreateValidQuery()
    {
        return new JobHistoryQuery
        {
            // All required fields for a happy‑path validation
            PageNumber = 1,
            PageSize = 10,
            // Optional fields left null (they are valid when not set)
            Status = null,
            From = null,
            To = null
        };
    }

    [Fact]
    public void Validate_HappyPath_ReturnsEmptyList()
    {
        // Arrange
        var query = CreateValidQuery();

        // Act
        var errors = query.Validate();

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void IsValid_HappyPath_ReturnsTrue()
    {
        // Arrange
        var query = CreateValidQuery();

        // Act
        var result = query.IsValid();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void EnsureValid_HappyPath_DoesNotThrow()
    {
        // Arrange
        var query = CreateValidQuery();

        // Act / Assert
        var exception = Record.Exception(() => query.EnsureValid());
        Assert.Null(exception);
    }

    [Fact]
    public void Validate_WithValidStatus_DoesNotAddError()
    {
        // Arrange
        var query = CreateValidQuery();
        query.Status = Enum.GetValues<ExecutionStatus>().First();

        // Act
        var errors = query.Validate();

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_InvalidPageNumber_ReturnsError()
    {
        // Arrange
        var query = CreateValidQuery();
        query.PageNumber = 0; // below MinPageNumber (1)

        // Act
        var errors = query.Validate();

        // Assert
        Assert.Contains(errors, e => e.Contains("PageNumber"));
    }

    [Fact]
    public void Validate_InvalidPageSize_TooLarge_ReturnsError()
    {
        // Arrange
        var query = CreateValidQuery();
        query.PageSize = 201; // above MaxPageSize (200)

        // Act
        var errors = query.Validate();

        // Assert
        Assert.Contains(errors, e => e.Contains("PageSize"));
    }

    [Fact]
    public void Validate_FromAfterTo_ReturnsError()
    {
        // Arrange
        var query = CreateValidQuery();
        query.From = DateTime.UtcNow.AddHours(1);
        query.To   = DateTime.UtcNow;

        // Act
        var errors = query.Validate();

        // Assert
        Assert.Contains(errors, e => e.Contains("From date cannot be after To date"));
    }

    [Fact]
    public void Validate_NullQuery_ThrowsArgumentNullException()
    {
        // Arrange
        JobHistoryQuery? query = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => query!.Validate());
    }

    [Fact]
    public void IsValid_NullQuery_ReturnsFalse()
    {
        // Arrange
        JobHistoryQuery? query = null;

        // Act
        var result = query.IsValid();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void EnsureValid_NullQuery_ThrowsArgumentNullException()
    {
        // Arrange
        JobHistoryQuery? query = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => query!.EnsureValid());
    }

    [Fact]
    public void EnsureValid_InvalidQuery_ThrowsArgumentException_WithAllProblems()
    {
        // Arrange
        var query = new JobHistoryQuery
        {
            PageNumber = 0,   // invalid
            PageSize = 0,     // invalid
            From = DateTime.UtcNow.AddHours(2),
            To   = DateTime.UtcNow.AddHours(1) // From > To
        };

        // Act
        var ex = Assert.Throws<ArgumentException>(() => query.EnsureValid());

        // Assert
        Assert.Contains("PageNumber", ex.Message);
        Assert.Contains("PageSize", ex.Message);
        Assert.Contains("From date cannot be after To date", ex.Message);
    }
}
