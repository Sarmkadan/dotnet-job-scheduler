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

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheService"/> class.
    /// </summary>
    /// <param name="cache">The memory cache used to store entries.</param>
    /// <param name="logger">The logger used to record cache activity.</param>
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

    /// <summary>
    /// Releases the cleanup timer used by the cache service.
    /// </summary>
    public void Dispose()
    {
        _cleanupTimer?.Dispose();
    }

    /// <summary>
    /// Gets value from cache if exists and is not expired.
    /// Returns null if not found or expired.
    /// </summary>
    /// <typeparam name="T">The reference type of the cached value.</typeparam>
    /// <param name="key">The key of the cache entry.</param>
    /// <returns>A task whose result is the cached value, or <see langword="null"/> when no matching value is available.</returns>
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
    /// <typeparam name="T">The reference type of the value to cache.</typeparam>
    /// <param name="key">The key of the cache entry.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="expiration">The absolute expiration interval, or <see langword="null"/> to use the default interval.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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
    /// <param name="key">The key of the cache entry to remove.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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
    /// <param name="keyPattern">The text that matching cache keys contain.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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
    /// <typeparam name="T">The reference type of the cached value.</typeparam>
    /// <param name="key">The key of the cache entry.</param>
    /// <param name="factory">The function used to obtain a value when the cache does not contain one.</param>
    /// <param name="expiration">The absolute expiration interval, or <see langword="null"/> to use the default interval.</param>
    /// <returns>A task whose result is the cached or created value, or <see langword="null"/> when no value is available.</returns>
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
    /// <returns>A task that represents the asynchronous operation.</returns>
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
    /// <returns>A task that represents the asynchronous operation.</returns>
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
    /// <returns>The current cache statistics.</returns>
    public CacheStatistics GetStatistics()
    {
        return new CacheStatistics
        {
            TotalKeys = _keys.Count,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Returns a string that represents the cache service statistics.
    /// </summary>
    /// <returns>A string containing the total key count and statistics timestamp.</returns>
    public override string ToString()
    {
        var stats = GetStatistics();
        return $"CacheService {{ TotalKeys = {stats.TotalKeys}, Timestamp = {stats.Timestamp} }}";
    }
}

/// <summary>
/// Cache key generator to ensure consistency across the application.
/// WHY: Centralized key generation prevents typos and naming inconsistencies.
/// </summary>
public static class CacheKeyGenerator
{
    /// <summary>
    /// Creates a cache key for a job identifier.
    /// </summary>
    /// <param name="jobId">The job identifier.</param>
    /// <returns>The cache key for the job.</returns>
    public static string JobKey(Guid jobId) => $"job:{jobId}";

    /// <summary>
    /// Creates a cache key for a job name.
    /// </summary>
    /// <param name="jobName">The job name.</param>
    /// <returns>The cache key for the named job.</returns>
    public static string JobKey(string jobName) => $"job:name:{jobName}";

    /// <summary>
    /// Creates a cache key for a page of job executions.
    /// </summary>
    /// <param name="jobId">The job identifier.</param>
    /// <param name="pageNumber">The page number.</param>
    /// <returns>The cache key for the requested job executions page.</returns>
    public static string JobExecutionsKey(Guid jobId, int pageNumber = 1) => $"job:{jobId}:executions:page:{pageNumber}";

    /// <summary>
    /// Creates a cache key for job statistics.
    /// </summary>
    /// <param name="jobId">The job identifier.</param>
    /// <returns>The cache key for the job statistics.</returns>
    public static string JobStatsKey(Guid jobId) => $"job:{jobId}:stats";

    /// <summary>
    /// Gets the cache key for all jobs.
    /// </summary>
    /// <returns>The cache key for all jobs.</returns>
    public static string AllJobsKey() => "jobs:all";

    /// <summary>
    /// Creates a cache key for jobs with a specified status.
    /// </summary>
    /// <param name="status">The job status.</param>
    /// <returns>The cache key for jobs with the specified status.</returns>
    public static string JobsByStatusKey(string status) => $"jobs:status:{status}";

    /// <summary>
    /// Gets the cache key for system statistics.
    /// </summary>
    /// <returns>The cache key for system statistics.</returns>
    public static string SystemStatsKey() => "system:stats";

    /// <summary>
    /// Gets the cache key for the queue status.
    /// </summary>
    /// <returns>The cache key for the queue status.</returns>
    public static string QueueStatusKey() => "queue:status";

    /// <summary>
    /// Gets the cache key for the scheduler configuration.
    /// </summary>
    /// <returns>The cache key for the scheduler configuration.</returns>
    public static string SchedulerConfigKey() => "scheduler:config";

    /// <summary>
    /// Creates a cache key for an execution identifier.
    /// </summary>
    /// <param name="executionId">The execution identifier.</param>
    /// <returns>The cache key for the execution.</returns>
    public static string ExecutionKey(Guid executionId) => $"execution:{executionId}";
}

/// <summary>
/// Contains a snapshot of cache statistics.
/// </summary>
public sealed class CacheStatistics
{
    /// <summary>
    /// Gets or sets the number of tracked cache keys.
    /// </summary>
    public int TotalKeys { get; set; }

    /// <summary>
    /// Gets or sets the time at which the statistics were captured.
    /// </summary>
    public DateTime Timestamp { get; set; }
}
