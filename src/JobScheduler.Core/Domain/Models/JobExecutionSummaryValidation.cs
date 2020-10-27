#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using JobScheduler.Core.Constants;

namespace JobScheduler.Core.Domain.Models;

/// <summary>
/// Provides validation helpers for <see cref="JobExecutionSummary"/> instances.
/// Validates business rules, data integrity, and consistency constraints.
/// </summary>
public static class JobExecutionSummaryValidation
{
    /// <summary>
    /// Validates the specified <see cref="JobExecutionSummary"/> instance.
    /// </summary>
    /// <param name="value">The job execution summary to validate.</param>
    /// <returns>A list of validation errors; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this JobExecutionSummary? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate counts consistency
        if (value.TotalExecutions < 0)
        {
            errors.Add($"TotalExecutions must be non-negative, but was {value.TotalExecutions}.");
        }

        if (value.SuccessCount < 0)
        {
            errors.Add($"SuccessCount must be non-negative, but was {value.SuccessCount}.");
        }

        if (value.FailureCount < 0)
        {
            errors.Add($"FailureCount must be non-negative, but was {value.FailureCount}.");
        }

        if (value.TimedOutCount < 0)
        {
            errors.Add($"TimedOutCount must be non-negative, but was {value.TimedOutCount}.");
        }

        if (value.CancelledCount < 0)
        {
            errors.Add($"CancelledCount must be non-negative, but was {value.CancelledCount}.");
        }

        // Validate counts don't exceed total
        if (value.TotalExecutions > 0)
        {
            var sum = value.SuccessCount + value.FailureCount + value.TimedOutCount + value.CancelledCount;
            if (sum > value.TotalExecutions)
            {
                errors.Add($"Sum of execution counts ({sum}) exceeds TotalExecutions ({value.TotalExecutions}).");
            }

            // Validate individual counts against total when total is known
            if (value.SuccessCount > value.TotalExecutions)
            {
                errors.Add($"SuccessCount ({value.SuccessCount}) cannot exceed TotalExecutions ({value.TotalExecutions}).");
            }

            if (value.FailureCount > value.TotalExecutions)
            {
                errors.Add($"FailureCount ({value.FailureCount}) cannot exceed TotalExecutions ({value.TotalExecutions}).");
            }

            if (value.TimedOutCount > value.TotalExecutions)
            {
                errors.Add($"TimedOutCount ({value.TimedOutCount}) cannot exceed TotalExecutions ({value.TotalExecutions}).");
            }

            if (value.CancelledCount > value.TotalExecutions)
            {
                errors.Add($"CancelledCount ({value.CancelledCount}) cannot exceed TotalExecutions ({value.TotalExecutions}).");
            }
        }
        else
        {
            // When TotalExecutions is 0, all counts must be 0
            if (value.SuccessCount != 0)
            {
                errors.Add($"SuccessCount must be 0 when TotalExecutions is 0, but was {value.SuccessCount}.");
            }

            if (value.FailureCount != 0)
            {
                errors.Add($"FailureCount must be 0 when TotalExecutions is 0, but was {value.FailureCount}.");
            }

            if (value.TimedOutCount != 0)
            {
                errors.Add($"TimedOutCount must be 0 when TotalExecutions is 0, but was {value.TimedOutCount}.");
            }

            if (value.CancelledCount != 0)
            {
                errors.Add($"CancelledCount must be 0 when TotalExecutions is 0, but was {value.CancelledCount}.");
            }
        }

        // Validate duration values
        if (value.AverageDurationMs < 0)
        {
            errors.Add($"AverageDurationMs must be non-negative, but was {value.AverageDurationMs}.");
        }

        if (value.MinDurationMs < 0)
        {
            errors.Add($"MinDurationMs must be non-negative, but was {value.MinDurationMs}.");
        }

        if (value.MaxDurationMs < 0)
        {
            errors.Add($"MaxDurationMs must be non-negative, but was {value.MaxDurationMs}.");
        }

        // Validate min/max duration relationship
        if (value.MinDurationMs > value.MaxDurationMs)
        {
            errors.Add($"MinDurationMs ({value.MinDurationMs}) cannot be greater than MaxDurationMs ({value.MaxDurationMs}).");
        }

        // Validate average is within min/max bounds
        if (value.AverageDurationMs > 0 && (value.MinDurationMs > 0 || value.MaxDurationMs > 0))
        {
            if (value.AverageDurationMs < value.MinDurationMs)
            {
                errors.Add($"AverageDurationMs ({value.AverageDurationMs}) cannot be less than MinDurationMs ({value.MinDurationMs}).");
            }

            if (value.AverageDurationMs > value.MaxDurationMs)
            {
                errors.Add($"AverageDurationMs ({value.AverageDurationMs}) cannot be greater than MaxDurationMs ({value.MaxDurationMs}).");
            }
        }

        // Validate LastExecutedAt when LastStatus is set
        if (value.LastStatus.HasValue && !value.LastExecutedAt.HasValue)
        {
            errors.Add("LastExecutedAt must be set when LastStatus is not null.");
        }

        // Validate LastExecutedAt is not in the future
        if (value.LastExecutedAt.HasValue && value.LastExecutedAt.Value > DateTime.UtcNow)
        {
            errors.Add($"LastExecutedAt ({value.LastExecutedAt.Value:O}) cannot be in the future.");
        }

        // Validate LastExecutedAt is not default when set
        if (value.LastExecutedAt.HasValue && value.LastExecutedAt.Value == default)
        {
            errors.Add("LastExecutedAt cannot be default(DateTime).");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="JobExecutionSummary"/> is valid.
    /// </summary>
    /// <param name="value">The job execution summary to check.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static bool IsValid(this JobExecutionSummary? value)
    {
        return value?.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="JobExecutionSummary"/> is valid.
    /// </summary>
    /// <param name="value">The job execution summary to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the summary is invalid, containing a list of validation errors.</exception>
    public static void EnsureValid(this JobExecutionSummary? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"JobExecutionSummary is invalid. Validation errors:{Environment.NewLine}- {
                    string.Join($"{Environment.NewLine}- ", errors)}");
        }
    }
}