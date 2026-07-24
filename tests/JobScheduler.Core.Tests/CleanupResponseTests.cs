// tests/JobScheduler.Core.Tests/CleanupResponseTests.cs
using System;
using JobScheduler.Core.Domain.Models;
using Xunit;

namespace JobScheduler.Core.Tests;

public class CleanupResponseTests
{
    [Fact]
    public void DefaultValues_ReturnExpectedDefaults()
    {
        // Arrange & Act
        var response = new CleanupResponse();

        // Assert
        Assert.Equal(0, response.DeletedCount);
        Assert.Equal(default(DateTime), response.CutoffDate);
        Assert.Equal(string.Empty, response.Message);
    }

    [Fact]
    public void SetProperties_StoresValuesCorrectly()
    {
        // Arrange
        var expectedDeleted = 42;
        var expectedCutoff = new DateTime(2023, 12, 31, 23, 59, 59, DateTimeKind.Utc);
        var expectedMessage = "Cleanup completed successfully.";

        var response = new CleanupResponse
        {
            DeletedCount = expectedDeleted,
            CutoffDate = expectedCutoff,
            Message = expectedMessage
        };

        // Assert
        Assert.Equal(expectedDeleted, response.DeletedCount);
        Assert.Equal(expectedCutoff, response.CutoffDate);
        Assert.Equal(expectedMessage, response.Message);
    }

    [Fact]
    public void DeletedCount_CanBeNegative()
    {
        // Arrange
        var response = new CleanupResponse { DeletedCount = -5 };

        // Assert
        Assert.Equal(-5, response.DeletedCount);
    }

    [Fact]
    public void Message_CanBeNull()
    {
        // Arrange
        var response = new CleanupResponse { Message = null! };

        // Assert
        Assert.Null(response.Message);
    }

    [Fact]
    public void CutoffDate_CanBeMaxValue()
    {
        // Arrange
        var maxDate = DateTime.MaxValue;
        var response = new CleanupResponse { CutoffDate = maxDate };

        // Assert
        Assert.Equal(maxDate, response.CutoffDate);
    }
}
