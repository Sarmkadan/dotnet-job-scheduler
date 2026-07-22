#nullable enable

using System;
using JobScheduler.Core.Exceptions;
using Xunit;

namespace JobScheduler.Core.Tests
{
    /// <summary>
    /// Unit tests for <see cref="CronExpressionException"/>.
    /// </summary>
    public sealed class CronExpressionExceptionTests
    {
        [Fact]
        public void Constructor_WithCronAndMessage_SetsPropertiesAndMessage()
        {
            // Arrange
            const string cron = "0 0 * * *";
            const string msg = "Invalid field";

            // Act
            var ex = new CronExpressionException(cron, msg);

            // Assert
            Assert.Equal(cron, ex.CronExpression);
            Assert.Equal($"Invalid cron expression '{cron}': {msg}", ex.Message);
            Assert.Null(ex.InnerException);
        }

        [Fact]
        public void Constructor_WithCronMessageAndInnerException_SetsAllProperties()
        {
            // Arrange
            const string cron = "*/5 * * * *";
            const string msg = "Bad syntax";
            var inner = new InvalidOperationException("inner");

            // Act
            var ex = new CronExpressionException(cron, msg, inner);

            // Assert
            Assert.Equal(cron, ex.CronExpression);
            Assert.Equal($"Invalid cron expression '{cron}': {msg}", ex.Message);
            Assert.Same(inner, ex.InnerException);
        }

        [Fact]
        public void Constructor_NullCronExpression_HandlesGracefully()
        {
            // Arrange
            string? cron = null;
            const string msg = "null cron";

            // Act
            var ex = new CronExpressionException(cron!, msg);

            // Assert
            Assert.Null(ex.CronExpression);
            Assert.Equal($"Invalid cron expression '': {msg}", ex.Message);
        }

        [Fact]
        public void Constructor_EmptyMessage_IncludesEmptyMessageInOutput()
        {
            // Arrange
            const string cron = "0 12 * * MON";
            const string msg = "";

            // Act
            var ex = new CronExpressionException(cron, msg);

            // Assert
            Assert.Equal(cron, ex.CronExpression);
            Assert.Equal($"Invalid cron expression '{cron}': {msg}", ex.Message);
        }

        [Fact]
        public void Inherits_From_JobSchedulerException()
        {
            // Arrange
            var ex = new CronExpressionException("*/10 * * * *", "test");

            // Act & Assert
            Assert.IsAssignableFrom<JobSchedulerException>(ex);
        }
    }
}
