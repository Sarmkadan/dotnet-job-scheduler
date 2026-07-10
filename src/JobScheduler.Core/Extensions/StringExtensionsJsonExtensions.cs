#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JobScheduler.Core.Extensions;

/// <summary>
/// Provides System.Text.Json serialization and deserialization extensions for StringExtensions.
/// Enables round-trip serialization of StringExtensions type information for persistence scenarios.
/// </summary>
public static class StringExtensionsJsonExtensions
{
    /// <summary>
    /// JSON serializer options configured for camelCase property naming.
    /// </summary>
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    /// <summary>
    /// Converts the StringExtensions type marker to a JSON string representation.
    /// Useful for persisting type information in configuration or metadata.
    /// </summary>
    /// <param name="value">The StringExtensions type marker (typically passed as typeof(StringExtensions)).</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representing the type information.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJson(this Type value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions)
            {
                WriteIndented = true,
            }
            : _jsonOptions;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a JSON string back to the StringExtensions type marker.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized Type object, or null if the JSON is null or empty.</returns>
    /// <exception cref="JsonException">Thrown when the JSON is malformed or cannot be deserialized.</exception>
    public static Type? FromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<Type>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string back to the StringExtensions type marker.
    /// Safely handles malformed JSON without throwing exceptions.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized Type object if successful; otherwise null.</param>
    /// <returns>True if deserialization succeeded; false otherwise.</returns>
    public static bool TryFromJson(string json, out Type? value)
    {
        value = null;

        if (string.IsNullOrEmpty(json))
        {
            return true;
        }

        try
        {
            value = JsonSerializer.Deserialize<Type>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}