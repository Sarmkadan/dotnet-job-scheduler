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
    /// Unit tests for <see cref="ConcurrencyException"/> exception plumbing.
    /// Focuses on exception message and inner-exception plumbing.
    /// </summary>
    public sealed class ConcurrencyExceptionTests
    {
        [Fact]
        public void Constructor_Parameterless_ProducesNonNullDefaultMessage()
        {
            // Arrange & Act
            var ex = new ConcurrencyException(Guid.NewGuid(), 1, 1);

            // Assert
            Assert.NotNull(ex.Message);
            Assert.NotEmpty(ex.Message);
        }

        [Fact]
        public void Constructor_MessageOnly_PreservesExactMessageText()
        {
            // Arrange
            var jobId = Guid.Parse("12345678-1234-1234-1234-123456789012");
            const int currentCount = 5;
            const int maxAllowed = 3;

            // Act
            var ex = new ConcurrencyException(jobId, currentCount, maxAllowed);

            // Assert
            Assert.Equal("Job 12345678-1234-1234-1234-123456789012 cannot execute: current concurrent executions (5) exceed maximum allowed (3).", ex.Message);
        }

        [Fact]
        public void InnerException_IsNull_WhenNotSupplied()
        {
            // Arrange & Act
            var ex = new ConcurrencyException(Guid.NewGuid(), 1, 1);

            // Assert
            Assert.Null(ex.InnerException);
        }

        [Fact]
        public void Inherits_From_JobSchedulerException()
        {
            // Arrange & Act
            var ex = new ConcurrencyException(Guid.NewGuid(), 1, 1);

            // Assert
            Assert.IsAssignableFrom<JobSchedulerException>(ex);
        }

        [Fact]
        public void CustomProperties_RoundTripCorrectly()
        {
            // Arrange
            var expectedJobId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            const int expectedCurrent = 7;
            const int expectedMax = 5;

            // Act
            var ex = new ConcurrencyException(expectedJobId, expectedCurrent, expectedMax);

            // Assert
            Assert.Equal(expectedJobId, ex.JobId);
            Assert.Equal(expectedCurrent, ex.CurrentConcurrentExecutions);
            Assert.Equal(expectedMax, ex.MaxAllowed);
        }

        [Fact]
        public void ErrorCode_IsSetTo_CONCURRENCY_LIMIT_EXCEEDED()
        {
            // Arrange & Act
            var ex = new ConcurrencyException(Guid.NewGuid(), 1, 1);

            // Assert
            Assert.Equal("CONCURRENCY_LIMIT_EXCEEDED", ex.ErrorCode);
        }

        [Fact]
        public void CanBeCaughtAs_JobSchedulerException()
        {
            // Arrange
            void Action() => throw new ConcurrencyException(Guid.NewGuid(), 3, 1);

            // Act & Assert
            var caught = Assert.ThrowsAny<JobSchedulerException>(Action);
            Assert.IsType<ConcurrencyException>(caught);
        }

        [Fact]
        public void Properties_AreMutable_AfterConstruction()
        {
            // Arrange
            var ex = new ConcurrencyException(Guid.NewGuid(), 1, 2);

            // Act
            ex.JobId = Guid.NewGuid();
            ex.CurrentConcurrentExecutions = 99;
            ex.MaxAllowed = 100;

            // Assert
            Assert.Equal(99, ex.CurrentConcurrentExecutions);
            Assert.Equal(100, ex.MaxAllowed);
        }

        [Fact]
        public void Message_Format_IncludesAllRequiredInformation()
        {
            // Arrange
            var jobId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            const int current = 15;
            const int max = 10;

            // Act
            var ex = new ConcurrencyException(jobId, current, max);

            // Assert
            Assert.Contains(jobId.ToString(), ex.Message);
            Assert.Contains("15", ex.Message);
            Assert.Contains("10", ex.Message);
            Assert.Contains("concurrent executions", ex.Message);
            Assert.Contains("exceed maximum allowed", ex.Message);
        }
    }
}