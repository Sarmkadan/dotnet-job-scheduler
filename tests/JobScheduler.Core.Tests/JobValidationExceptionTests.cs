#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using Xunit;
using JobScheduler.Core.Exceptions;

namespace JobScheduler.Core.Tests;

/// <summary>
/// Unit tests for JobValidationException.
/// Validates exception construction, property assignment, and inheritance.
/// </summary>
public sealed class JobValidationExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_SetsMessageAndDefaultsPropertyName()
    {
        // Arrange
        var message = "Job validation failed";

        // Act
        var exception = new JobValidationException(message);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Null(exception.PropertyName);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void Constructor_WithMessageAndPropertyName_SetsPropertiesCorrectly()
    {
        // Arrange
        var message = "Invalid priority";
        var propertyName = "Priority";

        // Act
        var exception = new JobValidationException(message, propertyName);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(propertyName, exception.PropertyName);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_SetsPropertiesCorrectly()
    {
        // Arrange
        var message = "Validation error occurred";
        var innerException = new InvalidOperationException("Inner failure");

        // Act
        var exception = new JobValidationException(message, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Same(innerException, exception.InnerException);
        Assert.Null(exception.PropertyName);
    }

    [Fact]
    public void PropertyName_CanBeModifiedAfterConstruction()
    {
        // Arrange
        var exception = new JobValidationException("Test");
        var newPropertyName = "UpdatedProperty";

        // Act
        exception.PropertyName = newPropertyName;

        // Assert
        Assert.Equal(newPropertyName, exception.PropertyName);
    }

    [Fact]
    public void JobValidationException_IsAssignableToJobSchedulerException()
    {
        // Arrange & Act
        var exception = new JobValidationException("Test");

        // Assert
        Assert.IsAssignableFrom<JobSchedulerException>(exception);
    }
}
