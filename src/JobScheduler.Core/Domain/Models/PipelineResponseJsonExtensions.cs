#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JobScheduler.Core.Domain.Models;

/// <summary>
/// Provides JSON serialization extensions for <see cref="PipelineResponse"/>.
/// </summary>
public static class PipelineResponseJsonExtensions
{
    /// <summary>
    /// Serializes the specified <see cref="PipelineResponse"/> to a JSON string using camel-case property names.
    /// </summary>
    /// <param name="response">The pipeline response to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the pipeline response.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="response"/> is null.</exception>
    public static string ToJson(this PipelineResponse response, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(response);

        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = indented,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        return JsonSerializer.Serialize(response, jsonOptions);
    }
}
