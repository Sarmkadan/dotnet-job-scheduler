namespace JobScheduler.Core.Tests
{
    using Xunit;
    using JobScheduler.Core.Configuration;

    public class JobSchedulerSettingsExtensionsTests
    {
        [Fact]
        public void Validate_HappyPath_ReturnsEmptyList()
        {
            // Arrange
            var settings = new JobSchedulerSettings
            {
                ConnectionString = "test",
                MaxConcurrentJobs = 10,
                DefaultTimeoutSeconds = 30,
                DefaultMaxRetries = 5,
                DefaultRetryBackoffSeconds = 10,
                QueuePollIntervalMs = 100,
                EnableCleanup = true,
                CleanupIntervalMs = 500,
                MaxJobNameLength = 255,
                MaxCronExpressionLength = 255
            };

            // Act
            var result = JobSchedulerSettingsExtensions.Validate(settings);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void Validate_NullSettings_ThrowsArgumentNullException()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => JobSchedulerSettingsExtensions.Validate(null));
        }

        [Fact]
        public void Validate_EmptyConnectionString_ReturnsError()
        {
            // Arrange
            var settings = new JobSchedulerSettings
            {
                ConnectionString = string.Empty,
                MaxConcurrentJobs = 10,
                DefaultTimeoutSeconds = 30,
                DefaultMaxRetries = 5,
                DefaultRetryBackoffSeconds = 10,
                QueuePollIntervalMs = 100,
                EnableCleanup = true,
                CleanupIntervalMs = 500,
                MaxJobNameLength = 255,
                MaxCronExpressionLength = 255
            };

            // Act
            var result = JobSchedulerSettingsExtensions.Validate(settings);

            // Assert
            Assert.Single(result);
        }

        [Fact]
        public void Clone_HappyPath_ReturnsNewSettings()
        {
            // Arrange
            var settings = new JobSchedulerSettings
            {
                ConnectionString = "test",
                MaxConcurrentJobs = 10,
                DefaultTimeoutSeconds = 30,
                DefaultMaxRetries = 5,
                DefaultRetryBackoffSeconds = 10,
                QueuePollIntervalMs = 100,
                EnableCleanup = true,
                CleanupIntervalMs = 500,
                MaxJobNameLength = 255,
                MaxCronExpressionLength = 255
            };

            // Act
            var result = JobSchedulerSettingsExtensions.Clone(settings);

            // Assert
            Assert.NotSame(settings, result);
            Assert.Equal(settings.ConnectionString, result.ConnectionString);
            Assert.Equal(settings.MaxConcurrentJobs, result.MaxConcurrentJobs);
            Assert.Equal(settings.DefaultTimeoutSeconds, result.DefaultTimeoutSeconds);
            Assert.Equal(settings.DefaultMaxRetries, result.DefaultMaxRetries);
            Assert.Equal(settings.DefaultRetryBackoffSeconds, result.DefaultRetryBackoffSeconds);
            Assert.Equal(settings.QueuePollIntervalMs, result.QueuePollIntervalMs);
            Assert.Equal(settings.EnableCleanup, result.EnableCleanup);
            Assert.Equal(settings.CleanupIntervalMs, result.CleanupIntervalMs);
            Assert.Equal(settings.MaxJobNameLength, result.MaxJobNameLength);
            Assert.Equal(settings.MaxCronExpressionLength, result.MaxCronExpressionLength);
        }

        [Fact]
        public void IsCleanupEnabled_HappyPath_ReturnsTrue()
        {
            // Arrange
            var settings = new JobSchedulerSettings
            {
                ConnectionString = "test",
                MaxConcurrentJobs = 10,
                DefaultTimeoutSeconds = 30,
                DefaultMaxRetries = 5,
                DefaultRetryBackoffSeconds = 10,
                QueuePollIntervalMs = 100,
                EnableCleanup = true,
                CleanupIntervalMs = 500,
                MaxJobNameLength = 255,
                MaxCronExpressionLength = 255
            };

            // Act
            var result = JobSchedulerSettingsExtensions.IsCleanupEnabled(settings);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void GetEffectiveTimeoutMs_HappyPath_ReturnsTimeout()
        {
            // Arrange
            var settings = new JobSchedulerSettings
            {
                ConnectionString = "test",
                MaxConcurrentJobs = 10,
                DefaultTimeoutSeconds = 30,
                DefaultMaxRetries = 5,
                DefaultRetryBackoffSeconds = 10,
                QueuePollIntervalMs = 100,
                EnableCleanup = true,
                CleanupIntervalMs = 500,
                MaxJobNameLength = 255,
                MaxCronExpressionLength = 255
            };

            // Act
            var result = JobSchedulerSettingsExtensions.GetEffectiveTimeoutMs(settings);

            // Assert
            Assert.Equal(settings.DefaultTimeoutSeconds * 1000, result);
        }

        [Fact]
        public void GetMaxJobNameLength_HappyPath_ReturnsMaxLength()
        {
            // Arrange
            var settings = new JobSchedulerSettings
            {
                ConnectionString = "test",
                MaxConcurrentJobs = 10,
                DefaultTimeoutSeconds = 30,
                DefaultMaxRetries = 5,
                DefaultRetryBackoffSeconds = 10,
                QueuePollIntervalMs = 100,
                EnableCleanup = true,
                CleanupIntervalMs = 500,
                MaxJobNameLength = 255,
                MaxCronExpressionLength = 255
            };

            // Act
            var result = JobSchedulerSettingsExtensions.GetMaxJobNameLength(settings);

            // Assert
            Assert.Equal(settings.MaxJobNameLength, result);
        }
    }
}
