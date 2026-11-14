#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;

namespace JobScheduler.Core.Domain.Entities;

/// <summary>
/// Provides validation helpers for <see cref="RetryPolicy"/> entities.
/// Validates all public members according to business rules and constraints.
/// </summary>
public static class RetryPolicyValidation
{
    private const int MaxRetriesLimit = 100;
    private const double MinBackoffMultiplier = 1.0;
    private const double MaxBackoffMultiplier = 10.0;

    /// <summary>
    /// Validates a <see cref="RetryPolicy"/> instance and returns a list of human-readable validation problems.
    /// </summary>
    /// <param name="value">The retry policy to validate.</param>
    /// <returns>An immutable list of validation error messages. Empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this RetryPolicy value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        if (value.MaxRetries < 0 || value.MaxRetries > MaxRetriesLimit)
        {
            errors.Add($"MaxRetries must be between 0 and {MaxRetriesLimit}. Current value: {value.MaxRetries}.");
        }

        // WHY: Must agree with RetryPolicy.EnsureValid. A zero initial delay is legal (retry immediately);
        // rejecting it here would silently replace a registered policy with defaults at runtime.
        if (value.InitialBackoffSeconds < 0 || value.InitialBackoffSeconds > value.MaxBackoffSeconds)
        {
            errors.Add($"InitialBackoffSeconds must be >= 0 and <= MaxBackoffSeconds. Current values: Initial={value.InitialBackoffSeconds}, Max={value.MaxBackoffSeconds}.");
        }

        if (value.BackoffMultiplier < MinBackoffMultiplier || value.BackoffMultiplier > MaxBackoffMultiplier)
        {
            errors.Add($"BackoffMultiplier must be between {MinBackoffMultiplier} and {MaxBackoffMultiplier}. Current value: {value.BackoffMultiplier}.");
        }

        return errors.AsReadOnly();
    }
}
