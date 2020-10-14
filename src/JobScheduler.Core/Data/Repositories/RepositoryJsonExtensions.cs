#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JobScheduler.Core.Data.Repositories;

/// <summary>
/// Provides JSON serialization and deserialization extensions for repository types.
/// </summary>
public static class RepositoryJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };


    /// <summary>
    /// Serializes the repository instance to a JSON string.
    /// </summary>
    /// <param name="value">The repository instance to serialize</param>
    /// <param name="indented">Whether to format the JSON with indentation</param>
    /// <returns>A JSON string representation of the repository</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null</exception>
    public static string ToJson(this object value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_jsonSerializerOptions)
            {
                WriteIndented = true
            }
            : _jsonSerializerOptions;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a JSON string to a repository instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize</param>
    /// <returns>The deserialized repository instance, or null if the JSON is null or empty</returns>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized</exception>
    public static object? FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<object>(json, _jsonSerializerOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a repository instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize</param>
    /// <param name="value">The deserialized repository instance, or null if deserialization fails</param>
    /// <returns>True if deserialization succeeds; otherwise, false</returns>
    public static bool TryFromJson(string json, out object? value)
    {
        value = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            return true;
        }

        try
        {
            value = JsonSerializer.Deserialize<object>(json, _jsonSerializerOptions);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}