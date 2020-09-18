using System;
using System.Collections.Generic;

namespace JobScheduler.Core.Configuration
{
    public static class JobSchedulerSettingsExtensions
    {
        /// <summary>
        /// Validates the JobSchedulerSettings configuration and returns a list of validation errors.
        /// Returns empty list if configuration is valid.
        /// </summary>
        public static List<string> Validate(this JobSchedulerSettings settings)
        {
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
        public static JobSchedulerSettings Clone(this JobSchedulerSettings settings)
        {
            if (settings == null)
            {
                return null!;
            }

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
        public static bool IsCleanupEnabled(this JobSchedulerSettings settings)
        {
            return settings.EnableCleanup;
        }

        /// <summary>
        /// Gets the effective timeout for a job in milliseconds.
        /// Combines DefaultTimeoutSeconds with any job-specific overrides.
        /// </summary>
        public static int GetEffectiveTimeoutMs(this JobSchedulerSettings settings, int? jobSpecificTimeoutSeconds = null)
        {
            if (jobSpecificTimeoutSeconds.HasValue && jobSpecificTimeoutSeconds.Value > 0)
            {
                return jobSpecificTimeoutSeconds.Value * 1000;
            }

            return settings.DefaultTimeoutSeconds * 1000;
        }

        /// <summary>
        /// Gets the maximum allowed length for a job name based on configuration.
        /// Returns the configured MaxJobNameLength, or a sensible default of 255 if not configured.
        /// </summary>
        public static int GetMaxJobNameLength(this JobSchedulerSettings settings)
        {
            return settings?.MaxJobNameLength > 0 ? settings.MaxJobNameLength : 255;
        }
    }
}
