using System.Text.Json;

namespace JobScheduler.Core.Services;

/// <summary>
/// Provides JSON serialization extensions for <see cref="RetryStatistics"/>.
/// </summary>
public static class RetryStatisticsJsonExtensions
{
    /// <summary>
    /// Serializes the specified retry statistics to a JSON string using camel-case property names.
    /// </summary>
    /// <param name="statistics">The retry statistics to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the retry statistics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="statistics"/> is null.</exception>
    public static string ToJson(this RetryStatistics statistics, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(statistics);

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = indented
        };

        return JsonSerializer.Serialize(statistics, options);
    }
}
