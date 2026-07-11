#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Buffers;
using System.Security.Cryptography;
using System.Text;

namespace JobScheduler.Core.Extensions;

/// <summary>
/// String extension methods for common operations like hashing, encoding, and validation.
/// These utilities are used throughout the scheduler for data processing and security.
/// </summary>
public static class StringExtensions
{
    // Characters that must be escaped in JSON string values.
    private static readonly SearchValues<char> _jsonEscapeChars =
        SearchValues.Create(['\\', '"', '\n', '\r', '\t']);

    /// <summary>
    /// Computes SHA256 hash of the string.
    /// Used for secure job handler parameter hashing and fingerprinting.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="input"/> is <see langword="null"/>.</exception>
    public static string ToSha256(this string input)
    {
        ArgumentNullException.ThrowIfNull(input);

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
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="input"/> is <see langword="null"/>.</exception>
    public static string Truncate(this string input, int maxLength, bool addEllipsis = true)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxLength, 0);

        if (input.Length <= maxLength)
            return input;

        var result = input.Substring(0, maxLength);
        return addEllipsis ? result + "..." : result;
    }

    /// <summary>
    /// Converts string to slug format (lowercase with hyphens).
    /// Useful for generating URL-friendly identifiers from job names.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="input"/> is <see langword="null"/>.</exception>
    public static string ToSlug(this string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        // Single-pass over source characters — no intermediate strings, no LINQ overhead.
        // Rent a buffer from the pool for inputs that exceed safe stackalloc size.
        char[]? rented = null;
        Span<char> buffer = input.Length <= 256
            ? stackalloc char[input.Length]
            : (rented = ArrayPool<char>.Shared.Rent(input.Length));

        try
        {
            int writePos = 0;
            bool lastWasHyphen = false;

            foreach (char c in input)
            {
                char lower = char.ToLowerInvariant(c);
                char mapped = lower is ' ' or '_' ? '-' : lower;

                if (!char.IsLetterOrDigit(mapped) && mapped != '-')
                    continue;

                if (mapped == '-')
                {
                    if (lastWasHyphen || writePos == 0)
                        continue;
                    lastWasHyphen = true;
                }
                else
                {
                    lastWasHyphen = false;
                }

                buffer[writePos++] = mapped;
            }

            // Trim trailing hyphens
            while (writePos > 0 && buffer[writePos - 1] == '-')
                writePos--;

            return new string(buffer[..writePos]);
        }
        finally
        {
            if (rented is not null)
                ArrayPool<char>.Shared.Return(rented);
        }
    }

    /// <summary>
    /// Safely encodes string for JSON without manual escaping.
    /// Prevents JSON injection vulnerabilities.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="input"/> is <see langword="null"/>.</exception>
    public static string JsonEscape(this string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (string.IsNullOrEmpty(input))
            return input;

        var span = input.AsSpan();
        int firstSpecial = span.IndexOfAny(_jsonEscapeChars);

        // Fast path: no escaping required — return the original string with no allocation.
        if (firstSpecial < 0) return input;

        var sb = new StringBuilder(input.Length + 16);

        // Append clean prefix in one shot, then process special characters individually.
        if (firstSpecial > 0)
            sb.Append(span[..firstSpecial]);

        for (int i = firstSpecial; i < span.Length; i++)
        {
            switch (span[i])
            {
                case '\\': sb.Append("\\\\"); break;
                case '"': sb.Append("\\\""); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default: sb.Append(span[i]); break;
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Determines if string is a valid GUID.
    /// Used for parameter validation in job handlers.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="input"/> is <see langword="null"/>.</exception>
    public static bool IsValidGuid(this string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return Guid.TryParse(input, out _);
    }

    /// <summary>
    /// Determines if string is a valid email address.
    /// Used for notification service configuration validation.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="input"/> is <see langword="null"/>.</exception>
    public static bool IsValidEmail(this string input)
    {
        ArgumentNullException.ThrowIfNull(input);

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
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="input"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="count"/> is negative.</exception>
    public static string Repeat(this string input, int count)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        if (count == 0)
            return string.Empty;

        var sb = new StringBuilder(input.Length * count);
        for (int i = 0; i < count; i++)
            sb.Append(input);
        return sb.ToString();
    }

    /// <summary>
    /// Masks sensitive parts of string (e.g., API keys, passwords).
    /// Used in logging to prevent credential leakage.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="input"/> is <see langword="null"/>.</exception>
    public static string Mask(this string input, int revealEnd = 4)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentOutOfRangeException.ThrowIfNegative(revealEnd);

        if (input.Length <= revealEnd)
            return new string('*', Math.Max(0, input.Length));

        var reveal = input.Substring(input.Length - revealEnd);
        return new string('*', input.Length - revealEnd) + reveal;
    }

    /// <summary>
    /// Converts delimited string to list.
    /// Common for parsing CSV-like configuration values.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="input"/> is <see langword="null"/>.</exception>
    public static List<string> ToList(this string input, string delimiter = ",")
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(delimiter);

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
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="input"/> is <see langword="null"/>.</exception>
    public static bool IsAlphanumericWithUnderscore(this string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return input.All(c => char.IsLetterOrDigit(c) || c == '_');
    }
}