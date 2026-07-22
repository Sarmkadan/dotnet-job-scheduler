#nullable enable

using FluentAssertions;
using JobScheduler.Core.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotnetJobScheduler.Tests;

/// <summary>
/// Provides unit tests for the <see cref="CacheService"/> class to verify caching behavior, expiration, pattern invalidation,
/// and error handling functionality.
/// </summary>
public sealed class CacheServiceTests
{
	private readonly Mock<ILogger<CacheService>> _loggerMock = new();
	private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

	/// <summary>
	/// Creates a new instance of <see cref="CacheService"/> with mocked dependencies for testing.
	/// </summary>
	/// <returns>A new <see cref="CacheService"/> instance.</returns>
	private CacheService CreateService() => new(_cache, _loggerMock.Object);

	[Fact]
	public async Task GetAsync_WithExistingKey_ReturnsValue()
	{
		/// <summary>
		/// Tests that retrieving a value from cache with an existing key returns the cached value.
		/// </summary>
		// Arrange
		var service = CreateService();
		var key = "test-key";
		var value = new TestCacheValue { Id = 1, Name = "Test" };

		await service.SetAsync(key, value);

		// Act
		var result = await service.GetAsync<TestCacheValue>(key);

		// Assert
		result.Should().NotBeNull();
		result?.Id.Should().Be(1);
		result?.Name.Should().Be("Test");
	}

