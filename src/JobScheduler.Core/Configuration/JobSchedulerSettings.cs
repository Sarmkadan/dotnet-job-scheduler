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
    public string? ConnectionString { get; set; }
    public int MaxConcurrentJobs { get; set; } = 10;
    public int DefaultTimeoutSeconds { get; set; } = 300;
    public int DefaultMaxRetries { get; set; } = 3;
    public int DefaultRetryBackoffSeconds { get; set; } = 5;
    public int QueuePollIntervalMs { get; set; } = 5000;
    public bool EnableCleanup { get; set; } = true;
    public int CleanupIntervalMs { get; set; } = 300000;
    public int MaxJobNameLength { get; set; } = 255;
    public int MaxCronExpressionLength { get; set; } = 255;
}

/// <summary>
/// Notification service configuration settings.
/// </summary>
public sealed class NotificationSettings
{
    public bool EnableWebhooks { get; set; } = false;
    public bool EnableSlack { get; set; } = false;
    public bool EnableEmail { get; set; } = false;

    public string? SlackWebhookUrl { get; set; }
    public string? SmtpServer { get; set; }
    public int SmtpPort { get; set; } = 587;
    public string? SmtpUsername { get; set; }
    public string? SmtpPassword { get; set; }
    public string? SmtpFromEmail { get; set; }
    public List<string> AlertEmails { get; set; } = new();
}

/// <summary>
/// Caching layer configuration.
/// </summary>
public sealed class CachingSettings
{
    public bool EnableCache { get; set; } = true;
    public int DefaultCacheDurationMinutes { get; set; } = 60;
    public int MaxCacheEntries { get; set; } = 10000;
    public bool EnableDistributedCache { get; set; } = false;
    public string? RedisConnectionString { get; set; }
}

/// <summary>
/// Security and authentication settings.
/// </summary>
public sealed class SecuritySettings
{
    public bool EnableApiKeyAuth { get; set; } = false;
    public List<ApiKeyConfig> ApiKeys { get; set; } = new();
    public bool RequireHttps { get; set; } = true;
    public bool EnableCors { get; set; } = false;
    public List<string> CorsOrigins { get; set; } = new();
}

/// <summary>
/// API key configuration for secured endpoints.
/// </summary>
public sealed class ApiKeyConfig
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Active { get; set; } = true;
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// Logging and monitoring configuration.
/// </summary>
public sealed class LoggingSettings
{
    public string LogLevel { get; set; } = "Information";
    public bool EnableDetailedLogging { get; set; } = false;
    public bool EnableAuditLogging { get; set; } = true;
    public int AuditLogRetentionDays { get; set; } = 90;
    public string? LogFilePath { get; set; }
    public bool EnableStructuredLogging { get; set; } = false;
}

/// <summary>
/// Performance monitoring settings.
/// </summary>
public sealed class PerformanceSettings
{
    public bool EnablePerformanceMonitoring { get; set; } = true;
    public int MetricsRetentionMinutes { get; set; } = 1440; // 24 hours
    public bool EnableSlowQueryLogging { get; set; } = true;
    public int SlowQueryThresholdMs { get; set; } = 1000;
    public bool EnablePercentileTracking { get; set; } = true;
}

/// <summary>
/// Database and persistence settings.
/// </summary>
public sealed class PersistenceSettings
{
    public string? DatabaseProvider { get; set; } = "SqlServer"; // SqlServer, PostgreSQL, SQLite, etc.
    public int CommandTimeoutSeconds { get; set; } = 30;
    public bool EnableAutoMigration { get; set; } = true;
    public int MaxConnectionPoolSize { get; set; } = 100;
    public bool EnableQueryLogging { get; set; } = false;
}

/// <summary>
/// Distributed scheduler settings for multi-instance deployments.
/// </summary>
public sealed class DistributedSettings
{
    public bool EnableDistributed { get; set; } = false;
    public string? ServiceName { get; set; }
    public string? ServiceInstanceId { get; set; }
    public bool EnableServiceDiscovery { get; set; } = false;
    public string? ServiceRegistryUrl { get; set; }
    public int HeartbeatIntervalSeconds { get; set; } = 30;
}

/// <summary>
/// Feature flag settings for A/B testing and gradual rollouts.
/// </summary>
public sealed class FeatureFlags
{
    public bool EnableAdvancedScheduling { get; set; } = false;
    public bool EnableJobChaining { get; set; } = false;
    public bool EnableWorkflows { get; set; } = false;
    public bool EnableDistributedLocking { get; set; } = false;
}
