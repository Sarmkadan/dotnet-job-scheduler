#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ====================================================================

using BenchmarkDotNet.Attributes;
using JobScheduler.Core.Services;

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
    public void Setup() => _cacheService = new CacheService();

    /// <summary>Cache miss followed by cache hit for same key.</summary>
    [Benchmark]
    public object GetOrAdd_CacheMissThenHit()
    {
        const string key = "test-cron-expression-0 9 * * *";
        var value = _cacheService!.GetOrAdd(key, k => k, TimeSpan.FromHours(1));
        // Second access should hit cache
        var cachedValue = _cacheService.GetOrAdd(key, k => "different-value", TimeSpan.FromHours(1));
        return cachedValue;
    }

    /// <summary>Cache expiration and cleanup.</summary>
    [Benchmark]
    public async Task GetOrAdd_WithExpiration()
    {
        const string key = "expiring-key";
        var value = _cacheService!.GetOrAdd(key, k => "value", TimeSpan.FromMilliseconds(10));
        await Task.Delay(20); // Wait for expiration
        var expiredValue = _cacheService.GetOrAdd(key, k => "new-value", TimeSpan.FromHours(1));
        _ = expiredValue;
    }

    /// <summary>Concurrent cache access (thread-safe operations).</summary>
    [Benchmark]
    public void GetOrAdd_ConcurrentAccess()
    {
        Parallel.For(0, 100, i =>
        {
            var key = $"concurrent-key-{i}";
            _ = _cacheService!.GetOrAdd(key, k => "value", TimeSpan.FromHours(1));
        });
    }

    /// <summary>Cache eviction by size limit.</summary>
    [Benchmark]
    public void GetOrAdd_SizeLimitEviction()
    {
        // Fill cache beyond default limit of 1000 items
        for (int i = 0; i < 1500; i++)
        {
            var key = $"size-test-{i}";
            _ = _cacheService!.GetOrAdd(key, k => "value", TimeSpan.FromHours(1));
        }
    }

    /// <summary>Cache hit vs miss performance comparison.</summary>
    [Benchmark(Baseline = true)]
    public object GetOrAdd_CacheHit()
    {
        const string key = "hot-cache-key";
        // Prime the cache
        _ = _cacheService!.GetOrAdd(key, k => "value", TimeSpan.FromHours(1));
        // Access from cache
        return _cacheService.GetOrAdd(key, k => "different-value", TimeSpan.FromHours(1));
    }

    /// <summary>Cache removal operations.</summary>
    [Benchmark]
    public void Remove()
    {
        const string key = "removable-key";
        _cacheService!.GetOrAdd(key, k => "value", TimeSpan.FromHours(1));
        _cacheService.Remove(key);
        _ = _cacheService.GetOrAdd(key, k => "new-value", TimeSpan.FromHours(1));
    }

    /// <summary>Clear entire cache.</summary>
    [Benchmark]
    public void Clear()
    {
        // Add some items
        for (int i = 0; i < 100; i++)
        {
            var key = $"clear-test-{i}";
            _ = _cacheService!.GetOrAdd(key, k => "value", TimeSpan.FromHours(1));
        }
        _cacheService.Clear();
    }
}