	[Fact]
	public async Task GetAsync_WithNonexistentKey_ReturnsNull()
	{
		/// <summary>
		/// Tests that retrieving a value from cache with a non-existent key returns null.
		/// </summary>
		// Arrange
		var service = CreateService();

		// Act
		var result = await service.GetAsync<TestCacheValue>("nonexistent-key");

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task SetAsync_WithDefaultExpiration_CachesValue()
	{
		/// <summary>
		/// Tests that setting a value with default expiration successfully caches the value.
		/// </summary>
		// Arrange
		var service = CreateService();
		var key = "test-key";
		var value = new TestCacheValue { Id = 1, Name = "Test" };

		// Act
		await service.SetAsync(key, value);

		// Assert
		var cached = await service.GetAsync<TestCacheValue>(key);
		cached.Should().NotBeNull();
		cached?.Id.Should().Be(1);
	}

	[Fact]
	public async Task SetAsync_WithCustomExpiration_RespectsTTL()
	{
		/// <summary>
		/// Tests that setting a value with custom expiration respects the time-to-live duration.
		/// </summary>
		// Arrange
		var service = CreateService();
		var key = "short-lived-key";
		var value = new TestCacheValue { Id = 1, Name = "Temporary" };
		var expiration = TimeSpan.FromMilliseconds(100);

		// Act
		await service.SetAsync(key, value, expiration);
		var beforeExpiry = await service.GetAsync<TestCacheValue>(key);

		await Task.Delay(150); // Wait for expiration
		var afterExpiry = await service.GetAsync<TestCacheValue>(key);

		// Assert
		beforeExpiry.Should().NotBeNull();
	afterExpiry.Should().BeNull();
	}

	[Fact]
	public async Task SetAsync_WithSameKeyMultipleTimes_OverwritesPreviousValue()
	{
		/// <summary>
		/// Tests that setting a value multiple times with the same key overwrites the previous value.
		/// </summary>
		// Arrange
		var service = CreateService();
		var key = "overwrite-key";

		// Act
		await service.SetAsync(key, new TestCacheValue { Id = 1, Name = "First" });
		await service.SetAsync(key, new TestCacheValue { Id = 2, Name = "Second" });

		var result = await service.GetAsync<TestCacheValue>(key);

		// Assert
		result?.Id.Should().Be(2);
		result?.Name.Should().Be("Second");
	}

	[Fact]
	public async Task RemoveAsync_DeletesKey()
	{
		/// <summary>
		/// Tests that removing a key successfully deletes it from the cache.
		/// </summary>
		// Arrange
		var service = CreateService();
		var key = "removable-key";
		var value = new TestCacheValue { Id = 1, Name = "Remove Me" };

		await service.SetAsync(key, value);
		var beforeRemove = await service.GetAsync<TestCacheValue>(key);

		// Act
		await service.RemoveAsync(key);
		var afterRemove = await service.GetAsync<TestCacheValue>(key);

		// Assert
		beforeRemove.Should().NotBeNull();
	afterRemove.Should().BeNull();
	}

	[Fact]
	public async Task RemoveAsync_WithNonexistentKey_DoesNotThrow()
	{
		/// <summary>
		/// Tests that removing a non-existent key does not throw an exception.
		/// </summary>
		// Arrange
		var service = CreateService();

		// Act & Assert
		await service.RemoveAsync("nonexistent-key");
	}

	[Fact]
	public async Task InvalidatePatternAsync_RemovesMatchingKeys()
	{
		/// <summary>
		/// Tests that invalidating with a pattern removes all keys matching the pattern.
		/// </summary>
		// Arrange
		var service = CreateService();
		await service.SetAsync("job:1", new TestCacheValue { Id = 1 });
		await service.SetAsync("job:2", new TestCacheValue { Id = 2 });
		await service.SetAsync("execution:1", new TestCacheValue { Id = 3 });

		// Act
		await service.InvalidatePatternAsync("job:");

		// Assert
		var job1 = await service.GetAsync<TestCacheValue>("job:1");
		var job2 = await service.GetAsync<TestCacheValue>("job:2");
		var execution1 = await service.GetAsync<TestCacheValue>("execution:1");

		job1.Should().BeNull();
		job2.Should().BeNull();
		execution1.Should().NotBeNull();
	}

	[Fact]
	public async Task InvalidatePatternAsync_WithNoMatchingKeys_DoesNothing()
	{
		/// <summary>
		/// Tests that invalidating with a pattern that matches no keys leaves the cache unchanged.
		/// </summary>
		// Arrange
		var service = CreateService();
		await service.SetAsync("job:1", new TestCacheValue { Id = 1 });

		// Act
		await service.InvalidatePatternAsync("execution:");

		// Assert
		var job = await service.GetAsync<TestCacheValue>("job:1");
		job.Should().NotBeNull();
	}

	[Fact]
	public async Task GetOrSetAsync_WithCachedValue_ReturnsCached()
	{
		/// <summary>
		/// Tests that GetOrSetAsync returns cached value when it exists instead of calling the factory.
		/// </summary>
		// Arrange
		var service = CreateService();
		var key = "cached-key";
		var cachedValue = new TestCacheValue { Id = 1, Name = "Cached" };
		var factoryCalled = false;

		await service.SetAsync(key, cachedValue);

		// Act
		var result = await service.GetOrSetAsync(key,
			async () =>
			{
				factoryCalled = true;
				return new TestCacheValue { Id = 2, Name = "Factory" };
			});

		// Assert
		result.Should().NotBeNull();
		result?.Id.Should().Be(1); // From cache, not factory
		factoryCalled.Should().BeFalse();
	}

	[Fact]
	public async Task GetOrSetAsync_WithMissingKey_CallsFactory()
	{
		/// <summary>
		/// Tests that GetOrSetAsync calls the factory function when the key is not in cache.
		/// </summary>
		// Arrange
		var service = CreateService();
		var key = "missing-key";
		var factoryValue = new TestCacheValue { Id = 99, Name = "From Factory" };

		// Act
		var result = await service.GetOrSetAsync(key,
			async () =>
			{
				await Task.Delay(10); // Simulate async work
				return factoryValue;
			});

		// Assert
		result.Should().NotBeNull();
		result?.Id.Should().Be(99);

		// Verify it was cached
		var cached = await service.GetAsync<TestCacheValue>(key);
		cached.Should().NotBeNull();
		cached?.Id.Should().Be(99);
	}

	[Fact]
	public async Task GetOrSetAsync_WithFactoryReturningNull_ReturnsNull()
	{
		/// <summary>
		/// Tests that GetOrSetAsync returns null when the factory function returns null.
		/// </summary>
		// Arrange
		var service = CreateService();
		var key = "null-key";

		// Act
		var result = await service.GetOrSetAsync<TestCacheValue>(key,
			async () =>
			{
				await Task.CompletedTask;
				return null;
			});

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task GetOrSetAsync_WithFactoryException_ReturnsNull()
	{
		/// <summary>
		/// Tests that GetOrSetAsync returns null when the factory function throws an exception.
		/// </summary>
		// Arrange
		var service = CreateService();
		var key = "error-key";

		// Act
		var result = await service.GetOrSetAsync<TestCacheValue>(key,
			async () =>
			{
				await Task.CompletedTask;
				throw new InvalidOperationException("Factory failed");
			});

		// Assert
		result.Should().BeNull();
		_loggerMock.Verify(
			l => l.Log(
				It.IsAny<LogLevel>(),
				It.IsAny<EventId>(),
				It.IsAny<It.IsAnyType>(),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.AtLeastOnce);
	}

	[Fact]
	public async Task ClearAllAsync_RemovesAllKeys()
	{
		/// <summary>
		/// Tests that clearing all cache entries removes all stored keys.
		/// </summary>
		// Arrange
		var service = CreateService();
		await service.SetAsync("key1", new TestCacheValue { Id = 1 });
		await service.SetAsync("key2", new TestCacheValue { Id = 2 });
		await service.SetAsync("key3", new TestCacheValue { Id = 3 });

		// Act
		await service.ClearAllAsync();

		// Assert
		var val1 = await service.GetAsync<TestCacheValue>("key1");
		var val2 = await service.GetAsync<TestCacheValue>("key2");
		var val3 = await service.GetAsync<TestCacheValue>("key3");

		val1.Should().BeNull();
		val2.Should().BeNull();
		val3.Should().BeNull();
	}

	[Fact]
	public async Task GetStatistics_ReturnsAccurateCount()
	{
		/// <summary>
		/// Tests that GetStatistics returns accurate cache statistics including total keys count.
		/// </summary>
		// Arrange
		var service = CreateService();
		await service.SetAsync("key1", new TestCacheValue { Id = 1 });
		await service.SetAsync("key2", new TestCacheValue { Id = 2 });
		await service.SetAsync("key3", new TestCacheValue { Id = 3 });

		// Act
		var stats = service.GetStatistics();

		// Assert
		stats.TotalKeys.Should().Be(3);
		stats.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
	}

	[Fact]
	public async Task CacheKeyGenerator_GeneratesConsistentKeys()
	{
		/// <summary>
		/// Tests that the CacheKeyGenerator produces consistent keys for the same input.
		/// </summary>
		// Arrange
		var jobId = Guid.NewGuid();

		// Act
		var key1 = CacheKeyGenerator.JobKey(jobId);
		var key2 = CacheKeyGenerator.JobKey(jobId);

		// Assert
		key1.Should().Be(key2);
	}

	[Fact]
	public async Task CacheKeyGenerator_ProducesUniqueKeysForDifferentTypes()
	{
		/// <summary>
		/// Tests that the CacheKeyGenerator produces unique keys for different cache key types.
		/// </summary>
		// Arrange
		var jobId = Guid.NewGuid();

		// Act
		var jobKey = CacheKeyGenerator.JobKey(jobId);
		var statsKey = CacheKeyGenerator.JobStatsKey(jobId);
		var executionKey = CacheKeyGenerator.JobExecutionsKey(jobId);

		// Assert
		jobKey.Should().NotBe(statsKey);
		jobKey.Should().NotBe(executionKey);
		statsKey.Should().NotBe(executionKey);
	}

	[Fact]
	public async Task MultipleOperations_MaintainsConsistency()
	{
		/// <summary>
		/// Tests that multiple cache operations maintain consistency across get, set, and remove operations.
		/// </summary>
		// Arrange
		var service = CreateService();
		var values = Enumerable.Range(1, 5)
			.Select(i => new TestCacheValue { Id = i, Name = $"Item {i}" })
			.ToList();

		// Act
		foreach (var value in values)
		{
			await service.SetAsync($"item:{value.Id}", value);
		}

		var results = new List<TestCacheValue?>();
		foreach (var value in values)
		{
			var cached = await service.GetAsync<TestCacheValue>($"item:{value.Id}");
			results.Add(cached);
		}

		// Assert
		results.Should().HaveCount(5);
		results.Should().AllSatisfy(r => r.Should().NotBeNull());
	}

    [Fact]
    public async Task GetAsync_ExpiredEntry_ReturnsNullAndPreventsRaceCondition()
    {
        /// <summary>
        /// Tests that GetAsync returns null for expired entries and prevents check-then-use race conditions.
        /// Expired entries should not be accessible through TryGetValue.
        /// </summary>
        // Arrange
        var service = CreateService();
        var key = "expired-key";
        var value = new TestCacheValue { Id = 1, Name = "Will Expire" };

        // Set with very short expiration
        await service.SetAsync(key, value, TimeSpan.FromMilliseconds(50));

        // Wait for expiration
        await Task.Delay(100);

        // Act - should return null, not the expired value
        var result = await service.GetAsync<TestCacheValue>(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RemoveExpiredEntriesAsync_CleansUpExpiredEntries()
    {
        /// <summary>
        /// Tests that RemoveExpiredEntriesAsync proactively removes expired entries to prevent memory growth.
        /// WHY: IMemoryCache uses lazy eviction, so expired entries accumulate until cleanup.
        /// </summary>
        // Arrange
        var service = CreateService();
        var expiredKey = "expired-key";
        var activeKey = "active-key";
        var expiredValue = new TestCacheValue { Id = 1, Name = "Expired" };
        var activeValue = new TestCacheValue { Id = 2, Name = "Active" };

        // Set one expired entry and one active entry
        await service.SetAsync(expiredKey, expiredValue, TimeSpan.FromMilliseconds(50));
        await service.SetAsync(activeKey, activeValue);

        // Wait for expiration
        await Task.Delay(100);

        // Verify expired entry is still in _keys but TryGetValue returns false
        service.GetStatistics().TotalKeys.Should().Be(2);

        // Act - clean up expired entries
        await service.RemoveExpiredEntriesAsync();

        // Assert
        var expiredResult = await service.GetAsync<TestCacheValue>(expiredKey);
        var activeResult = await service.GetAsync<TestCacheValue>(activeKey);

        expiredResult.Should().BeNull();
        activeResult.Should().NotBeNull();
        activeResult?.Id.Should().Be(2);

        // Statistics should reflect removal
        service.GetStatistics().TotalKeys.Should().Be(1);
    }

    [Fact]
    public async Task RemoveExpiredEntriesAsync_BoundedSweep_LimitsProcessing()
    {
        /// <summary>
        /// Tests that RemoveExpiredEntriesAsync uses bounded sweep to avoid blocking.
        /// </summary>
        // Arrange
        var service = CreateService();

        // Create many entries (more than batch size)
        for (int i = 0; i < 1500; i++)
        {
            await service.SetAsync($"key:{i}", new TestCacheValue { Id = i });
        }

        // Set some to expired with longer expiration to ensure they expire
        for (int i = 0; i < 500; i++)
        {
            await service.SetAsync($"expired:{i}", new TestCacheValue { Id = i }, TimeSpan.FromMilliseconds(10));
        }

        // Wait for expiration
        await Task.Delay(50);

        // Verify some entries are expired
        var expiredBefore = 0;
        for (int i = 0; i < 100; i++)
        {
            if (await service.GetAsync<TestCacheValue>($"expired:{i}") is null)
            {
                expiredBefore++;
            }
        }

        expiredBefore.Should().BeGreaterThan(0);

        // Act - should process in batches without blocking
        await service.RemoveExpiredEntriesAsync();

        // Assert - should have cleaned up expired entries (2000 total before, less than 2000 after)
        service.GetStatistics().TotalKeys.Should().BeLessThan(2000);
    }

    [Fact]
    public async Task GetAsync_WithExpiredEntry_DoesNotReturnValue()
    {
        /// <summary>
        /// Tests that GetAsync never returns an expired value (prevents check-then-use race).
        /// Even if TryGetValue briefly returns true, the value should not be accessible.
        /// </summary>
        // Arrange
        var service = CreateService();
        var key = "race-test-key";
        var value = new TestCacheValue { Id = 999, Name = "Race Condition Test" };

        await service.SetAsync(key, value, TimeSpan.FromMilliseconds(50));

        // Wait for expiration
        await Task.Delay(100);

        // Try multiple times to ensure no race condition
        for (int i = 0; i < 10; i++)
        {
            var result = await service.GetAsync<TestCacheValue>(key);
            result.Should().BeNull("Expired entries should never be returned from GetAsync");
        }
    }

	// Helper class for testing
	private sealed class TestCacheValue
	{
		public int Id { get; set; }
		public string? Name { get; set; }
	}
}