#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ====================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BenchmarkDotNet.Running;
using JobScheduler.Core.Services;

namespace JobScheduler.Benchmarks;

/// <summary>
/// Extension methods for <see cref="CacheServiceBenchmarks"/> that provide additional benchmarking scenarios
/// and helper methods for measuring cache service performance.
/// </summary>
public static class CacheServiceBenchmarksExtensions
{
    /// <summary>
    /// Measures the performance of bulk cache operations by adding multiple items in parallel.
    /// Useful for testing cache initialization scenarios and bulk loading.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance.</param>
    /// <param name="count">Number of items to add to cache.</param>
    /// <param name="keyPrefix">Prefix for cache keys.</param>
    /// <returns>Task representing the operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is less than 1.</exception>
    public static async Task BulkInsert_ParallelAsync(this CacheServiceBenchmarks benchmarks, int count, string keyPrefix = "bulk")
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);

        var tasks = new List<Task<string?>>(count);
        for (int i = 0; i < count; i++)
        {
            int index = i; // Capture for async lambda
            string key = $"{keyPrefix}-{index}";
            tasks.Add(benchmarks.GetOrAdd_CacheMissThenHit(key));
        }

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Measures cache hit ratio under concurrent load with controlled miss rate.
    /// Useful for testing cache effectiveness and hit/miss performance characteristics.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance.</param>
    /// <param name="totalRequests">Total number of cache requests.</param>
    /// <param name="missRatio">Ratio of requests that should result in cache misses (0.0 to 1.0).</param>
    /// <returns>Tuple containing hit count, miss count, and hit ratio.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="totalRequests"/> is less than 1.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="missRatio"/> is not between 0.0 and 1.0.</exception>
    public static async Task<(int Hits, int Misses, double HitRatio)> MeasureHitRatioAsync(
        this CacheServiceBenchmarks benchmarks,
        int totalRequests,
        double missRatio)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        ArgumentOutOfRangeException.ThrowIfLessThan(totalRequests, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(1.0 - missRatio, 0.0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(missRatio, 1.0);

        var random = new Random();
        int hits = 0;
        int misses = 0;

        for (int i = 0; i < totalRequests; i++)
        {
            bool shouldMiss = random.NextDouble() < missRatio;
            string key = shouldMiss ? $"miss-key-{i}" : $"hit-key-{i % 100}";

            string? result = await benchmarks.GetOrAdd_CacheHit(key);
            if (result != null)
            {
                hits++;
            }
            else
            {
                misses++;
            }
        }

        double hitRatio = hits > 0 ? (double)hits / totalRequests : 0;
        return (hits, misses, hitRatio);
    }

    /// <summary>
    /// Measures the performance impact of cache operations with different expiration times.
    /// Useful for testing how expiration policies affect cache performance.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance.</param>
    /// <param name="expirationTime">Expiration time for cache entries.</param>
    /// <param name="iterations">Number of cache operations to perform.</param>
    /// <returns>Average time per operation in milliseconds.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="iterations"/> is less than 1.</exception>
    public static async Task<double> MeasureExpirationImpactAsync(
        this CacheServiceBenchmarks benchmarks,
        TimeSpan expirationTime,
        int iterations = 100)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        ArgumentOutOfRangeException.ThrowIfLessThan(iterations, 1);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
        {
            await benchmarks.GetOrAdd_WithExpiration(expirationTime);
        }

        stopwatch.Stop();
        return stopwatch.Elapsed.TotalMilliseconds / iterations;
    }

    /// <summary>
    /// Measures the performance of cache operations with size constraints.
    /// Useful for testing cache eviction policies and memory pressure scenarios.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance.</param>
    /// <param name="initialSize">Initial number of items to add to cache.</param>
    /// <param name="evictionThreshold">Number of items that should trigger eviction.</param>
    /// <returns>Task representing the operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="initialSize"/> is less than 0.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="evictionThreshold"/> is less than 1.</exception>
    public static async Task MeasureSizeLimitedEvictionAsync(
        this CacheServiceBenchmarks benchmarks,
        int initialSize,
        int evictionThreshold)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        ArgumentOutOfRangeException.ThrowIfLessThan(initialSize, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(evictionThreshold, 1);

        // Add initial items
        for (int i = 0; i < initialSize; i++)
        {
            await benchmarks.GetOrAdd_SizeLimitEviction();
        }

        // Trigger eviction by adding more items
        await benchmarks.GetOrAdd_SizeLimitEviction();
    }
}