#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using JobScheduler.Core.Configuration;
using JobScheduler.Core.Data;

namespace JobScheduler.Core.Tests;

/// <summary>
/// Unit tests for DependencyInjectionExtensions and JobSchedulerOptions.
/// Validates service registration, middleware wiring, database initialization,
/// and configuration validation behavior.
/// </summary>
public sealed class DependencyInjectionExtensionsTests
{
    private static string NewInMemoryConnectionString()
    {
        // A dedicated in-memory SQLite connection string per test avoids
        // cross-test database interference (each unique name is its own db).
        return $"Data Source=file:{Guid.NewGuid():N}?mode=memory&cache=shared";
    }

    [Fact]
    public void AddJobScheduler_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            DependencyInjectionExtensions.AddJobScheduler(services!));
    }

    [Fact]
    public void AddJobScheduler_WithoutConnectionString_ThrowsWhenBuildingDbContext()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddJobScheduler();

        // Act
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        // Assert - options.ConnectionString is null by default, so resolving the
        // context (which validates the connection string) must throw.
        Assert.ThrowsAny<ArgumentException>(() =>
            scope.ServiceProvider.GetRequiredService<JobSchedulerContext>());
    }

    [Fact]
    public void AddJobScheduler_WithValidConfiguration_RegistersCoreServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        var result = services.AddJobScheduler(options =>
        {
            options.ConnectionString = NewInMemoryConnectionString();
        });

        // Assert
        Assert.Same(services, result);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<JobSchedulerContext>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<Services.CronExpressionService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<Services.JobSchedulerService>());
    }

    [Fact]
    public void UseJobSchedulerMiddleware_WithNullApp_ThrowsArgumentNullException()
    {
        // Arrange
        Microsoft.AspNetCore.Builder.IApplicationBuilder? app = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            DependencyInjectionExtensions.UseJobSchedulerMiddleware(app!));
    }

    [Fact]
    public async Task InitializeDatabaseAsync_WithNullServiceProvider_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceProvider? provider = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            DependencyInjectionExtensions.InitializeDatabaseAsync(provider!));
    }

    [Fact]
    public async Task InitializeDatabaseAsync_WithValidProvider_AppliesMigrationsSuccessfully()
    {
        // Arrange
        var connectionString = NewInMemoryConnectionString();
        var keepAlive = new SqliteConnection(connectionString);
        keepAlive.Open();

        try
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddJobScheduler(options => options.ConnectionString = connectionString);
            using var provider = services.BuildServiceProvider();

            // Act
            await provider.InitializeDatabaseAsync();

            // Assert - after migration the scheduler configuration should validate cleanly.
            provider.ValidateSchedulerConfiguration();
        }
        finally
        {
            keepAlive.Close();
        }
    }

    [Fact]
    public void ValidateSchedulerConfiguration_WithNullServiceProvider_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceProvider? provider = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            DependencyInjectionExtensions.ValidateSchedulerConfiguration(provider!));
    }

    [Fact]
    public void ValidateSchedulerConfiguration_WithoutJobSchedulerRegistered_ThrowsInvalidOperationException()
    {
        // Arrange - an empty container is missing every required registration.
        var services = new ServiceCollection();
        using var provider = services.BuildServiceProvider();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            provider.ValidateSchedulerConfiguration());
        Assert.Contains("AddJobScheduler", ex.Message);
        Assert.NotNull(ex.InnerException);
    }

    [Fact]
    public void JobSchedulerOptions_DefaultValues_MatchSchedulerConstants()
    {
        // Arrange
        var options = new JobSchedulerOptions();

        // Assert - defaults come from SchedulerConstants; boundary/default check.
        Assert.Null(options.ConnectionString);
        Assert.Equal(Constants.SchedulerConstants.DefaultMaxConcurrentJobs, options.MaxConcurrentJobs);
        Assert.Equal(Constants.SchedulerConstants.DefaultExecutionTimeoutSeconds, options.DefaultTimeoutSeconds);
        Assert.Equal(Constants.SchedulerConstants.DefaultMaxRetries, options.DefaultMaxRetries);
        Assert.Equal(Constants.SchedulerConstants.DefaultRetryBackoffSeconds, options.DefaultRetryBackoffSeconds);
        Assert.Equal(Constants.SchedulerConstants.QueuePollIntervalMs, options.QueuePollIntervalMs);
        Assert.False(options.EnableLeaderElection);
    }

    [Fact]
    public void JobSchedulerOptions_CustomValues_AreAssignedCorrectly()
    {
        // Arrange
        var options = new JobSchedulerOptions
        {
            ConnectionString = "Data Source=test.db",
            MaxConcurrentJobs = 0,
            DefaultTimeoutSeconds = -1,
            DefaultMaxRetries = int.MaxValue,
            DefaultRetryBackoffSeconds = 0,
            QueuePollIntervalMs = 1
        };

        // Assert - the options object itself performs no validation; it is a
        // plain settings bag, so even boundary/nonsensical values round-trip as-is.
        Assert.Equal("Data Source=test.db", options.ConnectionString);
        Assert.Equal(0, options.MaxConcurrentJobs);
        Assert.Equal(-1, options.DefaultTimeoutSeconds);
        Assert.Equal(int.MaxValue, options.DefaultMaxRetries);
        Assert.Equal(0, options.DefaultRetryBackoffSeconds);
        Assert.Equal(1, options.QueuePollIntervalMs);
    }
}
