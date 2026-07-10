#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
//
// System.Text.Json serialization extensions for AuditLogger
// =============================================================================

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace JobScheduler.Core.Services;

/// <summary>
/// Provides System.Text.Json serialization and deserialization extensions for <see cref="AuditLogger"/>.
/// WHY: Enables consistent JSON serialization/deserialization across the application
/// with proper camelCase naming and error handling.
/// </summary>
public static class AuditLoggerJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        {
            Modifiers = {
                static typeInfo =>
                {
                    if (typeInfo.Type == typeof(AuditLogger))
                    {
                        typeInfo.CreateObject = () => throw new InvalidOperationException(
                            "AuditLogger cannot be deserialized because it requires dependency injection.");
                    }
                }
            }
        }
    };

    /// <summary>
    /// Serializes the <see cref="AuditLogger"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The audit logger instance to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the audit logger.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJson(this AuditLogger value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
            : _jsonOptions;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a JSON string into an <see cref="AuditLogger"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized audit logger instance, or null if the JSON is null or whitespace.</returns>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
    public static AuditLogger? FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<AuditLogger>(json, _jsonOptions);
        }
        catch (JsonException ex)
        {
            throw new JsonException("Failed to deserialize AuditLogger from JSON.", ex);
        }
    }

    /// <summary>
    /// Attempts to deserialize a JSON string into an <see cref="AuditLogger"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized audit logger instance if successful.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    public static bool TryFromJson(string json, out AuditLogger? value)
    {
        value = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<AuditLogger>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}