#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ====================================================================

using BenchmarkDotNet.Attributes;
using JobScheduler.Core.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace JobScheduler.Benchmarks;

/// <summary>
/// Measures CacheService operations that provide in-memory caching for:
/// - Cron expression schedules (cached after first parse)
/// - Job metadata and configuration
/// - Performance metrics and statistics
/// - Distributed lock leases
/// These operations are on the hot path for schedule evaluation and job metadata access.
/// </summary>
[MemoryDiagnoser]
public sealed class CacheServiceBenchmarks
{
    private CacheService? _cacheService;

    [GlobalSetup]
    public void Setup()
    {
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var logger = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Error))
            .CreateLogger<CacheService>();
        _cacheService = new CacheService(memoryCache, logger);
    }

    /// <summary>Cache miss followed by cache hit for same key.</summary>
    [Benchmark]
    public async Task<string?> GetOrAdd_CacheMissThenHit(string key = "test-cron-expression-0 9 * * *")
    {
        string keyParam = key;
        var value = await _cacheService!.GetOrSetAsync(keyParam, () => Task.FromResult<string?>("value"), TimeSpan.FromHours(1));
        // Second access should hit cache
        var cachedValue = await _cacheService.GetOrSetAsync(keyParam, () => Task.FromResult<string?>("different-value"), TimeSpan.FromHours(1));
        return cachedValue;
    }

    /// <summary>Cache expiration and cleanup.</summary>
    [Benchmark]
    public async Task GetOrAdd_WithExpiration(TimeSpan expirationTime = default)
    {
        var keyParam = "expiring-key";
        var value = await _cacheService!.GetOrSetAsync(keyParam, () => Task.FromResult<string?>("value"), expirationTime);
        await Task.Delay(20); // Wait for expiration
        var expiredValue = await _cacheService.GetOrSetAsync(keyParam, () => Task.FromResult<string?>("new-value"), TimeSpan.FromHours(1));
        _ = expiredValue;
    }

    /// <summary>Concurrent cache access (thread-safe operations).</summary>
    [Benchmark]
    public void GetOrAdd_ConcurrentAccess()
    {
        Parallel.For(0, 100, i =>
        {
            var key = $"concurrent-key-{i}";
            _ = _cacheService!.GetOrSetAsync(key, () => Task.FromResult<string?>("value"), TimeSpan.FromHours(1))
                .GetAwaiter().GetResult();
        });
    }

    /// <summary>Cache eviction by size limit.</summary>
    [Benchmark]
    public async Task GetOrAdd_SizeLimitEviction()
    {
        // Fill cache beyond default limit of 1000 items
        for (int i = 0; i < 1500; i++)
        {
            var key = $"size-test-{i}";
            _ = await _cacheService!.GetOrSetAsync(key, () => Task.FromResult<string?>("value"), TimeSpan.FromHours(1));
        }
    }

    /// <summary>Cache hit vs miss performance comparison.</summary>
    [Benchmark(Baseline = true)]
    public async Task<string?> GetOrAdd_CacheHit(string key = "hot-cache-key")
    {
        var keyParam = key;
        // Prime the cache
        _ = await _cacheService!.GetOrSetAsync(keyParam, () => Task.FromResult<string?>("value"), TimeSpan.FromHours(1));
        // Access from cache
        return await _cacheService.GetOrSetAsync(keyParam, () => Task.FromResult<string?>("different-value"), TimeSpan.FromHours(1));
    }

    /// <summary>Cache removal operations.</summary>
    [Benchmark]
    public async Task Remove()
    {
        const string key = "removable-key";
        await _cacheService!.GetOrSetAsync(key, () => Task.FromResult<string?>("value"), TimeSpan.FromHours(1));
        await _cacheService.RemoveAsync(key);
        _ = await _cacheService.GetOrSetAsync(key, () => Task.FromResult<string?>("new-value"), TimeSpan.FromHours(1));
    }

    /// <summary>Clear entire cache.</summary>
    [Benchmark]
    public async Task Clear()
    {
        // Add some items
        for (int i = 0; i < 100; i++)
        {
            var key = $"clear-test-{i}";
            _ = await _cacheService!.GetOrSetAsync(key, () => Task.FromResult<string?>("value"), TimeSpan.FromHours(1));
        }
        await _cacheService.ClearAllAsync();
    }
}
