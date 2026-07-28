#nullable enable
using System;
using JobScheduler.Core.Configuration;
using Xunit;

namespace JobScheduler.Core.Tests;

public class JobSchedulerSettingsTests
{
    [Fact]
    public void DefaultValues_ShouldMatchExpected()
    {
        // Arrange
        var settings = new JobSchedulerSettings();

        // Assert
        Assert.Null(settings.ConnectionString);
        Assert.Equal(10, settings.MaxConcurrentJobs);
        Assert.Equal(300, settings.DefaultTimeoutSeconds);
        Assert.Equal(3, settings.DefaultMaxRetries);
        Assert.Equal(5, settings.DefaultRetryBackoffSeconds);
        Assert.Equal(5000, settings.QueuePollIntervalMs);
        Assert.True(settings.EnableCleanup);
        Assert.Equal(300_000, settings.CleanupIntervalMs);
        Assert.Equal(255, settings.MaxJobNameLength);
        Assert.Equal(255, settings.MaxCronExpressionLength);
    }

    [Fact]
    public void SettingProperties_ShouldPersistValues()
    {
        // Arrange
        var settings = new JobSchedulerSettings
        {
            ConnectionString = "Server=.;Database=Jobs;Trusted_Connection=True;",
            MaxConcurrentJobs = 42,
            DefaultTimeoutSeconds = 60,
            DefaultMaxRetries = 7,
            DefaultRetryBackoffSeconds = 15,
            QueuePollIntervalMs = 250,
            EnableCleanup = false,
            CleanupIntervalMs = 120_000,
            MaxJobNameLength = 100,
            MaxCronExpressionLength = 50
        };

        // Assert
        Assert.Equal("Server=.;Database=Jobs;Trusted_Connection=True;", settings.ConnectionString);
        Assert.Equal(42, settings.MaxConcurrentJobs);
        Assert.Equal(60, settings.DefaultTimeoutSeconds);
        Assert.Equal(7, settings.DefaultMaxRetries);
        Assert.Equal(15, settings.DefaultRetryBackoffSeconds);
        Assert.Equal(250, settings.QueuePollIntervalMs);
        Assert.False(settings.EnableCleanup);
        Assert.Equal(120_000, settings.CleanupIntervalMs);
        Assert.Equal(100, settings.MaxJobNameLength);
        Assert.Equal(50, settings.MaxCronExpressionLength);
    }

    [Fact]
    public void ConnectionString_CanBeNullOrEmpty()
    {
        // Arrange
        var settings = new JobSchedulerSettings();

        // Act & Assert
        settings.ConnectionString = null;
        Assert.Null(settings.ConnectionString);

        settings.ConnectionString = string.Empty;
        Assert.Equal(string.Empty, settings.ConnectionString);
    }

    [Fact]
    public void NumericProperties_ShouldAcceptBoundaryValues()
    {
        // Arrange
        var settings = new JobSchedulerSettings
        {
            MaxConcurrentJobs = 0,
            DefaultTimeoutSeconds = 0,
            DefaultMaxRetries = 0,
            DefaultRetryBackoffSeconds = 0,
            QueuePollIntervalMs = 0,
            CleanupIntervalMs = 0,
            MaxJobNameLength = 0,
            MaxCronExpressionLength = 0
        };

        // Assert
        Assert.Equal(0, settings.MaxConcurrentJobs);
        Assert.Equal(0, settings.DefaultTimeoutSeconds);
        Assert.Equal(0, settings.DefaultMaxRetries);
        Assert.Equal(0, settings.DefaultRetryBackoffSeconds);
        Assert.Equal(0, settings.QueuePollIntervalMs);
        Assert.Equal(0, settings.CleanupIntervalMs);
        Assert.Equal(0, settings.MaxJobNameLength);
        Assert.Equal(0, settings.MaxCronExpressionLength);
    }
}
