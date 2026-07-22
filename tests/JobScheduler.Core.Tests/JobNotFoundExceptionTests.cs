#nullable enable

using System;
using JobScheduler.Core.Exceptions;
using Xunit;

namespace JobScheduler.Core.Tests
{
    /// <summary>
    /// Unit tests for <see cref="JobNotFoundException"/>.
    /// </summary>
    public sealed class JobNotFoundExceptionTests
    {
        [Fact]
        public void Constructor_WithGuid_SetsJobIdAndMessage()
        {
            // Arrange
            var jobId = Guid.NewGuid();

            // Act
            var ex = new JobNotFoundException(jobId);

            // Assert
            Assert.Equal(jobId, ex.JobId);
            Assert.Equal($"Job with ID '{jobId}' not found.", ex.Message);
        }

        [Fact]
        public void Constructor_WithGuidAndInnerException_SetsAllProperties()
        {
            // Arrange
            var jobId = Guid.NewGuid();
            var inner = new InvalidOperationException("inner");

            // Act
            var ex = new JobNotFoundException(jobId, inner);

            // Assert
            Assert.Equal(jobId, ex.JobId);
            Assert.Equal($"Job with ID '{jobId}' not found.", ex.Message);
            Assert.Same(inner, ex.InnerException);
        }

        [Fact]
        public void Constructor_WithString_SetsMessageCorrectly()
        {
            // Arrange
            const string jobName = "TestJob";

            // Act
            var ex = new JobNotFoundException(jobName);

            // Assert
            Assert.Equal($"Job with name '{jobName}' not found.", ex.Message);
        }

        [Fact]
        public void Constructor_WithNullString_DoesNotThrowAndMessageContainsEmptyString()
        {
            // Arrange
            string? jobName = null;

            // Act
            var ex = new JobNotFoundException(jobName!);

            // Assert
            Assert.Equal("Job with name '' not found.", ex.Message);
        }

        [Fact]
        public void JobId_Property_IsMutable()
        {
            // Arrange
            var originalId = Guid.NewGuid();
            var ex = new JobNotFoundException(originalId);

            // Act
            var newId = Guid.NewGuid();
            ex.JobId = newId;

            // Assert
            Assert.Equal(newId, ex.JobId);
        }

        [Fact]
        public void Message_WithGuidEmpty_IncludesEmptyGuid()
        {
            // Arrange
            var emptyId = Guid.Empty;

            // Act
            var ex = new JobNotFoundException(emptyId);

            // Assert
            Assert.Equal($"Job with ID '{emptyId}' not found.", ex.Message);
        }

        [Fact]
        public void Message_WithStringEmpty_IncludesEmptyString()
        {
            // Arrange
            const string emptyName = "";

            // Act
            var ex = new JobNotFoundException(emptyName);

            // Assert
            Assert.Equal("Job with name '' not found.", ex.Message);
        }

        [Fact]
        public void Inherits_From_JobSchedulerException()
        {
            // Arrange
            var ex = new JobNotFoundException(Guid.NewGuid());

            // Act & Assert
            Assert.IsAssignableFrom<JobSchedulerException>(ex);
        }
    }
}
