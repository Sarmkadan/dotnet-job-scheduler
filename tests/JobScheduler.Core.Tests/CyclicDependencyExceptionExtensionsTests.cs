#nullable enable
using System;
using System.Collections.Generic;
using JobScheduler.Core.Exceptions;
using Xunit;

namespace JobScheduler.Core.Tests;

public sealed class CyclicDependencyExceptionExtensionsTests
{
    private static readonly Guid _jobId = Guid.Parse("12345678-1234-1234-1234-123456789abc");
    private static readonly Guid _dependsOnId = Guid.Parse("87654321-4321-4321-4321-cba987654321");
    private static readonly Guid _anotherJobId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid _emptyId = Guid.Empty;
    private static readonly string _errorCode = "CYCLIC_DEP_001";
    private static readonly string _anotherErrorCode = "CYCLIC_DEP_002";

    [Fact]
    public void GetDescription_WithValidException_ReturnsFormattedDescription()
    {
        // Arrange
        var exception = new CyclicDependencyException(_jobId, _dependsOnId);

        // Act
        var description = exception.GetDescription();

        // Assert
        Assert.NotNull(description);
        Assert.NotEmpty(description);
        Assert.Contains(_jobId.ToString(), description);
        Assert.Contains(_dependsOnId.ToString(), description);
    }

