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
/// Provides System.Text.Json serialization and deserialization extensions for string values.
/// Enables safe JSON serialization/deserialization of strings with proper escaping and validation.
/// </summary>
public static class StringExtensionsJsonExtensions
{
    /// <summary>
    /// JSON serializer options configured for camelCase property naming and safe string handling.
    /// </summary>
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    /// <summary>
    /// Serializes a string to JSON format.
    /// Useful for embedding strings in JSON documents or configuration values.
    /// </summary>
    /// <param name="value">The string value to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the input string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJson(this string value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
            : _jsonOptions;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a JSON string back to a plain string value.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized string, or null if the JSON is null or empty.</returns>
    /// <exception cref="JsonException">Thrown when the JSON is malformed or cannot be deserialized.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    public static string? FromJson(string? json)
    {
        ArgumentNullException.ThrowIfNull(json);

        return json.Length == 0
            ? null
            : JsonSerializer.Deserialize<string>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string back to a plain string value.
    /// Safely handles malformed JSON without throwing exceptions.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized string if successful; otherwise null.</param>
    /// <returns>True if deserialization succeeded; false otherwise.</returns>
    public static bool TryFromJson(string? json, out string? value)
    {
        value = null;

        if (json is null or "")
        {
            return true;
        }

        try
        {
            value = JsonSerializer.Deserialize<string>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}