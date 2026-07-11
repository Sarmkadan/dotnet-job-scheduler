#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using JobScheduler.Core.Constants;
using System.Globalization;

namespace JobScheduler.Core.Domain.Models;

/// <summary>
/// Provides validation helpers for <see cref="JobHistoryQuery"/> instances.
/// </summary>
public static class JobHistoryQueryValidation
{
    private const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ssZ";
    private const int MinPageNumber = 1;
    private const int MaxPageSize = 200;
    private const int DefaultPageSize = 20;

    /// <summary>
    /// Validates the specified <see cref="JobHistoryQuery"/> instance.
    /// </summary>
    /// <param name="value">The query to validate.</param>
    /// <returns>A list of human-readable validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this JobHistoryQuery? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate Status
        if (value.Status.HasValue)
        {
            var validStatuses = Enum.GetValues<ExecutionStatus>();
            if (!validStatuses.Contains(value.Status.Value))
            {
                errors.Add($"Status '{value.Status}' is invalid. Valid values are: {string.Join(", ", validStatuses)}.");
            }
        }

        // Validate From date
        if (value.From.HasValue)
        {
            if (value.From.Value.Kind != DateTimeKind.Utc)
            {
                errors.Add("From date must be in UTC timezone.");
            }
            else if (value.From.Value == default)
            {
                errors.Add("From date cannot be the default DateTime value.");
            }
        }

        // Validate To date
        if (value.To.HasValue)
        {
            if (value.To.Value.Kind != DateTimeKind.Utc)
            {
                errors.Add("To date must be in UTC timezone.");
            }
            else if (value.To.Value == default)
            {
                errors.Add("To date cannot be the default DateTime value.");
            }
        }

        // Validate date range
        if (value.From.HasValue && value.To.HasValue)
        {
            if (value.From.Value > value.To.Value)
            {
                errors.Add("From date cannot be after To date.");
            }
        }

        // Validate PageNumber
        if (value.PageNumber < MinPageNumber)
        {
            errors.Add($"PageNumber must be at least {MinPageNumber}, but was {value.PageNumber}.");
        }

        // Validate PageSize
        if (value.PageSize < 1)
        {
            errors.Add($"PageSize must be at least 1, but was {value.PageSize}.");
        }
        else if (value.PageSize > MaxPageSize)
        {
            errors.Add($"PageSize must be at most {MaxPageSize}, but was {value.PageSize}.");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="JobHistoryQuery"/> instance is valid.
    /// </summary>
    /// <param name="value">The query to check.</param>
    /// <returns><see langword="true"/> if the query is valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static bool IsValid(this JobHistoryQuery? value)
    {
        return value is not null && Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="JobHistoryQuery"/> instance is valid.
    /// </summary>
    /// <param name="value">The query to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the query is invalid, containing a list of all validation problems.</exception>
    public static void EnsureValid(this JobHistoryQuery? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = Validate(value);
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"JobHistoryQuery is invalid. Problems: {string.Join("; ", errors)}",
                nameof(value));
        }
    }
}