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
    /// Unit tests for <see cref="ExecutionException"/> exception plumbing.
    /// Focuses on exception message and inner-exception plumbing.
    /// </summary>
    public sealed class ExecutionExceptionTests
    {
        [Fact]
        public void Constructor_Parameterless_ProducesNonNullDefaultMessage()
        {
            // Arrange & Act
            var ex = new ExecutionException("test message", Guid.NewGuid(), Guid.NewGuid());

            // Assert
            Assert.NotNull(ex.Message);
            Assert.NotEmpty(ex.Message);
        }

        [Fact]
        public void Constructor_MessageOnly_PreservesExactMessageText()
        {
            // Arrange
            const string expectedMessage = "Job execution failed with database timeout";

            var executionId = Guid.NewGuid();
            var jobId = Guid.NewGuid();

            // Act
            var ex = new ExecutionException(expectedMessage, executionId, jobId);

            // Assert
            Assert.Equal(expectedMessage, ex.Message);
        }

        [Fact]
        public void Constructor_MessageAndInnerException_PreservesMessageAndInnerException()
        {
            // Arrange
            const string expectedMessage = "Job execution failed due to inner error";
            var innerException = new InvalidOperationException("Database connection failed");
            var executionId = Guid.NewGuid();
            var jobId = Guid.NewGuid();

            // Act
            var ex = new ExecutionException(expectedMessage, executionId, jobId, innerException);

            // Assert
            Assert.Equal(expectedMessage, ex.Message);
            Assert.Same(innerException, ex.InnerException);
        }

        [Fact]
        public void InnerException_IsNull_WhenNotSupplied()
        {
            // Arrange
            var executionId = Guid.NewGuid();
            var jobId = Guid.NewGuid();

            // Act
            var ex = new ExecutionException("test message", executionId, jobId);

            // Assert
            Assert.Null(ex.InnerException);
        }

        [Fact]
        public void Inherits_From_JobSchedulerException()
        {
            // Arrange & Act
            var ex = new ExecutionException("msg", Guid.NewGuid(), Guid.NewGuid());

            // Assert
            Assert.IsAssignableFrom<JobSchedulerException>(ex);
        }

        [Fact]
        public void CustomProperties_RoundTripCorrectly_MessageConstructor()
        {
            // Arrange
            const string expectedMessage = "Job failed";
            var expectedExecutionId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var expectedJobId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            const int expectedAttemptNumber = 0;

            // Act
            var ex = new ExecutionException(expectedMessage, expectedExecutionId, expectedJobId);

            // Assert
            Assert.Equal(expectedExecutionId, ex.ExecutionId);
            Assert.Equal(expectedJobId, ex.JobId);
            Assert.Equal(expectedAttemptNumber, ex.AttemptNumber);
            Assert.Equal(expectedMessage, ex.Message);
        }

        [Fact]
        public void CustomProperties_RoundTripCorrectly_AttemptConstructor()
        {
            // Arrange
            const string expectedMessage = "Job failed on retry";
            var expectedExecutionId = Guid.Parse("33333333-3333-3333-3333-333333333333");
            var expectedJobId = Guid.Parse("44444444-4444-4444-4444-444444444444");
            const int expectedAttemptNumber = 3;

            // Act
            var ex = new ExecutionException(expectedMessage, expectedExecutionId, expectedJobId, expectedAttemptNumber);

            // Assert
            Assert.Equal(expectedExecutionId, ex.ExecutionId);
            Assert.Equal(expectedJobId, ex.JobId);
            Assert.Equal(expectedAttemptNumber, ex.AttemptNumber);
            Assert.Equal(expectedMessage, ex.Message);
        }

        [Fact]
        public void CustomProperties_RoundTripCorrectly_InnerExceptionConstructor()
        {
            // Arrange
            const string expectedMessage = "Job failed due to inner error";
            var expectedExecutionId = Guid.Parse("55555555-5555-5555-5555-555555555555");
            var expectedJobId = Guid.Parse("66666666-6666-6666-6666-666666666666");
            var innerException = new InvalidOperationException("Inner failure");

            // Act
            var ex = new ExecutionException(expectedMessage, expectedExecutionId, expectedJobId, innerException);

            // Assert
            Assert.Equal(expectedExecutionId, ex.ExecutionId);
            Assert.Equal(expectedJobId, ex.JobId);
            Assert.Same(innerException, ex.InnerException);
            Assert.Equal(expectedMessage, ex.Message);
        }

        [Fact]
        public void ErrorCode_IsSetTo_EXECUTION_ERROR()
        {
            // Arrange & Act
            var ex = new ExecutionException("msg", Guid.NewGuid(), Guid.NewGuid());

            // Assert
            Assert.Equal("EXECUTION_ERROR", ex.ErrorCode);
        }

        [Fact]
        public void CanBeCaughtAs_JobSchedulerException()
        {
            // Arrange
            void Action() => throw new ExecutionException("test", Guid.NewGuid(), Guid.NewGuid());

            // Act & Assert
            var caught = Assert.ThrowsAny<JobSchedulerException>(Action);
            Assert.IsType<ExecutionException>(caught);
        }

        [Fact]
        public void Properties_AreMutable_AfterConstruction()
        {
            // Arrange
            var ex = new ExecutionException("msg", Guid.NewGuid(), Guid.NewGuid());
            var newExecutionId = Guid.NewGuid();
            var newJobId = Guid.NewGuid();
            const int newAttemptNumber = 5;

            // Act
            ex.ExecutionId = newExecutionId;
            ex.JobId = newJobId;
            ex.AttemptNumber = newAttemptNumber;

            // Assert
            Assert.Equal(newExecutionId, ex.ExecutionId);
            Assert.Equal(newJobId, ex.JobId);
            Assert.Equal(newAttemptNumber, ex.AttemptNumber);
        }
    }
}