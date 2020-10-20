# CacheService

A thread-safe, asynchronous caching service that provides key-value storage with pattern-based invalidation, statistics tracking, and integration with job scheduling metadata. It is designed to reduce database load and improve performance for frequently accessed or computationally expensive data in the `dotnet-job-scheduler` system.

## API

### `CacheService`

The primary service class providing cache operations. Internally uses a distributed or in-memory cache provider depending on configuration.

### `public async Task<T?> GetAsync<T>(string key)`

Retrieves a value from the cache by its key.

- **Parameters**:
  - `key` (string): The unique identifier for the cached item.
- **Returns**: The deserialized value of type `T`, or `null` if the key does not exist or the value is not of type `T`.
- **Throws**: `ArgumentNullException` if `key` is `null`.

### `public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)`

Stores a value in the cache with an optional expiration time.

- **Parameters**:
  - `key` (string): The unique identifier for the cached item.
  - `value` (T): The value to cache.
  - `expiration` (TimeSpan?, optional): The duration after which the item expires. If `null`, uses default cache TTL.
- **Returns**: A `Task` representing the asynchronous operation.
- **Throws**: `ArgumentNullException` if `key` is `null` or `value` is `null`.

### `public async Task RemoveAsync(string key)`

Removes a single item from the cache by its key.

- **Parameters**:
  - `key` (string): The key of the item to remove.
- **Returns**: A `Task` representing the asynchronous operation.
- **Throws**: `ArgumentNullException` if `key` is `null`.

### `public async Task InvalidatePatternAsync(string pattern)`

Removes all cache entries whose keys match the given pattern. Pattern matching is provider-specific (e.g., Redis glob-style or in-memory prefix-based).

- **Parameters**:
  - `pattern` (string): A pattern to match keys against (e.g., `"job:*"`).
- **Returns**: A `Task` representing the asynchronous operation.
- **Throws**: `ArgumentNullException` if `pattern` is `null`.

### `public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> valueFactory, TimeSpan? expiration = null)`

Retrieves a value from the cache; if not found, computes it using `valueFactory`, stores it, and returns it.

- **Parameters**:
  - `key` (string): The unique identifier for the cached item.
  - `valueFactory` (Func<Task<T>>): A function to compute the value if it is not in cache.
  - `expiration` (TimeSpan?, optional): The duration after which the item expires.
- **Returns**: The cached or newly computed value of type `T`.
- **Throws**:
  - `ArgumentNullException` if `key` is `null` or `valueFactory` is `null`.
  - Any exception thrown by `valueFactory`.

### `public async Task ClearAllAsync()`

Removes all entries from the cache.

- **Returns**: A `Task` representing the asynchronous operation.

### `public CacheStatistics GetStatistics()`

Returns a snapshot of current cache statistics, including total keys and timestamp.

- **Returns**: A `CacheStatistics` object containing:
  - `TotalKeys` (int): The number of keys currently stored.
  - `Timestamp` (DateTime): The UTC time when the statistics were captured.
- **Note**: The returned statistics reflect a point-in-time snapshot and may be immediately outdated.

### `public static string JobKey`

Constant string used as a prefix for all job-related cache keys (e.g., `"job:{jobId}"`).

### `public static string JobExecutionsKey`

Constant string used as a key for storing job execution metadata (e.g., `"job:executions"`).

### `public static string JobStatsKey`

Constant string used as a key for storing aggregated job statistics (e.g., `"job:stats"`).

### `public static string AllJobsKey`

Constant string used as a key for storing the complete list of job identifiers (e.g., `"jobs:all"`).

### `public static string JobsByStatusKey`

Constant string used as a prefix for grouping jobs by status (e.g., `"jobs:status:{status}"`).

### `public static string SystemStatsKey`

Constant string used as a key for storing system-wide statistics (e.g., `"system:stats"`).

### `public static string QueueStatusKey`

Constant string used as a key for tracking queue status (e.g., `"queue:status"`).

### `public static string SchedulerConfigKey`

Constant string used as a key for storing scheduler configuration (e.g., `"scheduler:config"`).

### `public static string ExecutionKey`

Constant string used as a prefix for execution-specific cache entries (e.g., `"exec:{executionId}"`).

### `public int TotalKeys`

Gets the total number of keys currently stored in the cache. This value is part of the `CacheStatistics` object returned by `GetStatistics()`.

### `public DateTime Timestamp`

Gets the UTC timestamp when the `CacheStatistics` were captured. This value is part of the `CacheStatistics` object returned by `GetStatistics()`.

## Usage

### Example 1: Caching Job Configuration
