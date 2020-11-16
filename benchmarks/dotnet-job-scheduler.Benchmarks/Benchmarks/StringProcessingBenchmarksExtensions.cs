#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text;

namespace JobScheduler.Benchmarks;

/// <summary>
/// Extension methods for string processing utilities commonly needed in job scheduling scenarios.
/// Provides methods for URL slug generation, JSON escaping, string truncation, and sensitive data masking.
/// </summary>
public static class StringProcessingBenchmarksExtensions
{
    /// <summary>
    /// Converts a string to a URL-friendly slug, ensuring it starts with a letter or number
    /// and contains only lowercase letters, numbers, hyphens, and underscores.
    /// </summary>
    /// <param name="input">The input string to convert.</param>
    /// <returns>A URL-friendly slug representation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> is <see langword="null"/></exception>
    public static string ToSlugUrlFriendly(this string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var sb = new StringBuilder(input.Length);
        bool previousIsHyphen = false;

        foreach (char c in input)
        {
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(char.ToLowerInvariant(c));
                previousIsHyphen = false;
            }
            else if (c is ' ' or '_' or '-')
            {
                if (!previousIsHyphen && sb.Length > 0)
                {
                    sb.Append('-');
                    previousIsHyphen = true;
                }
            }
            else if (c == '.')
            {
                sb.Append('-');
                previousIsHyphen = true;
            }
        }

        // Ensure it starts with a letter or number
        if (sb.Length == 0)
            return "job";

        if (!char.IsLetterOrDigit(sb[0]))
            sb.Insert(0, 'j');

        // Remove trailing hyphens
        while (sb.Length > 0 && sb[^1] == '-')
        {
            sb.Length--;
        }

        return sb.ToString();
    }

    /// <summary>
    /// Escapes special characters for safe inclusion in JSON strings.
    /// Handles quotes, backslashes, control characters, and ensures proper escaping.
    /// </summary>
    /// <param name="input">The input string to escape.</param>
    /// <returns>An escaped JSON string.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> is <see langword="null"/></exception>
    public static string JsonEscapeFull(this string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var sb = new StringBuilder(input.Length * 2);
        foreach (char c in input)
        {
            _ = c switch
            {
                '"' => sb.Append("\\\""),
                '\\' => sb.Append("\\\\"),
                '\b' => sb.Append("\\b"),
                '\f' => sb.Append("\\f"),
                '\n' => sb.Append("\\n"),
                '\r' => sb.Append("\\r"),
                '\t' => sb.Append("\\t"),
                _ when char.IsControl(c) => sb.AppendFormat("\\u{0:x4}", (int)c),
                _ => sb.Append(c)
            };
        }

        return sb.ToString();
    }

    /// <summary>
    /// Truncates a string to a maximum length, adding ellipsis if truncated.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <param name="maxLength">Maximum length including ellipsis.</param>
    /// <returns>The truncated string with ellipsis if needed.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxLength"/> is less than 0</exception>
    public static string TruncateWithEllipsis(this string input, int maxLength)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxLength);

        if (string.IsNullOrEmpty(input) || input.Length <= maxLength)
            return input;

        return maxLength <= 3
            ? new string('.', maxLength)
            : input[..(maxLength - 3)] + "...";
    }

    /// <summary>
    /// Masks sensitive data by showing only the first and last few characters.
    /// </summary>
    /// <param name="input">The input string to mask.</param>
    /// <param name="keepChars">Number of characters to keep at start and end.</param>
    /// <returns>A masked version of the input.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="keepChars"/> is less than 0</exception>
    public static string MaskSensitive(this string input, int keepChars = 4)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(keepChars);

        if (string.IsNullOrEmpty(input) || input.Length <= keepChars * 2)
            return new string('*', Math.Max(4, input?.Length ?? 4));

        return input[..keepChars] + new string('*', input.Length - keepChars * 2) + input[^keepChars..];
    }
}