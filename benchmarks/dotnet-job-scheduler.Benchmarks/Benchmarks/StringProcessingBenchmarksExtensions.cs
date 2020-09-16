#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text;

namespace JobScheduler.Benchmarks;

/// <summary>
/// Extension methods for <see cref="StringProcessingBenchmarks"/> that provide additional
/// string processing utilities commonly needed in job scheduling scenarios.
/// </summary>
public static class StringProcessingBenchmarksExtensions
{
    /// <summary>
    /// Converts a string to a URL-friendly slug, ensuring it starts with a letter or number
    /// and contains only lowercase letters, numbers, hyphens, and underscores.
    /// </summary>
    /// <param name="input">The input string to convert.</param>
    /// <returns>A URL-friendly slug representation.</returns>
    public static string ToSlugUrlFriendly(this string input)
    {
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
            else if (c == ' ' || c == '_' || c == '-')
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
    public static string JsonEscapeFull(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var sb = new StringBuilder(input.Length * 2);
        foreach (char c in input)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\b': sb.Append("\\b"); break;
                case '\f': sb.Append("\\f"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (char.IsControl(c))
                    {
                        sb.AppendFormat("\\u{0:x4}", (int)c);
                    }
                    else
                    {
                        sb.Append(c);
                    }
                    break;
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Truncates a string to a maximum length, adding ellipsis if truncated.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <param name="maxLength">Maximum length including ellipsis.</param>
    /// <returns>The truncated string with ellipsis if needed.</returns>
    public static string TruncateWithEllipsis(this string input, int maxLength)
    {
        if (string.IsNullOrEmpty(input) || input.Length <= maxLength)
            return input;

        if (maxLength <= 3)
            return new string('.', maxLength);

        return input[..(maxLength - 3)] + "...";
    }

    /// <summary>
    /// Masks sensitive data by showing only the first and last few characters.
    /// </summary>
    /// <param name="input">The input string to mask.</param>
    /// <param name="keepChars">Number of characters to keep at start and end.</param>
    /// <returns>A masked version of the input.</returns>
    public static string MaskSensitive(this string input, int keepChars = 4)
    {
        if (string.IsNullOrEmpty(input) || input.Length <= keepChars * 2)
            return new string('*', Math.Max(4, input?.Length ?? 4));

        return input[..keepChars] + new string('*', input.Length - keepChars * 2) + input[^keepChars..];
    }
}