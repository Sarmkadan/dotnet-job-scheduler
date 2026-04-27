#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace JobScheduler.Core.Constants;

/// <summary>
/// Core constants used throughout the job scheduler system.
/// Defines timeouts, limits, and configuration defaults.
/// </summary>
public static class SchedulerConstants
{
    /// <summary>Default maximum number of concurrent job executions</summary>
    public const int DefaultMaxConcurrentJobs = 10;

    /// <summary>Default job execution timeout in seconds</summary>
    public const int DefaultExecutionTimeoutSeconds = 300;

    /// <summary>Default maximum retry attempts for failed jobs</summary>
    public const int DefaultMaxRetries = 3;

    /// <summary>Default initial backoff delay for retries in seconds</summary>
    public const int DefaultRetryBackoffSeconds = 5;

    /// <summary>Default maximum backoff delay for retries in seconds</summary>
    public const int DefaultMaxRetryBackoffSeconds = 300;

    /// <summary>Backoff multiplier for exponential backoff strategy</summary>
    public const double RetryBackoffMultiplier = 2.0;

    /// <summary>Default heartbeat interval in milliseconds</summary>
    public const int DefaultHeartbeatIntervalMs = 5000;

    /// <summary>Maximum job name length in characters</summary>
    public const int MaxJobNameLength = 256;

    /// <summary>Maximum cron expression length in characters</summary>
    public const int MaxCronExpressionLength = 100;

    /// <summary>Maximum concurrent jobs per priority level</summary>
    public const int MaxJobsPerPriority = 20;

    /// <summary>Queue poll interval in milliseconds for checking scheduled jobs</summary>
    public const int QueuePollIntervalMs = 1000;

    /// <summary>Cleanup interval for orphaned executions in milliseconds</summary>
    public const int CleanupIntervalMs = 300000;

    /// <summary>Maximum execution history retention in days</summary>
    public const int ExecutionHistoryRetentionDays = 30;
}
