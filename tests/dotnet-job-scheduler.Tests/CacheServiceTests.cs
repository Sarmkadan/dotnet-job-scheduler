#nullable enable

using FluentAssertions;
using JobScheduler.Core.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotnetJobScheduler.Tests;

public sealed class CacheServiceTests
{
    private readonly Mock<ILogger<CacheService>> _loggerMock = new();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

    private CacheService CreateService() => new(_cache, _loggerMock.Object);

    [Fact]
    public async Task GetAsync_WithExistingKey_ReturnsValue()
    {
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
        // Arrange
        var service = CreateService();

        // Act & Assert
        await service.RemoveAsync("nonexistent-key");
    }

    [Fact]
    public async Task InvalidatePatternAsync_RemovesMatchingKeys()
    {
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

    // Helper class for testing
    private sealed class TestCacheValue
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }
}
