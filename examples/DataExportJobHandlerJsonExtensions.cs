#nullable enable

using System;
using System.Text.Json;

/// <summary>
/// Provides JSON serialization and deserialization extensions for <see cref="DataExportJobHandler"/>.
/// </summary>
public static class DataExportJobHandlerJsonExtensions
{
    private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Serializes the <see cref="DataExportJobHandler"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The handler instance to serialize. Cannot be null.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the handler.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <remarks>This method uses the <see cref="JsonSerializerOptions"/> instance to determine whether to write the JSON with indentation.</remarks>
    public static string ToJson(this DataExportJobHandler value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_options) { WriteIndented = true }
            : _options;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a JSON string to a <see cref="DataExportJobHandler"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize. Cannot be null or empty.</param>
    /// <returns>The deserialized handler instance, or null if deserialization fails.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
    /// <remarks>This method uses the <see cref="JsonSerializerOptions"/> instance to deserialize the JSON string.</remarks>
    public static DataExportJobHandler? FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        return JsonSerializer.Deserialize<DataExportJobHandler>(json, _options);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="DataExportJobHandler"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize. Cannot be null or empty.</param>
    /// <param name="value">Receives the deserialized handler instance if successful; otherwise, null.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
    /// <remarks>This method uses the <see cref="JsonSerializerOptions"/> instance to deserialize the JSON string.</remarks>
    public static bool TryFromJson(string json, out DataExportJobHandler? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            value = JsonSerializer.Deserialize<DataExportJobHandler>(json, _options);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}
