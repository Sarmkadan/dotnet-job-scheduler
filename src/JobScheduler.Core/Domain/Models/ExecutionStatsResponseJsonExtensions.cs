#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JobScheduler.Core.Domain.Models;

/// <summary>
/// Provides JSON serialization and deserialization extensions for <see cref="ExecutionStatsResponse"/>.
/// </summary>
public static class ExecutionStatsResponseJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Serializes the <see cref="ExecutionStatsResponse"/> to a JSON string.
    /// </summary>
    /// <param name="value">The execution stats response to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the execution stats response.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJson(this ExecutionStatsResponse value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        return JsonSerializer.Serialize(value, indented ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true } : _jsonOptions);
    }

    /// <summary>
    /// Deserializes a JSON string to an <see cref="ExecutionStatsResponse"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>An <see cref="ExecutionStatsResponse"/> instance, or null if the JSON is empty or whitespace.</returns>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
    public static ExecutionStatsResponse? FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<ExecutionStatsResponse>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to an <see cref="ExecutionStatsResponse"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized instance if successful.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    public static bool TryFromJson(string json, out ExecutionStatsResponse? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            value = JsonSerializer.Deserialize<ExecutionStatsResponse>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}