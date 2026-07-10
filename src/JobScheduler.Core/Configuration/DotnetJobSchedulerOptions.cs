using Microsoft.Extensions.Options;

/// <summary>
/// Options for the Dotnet Job Scheduler.
/// </summary>
public class DotnetJobSchedulerOptions
{
    /// <summary>
    /// The connection string to use for database operations.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// The maximum number of concurrent jobs to run.
    /// </summary>
    public int MaxConcurrentJobs { get; set; }

    /// <summary>
    /// The default timeout in seconds for job execution.
    /// </summary>
    public int DefaultTimeoutSeconds { get; set; }

    /// <summary>
    /// The default maximum number of retries for a job.
    /// </summary>
    public int DefaultMaxRetries { get; set; }

    /// <summary>
    /// The default retry backoff interval in seconds.
    /// </summary>
    public int DefaultRetryBackoffSeconds { get; set; }

    /// <summary>
    /// The interval in milliseconds to poll the queue for new jobs.
    /// </summary>
    public int QueuePollIntervalMs { get; set; }

    /// <summary>
    /// Whether to enable cleanup of completed jobs.
    /// </summary>
    public bool EnableCleanup { get; set; }

    /// <summary>
    /// The interval in milliseconds to run the cleanup job.
    /// </summary>
    public int CleanupIntervalMs { get; set; }
}
