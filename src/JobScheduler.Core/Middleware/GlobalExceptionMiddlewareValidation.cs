#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;

namespace JobScheduler.Core.Middleware;

/// <summary>
/// Provides validation helpers for GlobalExceptionMiddleware and related error handling components.
/// WHY: Centralized validation ensures consistent error response quality and prevents runtime failures.
/// </summary>
public static class GlobalExceptionMiddlewareValidation
{
    /// <summary>
    /// Validates an ErrorResponse object for completeness and correctness.
    /// </summary>
    /// <param name="value">The ErrorResponse to validate</param>
    /// <returns>A list of validation problems (empty if valid)</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null</exception>
    public static IReadOnlyList<string> Validate(this ErrorResponse value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate Message (required)
        if (string.IsNullOrWhiteSpace(value.Message))
        {
            problems.Add($"Message must be a non-empty string. Current value: '{value.Message}'");
        }

        // Validate Timestamp (must be a valid date, not default)
        if (value.Timestamp == default)
        {
            problems.Add("Timestamp must be set to a valid DateTime (cannot be default/DateTime.MinValue)");
        }
        else if (value.Timestamp.Kind != DateTimeKind.Utc)
        {
            problems.Add("Timestamp must be in UTC timezone");
        }
        else if (value.Timestamp < DateTime.UtcNow.AddYears(-1))
        {
            problems.Add("Timestamp appears to be too old (more than 1 year in the past)");
        }
        else if (value.Timestamp > DateTime.UtcNow.AddMinutes(5))
        {
            problems.Add("Timestamp appears to be in the future (more than 5 minutes ahead)");
        }

        // Validate ExceptionType (optional but should be valid if present)
        if (value.ExceptionType is not null && string.IsNullOrWhiteSpace(value.ExceptionType))
        {
            problems.Add("ExceptionType must be null or a non-empty string");
        }

        // Validate StackTrace (optional but should be valid if present)
        if (value.StackTrace is not null && string.IsNullOrWhiteSpace(value.StackTrace))
        {
            problems.Add("StackTrace must be null or a non-empty string");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether an ErrorResponse is valid.
    /// </summary>
    /// <param name="value">The ErrorResponse to check</param>
    /// <returns>True if valid; otherwise false</returns>
    public static bool IsValid(this ErrorResponse value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that an ErrorResponse is valid, throwing an exception if it is not.
    /// </summary>
    /// <param name="value">The ErrorResponse to validate</param>
    /// <exception cref="ArgumentException">Thrown when value is invalid, with a list of problems</exception>
    public static void EnsureValid(this ErrorResponse value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();

        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"ErrorResponse is invalid. Problems: {string.Join("; ", problems)}");
        }
    }
}