// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Globalization;
using System.Text.Json;

namespace JobScheduler.Core.Utilities;

/// <summary>
/// Utility class for safe parsing and type conversion operations.
/// Provides consistent error handling and default value support across the scheduler.
/// WHY: Centralizes parsing logic to prevent data type mismatch issues and inconsistent conversions.
/// </summary>
public static class ParseUtility
{
    /// <summary>
    /// Safely parses integer from string with default fallback.
    /// Returns defaultValue if parsing fails or input is null/empty.
    /// </summary>
    public static int ParseInt(string? value, int defaultValue = 0)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    /// <summary>
    /// Safely parses long from string with default fallback.
    /// </summary>
    public static long ParseLong(string? value, long defaultValue = 0)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        return long.TryParse(value, out var result) ? result : defaultValue;
    }

    /// <summary>
    /// Safely parses double from string with default fallback.
    /// Uses invariant culture to handle both decimal points and commas.
    /// </summary>
    public static double ParseDouble(string? value, double defaultValue = 0.0)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        return double.TryParse(value, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture, out var result) ? result : defaultValue;
    }

    /// <summary>
    /// Safely parses boolean from string with default fallback.
    /// Recognizes: true/false, yes/no, 1/0, on/off (case-insensitive).
    /// </summary>
    public static bool ParseBool(string? value, bool defaultValue = false)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        return value switch
        {
            "true" or "yes" or "1" or "on" => true,
            "false" or "no" or "0" or "off" => false,
            _ => defaultValue
        };
    }

    /// <summary>
    /// Safely parses DateTime from string using invariant culture.
    /// Returns defaultValue if parsing fails.
    /// </summary>
    public static DateTime ParseDateTime(string? value, DateTime? defaultValue = null)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue ?? DateTime.MinValue;

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var result))
            return result;

        return defaultValue ?? DateTime.MinValue;
    }

    /// <summary>
    /// Safely parses Guid from string.
    /// Returns Guid.Empty if parsing fails.
    /// </summary>
    public static Guid ParseGuid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Guid.Empty;

        return Guid.TryParse(value, out var result) ? result : Guid.Empty;
    }

    /// <summary>
    /// Safely parses enum from string with fallback to default value.
    /// Case-insensitive and handles numeric values.
    /// </summary>
    public static T ParseEnum<T>(string? value, T defaultValue) where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        if (Enum.TryParse<T>(value, ignoreCase: true, out var result))
            return result;

        // Try parsing by numeric value
        if (int.TryParse(value, out var numValue) && Enum.IsDefined(typeof(T), numValue))
            return (T)Enum.ToObject(typeof(T), numValue);

        return defaultValue;
    }

    /// <summary>
    /// Safely parses JSON string into typed object.
    /// Returns null if JSON is invalid or deserialization fails.
    /// </summary>
    public static T? ParseJson<T>(string? json) where T : class
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Safely parses TimeSpan from string.
    /// Supports formats: HH:MM:SS, MM:SS, and total seconds.
    /// </summary>
    public static TimeSpan ParseTimeSpan(string? value, TimeSpan? defaultValue = null)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue ?? TimeSpan.Zero;

        // Try standard TimeSpan format
        if (TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var result))
            return result;

        // Try parsing as total seconds
        if (long.TryParse(value, out var seconds))
            return TimeSpan.FromSeconds(seconds);

        return defaultValue ?? TimeSpan.Zero;
    }

    /// <summary>
    /// Parses priority string to numeric priority level (1-4).
    /// Used for job scheduling and queue ordering.
    /// </summary>
    public static int ParsePriority(string? priority)
    {
        if (string.IsNullOrWhiteSpace(priority))
            return 2; // Default: Normal

        return priority.ToLowerInvariant() switch
        {
            "critical" => 4,
            "high" => 3,
            "normal" or "medium" => 2,
            "low" => 1,
            _ => ParseInt(priority, 2)
        };
    }

    /// <summary>
    /// Formats bytes as human-readable file size (B, KB, MB, GB, TB).
    /// </summary>
    public static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:F2} {sizes[order]}";
    }

    /// <summary>
    /// Formats milliseconds as human-readable duration (ms, s, m, h).
    /// </summary>
    public static string FormatDuration(long milliseconds)
    {
        if (milliseconds < 1000)
            return $"{milliseconds}ms";

        var seconds = milliseconds / 1000;
        if (seconds < 60)
            return $"{seconds}s";

        var minutes = seconds / 60;
        if (minutes < 60)
            return $"{minutes}m {seconds % 60}s";

        var hours = minutes / 60;
        return $"{hours}h {minutes % 60}m";
    }

    /// <summary>
    /// Formats percentage value with specified decimal places.
    /// </summary>
    public static string FormatPercentage(double value, int decimals = 2)
    {
        return value.ToString("F" + decimals) + "%";
    }

    /// <summary>
    /// Parses CSV line handling quoted fields with commas.
    /// Returns list of values with quotes removed and escapes unescaped.
    /// </summary>
    public static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = string.Empty;
        var inQuotes = false;

        foreach (var c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.Trim());
                current = string.Empty;
            }
            else
            {
                current += c;
            }
        }

        result.Add(current.Trim());
        return result;
    }

    /// <summary>
    /// Escapes CSV field for safe writing.
    /// Adds quotes if field contains commas, quotes, or newlines.
    /// </summary>
    public static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
            return string.Empty;

        if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }

        return field;
    }
}
