#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.RegularExpressions;
using JobScheduler.Core.Constants;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Exceptions;

namespace JobScheduler.Core.Utilities;

/// <summary>
/// Centralized validation utility for job scheduler operations.
/// Ensures consistent validation logic and error messaging across the system.
/// WHY: Centralizing validation prevents inconsistent rules between different components.
/// </summary>
public static class ValidationUtility
{
    /// <summary>
    /// Validates job name format and constraints.
    /// Job names must be alphanumeric with optional underscores and hyphens.
    /// </summary>
    public static ValidationResult ValidateJobName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return new ValidationResult(false, "Job name is required");

        if (name.Length > SchedulerConstants.MaxJobNameLength)
            return new ValidationResult(false, $"Job name exceeds maximum length of {SchedulerConstants.MaxJobNameLength}");

        if (!Regex.IsMatch(name, @"^[a-zA-Z0-9_\-]+$"))
            return new ValidationResult(false, "Job name contains invalid characters. Only alphanumeric, underscores, and hyphens are allowed");

        return new ValidationResult(true);
    }

    /// <summary>
    /// Validates cron expression format.
    /// Must be a valid 5-field POSIX cron expression.
    /// </summary>
    public static ValidationResult ValidateCronExpression(string? expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return new ValidationResult(false, "Cron expression is required");

        if (expression.Length > SchedulerConstants.MaxCronExpressionLength)
            return new ValidationResult(false, $"Cron expression exceeds maximum length of {SchedulerConstants.MaxCronExpressionLength}");

        var parts = expression.Split(' ');
        if (parts.Length != 5)
            return new ValidationResult(false, "Cron expression must have exactly 5 fields (minute hour day month dayofweek)");

        // Basic validation of each field
        var fieldValidations = new[]
        {
            ValidateCronField(parts[0], 0, 59, "minute"),
            ValidateCronField(parts[1], 0, 23, "hour"),
            ValidateCronField(parts[2], 1, 31, "day"),
            ValidateCronField(parts[3], 1, 12, "month"),
            ValidateCronField(parts[4], 0, 6, "day of week")
        };

        var invalidField = fieldValidations.FirstOrDefault(v => !v.IsValid);
        if (invalidField is not null)
            return invalidField;

        return new ValidationResult(true);
    }

    /// <summary>
    /// Validates handler type format (must be valid assembly-qualified type name).
    /// </summary>
    public static ValidationResult ValidateHandlerType(string? handlerType)
    {
        if (string.IsNullOrWhiteSpace(handlerType))
            return new ValidationResult(false, "Handler type is required");

        if (handlerType.Length > 500)
            return new ValidationResult(false, "Handler type exceeds maximum length");

        // Basic format validation - should contain at least namespace and type
        if (!handlerType.Contains('.') || !handlerType.Contains(','))
            return new ValidationResult(false, "Handler type must be in format 'Namespace.Type, AssemblyName'");

        return new ValidationResult(true);
    }

    /// <summary>
    /// Validates job configuration parameters.
    /// Checks retry policy, timeout, and concurrency settings.
    /// </summary>
    public static ValidationResult ValidateJobConfiguration(Job job)
    {
        if (job.MaxRetries < 0 || job.MaxRetries > 100)
            return new ValidationResult(false, "Max retries must be between 0 and 100");

        if (job.RetryBackoffSeconds < 0 || job.RetryBackoffSeconds > 3600)
            return new ValidationResult(false, "Retry backoff must be between 0 and 3600 seconds");

        if (job.ExecutionTimeoutSeconds <= 0 || job.ExecutionTimeoutSeconds > 86400)
            return new ValidationResult(false, "Execution timeout must be between 1 and 86400 seconds");

        if (job.MaxConcurrentExecutions <= 0 || job.MaxConcurrentExecutions > 1000)
            return new ValidationResult(false, "Max concurrent executions must be between 1 and 1000");

        return new ValidationResult(true);
    }

    /// <summary>
    /// Validates JSON parameter string format.
    /// Ensures it can be parsed as valid JSON.
    /// </summary>
    public static ValidationResult ValidateJsonParameters(string? jsonParams)
    {
        if (string.IsNullOrWhiteSpace(jsonParams))
            return new ValidationResult(true); // JSON params are optional

        if (jsonParams.Length > 10000)
            return new ValidationResult(false, "Handler parameters exceed maximum length");

        try
        {
            System.Text.Json.JsonDocument.Parse(jsonParams);
            return new ValidationResult(true);
        }
        catch
        {
            return new ValidationResult(false, "Handler parameters must be valid JSON");
        }
    }

    /// <summary>
    /// Validates page number and size parameters.
    /// Ensures reasonable pagination boundaries.
    /// </summary>
    public static ValidationResult ValidatePagination(int pageNumber, int pageSize)
    {
        if (pageNumber < 1)
            return new ValidationResult(false, "Page number must be 1 or greater");

        if (pageSize < 1 || pageSize > 500)
            return new ValidationResult(false, "Page size must be between 1 and 500");

        return new ValidationResult(true);
    }

    /// <summary>
    /// Validates retry backoff strategy type.
    /// </summary>
    public static ValidationResult ValidateRetryStrategy(string? strategy)
    {
        var validStrategies = new[] { "Fixed", "Linear", "Exponential" };

        if (string.IsNullOrWhiteSpace(strategy))
            return new ValidationResult(true); // Default is acceptable

        if (!validStrategies.Contains(strategy, StringComparer.OrdinalIgnoreCase))
            return new ValidationResult(false, $"Retry strategy must be one of: {string.Join(", ", validStrategies)}");

        return new ValidationResult(true);
    }

    /// <summary>
    /// Validates a single cron field.
    /// </summary>
    private static ValidationResult ValidateCronField(string field, int min, int max, string fieldName)
    {
        if (field == "*" || field == "?")
            return new ValidationResult(true);

        // Handle ranges and lists
        var parts = field.Split(',');
        foreach (var part in parts)
        {
            if (part.Contains('-'))
            {
                var rangeParts = part.Split('-');
                if (rangeParts.Length != 2 || !int.TryParse(rangeParts[0], out var start) || !int.TryParse(rangeParts[1], out var end))
                    return new ValidationResult(false, $"Invalid range in {fieldName}");

                if (start < min || end > max || start > end)
                    return new ValidationResult(false, $"{fieldName} range must be between {min} and {max}");
            }
            else if (part.Contains('/'))
            {
                var stepParts = part.Split('/');
                if (stepParts.Length != 2)
                    return new ValidationResult(false, $"Invalid step in {fieldName}");

                if (!int.TryParse(stepParts[1], out var step) || step <= 0)
                    return new ValidationResult(false, $"Invalid step value in {fieldName}");
            }
            else
            {
                if (!int.TryParse(part, out var value) || value < min || value > max)
                    return new ValidationResult(false, $"{fieldName} must be between {min} and {max}");
            }
        }

        return new ValidationResult(true);
    }
}

public class ValidationResult
{
    public bool IsValid { get; }
    public string Message { get; }

    public ValidationResult(bool isValid, string message = "")
    {
        IsValid = isValid;
        Message = message;
    }

    public void ThrowIfInvalid()
    {
        if (!IsValid)
            throw new JobValidationException(Message);
    }
}
