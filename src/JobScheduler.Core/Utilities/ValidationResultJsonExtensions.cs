#nullable enable

using System.Text.Json;

namespace JobScheduler.Core.Utilities;

/// <summary>
/// Provides JSON serialization extensions for <see cref="ValidationResult"/> instances.
/// </summary>
public static class ValidationResultJsonExtensions
{
    /// <summary>
    /// Serializes a validation result to JSON using camel-case property names.
    /// </summary>
    /// <param name="result">The validation result to serialize.</param>
    /// <param name="indented">
    /// <see langword="true"/> to format the JSON with indentation; otherwise, <see langword="false"/>.
    /// </param>
    /// <returns>A JSON string representing the validation result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> is <see langword="null"/>.</exception>
    public static string ToJson(this ValidationResult result, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(result);

        return JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = indented
        });
    }
}
