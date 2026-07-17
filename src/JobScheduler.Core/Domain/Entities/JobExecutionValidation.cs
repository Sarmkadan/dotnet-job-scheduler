#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using JobScheduler.Core.Constants;

namespace JobScheduler.Core.Domain.Entities;

/// <summary>
/// Provides validation helpers for <see cref="JobExecution"/> entities.
/// Validates all public members according to business rules and constraints.
/// </summary>
public static class JobExecutionValidation
{
    /// <summary>
    /// Validates a <see cref="JobExecution"/> instance and returns a list of human-readable validation problems.
    /// </summary>
    /// <param name="value">The job execution to validate.</param>
    /// <returns>An immutable list of validation error messages. Empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this JobExecution value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>(20);

        // Validate JobId
        if (value.JobId == Guid.Empty)
        {
            errors.Add("JobId must be a non-empty GUID.");
        }

        // Validate Status
        if (!Enum.IsDefined(typeof(ExecutionStatus), value.Status))
        {
            errors.Add("Status must be a valid ExecutionStatus value.");
        }

        // Validate StartedAt
        if (value.StartedAt == default)
        {
            errors.Add("StartedAt must be set to a non-default DateTime value.");
        }
        else if (value.StartedAt > DateTime.UtcNow.AddMinutes(5))
        {
            errors.Add("StartedAt cannot be in the future.");
        }

        // Validate CompletedAt
        if (value.CompletedAt.HasValue)
        {
            if (value.CompletedAt < value.StartedAt)
            {
                errors.Add("CompletedAt cannot be earlier than StartedAt.");
            }

            if (value.CompletedAt > DateTime.UtcNow.AddMinutes(5))
            {
                errors.Add("CompletedAt cannot be in the future.");
            }
        }

        // Validate DurationMilliseconds
        if (value.DurationMilliseconds < 0)
        {
            errors.Add("DurationMilliseconds cannot be negative.");
        }
        else if (value.CompletedAt.HasValue && value.DurationMilliseconds > 0)
        {
            var calculatedDuration = (long)(value.CompletedAt.Value - value.StartedAt).TotalMilliseconds;
            if (Math.Abs(value.DurationMilliseconds - calculatedDuration) > 1000)
            {
                errors.Add("DurationMilliseconds does not match the actual duration between StartedAt and CompletedAt.");
            }
        }

        // Validate AttemptNumber
        if (value.AttemptNumber < 1)
        {
            errors.Add("AttemptNumber must be at least 1.");
        }
        else if (value.AttemptNumber > 1000)
        {
            errors.Add("AttemptNumber cannot exceed 1000.");
        }

        // Validate ExecutorName
        if (string.IsNullOrWhiteSpace(value.ExecutorName))
        {
            errors.Add("ExecutorName must be a non-empty string.");
        }
        else if (value.ExecutorName.Length > 255)
        {
            errors.Add("ExecutorName cannot exceed 255 characters.");
        }

        // Validate ExecutorInstance
        if (!string.IsNullOrWhiteSpace(value.ExecutorInstance) && value.ExecutorInstance.Length > 255)
        {
            errors.Add("ExecutorInstance cannot exceed 255 characters.");
        }

        // Validate CreatedAt
        if (value.CreatedAt == default)
        {
            errors.Add("CreatedAt must be set to a non-default DateTime value.");
        }
        else if (value.CreatedAt > DateTime.UtcNow.AddMinutes(5))
        {
            errors.Add("CreatedAt cannot be in the future.");
        }
        else if (value.CreatedAt < value.StartedAt.AddMinutes(-5))
        {
            errors.Add("CreatedAt cannot be more than 5 minutes before StartedAt.");
        }

        // Validate MemoryUsageMb
        if (value.MemoryUsageMb < 0)
        {
            errors.Add("MemoryUsageMb cannot be negative.");
        }
        else if (value.MemoryUsageMb > 1_000_000)
        {
            errors.Add("MemoryUsageMb cannot exceed 1,000,000 MB (1 TB).");
        }

        // Validate CpuUsagePercent
        if (value.CpuUsagePercent is < 0 or > 100)
        {
            errors.Add("CpuUsagePercent must be between 0 and 100 inclusive.");
        }

        // Validate Status-specific constraints
        switch (value.Status)
        {
            case ExecutionStatus.Success when value.CompletedAt is null:
                errors.Add("CompletedAt must be set when Status is Success.");
                break;

            case ExecutionStatus.Failed:
                if (value.CompletedAt is null)
                {
                    errors.Add("CompletedAt must be set when Status is Failed.");
                }

                if (string.IsNullOrWhiteSpace(value.ErrorMessage))
                {
                    errors.Add("ErrorMessage must be set when Status is Failed.");
                }
                break;

            case ExecutionStatus.TimedOut when value.CompletedAt is null:
                errors.Add("CompletedAt must be set when Status is TimedOut.");
                break;

            case ExecutionStatus.Cancelled when value.CompletedAt is null:
                errors.Add("CompletedAt must be set when Status is Cancelled.");
                break;

            case ExecutionStatus.Skipped when value.CompletedAt is null:
                errors.Add("CompletedAt must be set when Status is Skipped.");
                break;

            case ExecutionStatus.Running when value.CompletedAt.HasValue:
                errors.Add("CompletedAt must not be set when Status is Running.");
                break;
        }

        // Validate Output length
        if (!string.IsNullOrWhiteSpace(value.Output) && value.Output.Length > 1_000_000)
        {
            errors.Add("Output cannot exceed 1,000,000 characters.");
        }

        // Validate ErrorMessage length
        if (!string.IsNullOrWhiteSpace(value.ErrorMessage) && value.ErrorMessage.Length > 10_000)
        {
            errors.Add("ErrorMessage cannot exceed 10,000 characters.");
        }

        // Validate StackTrace length
        if (!string.IsNullOrWhiteSpace(value.StackTrace) && value.StackTrace.Length > 100_000)
        {
            errors.Add("StackTrace cannot exceed 100,000 characters.");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="JobExecution"/> is valid.
    /// </summary>
    /// <param name="value">The job execution to check.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static bool IsValid(this JobExecution value) => value.Validate().Count == 0;

    /// <summary>
    /// Ensures that the specified <see cref="JobExecution"/> is valid, throwing an <see cref="ArgumentException"/> if not.
    /// </summary>
    /// <param name="value">The job execution to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is invalid.
    /// The exception message contains a list of all validation problems.</exception>
    public static void EnsureValid(this JobExecution value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        if (errors.Count != 0)
        {
            throw new ArgumentException(
                $"JobExecution is invalid. Validation errors: {string.Join("; ", errors)}",
                nameof(value));
        }
    }
}