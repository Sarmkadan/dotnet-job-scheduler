#nullable enable

using System.Text.Json;

namespace JobScheduler.Core.Services;

/// <summary>
/// Provides JSON serialization extensions for <see cref="CacheStatistics"/>.
/// </summary>
public static class CacheStatisticsJsonExtensions
{
    /// <summary>
    /// Serializes the specified cache statistics to JSON using camel-case property names.
    /// </summary>
    /// <param name="statistics">The cache statistics to serialize.</param>
    /// <param name="indented">
    /// <see langword="true"/> to format the JSON with indentation; otherwise, <see langword="false"/>.
    /// </param>
    /// <returns>A JSON string representing the cache statistics.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="statistics"/> is <see langword="null"/>.
    /// </exception>
    public static string ToJson(this CacheStatistics statistics, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(statistics);

        return JsonSerializer.Serialize(statistics, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = indented
        });
    }
}
