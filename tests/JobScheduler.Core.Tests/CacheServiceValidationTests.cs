using System;
using System.Collections.Generic;
using System.Linq;
using JobScheduler.Core.Services;
using Xunit;

namespace JobScheduler.Core.Tests;

public sealed class CacheServiceValidationTests
{
    [Fact]
    public void ValidateKey_ValidKey_ReturnsEmptyList()
    {
        var key = "validKey123";
        var problems = CacheServiceValidation.ValidateKey(key);
        Assert.Empty(problems);
    }

    [Fact]
    public void ValidateKey_InvalidKey_ReturnsProblems()
    {
        var key = "invalid key with spaces";
        var problems = CacheServiceValidation.ValidateKey(key);
        Assert.NotEmpty(problems);
        Assert.Contains("Cache key cannot contain whitespace characters.", problems);
    }

    [Fact]
    public void ValidateKey_NullOrEmpty_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CacheServiceValidation.ValidateKey(null));
        Assert.Throws<ArgumentException>(() => CacheServiceValidation.ValidateKey(string.Empty));
    }

    [Fact]
    public void ValidateKeyPattern_ValidPattern_ReturnsEmptyList()
    {
        var pattern = "prefix_*";
        var problems = CacheServiceValidation.ValidateKeyPattern(pattern);
        Assert.Empty(problems);
    }

    [Fact]
    public void ValidateKeyPattern_InvalidPattern_ReturnsProblems()
    {
        var pattern = "noWildcards";
        var problems = CacheServiceValidation.ValidateKeyPattern(pattern);
        Assert.NotEmpty(problems);
        Assert.Contains("Cache key pattern should contain wildcards (* or ?) for pattern matching.", problems);
    }

    [Fact]
    public void ValidateKeyPattern_NullOrEmpty_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CacheServiceValidation.ValidateKeyPattern(null));
        Assert.Throws<ArgumentException>(() => CacheServiceValidation.ValidateKeyPattern(string.Empty));
    }

    [Fact]
    public void ValidateCacheStatistics_Valid_ReturnsEmptyList()
    {
        var stats = new CacheStatistics
        {
            TotalKeys = 42,
            Timestamp = DateTime.UtcNow
        };
        var problems = CacheServiceValidation.Validate(stats);
        Assert.Empty(problems);
    }

    [Fact]
    public void ValidateCacheStatistics_Invalid_ReturnsProblems()
    {
        var stats = new CacheStatistics
        {
            TotalKeys = -1,
            Timestamp = DateTime.UtcNow.AddMinutes(10) // future
        };
        var problems = CacheServiceValidation.Validate(stats);
        Assert.NotEmpty(problems);
        Assert.Contains("TotalKeys cannot be negative.", problems);
        Assert.Contains("Timestamp cannot be in the future.", problems);
    }

    [Fact]
    public void IsValidKey_Valid_ReturnsTrue()
    {
        Assert.True(CacheServiceValidation.IsValidKey("validKey"));
    }

    [Fact]
    public void IsValidKey_Invalid_ReturnsFalse()
    {
        Assert.False(CacheServiceValidation.IsValidKey("invalid key"));
    }

    [Fact]
    public void IsValidKeyPattern_Valid_ReturnsTrue()
    {
        Assert.True(CacheServiceValidation.IsValidKeyPattern("prefix_*"));
    }

    [Fact]
    public void IsValidKeyPattern_Invalid_ReturnsFalse()
    {
        Assert.False(CacheServiceValidation.IsValidKeyPattern("noWildcards"));
    }

    [Fact]
    public void EnsureValidKey_Valid_NoException()
    {
        var key = "validKey";
        CacheServiceValidation.EnsureValidKey(key); // should not throw
    }

    [Fact]
    public void EnsureValidKey_Invalid_ThrowsArgumentException()
    {
        var key = "invalid key";
        Assert.Throws<ArgumentException>(() => CacheServiceValidation.EnsureValidKey(key));
    }

    [Fact]
    public void EnsureValidKeyPattern_Valid_NoException()
    {
        var pattern = "prefix_*";
        CacheServiceValidation.EnsureValidKeyPattern(pattern); // should not throw
    }

    [Fact]
    public void EnsureValidKeyPattern_Invalid_ThrowsArgumentException()
    {
        var pattern = "noWildcards";
        Assert.Throws<ArgumentException>(() => CacheServiceValidation.EnsureValidKeyPattern(pattern));
    }

    [Fact]
    public void EnsureValidCacheStatistics_Valid_NoException()
    {
        var stats = new CacheStatistics
        {
            TotalKeys = 10,
            Timestamp = DateTime.UtcNow
        };
        CacheServiceValidation.EnsureValid(stats); // should not throw
    }

    [Fact]
    public void EnsureValidCacheStatistics_Invalid_ThrowsArgumentException()
    {
        var stats = new CacheStatistics
        {
            TotalKeys = -5,
            Timestamp = DateTime.UtcNow.AddMinutes(10)
        };
        Assert.Throws<ArgumentException>(() => CacheServiceValidation.EnsureValid(stats));
    }
}
