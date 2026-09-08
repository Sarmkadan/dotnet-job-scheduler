#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Text.Json;

namespace JobScheduler.Core.Exceptions;

/// <summary>
/// Provides JSON serialization extensions for <see cref="CronExpressionException"/>.
/// </summary>
public static class CronExpressionExceptionJsonExtensions
{
    /// <summary>
    /// Serializes the specified <see cref="CronExpressionException"/> to a JSON string.
    /// </summary>
    /// <param name="exception">The exception to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string containing the exception type, message, cron expression, error code, and inner exception message.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is <see langword="null"/>.</exception>
    public static string ToJson(this CronExpressionException exception, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return JsonSerializer.Serialize(
            new
            {
                type = exception.GetType().Name,
                message = exception.Message,
                cronExpression = exception.CronExpression,
                errorCode = exception.ErrorCode,
                innerMessage = exception.InnerException?.Message
            },
            new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = indented });
    }
}
