#nullable enable

using System;
using System.Collections.Generic;
using JobScheduler.Core.Exceptions;
using Xunit;

namespace JobScheduler.Core.Tests
{
    /// <summary>
    /// Unit tests for <see cref="JobSchedulerExceptionExtensions"/>.
    /// </summary>
    public sealed class JobSchedulerExceptionExtensionsTests
    {
        [Fact]
        public void FormatDetails_WithExceptionWithoutErrorCode_ReturnsMessageOnly()
        {
            // Arrange
            var exception = new JobSchedulerException("Test error message");

            // Act
            var result = exception.FormatDetails();

            // Assert
            Assert.Equal("Test error message", result);
        }

        [Fact]
        public void FormatDetails_WithExceptionWithErrorCode_ReturnsFormattedMessage()
        {
            // Arrange
            var exception = new JobSchedulerException("Test error message", "JOB-001");

            // Act
            var result = exception.FormatDetails();

            // Assert
            Assert.Equal("Test error message (Error Code: JOB-001)", result);
        }

        [Fact]
        public void FormatDetails_WithNullErrorCode_ReturnsMessageOnly()
        {
            // Arrange
            var exception = new JobSchedulerException("Test error message", errorCode: null!);

            // Act
            var result = exception.FormatDetails();

            // Assert
            Assert.Equal("Test error message", result);
        }

        [Fact]
        public void FormatDetails_WithEmptyErrorCode_ReturnsFormattedMessage()
        {
            // Arrange
            var exception = new JobSchedulerException("Test error message", "");

            // Act
            var result = exception.FormatDetails();

            // Assert
            Assert.Equal("Test error message (Error Code: )", result);
        }

        [Fact]
        public void FormatDetails_WithWhitespaceErrorCode_ReturnsFormattedMessage()
        {
            // Arrange
            var exception = new JobSchedulerException("Test error message", "  JOB-001  ");

            // Act
            var result = exception.FormatDetails();

            // Assert
            Assert.Equal("Test error message (Error Code:   JOB-001  )", result);
        }

        [Fact]
        public void FormatDetails_WithNullException_ThrowsArgumentNullException()
        {
            // Arrange
            JobSchedulerException? exception = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => exception!.FormatDetails());
        }

        [Fact]
        public void IsSpecificError_WithMatchingErrorCode_ReturnsTrue()
        {
            // Arrange
            var exception = new JobSchedulerException("Test error", "JOB-001");

            // Act
            var result = exception.IsSpecificError("JOB-001");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsSpecificError_WithDifferentErrorCode_ReturnsFalse()
        {
            // Arrange
            var exception = new JobSchedulerException("Test error", "JOB-001");

            // Act
            var result = exception.IsSpecificError("JOB-002");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsSpecificError_WithNullErrorCode_ThrowsArgumentNullException()
        {
            // Arrange
            var exception = new JobSchedulerException("Test error", "JOB-001");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => exception.IsSpecificError(null!));
        }

        [Fact]
        public void IsSpecificError_WithEmptyErrorCode_ThrowsArgumentException()
        {
            // Arrange
            var exception = new JobSchedulerException("Test error", "JOB-001");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => exception.IsSpecificError(""));
        }

        [Fact]
        public void IsSpecificError_WithWhitespaceErrorCode_ReturnsFalse()
        {
            // Arrange
            var exception = new JobSchedulerException("Test error", "JOB-001");

            // Act
            var result = exception.IsSpecificError("   ");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsSpecificError_WithNullException_ThrowsArgumentNullException()
        {
            // Arrange
            JobSchedulerException? exception = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => exception!.IsSpecificError("JOB-001"));
        }

        [Fact]
        public void IsSpecificError_WithCaseInsensitiveMatching_ReturnsTrue()
        {
            // Arrange
            var exception = new JobSchedulerException("Test error", "JOB-001");

            // Act
            var result = exception.IsSpecificError("job-001");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsSpecificError_WithMixedCaseErrorCode_ReturnsTrue()
        {
            // Arrange
            var exception = new JobSchedulerException("Test error", "Job-001");

            // Act
            var result = exception.IsSpecificError("JOB-001");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void GetSummary_WithExceptionWithoutErrorCode_ReturnsCorrectSummary()
        {
            // Arrange
            var exception = new JobSchedulerException("Test error message");

            // Act
            var result = exception.GetSummary();

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal("JobSchedulerException", result["Type"]);
            Assert.Equal("Test error message", result["Message"]);
            Assert.Equal("N/A", result["ErrorCode"]);
        }

        [Fact]
        public void GetSummary_WithExceptionWithErrorCode_ReturnsCorrectSummary()
        {
            // Arrange
            var exception = new JobSchedulerException("Test error message", "JOB-001");

            // Act
            var result = exception.GetSummary();

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal("JobSchedulerException", result["Type"]);
            Assert.Equal("Test error message", result["Message"]);
            Assert.Equal("JOB-001", result["ErrorCode"]);
        }

        [Fact]
        public void GetSummary_WithDerivedException_ReturnsCorrectType()
        {
            // Arrange
            var exception = new JobNotFoundException(Guid.NewGuid());

            // Act
            var result = exception.GetSummary();

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal("JobNotFoundException", result["Type"]);
            Assert.StartsWith("Job with ID '", result["Message"].ToString());
            Assert.NotEqual("N/A", result["ErrorCode"]);
        }

        [Fact]
        public void GetSummary_WithNullException_ThrowsArgumentNullException()
        {
            // Arrange
            JobSchedulerException? exception = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => exception!.GetSummary());
        }

        [Fact]
        public void GetSummary_ReturnsReadOnlyDictionary()
        {
            // Arrange
            var exception = new JobSchedulerException("Test error message", "JOB-001");

            // Act
            var result = exception.GetSummary();

            // Assert
            Assert.IsAssignableFrom<IReadOnlyDictionary<string, object>>(result);
            Assert.IsType<Dictionary<string, object>>(result); // Implementation returns Dictionary
        }

        [Fact]
        public void GetSummary_DictionaryContainsExpectedKeys()
        {
            // Arrange
            var exception = new JobSchedulerException("Test error message", "JOB-001");

            // Act
            var result = exception.GetSummary();

            // Assert
            Assert.Contains("Type", result.Keys);
            Assert.Contains("Message", result.Keys);
            Assert.Contains("ErrorCode", result.Keys);
        }
    }
}