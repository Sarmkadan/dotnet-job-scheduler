#nullable enable
using System;
using System.Collections.Generic;
using JobScheduler.Core.Exceptions;
using Xunit;

namespace JobScheduler.Core.Tests;

public sealed class ExecutionExceptionExtensionsTests
{
    private static readonly Guid _executionId = Guid.Parse("12345678-1234-1234-1234-123456789abc");
    private static readonly Guid _jobId = Guid.Parse("87654321-4321-4321-4321-cba987654321");
    private static readonly Guid _emptyId = Guid.Empty;
    private static readonly string _message = "Test execution failed";
    private static readonly int _attemptNumber = 3;
    private static readonly int _maxAttempts = 5;

    [Fact]
    public void ToLogMessage_WithValidException_ReturnsFormattedMessage()
    {
        // Arrange
        var exception = new ExecutionException(_message, _executionId, _jobId, _attemptNumber);

        // Act
        var logMessage = exception.ToLogMessage();

        // Assert
        Assert.NotNull(logMessage);
        Assert.NotEmpty(logMessage);
        Assert.Contains("ExecutionException:", logMessage);
        Assert.Contains("ExecutionId=" + _executionId, logMessage);
        Assert.Contains("JobId=" + _jobId, logMessage);
        Assert.Contains("AttemptNumber=" + _attemptNumber, logMessage);
        Assert.Contains("Message=" + _message, logMessage);
    }

    [Fact]
    public void ToLogMessage_WithAttemptNumberZero_ReturnsCorrectFormat()
    {
        // Arrange
        var exception = new ExecutionException(_message, _executionId, _jobId, 0);

        // Act
        var logMessage = exception.ToLogMessage();

        // Assert
        Assert.Contains("AttemptNumber=0", logMessage);
    }

    [Fact]
    public void ToLogMessage_WithEmptyIds_ReturnsValidMessage()
    {
        // Arrange
        var exception = new ExecutionException(_message, _emptyId, _emptyId, _attemptNumber);

        // Act
        var logMessage = exception.ToLogMessage();

        // Assert
        Assert.NotNull(logMessage);
        Assert.Contains("ExecutionId=" + _emptyId, logMessage);
        Assert.Contains("JobId=" + _emptyId, logMessage);
        Assert.Contains("AttemptNumber=" + _attemptNumber, logMessage);
    }

    [Fact]
    public void ToLogMessage_WithNullException_ThrowsArgumentNullException()
    {
        // Arrange
        ExecutionException? exception = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => exception!.ToLogMessage());
    }

    [Fact]
    public void IsRetryable_WithAttemptLessThanMaxAttempts_ReturnsTrue()
    {
        // Arrange
        var exception = new ExecutionException(_message, _executionId, _jobId, 2);

        // Act
        var isRetryable = exception.IsRetryable(_maxAttempts);

        // Assert
        Assert.True(isRetryable);
    }

    [Fact]
    public void IsRetryable_WithAttemptEqualToMaxAttempts_ReturnsFalse()
    {
        // Arrange
        var exception = new ExecutionException(_message, _executionId, _jobId, _maxAttempts);

        // Act
        var isRetryable = exception.IsRetryable(_maxAttempts);

        // Assert
        Assert.False(isRetryable);
    }

    [Fact]
    public void IsRetryable_WithAttemptGreaterThanMaxAttempts_ReturnsFalse()
    {
        // Arrange
        var exception = new ExecutionException(_message, _executionId, _jobId, _maxAttempts + 1);

        // Act
        var isRetryable = exception.IsRetryable(_maxAttempts);

        // Assert
        Assert.False(isRetryable);
    }

    [Fact]
    public void IsRetryable_WithZeroMaxAttempts_ReturnsFalse()
    {
        // Arrange
        var exception = new ExecutionException(_message, _executionId, _jobId, 0);

        // Act
        var isRetryable = exception.IsRetryable(0);

        // Assert
        Assert.False(isRetryable);
    }

    [Fact]
    public void IsRetryable_WithNegativeMaxAttempts_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var exception = new ExecutionException(_message, _executionId, _jobId, 1);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => exception.IsRetryable(-1));
    }

    [Fact]
    public void IsRetryable_WithNullException_ThrowsArgumentNullException()
    {
        // Arrange
        ExecutionException? exception = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => exception!.IsRetryable(5));
    }

    [Fact]
    public void ToDictionary_WithValidException_ReturnsDictionaryWithAllProperties()
    {
        // Arrange
        var exception = new ExecutionException(_message, _executionId, _jobId, _attemptNumber);

        // Act
        var dictionary = exception.ToDictionary();

        // Assert
        Assert.NotNull(dictionary);
        Assert.Equal(4, dictionary.Count);
        Assert.Contains(dictionary, kvp => kvp.Key == "ExecutionId" && kvp.Value == _executionId.ToString());
        Assert.Contains(dictionary, kvp => kvp.Key == "JobId" && kvp.Value == _jobId.ToString());
        Assert.Contains(dictionary, kvp => kvp.Key == "AttemptNumber" && kvp.Value == _attemptNumber.ToString());
        Assert.Contains(dictionary, kvp => kvp.Key == "Message" && kvp.Value == _message);
    }

    [Fact]
    public void ToDictionary_WithEmptyIds_ReturnsDictionaryWithEmptyStringValues()
    {
        // Arrange
        var exception = new ExecutionException(_message, _emptyId, _emptyId, 0);

        // Act
        var dictionary = exception.ToDictionary();

        // Assert
        Assert.NotNull(dictionary);
        Assert.Equal(4, dictionary.Count);
        Assert.Equal(_emptyId.ToString(), dictionary["ExecutionId"]);
        Assert.Equal(_emptyId.ToString(), dictionary["JobId"]);
        Assert.Equal("0", dictionary["AttemptNumber"]);
        Assert.Equal(_message, dictionary["Message"]);
    }

    [Fact]
    public void ToDictionary_WithNullException_ThrowsArgumentNullException()
    {
        // Arrange
        ExecutionException? exception = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => exception!.ToDictionary());
    }

    [Fact]
    public void ToDictionary_ReturnsReadOnlyDictionary()
    {
        // Arrange
        var exception = new ExecutionException(_message, _executionId, _jobId, _attemptNumber);

        // Act
        var dictionary = exception.ToDictionary();

        // Assert
        Assert.IsAssignableFrom<IReadOnlyDictionary<string, string>>(dictionary);
    }

    [Fact]
    public void GetCorrelationInfo_WithValidException_ReturnsCorrectTuple()
    {
        // Arrange
        var exception = new ExecutionException(_message, _executionId, _jobId, _attemptNumber);

        // Act
        var (executionId, jobId) = exception.GetCorrelationInfo();

        // Assert
        Assert.Equal(_executionId, executionId);
        Assert.Equal(_jobId, jobId);
    }

    [Fact]
    public void GetCorrelationInfo_WithNullException_ThrowsArgumentNullException()
    {
        // Arrange
        ExecutionException? exception = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => exception!.GetCorrelationInfo());
    }
}