#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace JobScheduler.Core.Configuration;

/// <summary>
/// Encapsulates all configuration settings for the job scheduler.
/// WHY: Centralized configuration class improves maintainability and enables type-safe configuration.
/// </summary>
public sealed class JobSchedulerSettings
{
    /// <summary>
    /// Gets or sets the database connection string.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of jobs that may run concurrently. The default is 10.
    /// </summary>
    public int MaxConcurrentJobs { get; set; } = 10;

    /// <summary>
    /// Gets or sets the default job timeout, in seconds. The default is 300.
    /// </summary>
    public int DefaultTimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Gets or sets the default maximum number of retry attempts. The default is 3.
    /// </summary>
    public int DefaultMaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the default retry backoff, in seconds. The default is 5.
    /// </summary>
    public int DefaultRetryBackoffSeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets the queue polling interval, in milliseconds. The default is 5000.
    /// </summary>
    public int QueuePollIntervalMs { get; set; } = 5000;

    /// <summary>
    /// Gets or sets whether automatic cleanup is enabled. The default is <see langword="true"/>.
    /// </summary>
    public bool EnableCleanup { get; set; } = true;

    /// <summary>
    /// Gets or sets the cleanup interval, in milliseconds. The default is 300000.
    /// </summary>
    public int CleanupIntervalMs { get; set; } = 300000;

    /// <summary>
    /// Gets or sets the maximum job name length. The default is 255.
    /// </summary>
    public int MaxJobNameLength { get; set; } = 255;

    /// <summary>
    /// Gets or sets the maximum cron expression length. The default is 255.
    /// </summary>
    public int MaxCronExpressionLength { get; set; } = 255;

    public override string ToString() => $"JobSchedulerSettings {{ ConnectionString = {ConnectionString}, MaxConcurrentJobs = {MaxConcurrentJobs}, DefaultTimeoutSeconds = {DefaultTimeoutSeconds}, DefaultMaxRetries = {DefaultMaxRetries}, DefaultRetryBackoffSeconds = {DefaultRetryBackoffSeconds}, QueuePollIntervalMs = {QueuePollIntervalMs} }}";
}

/// <summary>
/// Notification service configuration settings.
/// </summary>
public sealed class NotificationSettings
{
    /// <summary>
    /// Gets or sets whether webhook notifications are enabled. The default is <see langword="false"/>.
    /// </summary>
    public bool EnableWebhooks { get; set; } = false;

    /// <summary>
    /// Gets or sets whether Slack notifications are enabled. The default is <see langword="false"/>.
    /// </summary>
    public bool EnableSlack { get; set; } = false;

    /// <summary>
    /// Gets or sets whether email notifications are enabled. The default is <see langword="false"/>.
    /// </summary>
    public bool EnableEmail { get; set; } = false;

    /// <summary>
    /// Gets or sets the Slack webhook URL.
    /// </summary>
    public string? SlackWebhookUrl { get; set; }

    /// <summary>
    /// Gets or sets the SMTP server host name.
    /// </summary>
    public string? SmtpServer { get; set; }

    /// <summary>
    /// Gets or sets the SMTP server port. The default is 587.
    /// </summary>
    public int SmtpPort { get; set; } = 587;

    /// <summary>
    /// Gets or sets the SMTP user name.
    /// </summary>
    public string? SmtpUsername { get; set; }

    /// <summary>
    /// Gets or sets the SMTP password.
    /// </summary>
    public string? SmtpPassword { get; set; }

    /// <summary>
    /// Gets or sets the sender email address used for SMTP messages.
    /// </summary>
    public string? SmtpFromEmail { get; set; }

    /// <summary>
    /// Gets or sets the email addresses that receive alerts. The default is an empty list.
    /// </summary>
    public List<string> AlertEmails { get; set; } = new();

    /// <summary>
    /// Returns a string representation of the notification settings.
    /// </summary>
    public override string ToString() => $"NotificationSettings {{ EnableWebhooks = {EnableWebhooks}, EnableSlack = {EnableSlack}, EnableEmail = {EnableEmail}, SmtpServer = {SmtpServer}, SmtpPort = {SmtpPort}, SmtpUsername = {SmtpUsername}, SmtpFromEmail = {SmtpFromEmail}, AlertEmails = {AlertEmails.Count} }}";
}

/// <summary>
/// Caching layer configuration.
/// </summary>
public sealed class CachingSettings
{
    /// <summary>
    /// Gets or sets whether caching is enabled. The default is <see langword="true"/>.
    /// </summary>
    public bool EnableCache { get; set; } = true;

    /// <summary>
    /// Gets or sets the default cache duration, in minutes. The default is 60.
    /// </summary>
    public int DefaultCacheDurationMinutes { get; set; } = 60;

    /// <summary>
    /// Gets or sets the maximum number of cache entries. The default is 10000.
    /// </summary>
    public int MaxCacheEntries { get; set; } = 10000;

    /// <summary>
    /// Gets or sets whether distributed caching is enabled. The default is <see langword="false"/>.
    /// </summary>
    public bool EnableDistributedCache { get; set; } = false;

