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
    /// Unit tests for <see cref="ConcurrencyException"/>.
    /// </summary>
    public sealed class ConcurrencyExceptionTests
    {
        [Fact]
        public void Constructor_SetsAllPropertiesCorrectly()
        {
            // Arrange
            var jobId = Guid.NewGuid();
            const int current = 5;
            const int max = 3;

            // Act
            var ex = new ConcurrencyException(jobId, current, max);

            // Assert
            Assert.Equal(jobId, ex.JobId);
            Assert.Equal(current, ex.CurrentConcurrentExecutions);
            Assert.Equal(max, ex.MaxAllowed);
        }

        [Fact]
        public void Message_ContainsJobIdAndCounts()
        {
            // Arrange
            var jobId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            const int current = 10;
            const int max = 2;
            var expectedFragment = $"Job {jobId} cannot execute: current concurrent executions ({current}) exceed maximum allowed ({max}).";

            // Act
            var ex = new ConcurrencyException(jobId, current, max);

            // Assert
            Assert.Equal(expectedFragment, ex.Message);
        }

        [Fact]
        public void Inherits_From_JobSchedulerException()
        {
            // Arrange
            var ex = new ConcurrencyException(Guid.NewGuid(), 1, 1);

            // Act & Assert
            Assert.IsAssignableFrom<JobSchedulerException>(ex);
        }

        [Fact]
        public void Properties_AreMutable_AfterConstruction()
        {
            // Arrange
            var originalJobId = Guid.NewGuid();
            var ex = new ConcurrencyException(originalJobId, 1, 2);

            // Act
            var newJobId = Guid.NewGuid();
            ex.JobId = newJobId;
            ex.CurrentConcurrentExecutions = 99;
            ex.MaxAllowed = 100;

            // Assert
            Assert.Equal(newJobId, ex.JobId);
            Assert.Equal(99, ex.CurrentConcurrentExecutions);
            Assert.Equal(100, ex.MaxAllowed);
        }

        [Fact]
        public void ThrowingException_CanBeCaughtAs_JobSchedulerException()
        {
            // Arrange
            void Action() => throw new ConcurrencyException(Guid.NewGuid(), 3, 1);

            // Act & Assert
            var caught = Assert.Throws<JobSchedulerException>(Action);
            Assert.IsType<ConcurrencyException>(caught);
        }

        [Fact]
        public void Constructor_Allows_ZeroAndNegativeValues()
        {
            // Arrange
            var jobId = Guid.Empty;
            const int current = 0;
            const int max = -1;

            // Act
            var ex = new ConcurrencyException(jobId, current, max);

            // Assert
            Assert.Equal(jobId, ex.JobId);
            Assert.Equal(current, ex.CurrentConcurrentExecutions);
            Assert.Equal(max, ex.MaxAllowed);
            // Message should still be formatted correctly even with edge values.
            var expectedMessage = $"Job {jobId} cannot execute: current concurrent executions ({current}) exceed maximum allowed ({max}).";
            Assert.Equal(expectedMessage, ex.Message);
        }
    }
}
