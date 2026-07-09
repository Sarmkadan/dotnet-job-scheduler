using Microsoft.Extensions.Options;

public class DotnetJobSchedulerOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public int MaxConcurrentJobs { get; set; }
    public int DefaultTimeoutSeconds { get; set; }
    public int DefaultMaxRetries { get; set; }
    public int DefaultRetryBackoffSeconds { get; set; }
    public int QueuePollIntervalMs { get; set; }
    public bool EnableCleanup { get; set; }
    public int CleanupIntervalMs { get; set; }
}
