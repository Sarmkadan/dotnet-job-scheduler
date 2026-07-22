#nullable enable

// =============================================================================
// Author: Automated Task
// =============================================================================

using System;
using JobScheduler.Core.Exceptions;
using Xunit;

namespace JobScheduler.Core.Tests
{
    /// <summary>
    /// Unit tests for <see cref="ExecutionException"/>.
    /// </summary>
    public sealed class ExecutionExceptionTests
    {
        [Fact]
        public void Constructor_SetsExecutionIdAndJobId_DefaultsAttemptNumber()
        {
            // Arrange
            var executionId = Guid.NewGuid();
            var jobId = Guid.NewGuid();
            var message = "Job failed";

            // Act
            var ex = new ExecutionException(message, executionId, jobId);

            // Assert
            Assert.Equal(executionId, ex.ExecutionId);
            Assert.Equal(jobId, ex.JobId);
            Assert.Equal(0, ex.AttemptNumber); // Default value
            Assert.Equal(message, ex.Message);
        }

        [Fact]
        public void Constructor_WithAttemptNumber_SetsAllProperties()
        {
            // Arrange
            var executionId = Guid.NewGuid();
            var jobId = Guid.NewGuid();
            var attemptNumber = 3;
            var message = "Job failed on retry";

            // Act
            var ex = new ExecutionException(message, executionId, jobId, attemptNumber);

            // Assert
            Assert.Equal(executionId, ex.ExecutionId);
            Assert.Equal(jobId, ex.JobId);
            Assert.Equal(attemptNumber, ex.AttemptNumber);
            Assert.Equal(message, ex.Message);
        }

        [Fact]
        public void Constructor_WithInnerException_SetsInnerException()
        {
            // Arrange
            var executionId = Guid.NewGuid();
            var jobId = Guid.NewGuid();
            var innerException = new InvalidOperationException("Inner failure");
            var message = "Job failed due to inner error";

            // Act
            var ex = new ExecutionException(message, executionId, jobId, innerException);

            // Assert
            Assert.Equal(executionId, ex.ExecutionId);
            Assert.Equal(jobId, ex.JobId);
            Assert.Same(innerException, ex.InnerException);
            Assert.Equal(message, ex.Message);
        }

        [Fact]
        public void Inherits_From_JobSchedulerException()
        {
            // Arrange
            var ex = new ExecutionException("msg", Guid.NewGuid(), Guid.NewGuid());

            // Act & Assert
            Assert.IsAssignableFrom<JobSchedulerException>(ex);
        }

        [Fact]
        public void Properties_AreMutable_AfterConstruction()
        {
            // Arrange
            var ex = new ExecutionException("msg", Guid.NewGuid(), Guid.NewGuid());
            var newExecId = Guid.NewGuid();
            var newJobId = Guid.NewGuid();
            var newAttempt = 10;

            // Act
            ex.ExecutionId = newExecId;
            ex.JobId = newJobId;
            ex.AttemptNumber = newAttempt;

            // Assert
            Assert.Equal(newExecId, ex.ExecutionId);
            Assert.Equal(newJobId, ex.JobId);
            Assert.Equal(newAttempt, ex.AttemptNumber);
        }

        [Fact]
        public void Constructor_AllowsEmptyGuids()
        {
            // Arrange
            var emptyGuid = Guid.Empty;

            // Act
            var ex = new ExecutionException("msg", emptyGuid, emptyGuid);

            // Assert
            Assert.Equal(emptyGuid, ex.ExecutionId);
            Assert.Equal(emptyGuid, ex.JobId);
        }
    }
}
