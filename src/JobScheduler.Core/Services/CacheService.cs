#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobScheduler.Core.Services;

/// <summary>
/// In-memory caching service for frequently accessed scheduler data.
/// Reduces database queries and improves response times for hot data.
/// WHY: Caching is critical for performance when dealing with large job sets.
/// </summary>
public sealed class CacheService : IDisposable
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CacheService> _logger;
    private readonly ConcurrentDictionary<string, byte> _keys; // Track all cache keys for invalidation
    private readonly System.Threading.Timer? _cleanupTimer;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);
    private readonly object _cleanupLock = new object();

    public CacheService(IMemoryCache cache, ILogger<CacheService> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _keys = new ConcurrentDictionary<string, byte>();

        // Start background cleanup timer to prevent unbounded memory growth from expired entries
        // Timer fires every 5 minutes to clean up expired entries proactively
        _cleanupTimer = new System.Threading.Timer(
            _ => RemoveExpiredEntriesAsync().GetAwaiter().GetResult(),
            null,
            _cleanupInterval,
            _cleanupInterval);
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
    }

    /// <summary>
    /// Gets value from cache if exists and is not expired.
    /// Returns null if not found or expired.
    /// </summary>
    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        try
        {
            // TryGetValue returns false for expired entries, preventing check-then-use race
            if (_cache.TryGetValue(key, out var value))
            {
                _logger.LogDebug("Cache hit for key: {Key}", key);
                return value as T;
            }

            // If it's in our tracker but not in cache, it's expired or evicted, remove it from tracker.
            _keys.TryRemove(key, out _);
            _cache.Remove(key); // Ensure it's fully removed from IMemoryCache as well

            _logger.LogDebug("Cache miss for key: {Key}", key);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error retrieving from cache: {Key}", key);
            return null;
        }
    }

    /// <summary>
    /// Sets value in cache with expiration time.
    /// WHY: Expiration prevents stale data and unbounded cache growth.
    /// </summary>
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        try
        {
            var cacheOptions = new MemoryCacheEntryOptions();

            if (expiration.HasValue)
            {
                cacheOptions.SetAbsoluteExpiration(expiration.Value);
            }
            else
            {
                cacheOptions.SetAbsoluteExpiration(TimeSpan.FromHours(1)); // Default 1 hour
            }

            _cache.Set(key, value, cacheOptions);
            _keys.TryAdd(key, 0);

            _logger.LogDebug("Set cache value for key: {Key} with expiration: {Expiration}",
                key, expiration?.TotalSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error setting cache value: {Key}", key);
        }
    }

    /// <summary>
    /// Removes specific key from cache.
    /// </summary>
    public async Task RemoveAsync(string key)
    {
        try
        {
            _cache.Remove(key);
            _keys.TryRemove(key, out _);
            _logger.LogDebug("Removed cache key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error removing cache key: {Key}", key);
        }
    }

    /// <summary>
    /// Clears all cache entries matching a pattern.
    /// Useful for invalidating related cache entries (e.g., all job stats).
    /// </summary>
    public async Task InvalidatePatternAsync(string keyPattern)
    {
        try
        {
            var matchingKeys = _keys.Keys.Where(k => k.Contains(keyPattern)).ToList();

            foreach (var key in matchingKeys)
            {
                _cache.Remove(key);
                _keys.TryRemove(key, out _);
            }

            _logger.LogInformation("Invalidated {Count} cache entries matching pattern: {Pattern}",
                matchingKeys.Count, keyPattern);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error invalidating cache pattern: {Pattern}", keyPattern);
        }
    }

    /// <summary>
    /// Gets value from cache or fetches using provided factory function.
    /// Common pattern for lazy cache population.
    /// </summary>
    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T?>> factory, TimeSpan? expiration = null) where T : class
    {
        var cached = await GetAsync<T>(key);
        if (cached is not null)
            return cached;

        try
        {
            var value = await factory();
            if (value is not null)
            {
                await SetAsync(key, value, expiration);
            }
            return value;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error in cache factory for key: {Key}", key);
            return null;
        }
    }

    /// <summary>
    /// Clears entire cache.
    /// Used during scheduler shutdown or maintenance.
    /// </summary>
    public async Task ClearAllAsync()
    {
        try
        {
            foreach (var key in _keys.Keys)
            {
                _cache.Remove(key);
            }
            _keys.Clear();
            _logger.LogInformation("Cleared entire cache");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error clearing cache");
        }
    }

    /// <summary>
    /// Removes all expired entries from cache to prevent unbounded memory growth.
    /// WHY: IMemoryCache uses lazy eviction, so expired entries remain until accessed or memory pressure occurs.
    /// This proactive cleanup prevents memory leaks from unbounded cache growth.
    /// Bounded sweep: limits to 1000 keys per call to avoid blocking.
    /// </summary>
    public async Task RemoveExpiredEntriesAsync()
    {
        try
        {
            var expiredKeys = new List<string>();
            var processedCount = 0;
            const int batchSize = 1000;

            // Collect expired keys in batches to avoid blocking
            foreach (var key in _keys.Keys)
            {
                processedCount++;

                // Check if entry exists and is expired by attempting to get it
                // TryGetValue returns false for expired entries
                if (!_cache.TryGetValue(key, out _))
                {
                    expiredKeys.Add(key);
                }

                // Limit batch size to prevent long-running operations
                if (expiredKeys.Count >= batchSize)
                {
                    break;
                }
            }

            // Remove expired entries
            foreach (var key in expiredKeys)
            {
                _cache.Remove(key);
                _keys.TryRemove(key, out _);
            }

            if (expiredKeys.Count > 0)
            {
                _logger.LogInformation("Removed {Count} expired cache entries (batch of {BatchSize})", expiredKeys.Count, batchSize);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error removing expired cache entries");
        }
    }

    /// <summary>
    /// Gets cache statistics for monitoring.
    /// </summary>
    public CacheStatistics GetStatistics()
    {
        return new CacheStatistics
        {
            TotalKeys = _keys.Count,
            Timestamp = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Cache key generator to ensure consistency across the application.
/// WHY: Centralized key generation prevents typos and naming inconsistencies.
/// </summary>
public static class CacheKeyGenerator
{
    public static string JobKey(Guid jobId) => $"job:{jobId}";
    public static string JobKey(string jobName) => $"job:name:{jobName}";
    public static string JobExecutionsKey(Guid jobId, int pageNumber = 1) => $"job:{jobId}:executions:page:{pageNumber}";
    public static string JobStatsKey(Guid jobId) => $"job:{jobId}:stats";
    public static string AllJobsKey() => "jobs:all";
    public static string JobsByStatusKey(string status) => $"jobs:status:{status}";
    public static string SystemStatsKey() => "system:stats";
    public static string QueueStatusKey() => "queue:status";
    public static string SchedulerConfigKey() => "scheduler:config";
    public static string ExecutionKey(Guid executionId) => $"execution:{executionId}";
}

public sealed class CacheStatistics
{
    public int TotalKeys { get; set; }
    public DateTime Timestamp { get; set; }
}
