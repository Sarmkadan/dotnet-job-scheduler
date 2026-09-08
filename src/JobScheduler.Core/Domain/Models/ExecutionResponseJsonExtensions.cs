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
/// Provides JSON serialization and deserialization extensions for <see cref="ExecutionResponse"/>.
/// </summary>
public static class ExecutionResponseJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Serializes the <see cref="ExecutionResponse"/> to a JSON string.
    /// </summary>
    /// <param name="response">The execution response to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the execution response.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="response"/> is null.</exception>
    public static string ToJson(this ExecutionResponse response, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(response);

        return JsonSerializer.Serialize(response, indented ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true } : _jsonOptions);
    }
}