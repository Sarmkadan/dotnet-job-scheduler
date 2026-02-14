// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Security.Cryptography;
using System.Text;

namespace JobScheduler.Core.Extensions;

/// <summary>
/// String extension methods for common operations like hashing, encoding, and validation.
/// These utilities are used throughout the scheduler for data processing and security.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Computes SHA256 hash of the string.
    /// Used for secure job handler parameter hashing and fingerprinting.
    /// </summary>
    public static string ToSha256(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        using (var sha256 = SHA256.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }

    /// <summary>
    /// Truncates string to specified length with optional ellipsis.
    /// Prevents UI overflow and database field size issues.
    /// </summary>
    public static string Truncate(this string input, int maxLength, bool addEllipsis = true)
    {
        if (string.IsNullOrEmpty(input) || input.Length <= maxLength)
            return input;

        var result = input.Substring(0, maxLength);
        return addEllipsis ? result + "..." : result;
    }

    /// <summary>
    /// Converts string to slug format (lowercase with hyphens).
    /// Useful for generating URL-friendly identifiers from job names.
    /// </summary>
    public static string ToSlug(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        return input
            .ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("_", "-")
            .Where(c => char.IsLetterOrDigit(c) || c == '-')
            .Aggregate(new StringBuilder(), (sb, c) =>
            {
                if (c == '-' && (sb.Length == 0 || sb[sb.Length - 1] == '-'))
                    return sb;
                return sb.Append(c);
            })
            .ToString()
            .Trim('-');
    }

    /// <summary>
    /// Safely encodes string for JSON without manual escaping.
    /// Prevents JSON injection vulnerabilities.
    /// </summary>
    public static string JsonEscape(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return input
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    /// <summary>
    /// Determines if string is a valid GUID.
    /// Used for parameter validation in job handlers.
    /// </summary>
    public static bool IsValidGuid(this string input)
    {
        return Guid.TryParse(input, out _);
    }

    /// <summary>
    /// Determines if string is a valid email address.
    /// Used for notification service configuration validation.
    /// </summary>
    public static bool IsValidEmail(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(input);
            return addr.Address == input;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Repeats a string N times.
    /// Useful for formatting and test data generation.
    /// </summary>
    public static string Repeat(this string input, int count)
    {
        if (count <= 0)
            return string.Empty;

        var sb = new StringBuilder();
        for (int i = 0; i < count; i++)
            sb.Append(input);

        return sb.ToString();
    }

    /// <summary>
    /// Masks sensitive parts of string (e.g., API keys, passwords).
    /// Used in logging to prevent credential leakage.
    /// </summary>
    public static string Mask(this string input, int revealEnd = 4)
    {
        if (string.IsNullOrEmpty(input) || input.Length <= revealEnd)
            return new string('*', Math.Max(0, input?.Length ?? 0));

        var reveal = input.Substring(input.Length - revealEnd);
        return new string('*', input.Length - revealEnd) + reveal;
    }

    /// <summary>
    /// Converts delimited string to list.
    /// Common for parsing CSV-like configuration values.
    /// </summary>
    public static List<string> ToList(this string input, string delimiter = ",")
    {
        if (string.IsNullOrEmpty(input))
            return new();

        return input
            .Split(delimiter)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();
    }

    /// <summary>
    /// Checks if string contains only alphanumeric characters and underscores.
    /// Used for validating job names and handler identifiers.
    /// </summary>
    public static bool IsAlphanumericWithUnderscore(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        return input.All(c => char.IsLetterOrDigit(c) || c == '_');
    }
}
