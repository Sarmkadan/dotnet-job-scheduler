using System;
using System.Collections.Generic;

namespace JobScheduler.Core.Configuration
{
    /// <summary>
    /// Provides extension methods for <see cref="JobSchedulerSettings"/> configuration validation and manipulation.
    /// </summary>
    public static class JobSchedulerSettingsExtensions
    {
        /// <summary>
        /// Validates the JobSchedulerSettings configuration and returns a list of validation errors.
        /// Returns empty list if configuration is valid.
        /// </summary>
        /// <param name="settings">The settings to validate.</param>
        /// <returns>A list of validation error messages. Empty if valid.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="settings"/> is null.</exception>
        public static List<string> Validate(this JobSchedulerSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(settings.ConnectionString))
            {
                errors.Add("ConnectionString is required.");
            }

            if (settings.MaxConcurrentJobs <= 0)
            {
                errors.Add("MaxConcurrentJobs must be greater than 0.");
            }

            if (settings.DefaultTimeoutSeconds <= 0)
            {
                errors.Add("DefaultTimeoutSeconds must be greater than 0.");
            }

            if (settings.DefaultMaxRetries < 0)
            {
                errors.Add("DefaultMaxRetries cannot be negative.");
            }

            if (settings.DefaultRetryBackoffSeconds <= 0)
            {
                errors.Add("DefaultRetryBackoffSeconds must be greater than 0.");
            }

            if (settings.QueuePollIntervalMs <= 0)
            {
                errors.Add("QueuePollIntervalMs must be greater than 0.");
            }

            if (settings.EnableCleanup && settings.CleanupIntervalMs <= 0)
            {
                errors.Add("CleanupIntervalMs must be greater than 0 when EnableCleanup is true.");
            }

            if (settings.MaxJobNameLength <= 0)
            {
                errors.Add("MaxJobNameLength must be greater than 0.");
            }

            if (settings.MaxCronExpressionLength <= 0)
            {
                errors.Add("MaxCronExpressionLength must be greater than 0.");
            }

            return errors;
        }

        /// <summary>
        /// Creates a deep copy of the JobSchedulerSettings instance.
        /// </summary>
        /// <param name="settings">The settings to clone.</param>
        /// <returns>A new instance with the same property values.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="settings"/> is null.</exception>
        public static JobSchedulerSettings Clone(this JobSchedulerSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            return new JobSchedulerSettings
            {
                ConnectionString = settings.ConnectionString,
                MaxConcurrentJobs = settings.MaxConcurrentJobs,
                DefaultTimeoutSeconds = settings.DefaultTimeoutSeconds,
                DefaultMaxRetries = settings.DefaultMaxRetries,
                DefaultRetryBackoffSeconds = settings.DefaultRetryBackoffSeconds,
                QueuePollIntervalMs = settings.QueuePollIntervalMs,
                EnableCleanup = settings.EnableCleanup,
                CleanupIntervalMs = settings.CleanupIntervalMs,
                MaxJobNameLength = settings.MaxJobNameLength,
                MaxCronExpressionLength = settings.MaxCronExpressionLength
            };
        }

        /// <summary>
        /// Determines if cleanup functionality is enabled.
        /// </summary>
        /// <param name="settings">The settings to check.</param>
        /// <returns>True if cleanup is enabled; otherwise false.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="settings"/> is null.</exception>
        public static bool IsCleanupEnabled(this JobSchedulerSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);
            return settings.EnableCleanup;
        }

        /// <summary>
        /// Gets the effective timeout for a job in milliseconds.
        /// Combines DefaultTimeoutSeconds with any job-specific overrides.
        /// </summary>
        /// <param name="settings">The settings to use.</param>
        /// <param name="jobSpecificTimeoutSeconds">Optional job-specific timeout in seconds.</param>
        /// <returns>The effective timeout in milliseconds.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="settings"/> is null.</exception>
        public static int GetEffectiveTimeoutMs(this JobSchedulerSettings settings, int? jobSpecificTimeoutSeconds = null)
        {
            ArgumentNullException.ThrowIfNull(settings);

            return (jobSpecificTimeoutSeconds.HasValue && jobSpecificTimeoutSeconds.Value > 0)
                ? jobSpecificTimeoutSeconds.Value * 1000
                : settings.DefaultTimeoutSeconds * 1000;
        }

        /// <summary>
        /// Gets the maximum allowed length for a job name based on configuration.
        /// Returns the configured MaxJobNameLength, or a sensible default of 255 if not configured.
        /// </summary>
        /// <param name="settings">The settings to use.</param>
        /// <returns>The maximum job name length.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="settings"/> is null.</exception>
        public static int GetMaxJobNameLength(this JobSchedulerSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);
            return settings.MaxJobNameLength > 0 ? settings.MaxJobNameLength : 255;
        }
    }
}