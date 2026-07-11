# CacheServiceBenchmarksExtensions

`CacheServiceBenchmarksExtensions` provides a set of asynchronous extension methods designed to stress-test and evaluate the performance characteristics of `ICacheService` implementations within the `dotnet-job-scheduler` framework. These utilities facilitate benchmarking key cache operations, including bulk insertion throughput, retrieval hit ratios, expiration overhead, and eviction strategies under memory-constrained scenarios.

## API

### BulkInsert_ParallelAsync
Executes a high-concurrency insertion workload into the cache to evaluate write performance.

*   **Signature:** `public static async Task BulkInsert_ParallelAsync(this ICacheService cache, int count, int parallelism)`
*   **Purpose:** Measures the throughput and latency of inserting a specified number of items using a defined degree of parallelism.
*   **Throws:** `ArgumentOutOfRangeException` if `count` or `parallelism` are non-positive.

### MeasureHitRatioAsync
Evaluates the effectiveness of the cache by performing a series of mixed read/write operations and calculating the resulting hit/miss statistics.

*   **Signature:** `public static async Task<(int Hits, int Misses, double HitRatio)> MeasureHitRatioAsync(this ICacheService cache, int totalRequests)`
*   **Purpose:** Determines the cache hit ratio under a synthetic workload.
*   **Return Value:** A tuple containing the absolute number of hits, misses, and the calculated hit ratio as a `double`.

### MeasureExpirationImpactAsync
Quantifies the performance overhead incurred by the cache service when managing item expirations.

*   **Signature:** `public static async Task<double> MeasureExpirationImpactAsync(this ICacheService cache, TimeSpan expiration)`
*   **Purpose:** Measures the latency impact of handling expired items during retrieval or background cleanup processes.
*   **Return Value:** The average latency (in milliseconds) attributed to expiration handling.

### MeasureSizeLimitedEvictionAsync
Assesses the performance and correctness of the cache's eviction policy when operating at a fixed capacity.

*   **Signature:** `public static async Task MeasureSizeLimitedEvictionAsync(this ICacheService cache, int capacity)`
*   **Purpose:** Validates that the cache respects the provided capacity limit and measures the overhead of eviction operations when the cache is saturated.
*   **Throws:** `InvalidOperationException` if the underlying cache implementation does not support bounded sizing.

## Usage

```csharp
// Example 1: Measuring Cache Hit Ratio
var cache = new MyCacheService();
var results = await cache.MeasureHitRatioAsync(10000);
Console.WriteLine($"Hits: {results.Hits}, Misses: {results.Misses}, Hit Ratio: {results.HitRatio:P2}");

// Example 2: Parallel Bulk Insertion
var cache = new MyCacheService();
int itemsToInsert = 5000;
int degreeOfParallelism = 8;
await cache.BulkInsert_ParallelAsync(itemsToInsert, degreeOfParallelism);
Console.WriteLine("Bulk insertion completed.");
```

## Notes

*   **Performance Impact:** These methods are designed for benchmarking and are computationally expensive. They should not be executed in production environments.
*   **Thread Safety:** While these extensions are `static`, they rely on the underlying `ICacheService` implementation. If the `ICacheService` is not thread-safe, `BulkInsert_ParallelAsync` may cause race conditions or internal state corruption.
*   **Resource Usage:** `MeasureSizeLimitedEvictionAsync` and `BulkInsert_ParallelAsync` can significantly spike CPU and memory utilization. Ensure the host environment has sufficient resources when running these benchmarks.
*   **Asynchronous Context:** All methods are `async` and should be awaited to ensure proper completion and resource cleanup.
