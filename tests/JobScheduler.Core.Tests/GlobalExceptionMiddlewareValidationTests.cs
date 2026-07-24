// tests/JobScheduler.Core.Tests/GlobalExceptionMiddlewareValidationTests.cs
using System;
using System.Collections.Generic;
using System.Globalization;
using JobScheduler.Core.Middleware;
using Xunit;

namespace JobScheduler.Core.Tests;

public class GlobalExceptionMiddlewareValidationTests
{
    [Fact]
    public void Validate_ReturnsEmptyList_WhenValueIsValid()
    {
        // Arrange
        var value = new ErrorResponse { Message = "Test message", Timestamp = DateTime.UtcNow };

        // Act
        var problems = GlobalExceptionMiddlewareValidation.Validate(value);

        // Assert
        Assert.Empty(problems);
    }

    [Fact]
    public void Validate_ReturnsListWithMessageProblem_WhenMessageIsEmpty()
    {
        // Arrange
        var value = new ErrorResponse { Message = string.Empty, Timestamp = DateTime.UtcNow };

        // Act
        var problems = GlobalExceptionMiddlewareValidation.Validate(value);

        // Assert
        Assert.Single(problems);
        Assert.Equal("Message must be a non-empty string. Current value: ''", problems[0]);
    }

    [Fact]
    public void Validate_ReturnsListWithTimestampProblem_WhenTimestampIsDefault()
    {
        // Arrange
        var value = new ErrorResponse { Message = "Test message", Timestamp = default };

        // Act
        var problems = GlobalExceptionMiddlewareValidation.Validate(value);

        // Assert
        Assert.Single(problems);
        Assert.Equal("Timestamp must be set to a valid DateTime (cannot be default/DateTime.MinValue)", problems[0]);
    }

    [Fact]
    public void Validate_ReturnsListWithExceptionTypeProblem_WhenExceptionTypeIsEmpty()
    {
        // Arrange
        var value = new ErrorResponse { Message = "Test message", Timestamp = DateTime.UtcNow, ExceptionType = string.Empty };

        // Act
        var problems = GlobalExceptionMiddlewareValidation.Validate(value);

        // Assert
        Assert.Single(problems);
        Assert.Equal("ExceptionType must be null or a non-empty string", problems[0]);
    }

    [Fact]
    public void IsValid_ReturnsTrue_WhenValueIsValid()
    {
        // Arrange
        var value = new ErrorResponse { Message = "Test message", Timestamp = DateTime.UtcNow };

        // Act
        var isValid = GlobalExceptionMiddlewareValidation.IsValid(value);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void IsValid_ReturnsFalse_WhenMessageIsEmpty()
    {
        // Arrange
        var value = new ErrorResponse { Message = string.Empty, Timestamp = DateTime.UtcNow };

        // Act
        var isValid = GlobalExceptionMiddlewareValidation.IsValid(value);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void EnsureValid_ThrowsArgumentException_WhenValueIsValid()
    {
        // Arrange
        var value = new ErrorResponse { Message = "Test message", Timestamp = DateTime.UtcNow };

        // Act and Assert
        Assert.Throws<ArgumentException>(() => GlobalExceptionMiddlewareValidation.EnsureValid(value));
    }

    [Fact]
    public void EnsureValid_ThrowsArgumentException_WhenMessageIsEmpty()
    {
        // Arrange
        var value = new ErrorResponse { Message = string.Empty, Timestamp = DateTime.UtcNow };

        // Act and Assert
        Assert.Throws<ArgumentException>(() => GlobalExceptionMiddlewareValidation.EnsureValid(value));
    }
}
