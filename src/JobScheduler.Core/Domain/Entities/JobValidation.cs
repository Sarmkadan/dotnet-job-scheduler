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
/// Provides validation helpers for <see cref="Job"/> entities.
/// Validates all public members according to business rules and constraints.
/// </summary>
public static class JobValidation
{
    private const int MaxRetriesLimit = 100;
    private const int MaxExecutionTimeoutSeconds = 86400;

    /// <summary>
    /// Validates a <see cref="Job"/> instance and returns a list of human-readable validation problems.
    /// </summary>
    /// <param name="value">The job to validate.</param>
    /// <returns>An immutable list of validation error messages. Empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this Job value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(value.Name) || value.Name.Length > SchedulerConstants.MaxJobNameLength)
        {
            errors.Add($"Name must be non-empty and at most {SchedulerConstants.MaxJobNameLength} characters.");
        }

        if (string.IsNullOrWhiteSpace(value.CronExpression) || value.CronExpression.Length > SchedulerConstants.MaxCronExpressionLength)
        {
            errors.Add($"CronExpression must be non-empty and at most {SchedulerConstants.MaxCronExpressionLength} characters.");
        }

        if (string.IsNullOrWhiteSpace(value.HandlerType))
        {
            errors.Add("HandlerType must be non-empty.");
        }

        if (value.MaxRetries < 0 || value.MaxRetries > MaxRetriesLimit)
        {
            errors.Add($"MaxRetries must be between 0 and {MaxRetriesLimit}. Current value: {value.MaxRetries}.");
        }

        if (value.ExecutionTimeoutSeconds <= 0 || value.ExecutionTimeoutSeconds > MaxExecutionTimeoutSeconds)
        {
            errors.Add($"ExecutionTimeoutSeconds must be between 1 and {MaxExecutionTimeoutSeconds}. Current value: {value.ExecutionTimeoutSeconds}.");
        }

        if (value.MaxConcurrentExecutions <= 0 || value.MaxConcurrentExecutions > SchedulerConstants.MaxJobsPerPriority)
        {
            errors.Add($"MaxConcurrentExecutions must be between 1 and {SchedulerConstants.MaxJobsPerPriority}. Current value: {value.MaxConcurrentExecutions}.");
        }

        return errors.AsReadOnly();
    }
}
