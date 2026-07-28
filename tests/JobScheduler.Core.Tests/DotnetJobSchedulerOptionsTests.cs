using Xunit;
using JobScheduler.Core.Configuration;

namespace JobScheduler.Core.Tests;

public class DotnetJobSchedulerOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new DotnetJobSchedulerOptions();

        // Assert
        Assert.Equal(string.Empty, options.ConnectionString);
        Assert.Equal(0, options.MaxConcurrentJobs);
        Assert.Equal(0, options.DefaultTimeoutSeconds);
        Assert.Equal(0, options.DefaultMaxRetries);
        Assert.Equal(0, options.DefaultRetryBackoffSeconds);
        Assert.Equal(0, options.QueuePollIntervalMs);
        Assert.False(options.EnableCleanup);
        Assert.Equal(0, options.CleanupIntervalMs);
    }

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var options = new DotnetJobSchedulerOptions();

        // Act
        options.ConnectionString = "Server=localhost;Database=TestDb;";
        options.MaxConcurrentJobs = 5;
        options.DefaultTimeoutSeconds = 60;
        options.DefaultMaxRetries = 3;
        options.DefaultRetryBackoffSeconds = 10;
        options.QueuePollIntervalMs = 1000;
        options.EnableCleanup = true;
        options.CleanupIntervalMs = 3600000;

        // Assert
        Assert.Equal("Server=localhost;Database=TestDb;", options.ConnectionString);
        Assert.Equal(5, options.MaxConcurrentJobs);
        Assert.Equal(60, options.DefaultTimeoutSeconds);
        Assert.Equal(3, options.DefaultMaxRetries);
        Assert.Equal(10, options.DefaultRetryBackoffSeconds);
        Assert.Equal(1000, options.QueuePollIntervalMs);
        Assert.True(options.EnableCleanup);
        Assert.Equal(3600000, options.CleanupIntervalMs);
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(int.MaxValue)]
    public void NumericProperties_AcceptBoundaryValues(int value)
    {
        // Arrange
        var options = new DotnetJobSchedulerOptions();

        // Act & Assert
        // Since these are just auto-properties with no validation, they should accept any int value
        options.MaxConcurrentJobs = value;
        Assert.Equal(value, options.MaxConcurrentJobs);

        options.DefaultTimeoutSeconds = value;
        Assert.Equal(value, options.DefaultTimeoutSeconds);

        options.DefaultMaxRetries = value;
        Assert.Equal(value, options.DefaultMaxRetries);

        options.DefaultRetryBackoffSeconds = value;
        Assert.Equal(value, options.DefaultRetryBackoffSeconds);

        options.QueuePollIntervalMs = value;
        Assert.Equal(value, options.QueuePollIntervalMs);

        options.CleanupIntervalMs = value;
        Assert.Equal(value, options.CleanupIntervalMs);
    }

    [Fact]
    public void ConnectionString_AcceptsNullOrEmpty()
    {
        // Arrange
        var options = new DotnetJobSchedulerOptions();

        // Act & Assert
        options.ConnectionString = null!;
        Assert.Null(options.ConnectionString);

        options.ConnectionString = string.Empty;
        Assert.Equal(string.Empty, options.ConnectionString);
    }
}