    [Fact]
    public void GetDescription_WithNullException_ThrowsArgumentNullException()
    {
        // Arrange
        CyclicDependencyException? exception = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => exception!.GetDescription());
    }

    [Fact]
    public void GetDescription_WithEmptyGuids_ReturnsValidDescription()
    {
        // Arrange
        var exception = new CyclicDependencyException(_emptyId, _emptyId);

        // Act
        var description = exception.GetDescription();

        // Assert
        Assert.NotNull(description);
        Assert.Contains(_emptyId.ToString(), description);
    }

    [Fact]
    public void InvolvesJob_WithMatchingJobId_ReturnsTrue()
    {
        // Arrange
        var exception = new CyclicDependencyException(_jobId, _dependsOnId);

        // Act
        var result = exception.InvolvesJob(_jobId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void InvolvesJob_WithMatchingDependsOnJobId_ReturnsTrue()
    {
        // Arrange
        var exception = new CyclicDependencyException(_jobId, _dependsOnId);

        // Act
        var result = exception.InvolvesJob(_dependsOnId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void InvolvesJob_WithNonMatchingJobId_ReturnsFalse()
    {
        // Arrange
        var exception = new CyclicDependencyException(_jobId, _dependsOnId);

        // Act
        var result = exception.InvolvesJob(_anotherJobId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void InvolvesJob_WithEmptyGuid_ReturnsFalse()
    {
        // Arrange
        var exception = new CyclicDependencyException(_jobId, _dependsOnId);

        // Act
        var result = exception.InvolvesJob(_emptyId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void InvolvesJob_WithNullException_ThrowsArgumentNullException()
    {
        // Arrange
        CyclicDependencyException? exception = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => exception!.InvolvesJob(Guid.NewGuid()));
    }

    [Fact]
    public void FormatDetails_WithValidException_ReturnsFormattedDetails()
    {
        // Arrange
        var exception = new CyclicDependencyException(_jobId, _dependsOnId);

        // Act
        var details = exception.FormatDetails();

        // Assert
        Assert.NotNull(details);
        Assert.NotEmpty(details);
        Assert.Contains("Cyclic dependency detected", details);
        Assert.Contains(_jobId.ToString(), details);
        Assert.Contains(_dependsOnId.ToString(), details);
    }

    [Fact]
    public void FormatDetails_WithErrorCode_ReturnsDetailsWithErrorCode()
    {
        // Arrange
        var exception = new CyclicDependencyException(_jobId, _dependsOnId) { ErrorCode = _errorCode };

        // Act
        var details = exception.FormatDetails();

        // Assert
        Assert.NotNull(details);
        Assert.Contains(_errorCode, details);
    }

    [Fact]
    public void FormatDetails_WithDefaultErrorCode_ReturnsDetailsWithErrorCode()
    {
        // Arrange
        var exception = new CyclicDependencyException(_jobId, _dependsOnId);

        // Act
        var details = exception.FormatDetails();

        // Assert
        Assert.NotNull(details);
        Assert.Contains("(Error Code: CYCLIC_DEPENDENCY_DETECTED)", details);
    }

    [Fact]
    public void FormatDetails_WithNullException_ThrowsArgumentNullException()
    {
        // Arrange
        CyclicDependencyException? exception = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => exception!.FormatDetails());
    }

    [Fact]
    public void FormatDetails_WithEmptyGuids_ReturnsValidDetails()
    {
        // Arrange
        var exception = new CyclicDependencyException(_emptyId, _emptyId);

        // Act
        var details = exception.FormatDetails();

        // Assert
        Assert.NotNull(details);
        Assert.Contains(_emptyId.ToString(), details);
    }

    [Fact]
    public void IsSpecificError_WithMatchingErrorCode_ReturnsTrue()
    {
        // Arrange
        var exception = new CyclicDependencyException(_jobId, _dependsOnId) { ErrorCode = _errorCode };

        // Act
        var result = exception.IsSpecificError(_errorCode);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsSpecificError_WithDifferentErrorCode_ReturnsFalse()
    {
        // Arrange
        var exception = new CyclicDependencyException(_jobId, _dependsOnId) { ErrorCode = _errorCode };

        // Act
        var result = exception.IsSpecificError(_anotherErrorCode);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsSpecificError_WithCaseInsensitiveMatching_ReturnsTrue()
    {
        // Arrange
        var exception = new CyclicDependencyException(_jobId, _dependsOnId) { ErrorCode = _errorCode };

        // Act
        var result = exception.IsSpecificError(_errorCode.ToLowerInvariant());

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsSpecificError_WithNullErrorCode_ThrowsArgumentNullException()
    {
        // Arrange
        var exception = new CyclicDependencyException(_jobId, _dependsOnId) { ErrorCode = _errorCode };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => exception.IsSpecificError(null!));
    }

    [Fact]
    public void IsSpecificError_WithEmptyErrorCode_ThrowsArgumentException()
    {
        // Arrange
        var exception = new CyclicDependencyException(_jobId, _dependsOnId) { ErrorCode = _errorCode };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => exception.IsSpecificError(string.Empty));
    }


    [Fact]
    public void IsSpecificError_WithNullException_ThrowsArgumentNullException()
    {
        // Arrange
        CyclicDependencyException? exception = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => exception!.IsSpecificError(_errorCode));
    }

    [Fact]
    public void GetSummary_WithValidException_ReturnsDictionaryWithAllProperties()
    {
        // Arrange
        var exception = new CyclicDependencyException(_jobId, _dependsOnId) { ErrorCode = _errorCode };

        // Act
        var summary = exception.GetSummary();

        // Assert
        Assert.NotNull(summary);
        Assert.Equal(5, summary.Count);
        Assert.Contains(summary, kvp => kvp.Key == "Type" && kvp.Value is string type && type.Contains("CyclicDependencyException"));
        Assert.Contains(summary, kvp => kvp.Key == "Message" && kvp.Value is string message && message.Contains("Cannot add dependency"));
        Assert.Contains(summary, kvp => kvp.Key == "ErrorCode" && kvp.Value is string errorCode && errorCode == _errorCode);
        Assert.Contains(summary, kvp => kvp.Key == "JobId" && kvp.Value is Guid jobId && jobId == _jobId);
        Assert.Contains(summary, kvp => kvp.Key == "DependsOnJobId" && kvp.Value is Guid dependsOnId && dependsOnId == _dependsOnId);
    }

    [Fact]
    public void GetSummary_WithoutExplicitErrorCode_ReturnsDictionaryWithDefaultErrorCode()
    {
        // Arrange
        var exception = new CyclicDependencyException(_jobId, _dependsOnId);

        // Act
        var summary = exception.GetSummary();

        // Assert
        Assert.NotNull(summary);
        Assert.Equal("CYCLIC_DEPENDENCY_DETECTED", summary["ErrorCode"]);
    }

    [Fact]
    public void GetSummary_WithEmptyGuids_ReturnsDictionaryWithEmptyGuids()
    {
        // Arrange
        var exception = new CyclicDependencyException(_emptyId, _emptyId) { ErrorCode = _errorCode };

        // Act
        var summary = exception.GetSummary();

        // Assert
        Assert.NotNull(summary);
        Assert.Equal(_emptyId, summary["JobId"]);
        Assert.Equal(_emptyId, summary["DependsOnJobId"]);
    }

    [Fact]
    public void GetSummary_WithNullException_ThrowsArgumentNullException()
    {
        // Arrange
        CyclicDependencyException? exception = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => exception!.GetSummary());
    }

    [Fact]
    public void GetSummary_ReturnsReadOnlyDictionary()
    {
        // Arrange
        var exception = new CyclicDependencyException(_jobId, _dependsOnId);

        // Act
        var summary = exception.GetSummary();

        // Assert
        Assert.IsAssignableFrom<IReadOnlyDictionary<string, object>>(summary);
    }
}