#nullable enable

using System;
using JobScheduler.Core.Exceptions;
using Xunit;

namespace JobScheduler.Core.Tests
{
    /// <summary>
    /// Unit tests for <see cref="JobSchedulerException"/>.
    /// </summary>
    public sealed class JobSchedulerExceptionTests
    {
        [Fact]
        public void Constructor_SetsMessageCorrectly()
        {
            // Arrange
            const string message = "Test message";

            // Act
            var ex = new JobSchedulerException(message);

            // Assert
            Assert.Equal(message, ex.Message);
        }

        [Fact]
        public void Constructor_WithErrorCode_SetsErrorCodeCorrectly()
        {
            // Arrange
            const string message = "Test message";
            const string errorCode = "Test error code";

            // Act
            var ex = new JobSchedulerException(message, errorCode);

            // Assert
            Assert.Equal(errorCode, ex.ErrorCode);
        }

        [Fact]
        public void Constructor_WithInnerException_SetsInnerExceptionCorrectly()
        {
            // Arrange
            const string message = "Test message";
            var innerException = new InvalidOperationException("Inner exception");

            // Act
            var ex = new JobSchedulerException(message, innerException);

            // Assert
            Assert.Same(innerException, ex.InnerException);
        }

        [Fact]
        public void ErrorCode_Property_ReturnsNull_WhenNotSet()
        {
            // Arrange
            var ex = new JobSchedulerException("Test message");

            // Act & Assert
            Assert.Null(ex.ErrorCode);
        }

        [Fact]
        public void ErrorCode_Property_ReturnsSetErrorCode()
        {
            // Arrange
            var ex = new JobSchedulerException("Test message", "Test error code");

            // Act & Assert
            Assert.Equal("Test error code", ex.ErrorCode);
        }

        [Fact]
        public void Message_Property_ReturnsMessage()
        {
            // Arrange
            var ex = new JobSchedulerException("Test message");

            // Act & Assert
            Assert.Equal("Test message", ex.Message);
        }

        [Fact]
        public void Throws_WhenMessageIsNull()
        {
            // Arrange
            string? message = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new JobSchedulerException(message));
        }

        [Fact]
        public void Throws_WhenErrorCodeIsNull()
        {
            // Arrange
            string? errorCode = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new JobSchedulerException("Test message", errorCode));
        }
    }
}
