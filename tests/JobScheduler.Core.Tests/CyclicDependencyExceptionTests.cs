#nullable enable

// =============================================================================
// Author: Automated Task (generated)
// =============================================================================

using System;
using JobScheduler.Core.Exceptions;
using Xunit;

namespace JobScheduler.Core.Tests
{
    /// <summary>
    /// Unit tests for <see cref="CyclicDependencyException"/>.
    /// </summary>
    public sealed class CyclicDependencyExceptionTests
    {
        [Fact]
        public void Constructor_SetsPropertiesAndMessage()
        {
            // Arrange
            var jobId = Guid.NewGuid();
            var dependsOnJobId = Guid.NewGuid();

            var expectedMessage =
                $"Cannot add dependency: job '{jobId}' → '{dependsOnJobId}' would introduce a cycle in the dependency graph.";

            // Act
            var ex = new CyclicDependencyException(jobId, dependsOnJobId);

            // Assert
            Assert.Equal(jobId, ex.JobId);
            Assert.Equal(dependsOnJobId, ex.DependsOnJobId);
            Assert.Equal(expectedMessage, ex.Message);
        }

        [Fact]
        public void Constructor_WithInnerException_SetsPropertiesAndInnerException()
        {
            // Arrange
            var jobId = Guid.NewGuid();
            var dependsOnJobId = Guid.NewGuid();
            var inner = new InvalidOperationException("inner");

            var expectedMessage =
                $"Cannot add dependency: job '{jobId}' → '{dependsOnJobId}' would introduce a cycle in the dependency graph.";

            // Act
            var ex = new CyclicDependencyException(jobId, dependsOnJobId, inner);

            // Assert
            Assert.Equal(jobId, ex.JobId);
            Assert.Equal(dependsOnJobId, ex.DependsOnJobId);
            Assert.Equal(expectedMessage, ex.Message);
            Assert.Same(inner, ex.InnerException);
        }

        [Fact]
        public void Inherits_From_JobSchedulerException()
        {
            // Arrange
            var ex = new CyclicDependencyException(Guid.NewGuid(), Guid.NewGuid());

            // Act & Assert
            Assert.IsAssignableFrom<JobSchedulerException>(ex);
        }

        [Fact]
        public void Constructor_Allows_EmptyGuids_MessageFormattedCorrectly()
        {
            // Arrange
            var empty = Guid.Empty;
            var expectedMessage =
                $"Cannot add dependency: job '{empty}' → '{empty}' would introduce a cycle in the dependency graph.";

            // Act
            var ex = new CyclicDependencyException(empty, empty);

            // Assert
            Assert.Equal(empty, ex.JobId);
            Assert.Equal(empty, ex.DependsOnJobId);
            Assert.Equal(expectedMessage, ex.Message);
        }

        [Fact]
        public void ThrowingException_CanBeCaughtAs_JobSchedulerException()
        {
            // Arrange
            void Action() => throw new CyclicDependencyException(Guid.NewGuid(), Guid.NewGuid());

            // Act & Assert
            var caught = Assert.Throws<JobSchedulerException>(Action);
            Assert.IsType<CyclicDependencyException>(caught);
        }
    }
}