    /// <summary>
    /// Gets or sets the Redis connection string.
    /// </summary>
    public string? RedisConnectionString { get; set; }

    /// <summary>
    /// Returns a string representation of the caching settings.
    /// </summary>
    public override string ToString() => $"CachingSettings {{ EnableCache = {EnableCache}, DefaultCacheDurationMinutes = {DefaultCacheDurationMinutes}, MaxCacheEntries = {MaxCacheEntries}, EnableDistributedCache = {EnableDistributedCache} }}";
}

/// <summary>
/// Security and authentication settings.
/// </summary>
public sealed class SecuritySettings
{
    /// <summary>
    /// Gets or sets whether API key authentication is enabled. The default is <see langword="false"/>.
    /// </summary>
    public bool EnableApiKeyAuth { get; set; } = false;

    /// <summary>
    /// Gets or sets the configured API keys. The default is an empty list.
    /// </summary>
    public List<ApiKeyConfig> ApiKeys { get; set; } = new();

    /// <summary>
    /// Gets or sets whether HTTPS is required. The default is <see langword="true"/>.
    /// </summary>
    public bool RequireHttps { get; set; } = true;

    /// <summary>
    /// Gets or sets whether cross-origin resource sharing is enabled. The default is <see langword="false"/>.
    /// </summary>
    public bool EnableCors { get; set; } = false;

    /// <summary>
    /// Gets or sets the allowed cross-origin resource sharing origins. The default is an empty list.
    /// </summary>
    public List<string> CorsOrigins { get; set; } = new();

    /// <summary>
    /// Returns a string representation of the security settings, masking any sensitive key values.
    /// </summary>
    public override string ToString() => $"SecuritySettings {{ EnableApiKeyAuth = {EnableApiKeyAuth}, ApiKeys = {ApiKeys.Count}, RequireHttps = {RequireHttps}, EnableCors = {EnableCors}, CorsOrigins = {CorsOrigins.Count} }}";
}

/// <summary>
/// API key configuration for secured endpoints.
/// </summary>
public sealed class ApiKeyConfig
{
    /// <summary>
    /// Gets or sets the API key value. The default is an empty string.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the API key name. The default is an empty string.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the API key is active. The default is <see langword="true"/>.
    /// </summary>
    public bool Active { get; set; } = true;

    /// <summary>
    /// Gets or sets the date and time when the API key expires.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Returns a string representation of the API key configuration, masking the key value.
    /// </summary>
    public override string ToString() => $"ApiKeyConfig {{ Key = {(string.IsNullOrEmpty(Key) ? "***" : $"*** (length {Key.Length})")}, Name = {Name}, Active = {Active}, ExpiresAt = {ExpiresAt} }}";
}

/// <summary>
/// Logging and monitoring configuration.
/// </summary>
public sealed class LoggingSettings
{
    /// <summary>
    /// Gets or sets the minimum logging level. The default is <c>Information</c>.
    /// </summary>
    public string LogLevel { get; set; } = "Information";

    /// <summary>
    /// Gets or sets whether detailed logging is enabled. The default is <see langword="false"/>.
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;

    /// <summary>
    /// Gets or sets whether audit logging is enabled. The default is <see langword="true"/>.
    /// </summary>
    public bool EnableAuditLogging { get; set; } = true;

    /// <summary>
    /// Gets or sets the audit log retention period, in days. The default is 90.
    /// </summary>
    public int AuditLogRetentionDays { get; set; } = 90;

    /// <summary>
    /// Gets or sets the log file path.
    /// </summary>
    public string? LogFilePath { get; set; }

    /// <summary>
    /// Gets or sets whether structured logging is enabled. The default is <see langword="false"/>.
    /// </summary>
    public bool EnableStructuredLogging { get; set; } = false;

    /// <summary>
    /// Returns a string representation of the logging settings.
    /// </summary>
    public override string ToString() => $"LoggingSettings {{ LogLevel = {LogLevel}, EnableDetailedLogging = {EnableDetailedLogging}, EnableAuditLogging = {EnableAuditLogging}, AuditLogRetentionDays = {AuditLogRetentionDays}, EnableStructuredLogging = {EnableStructuredLogging} }}";
}

/// <summary>
/// Performance monitoring settings.
/// </summary>
public sealed class PerformanceSettings
{
    /// <summary>
    /// Gets or sets whether performance monitoring is enabled. The default is <see langword="true"/>.
    /// </summary>
    public bool EnablePerformanceMonitoring { get; set; } = true;

    /// <summary>
    /// Gets or sets the metrics retention period, in minutes. The default is 1440.
    /// </summary>
    public int MetricsRetentionMinutes { get; set; } = 1440; // 24 hours

    /// <summary>
    /// Gets or sets whether slow query logging is enabled. The default is <see langword="true"/>.
    /// </summary>
    public bool EnableSlowQueryLogging { get; set; } = true;

    /// <summary>
    /// Gets or sets the slow query threshold, in milliseconds. The default is 1000.
    /// </summary>
    public int SlowQueryThresholdMs { get; set; } = 1000;

