using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace JobScheduler.Core.Services;

/// <summary>
/// Provides System.Text.Json serialization and deserialization extensions for <see cref="ExecutionStatisticsService"/>.
/// </summary>
public static class ExecutionStatisticsServiceJsonExtensions
{
    /// <summary>
    /// Attempts to deserialize a JSON string into an <see cref="ExecutionStatisticsService"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized execution statistics service, or null if deserialization fails.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };

    /// <summary>
    /// Serializes the <see cref="ExecutionStatisticsService"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The execution statistics service instance to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the execution statistics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJson(this ExecutionStatisticsService value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
            : _jsonOptions;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a JSON string into an <see cref="ExecutionStatisticsService"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized execution statistics service, or null if the JSON is null or whitespace.</returns>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
    public static ExecutionStatisticsService? FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<ExecutionStatisticsService>(json, _jsonOptions);
    }

    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    public static bool TryFromJson(string json, out ExecutionStatisticsService? value) =>
        TryFromJson(json, out value, _jsonOptions);

    /// <summary>
    /// Attempts to deserialize a JSON string into an <see cref="ExecutionStatisticsService"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized execution statistics service, or null if deserialization fails.</param>
    /// <param name="options">The JSON serialization options to use.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
    internal static bool TryFromJson(string json, out ExecutionStatisticsService? value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        value = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<ExecutionStatisticsService>(json, options);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}