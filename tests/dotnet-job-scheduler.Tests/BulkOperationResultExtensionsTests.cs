#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using JobScheduler.Core.Controllers;
using Xunit;

namespace DotnetJobScheduler.Tests;

/// <summary>
/// Tests for the <see cref="BulkOperationResultExtensions"/> class.
/// </summary>
public sealed class BulkOperationResultExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="BulkOperationResultExtensions.AllSucceeded"/> returns true when every result succeeded.
    /// </summary>
    [Fact]
    public void AllSucceeded_WhenAllResultsSucceeded_ReturnsTrue()
    {
        // Arrange
        var results = new[] { CreateResult(true), CreateResult(true) };

        // Act
        var allSucceeded = results.AllSucceeded();

        // Assert
        allSucceeded.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that <see cref="BulkOperationResultExtensions.AllSucceeded"/> returns false when any result failed.
    /// </summary>
    [Fact]
    public void AllSucceeded_WhenAnyResultFailed_ReturnsFalse()
    {
        // Arrange
        var results = new[] { CreateResult(true), CreateResult(false) };

        // Act
        var allSucceeded = results.AllSucceeded();

        // Assert
        allSucceeded.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that <see cref="BulkOperationResultExtensions.AllSucceeded"/> returns true for an empty collection,
    /// consistent with <see cref="Enumerable.All{TSource}"/>.
    /// </summary>
    [Fact]
    public void AllSucceeded_WhenCollectionIsEmpty_ReturnsTrue()
    {
        // Arrange
        var results = Array.Empty<BulkOperationResult>();

        // Act
        var allSucceeded = results.AllSucceeded();

        // Assert
        allSucceeded.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that <see cref="BulkOperationResultExtensions.AllSucceeded"/> throws when the collection is null.
    /// </summary>
    [Fact]
    public void AllSucceeded_WhenResultsIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        IEnumerable<BulkOperationResult> results = null!;

        // Act
        var act = () => results.AllSucceeded();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Verifies that <see cref="BulkOperationResultExtensions.SuccessRate"/> returns the fraction of successful results.
    /// </summary>
    [Fact]
    public void SuccessRate_WithMixedResults_ReturnsFraction()
    {
        // Arrange
        var results = new[]
        {
            CreateResult(true),
            CreateResult(true),
            CreateResult(true),
            CreateResult(false)
        };

        // Act
        var rate = results.SuccessRate();

        // Assert
        rate.Should().BeApproximately(0.75, 0.0001);
    }

    /// <summary>
    /// Verifies that <see cref="BulkOperationResultExtensions.SuccessRate"/> returns 0 for an empty collection
    /// instead of dividing by zero.
    /// </summary>
    [Fact]
    public void SuccessRate_WhenCollectionIsEmpty_ReturnsZero()
    {
        // Arrange
        var results = Array.Empty<BulkOperationResult>();

        // Act
        var rate = results.SuccessRate();

        // Assert
        rate.Should().Be(0);
    }

    /// <summary>
    /// Verifies that <see cref="BulkOperationResultExtensions.SuccessRate"/> returns 1 when all results succeeded.
    /// </summary>
    [Fact]
    public void SuccessRate_WhenAllSucceeded_ReturnsOne()
    {
        // Arrange
        var results = new[] { CreateResult(true), CreateResult(true) };

        // Act
        var rate = results.SuccessRate();

        // Assert
        rate.Should().Be(1);
    }

    /// <summary>
    /// Verifies that <see cref="BulkOperationResultExtensions.FailureMessages"/> returns only the error messages of failed results, in order.
    /// </summary>
    [Fact]
    public void FailureMessages_ReturnsErrorMessagesOfFailedResultsInOrder()
    {
        // Arrange
        var results = new[]
        {
            CreateResult(true),
            CreateResult(false, "Job not found"),
            CreateResult(true),
            CreateResult(false, "Internal server error")
        };

        // Act
        var messages = results.FailureMessages().ToList();

        // Assert
        messages.Should().Equal("Job not found", "Internal server error");
    }

    /// <summary>
    /// Verifies that <see cref="BulkOperationResultExtensions.FailureMessages"/> falls back to a message containing the job ID
    /// when a failed result has no error message.
    /// </summary>
    [Fact]
    public void FailureMessages_WhenErrorMessageMissing_UsesJobIdFallback()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var results = new[] { new BulkOperationResult { JobId = jobId, Success = false, ErrorMessage = " " } };

        // Act
        var messages = results.FailureMessages().ToList();

        // Assert
        messages.Should().ContainSingle().Which.Should().Be($"Job {jobId} failed");
    }

    /// <summary>
    /// Verifies that <see cref="BulkOperationResultExtensions.FailureMessages"/> returns an empty sequence when all results succeeded.
    /// </summary>
    [Fact]
    public void FailureMessages_WhenAllSucceeded_ReturnsEmpty()
    {
        // Arrange
        var results = new[] { CreateResult(true) };

        // Act
        var messages = results.FailureMessages();

        // Assert
        messages.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that <see cref="BulkOperationResultExtensions.FailureMessages"/> throws eagerly when the collection is null,
    /// without waiting for the sequence to be enumerated.
    /// </summary>
    [Fact]
    public void FailureMessages_WhenResultsIsNull_ThrowsArgumentNullExceptionImmediately()
    {
        // Arrange
        IEnumerable<BulkOperationResult> results = null!;

        // Act
        var act = () => results.FailureMessages();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Verifies that <see cref="BulkOperationResultExtensions.ToSummary"/> reports counts, percentage and failures.
    /// </summary>
    [Fact]
    public void ToSummary_WithMixedResults_ReturnsFormattedSummary()
    {
        // Arrange
        var results = new[]
        {
            CreateResult(true),
            CreateResult(true),
            CreateResult(true),
            CreateResult(false, "Job not found")
        };

        // Act
        var summary = results.ToSummary();

        // Assert
        summary.Should().Be("3 of 4 succeeded (75.0%), 1 failed");
    }

    /// <summary>
    /// Verifies that <see cref="BulkOperationResultExtensions.ToSummary"/> handles an empty collection without dividing by zero.
    /// </summary>
    [Fact]
    public void ToSummary_WhenCollectionIsEmpty_ReturnsZeroSummary()
    {
        // Arrange
        var results = Array.Empty<BulkOperationResult>();

        // Act
        var summary = results.ToSummary();

        // Assert
        summary.Should().Be("0 of 0 succeeded (0.0%)");
    }

    /// <summary>
    /// Creates a <see cref="BulkOperationResult"/> with a random job ID for use as a test fixture.
    /// </summary>
    /// <param name="success">Whether the operation succeeded.</param>
    /// <param name="errorMessage">Optional error message for failed operations.</param>
    /// <returns>A configured <see cref="BulkOperationResult"/>.</returns>
    private static BulkOperationResult CreateResult(bool success, string? errorMessage = null) =>
        new()
        {
            JobId = Guid.NewGuid(),
            Success = success,
            ErrorMessage = errorMessage
        };
}
