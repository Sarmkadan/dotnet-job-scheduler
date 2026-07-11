#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;

namespace JobScheduler.Core.Controllers;

/// <summary>
/// Provides validation helpers for HealthController and related response types.
/// Ensures health check endpoints return valid, meaningful data.
/// </summary>
public static class HealthControllerValidation
{
    /// <summary>
    /// Validates a HealthController instance and returns any validation problems.
    /// </summary>
    /// <param name="value">The HealthController instance to validate.</param>
    /// <returns>An empty list if valid, otherwise a list of human-readable validation errors.</returns>
    /// <exception cref="ArgumentNullException">Thrown if value is null.</exception>
    public static IReadOnlyList<string> Validate(this HealthController value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // HealthController has no public properties to validate
        // Constructor parameters are validated in constructor

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether a HealthController instance is valid.
    /// </summary>
    /// <param name="value">The HealthController instance to check.</param>
    /// <returns>True if valid; otherwise false.</returns>
    public static bool IsValid(this HealthController value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures a HealthController instance is valid, throwing an exception if not.
    /// </summary>
    /// <param name="value">The HealthController instance to validate.</param>
    /// <exception cref="ArgumentException">Thrown if value is null or invalid.</exception>
    public static void EnsureValid(this HealthController value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"HealthController is invalid:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
        }
    }

    /// <summary>
    /// Validates a HealthStatusResponse instance and returns any validation problems.
    /// </summary>
    /// <param name="value">The HealthStatusResponse instance to validate.</param>
    /// <returns>An empty list if valid, otherwise a list of human-readable validation errors.</returns>
    /// <exception cref="ArgumentNullException">Thrown if value is null.</exception>
    public static IReadOnlyList<string> Validate(this HealthStatusResponse value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        if (value.Timestamp == default)
        {
            errors.Add("Timestamp must be set to a non-default DateTime value.");
        }

        if (string.IsNullOrWhiteSpace(value.Version))
        {
            errors.Add("Version must be a non-empty string.");
        }
        else if (value.Version.Length > 50)
        {
            errors.Add("Version must be 50 characters or less.");
        }

        if (string.IsNullOrWhiteSpace(value.Status))
        {
            errors.Add("Status must be a non-empty string.");
        }
        else if (value.Status.Length > 20)
        {
            errors.Add("Status must be 20 characters or less.");
        }
        else if (!IsValidStatus(value.Status))
        {
            errors.Add("Status must be one of: OK, Degraded, Error.");
        }

        errors.AddRange(value.Database.Validate());
        errors.AddRange(value.Jobs.Validate());
        errors.AddRange(value.Executions.Validate());
        errors.AddRange(value.Memory.Validate());

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether a HealthStatusResponse instance is valid.
    /// </summary>
    /// <param name="value">The HealthStatusResponse instance to check.</param>
    /// <returns>True if valid; otherwise false.</returns>
    public static bool IsValid(this HealthStatusResponse value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures a HealthStatusResponse instance is valid, throwing an exception if not.
    /// </summary>
    /// <param name="value">The HealthStatusResponse instance to validate.</param>
    /// <exception cref="ArgumentException">Thrown if value is null or invalid.</exception>
    public static void EnsureValid(this HealthStatusResponse value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"HealthStatusResponse is invalid:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
        }
    }

    /// <summary>
    /// Validates a DatabaseStatus instance and returns any validation problems.
    /// </summary>
    /// <param name="value">The DatabaseStatus instance to validate.</param>
    /// <returns>An empty list if valid, otherwise a list of human-readable validation errors.</returns>
    /// <exception cref="ArgumentNullException">Thrown if value is null.</exception>
    public static IReadOnlyList<string> Validate(this DatabaseStatus value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // LastChecked should be set when Available is true, but can be default when Available is false
        if (value.Available && value.LastChecked == default)
        {
            errors.Add("LastChecked must be set when Database is Available.");
        }

        if (value.Available && !string.IsNullOrEmpty(value.ErrorMessage))
        {
            errors.Add("ErrorMessage should be empty when Database is Available.");
        }

        if (!value.Available && string.IsNullOrEmpty(value.ErrorMessage))
        {
            errors.Add("ErrorMessage must be set when Database is not Available.");
        }

        if (!string.IsNullOrEmpty(value.ErrorMessage) && value.ErrorMessage.Length > 500)
        {
            errors.Add("ErrorMessage must be 500 characters or less.");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether a DatabaseStatus instance is valid.
    /// </summary>
    /// <param name="value">The DatabaseStatus instance to check.</param>
    /// <returns>True if valid; otherwise false.</returns>
    public static bool IsValid(this DatabaseStatus value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures a DatabaseStatus instance is valid, throwing an exception if not.
    /// </summary>
    /// <param name="value">The DatabaseStatus instance to validate.</param>
    /// <exception cref="ArgumentException">Thrown if value is null or invalid.</exception>
    public static void EnsureValid(this DatabaseStatus value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"DatabaseStatus is invalid:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
        }
    }

    /// <summary>
    /// Validates a JobsStatus instance and returns any validation problems.
    /// </summary>
    /// <param name="value">The JobsStatus instance to validate.</param>
    /// <returns>An empty list if valid, otherwise a list of human-readable validation errors.</returns>
    /// <exception cref="ArgumentNullException">Thrown if value is null.</exception>
    public static IReadOnlyList<string> Validate(this JobsStatus value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        if (value.TotalCount < 0)
        {
            errors.Add("TotalCount must be a non-negative integer.");
        }

        if (value.ActiveCount < 0)
        {
            errors.Add("ActiveCount must be a non-negative integer.");
        }

        if (value.ActiveCount > value.TotalCount)
        {
            errors.Add("ActiveCount cannot exceed TotalCount.");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether a JobsStatus instance is valid.
    /// </summary>
    /// <param name="value">The JobsStatus instance to check.</param>
    /// <returns>True if valid; otherwise false.</returns>
    public static bool IsValid(this JobsStatus value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures a JobsStatus instance is valid, throwing an exception if not.
    /// </summary>
    /// <param name="value">The JobsStatus instance to validate.</param>
    /// <exception cref="ArgumentException">Thrown if value is null or invalid.</exception>
    public static void EnsureValid(this JobsStatus value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"JobsStatus is invalid:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
        }
    }

    /// <summary>
    /// Validates an ExecutionsStatus instance and returns any validation problems.
    /// </summary>
    /// <param name="value">The ExecutionsStatus instance to validate.</param>
    /// <returns>An empty list if valid, otherwise a list of human-readable validation errors.</returns>
    /// <exception cref="ArgumentNullException">Thrown if value is null.</exception>
    public static IReadOnlyList<string> Validate(this ExecutionsStatus value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        if (value.TotalCount < 0)
        {
            errors.Add("TotalCount must be a non-negative integer.");
        }

        if (value.SuccessRate < 0 || value.SuccessRate > 100)
        {
            errors.Add("SuccessRate must be between 0 and 100 (inclusive).");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether an ExecutionsStatus instance is valid.
    /// </summary>
    /// <param name="value">The ExecutionsStatus instance to check.</param>
    /// <returns>True if valid; otherwise false.</returns>
    public static bool IsValid(this ExecutionsStatus value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures an ExecutionsStatus instance is valid, throwing an exception if not.
    /// </summary>
    /// <param name="value">The ExecutionsStatus instance to validate.</param>
    /// <exception cref="ArgumentException">Thrown if value is null or invalid.</exception>
    public static void EnsureValid(this ExecutionsStatus value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"ExecutionsStatus is invalid:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
        }
    }

    /// <summary>
    /// Validates a MemoryStatus instance and returns any validation problems.
    /// </summary>
    /// <param name="value">The MemoryStatus instance to validate.</param>
    /// <returns>An empty list if valid, otherwise a list of human-readable validation errors.</returns>
    /// <exception cref="ArgumentNullException">Thrown if value is null.</exception>
    public static IReadOnlyList<string> Validate(this MemoryStatus value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        if (value.UsageMb < 0)
        {
            errors.Add("UsageMb must be a non-negative value.");
        }

        if (value.Threshold <= 0)
        {
            errors.Add("Threshold must be a positive value.");
        }

        if (value.UsageMb > value.Threshold)
        {
            errors.Add("UsageMb exceeds the configured Threshold.");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether a MemoryStatus instance is valid.
    /// </summary>
    /// <param name="value">The MemoryStatus instance to check.</param>
    /// <returns>True if valid; otherwise false.</returns>
    public static bool IsValid(this MemoryStatus value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures a MemoryStatus instance is valid, throwing an exception if not.
    /// </summary>
    /// <param name="value">The MemoryStatus instance to validate.</param>
    /// <exception cref="ArgumentException">Thrown if value is null or invalid.</exception>
    public static void EnsureValid(this MemoryStatus value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"MemoryStatus is invalid:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
        }
    }

    /// <summary>
    /// Validates a DiagnosticsResponse instance and returns any validation problems.
    /// </summary>
    /// <param name="value">The DiagnosticsResponse instance to validate.</param>
    /// <returns>An empty list if valid, otherwise a list of human-readable validation errors.</returns>
    /// <exception cref="ArgumentNullException">Thrown if value is null.</exception>
    public static IReadOnlyList<string> Validate(this DiagnosticsResponse value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        if (value.Timestamp == default)
        {
            errors.Add("Timestamp must be set to a non-default DateTime value.");
        }

        if (string.IsNullOrWhiteSpace(value.MachineName))
        {
            errors.Add("MachineName must be a non-empty string.");
        }
        else if (value.MachineName.Length > 100)
        {
            errors.Add("MachineName must be 100 characters or less.");
        }

        if (value.ProcessorCount <= 0)
        {
            errors.Add("ProcessorCount must be a positive integer.");
        }

        if (string.IsNullOrWhiteSpace(value.RuntimeVersion))
        {
            errors.Add("RuntimeVersion must be a non-empty string.");
        }
        else if (value.RuntimeVersion.Length > 100)
        {
            errors.Add("RuntimeVersion must be 100 characters or less.");
        }

        errors.AddRange(value.Memory.Validate());
        errors.AddRange(value.SystemStatistics.Validate());

        if (value.RecentErrors == null)
        {
            errors.Add("RecentErrors collection must be initialized.");
        }
        else if (value.RecentErrors.Count > 1000)
        {
            errors.Add("RecentErrors collection must contain 1000 items or less.");
        }
        else
        {
            for (int i = 0; i < value.RecentErrors.Count; i++)
            {
                var error = value.RecentErrors[i];
                if (error == null)
                {
                    errors.Add($"RecentErrors[{i}] must not be null.");
                }
                else
                {
                    errors.AddRange(error.Validate());
                }
            }
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether a DiagnosticsResponse instance is valid.
    /// </summary>
    /// <param name="value">The DiagnosticsResponse instance to check.</param>
    /// <returns>True if valid; otherwise false.</returns>
    public static bool IsValid(this DiagnosticsResponse value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures a DiagnosticsResponse instance is valid, throwing an exception if not.
    /// </summary>
    /// <param name="value">The DiagnosticsResponse instance to validate.</param>
    /// <exception cref="ArgumentException">Thrown if value is null or invalid.</exception>
    public static void EnsureValid(this DiagnosticsResponse value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"DiagnosticsResponse is invalid:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
        }
    }

    /// <summary>
    /// Validates a MemoryDiagnostics instance and returns any validation problems.
    /// </summary>
    /// <param name="value">The MemoryDiagnostics instance to validate.</param>
    /// <returns>An empty list if valid, otherwise a list of human-readable validation errors.</returns>
    /// <exception cref="ArgumentNullException">Thrown if value is null.</exception>
    public static IReadOnlyList<string> Validate(this MemoryDiagnostics value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        if (value.TotalMemoryMb < 0)
        {
            errors.Add("TotalMemoryMb must be a non-negative value.");
        }

        if (value.ManagedHeapSizeMb < 0)
        {
            errors.Add("ManagedHeapSizeMb must be a non-negative value.");
        }

        if (value.Gen0Collections < 0)
        {
            errors.Add("Gen0Collections must be a non-negative integer.");
        }

        if (value.Gen1Collections < 0)
        {
            errors.Add("Gen1Collections must be a non-negative integer.");
        }

        if (value.Gen2Collections < 0)
        {
            errors.Add("Gen2Collections must be a non-negative integer.");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether a MemoryDiagnostics instance is valid.
    /// </summary>
    /// <param name="value">The MemoryDiagnostics instance to check.</param>
    /// <returns>True if valid; otherwise false.</returns>
    public static bool IsValid(this MemoryDiagnostics value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures a MemoryDiagnostics instance is valid, throwing an exception if not.
    /// </summary>
    /// <param name="value">The MemoryDiagnostics instance to validate.</param>
    /// <exception cref="ArgumentException">Thrown if value is null or invalid.</exception>
    public static void EnsureValid(this MemoryDiagnostics value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"MemoryDiagnostics is invalid:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
        }
    }

    /// <summary>
    /// Validates a SystemDiagnostics instance and returns any validation problems.
    /// </summary>
    /// <param name="value">The SystemDiagnostics instance to validate.</param>
    /// <returns>An empty list if valid, otherwise a list of human-readable validation errors.</returns>
    /// <exception cref="ArgumentNullException">Thrown if value is null.</exception>
    public static IReadOnlyList<string> Validate(this SystemDiagnostics value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        if (value.TotalJobs < 0)
        {
            errors.Add("TotalJobs must be a non-negative integer.");
        }

        if (value.ActiveJobs < 0)
        {
            errors.Add("ActiveJobs must be a non-negative integer.");
        }

        if (value.TotalExecutions < 0)
        {
            errors.Add("TotalExecutions must be a non-negative integer.");
        }

        if (value.AverageSuccessRate < 0 || value.AverageSuccessRate > 100)
        {
            errors.Add("AverageSuccessRate must be between 0 and 100 (inclusive).");
        }

        if (value.AverageExecutionTimeMs < 0)
        {
            errors.Add("AverageExecutionTimeMs must be a non-negative value.");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether a SystemDiagnostics instance is valid.
    /// </summary>
    /// <param name="value">The SystemDiagnostics instance to check.</param>
    /// <returns>True if valid; otherwise false.</returns>
    public static bool IsValid(this SystemDiagnostics value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures a SystemDiagnostics instance is valid, throwing an exception if not.
    /// </summary>
    /// <param name="value">The SystemDiagnostics instance to validate.</param>
    /// <exception cref="ArgumentException">Thrown if value is null or invalid.</exception>
    public static void EnsureValid(this SystemDiagnostics value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"SystemDiagnostics is invalid:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
        }
    }

    /// <summary>
    /// Validates an ErrorLogEntry instance and returns any validation problems.
    /// </summary>
    /// <param name="value">The ErrorLogEntry instance to validate.</param>
    /// <returns>An empty list if valid, otherwise a list of human-readable validation errors.</returns>
    /// <exception cref="ArgumentNullException">Thrown if value is null.</exception>
    public static IReadOnlyList<string> Validate(this ErrorLogEntry value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(value.Message))
        {
            errors.Add("Message must be a non-empty string.");
        }
        else if (value.Message.Length > 1000)
        {
            errors.Add("Message must be 1000 characters or less.");
        }

        if (value.Count <= 0)
        {
            errors.Add("Count must be a positive integer.");
        }

        if (value.LastOccurred.HasValue && value.LastOccurred.Value > DateTime.UtcNow.AddHours(1))
        {
            errors.Add("LastOccurred cannot be in the future.");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether an ErrorLogEntry instance is valid.
    /// </summary>
    /// <param name="value">The ErrorLogEntry instance to check.</param>
    /// <returns>True if valid; otherwise false.</returns>
    public static bool IsValid(this ErrorLogEntry value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures an ErrorLogEntry instance is valid, throwing an exception if not.
    /// </summary>
    /// <param name="value">The ErrorLogEntry instance to validate.</param>
    /// <exception cref="ArgumentException">Thrown if value is null or invalid.</exception>
    public static void EnsureValid(this ErrorLogEntry value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"ErrorLogEntry is invalid:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
        }
    }

    private static bool IsValidStatus(string status)
    {
        return string.Equals(status, "OK", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(status, "Degraded", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(status, "Error", StringComparison.OrdinalIgnoreCase);
    }
}