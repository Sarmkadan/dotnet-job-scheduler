#nullable enable

using System;
using System.Text.Json;

namespace JobScheduler.Core.Domain.Models;

/// <summary>
/// Provides JSON serialization extensions for
/// <see cref="JobScheduler.Core.Domain.Models.CleanupResponse"/>.
/// </summary>
public static class CleanupResponseJsonExtensions
{
    /// <summary>
    /// Serializes a cleanup response to a JSON string using camel-case property names.
    /// </summary>
    /// <param name="response">The cleanup response to serialize.</param>
    /// <param name="indented">
    /// <see langword="true"/> to format the JSON with indentation; otherwise, <see langword="false"/>.
    /// </param>
    /// <returns>A JSON string representation of <paramref name="response"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="response"/> is <see langword="null"/>.
    /// </exception>
    public static string ToJson(
        this JobScheduler.Core.Domain.Models.CleanupResponse response,
        bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(response);

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = indented
        };

        return JsonSerializer.Serialize(response, options);
    }
}
