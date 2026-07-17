#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Text.Json;

/// <summary>
/// Provides System.Text.Json serialization extensions for <see cref="DailySalesReportJobHandler"/>.
/// </summary>
public static class DailySalesReportJobHandlerJsonExtensions
{
    private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Serializes the <see cref="DailySalesReportJobHandler"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The job handler instance to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the job handler.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static string ToJson(this DailySalesReportJobHandler value, bool indented = false)
        => JsonSerializer.Serialize(
            ArgumentNullException.ThrowIfNull(value),
            indented
                ? new JsonSerializerOptions(_options) { WriteIndented = true }
                : _options);

    /// <summary>
    /// Deserializes a JSON string to a <see cref="DailySalesReportJobHandler"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized job handler instance, or <see langword="null"/> if deserialization fails.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is <see langword="null"/>.</exception>
    /// <exception cref="JsonException"><paramref name="json"/> is invalid or cannot be deserialized to <see cref="DailySalesReportJobHandler"/>.</exception>
    public static DailySalesReportJobHandler? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        return JsonSerializer.Deserialize<DailySalesReportJobHandler>(json, _options);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="DailySalesReportJobHandler"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized job handler instance if successful.</param>
    /// <returns><see langword="true"/> if deserialization succeeds; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is <see langword="null"/>.</exception>
    public static bool TryFromJson(string json, out DailySalesReportJobHandler? value)
    {
        ArgumentNullException.ThrowIfNull(json);

        try
        {
            value = JsonSerializer.Deserialize<DailySalesReportJobHandler>(json, _options);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}