    /// <summary>
    /// Gets or sets whether percentile tracking is enabled. The default is <see langword="true"/>.
    /// </summary>
    public bool EnablePercentileTracking { get; set; } = true;

    /// <summary>
    /// Returns a string representation of the performance settings.
    /// </summary>
    public override string ToString() => $"PerformanceSettings {{ EnablePerformanceMonitoring = {EnablePerformanceMonitoring}, MetricsRetentionMinutes = {MetricsRetentionMinutes}, EnableSlowQueryLogging = {EnableSlowQueryLogging}, SlowQueryThresholdMs = {SlowQueryThresholdMs}, EnablePercentileTracking = {EnablePercentileTracking} }}";
}

/// <summary>
/// Database and persistence settings.
/// </summary>
public sealed class PersistenceSettings
{
    /// <summary>
    /// Gets or sets the database provider. The default is <c>SqlServer</c>.
    /// </summary>
    public string? DatabaseProvider { get; set; } = "SqlServer"; // SqlServer, PostgreSQL, SQLite, etc.

    /// <summary>
    /// Gets or sets the database command timeout, in seconds. The default is 30.
    /// </summary>
    public int CommandTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets whether automatic database migration is enabled. The default is <see langword="true"/>.
    /// </summary>
    public bool EnableAutoMigration { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum database connection pool size. The default is 100.
    /// </summary>
    public int MaxConnectionPoolSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets whether database query logging is enabled. The default is <see langword="false"/>.
    /// </summary>
    public bool EnableQueryLogging { get; set; } = false;

    /// <summary>
    /// Returns a string representation of the persistence settings.
    /// </summary>
    public override string ToString() => $"PersistenceSettings {{ DatabaseProvider = {DatabaseProvider}, CommandTimeoutSeconds = {CommandTimeoutSeconds}, EnableAutoMigration = {EnableAutoMigration}, MaxConnectionPoolSize = {MaxConnectionPoolSize}, EnableQueryLogging = {EnableQueryLogging} }}";
}

/// <summary>
/// Distributed scheduler settings for multi-instance deployments.
/// </summary>
public sealed class DistributedSettings
{
    /// <summary>
    /// Gets or sets whether distributed scheduling is enabled. The default is <see langword="false"/>.
    /// </summary>
    public bool EnableDistributed { get; set; } = false;

    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Gets or sets the service instance identifier.
    /// </summary>
    public string? ServiceInstanceId { get; set; }

    /// <summary>
    /// Gets or sets whether service discovery is enabled. The default is <see langword="false"/>.
    /// </summary>
    public bool EnableServiceDiscovery { get; set; } = false;

    /// <summary>
    /// Gets or sets the service registry URL.
    /// </summary>
    public string? ServiceRegistryUrl { get; set; }

    /// <summary>
    /// Gets or sets the heartbeat interval, in seconds. The default is 30.
    /// </summary>
    public int HeartbeatIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Enables database-backed distributed leader election so only one node fires
    /// jobs at each scheduled interval. The default is <see langword="false"/>.
    /// </summary>
    public bool EnableLeaderElection { get; set; } = false;

    /// <summary>
    /// Gets or sets the number of seconds before an un-renewed leader lease expires and another node may take over.
    /// The default is 30.
    /// </summary>
    public int LeaderElectionLeaseDurationSeconds { get; set; } = 30;

    /// <summary>
    /// Returns a string representation of the distributed settings.
    /// </summary>
    public override string ToString() => $"DistributedSettings {{ EnableDistributed = {EnableDistributed}, ServiceName = {ServiceName}, ServiceInstanceId = {ServiceInstanceId}, EnableServiceDiscovery = {EnableServiceDiscovery}, EnableLeaderElection = {EnableLeaderElection}, LeaderElectionLeaseDurationSeconds = {LeaderElectionLeaseDurationSeconds} }}";
}

/// <summary>
/// Feature flag settings for A/B testing and gradual rollouts.
/// </summary>
public sealed class FeatureFlags
{
    /// <summary>
    /// Gets or sets whether advanced scheduling is enabled. The default is <see langword="false"/>.
    /// </summary>
    public bool EnableAdvancedScheduling { get; set; } = false;

    /// <summary>
    /// Gets or sets whether job chaining is enabled. The default is <see langword="false"/>.
    /// </summary>
    public bool EnableJobChaining { get; set; } = false;

    /// <summary>
    /// Gets or sets whether workflows are enabled. The default is <see langword="false"/>.
    /// </summary>
    public bool EnableWorkflows { get; set; } = false;

    /// <summary>
    /// Gets or sets whether distributed locking is enabled. The default is <see langword="false"/>.
    /// </summary>
    public bool EnableDistributedLocking { get; set; } = false;

    /// <summary>
    /// Returns a string representation of the feature flags.
    /// </summary>
    public override string ToString() => $"FeatureFlags {{ EnableAdvancedScheduling = {EnableAdvancedScheduling}, EnableJobChaining = {EnableJobChaining}, EnableWorkflows = {EnableWorkflows}, EnableDistributedLocking = {EnableDistributedLocking} }}";
}
