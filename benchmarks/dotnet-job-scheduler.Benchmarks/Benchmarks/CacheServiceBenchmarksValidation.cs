#nullable enable

using System;
using System.Collections.Generic;
using JobScheduler.Core.Services;

namespace JobScheduler.Benchmarks;

/// <summary>
/// Provides validation helpers for <see cref="CacheServiceBenchmarks"/> instances.
/// Validates that benchmark setup is correct and cache service is in expected state.
/// </summary>
public static class CacheServiceBenchmarksValidation
{
    /// <summary>
    /// Validates that a <see cref="CacheServiceBenchmarks"/> instance is properly configured.
    /// </summary>
    /// <param name="value">The benchmark instance to validate.</param>
    /// <returns>A list of validation errors; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this CacheServiceBenchmarks value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate _cacheService field using reflection since it's private
        var cacheServiceField = typeof(CacheServiceBenchmarks).GetField(
            "_cacheService",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (cacheServiceField is null)
        {
            errors.Add("CacheServiceBenchmarks._cacheService field not found.");
            return errors.AsReadOnly();
        }

        var cacheService = cacheServiceField.GetValue(value) as CacheService;

        if (cacheService is null)
        {
            errors.Add("CacheServiceBenchmarks._cacheService is null. Setup() was not called or failed.");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="CacheServiceBenchmarks"/> instance is valid.
    /// </summary>
    /// <param name="value">The benchmark instance to check.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this CacheServiceBenchmarks value) => Validate(value).Count == 0;

    /// <summary>
    /// Ensures that the specified <see cref="CacheServiceBenchmarks"/> instance is valid.
    /// </summary>
    /// <param name="value">The benchmark instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the instance is not valid, containing a list of validation errors.</exception>
    public static void EnsureValid(this CacheServiceBenchmarks value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = Validate(value);

        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"CacheServiceBenchmarks instance is not valid. {string.Join(" ", errors)}",
                nameof(value));
        }
    }
}
