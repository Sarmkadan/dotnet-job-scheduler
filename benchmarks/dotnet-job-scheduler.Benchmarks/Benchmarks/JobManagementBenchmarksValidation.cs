#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ====================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using JobScheduler.Core.Constants;
using JobScheduler.Core.Extensions;
using JobScheduler.Core.Utilities;

namespace JobScheduler.Benchmarks;

/// <summary>
/// Provides validation helpers for <see cref="JobManagementBenchmarks"/> to ensure benchmark
/// configuration values are valid before execution. Validates string outputs, enum parsing,
/// and default value constraints.
/// </summary>
public static class JobManagementBenchmarksValidation
{
    /// <summary>
    /// Validates that all benchmark method outputs are valid and non-default.
    /// Returns a list of human-readable problems found, or empty list if valid.
    /// </summary>
    /// <param name="value">The JobManagementBenchmarks instance to validate.</param>
    /// <returns>List of validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    public static IReadOnlyList<string> Validate(this JobManagementBenchmarks value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate string outputs
        ValidateString(problems, nameof(value.GenerateJobSlug_Simple), value.GenerateJobSlug_Simple());
        ValidateString(problems, nameof(value.GenerateJobSlug_Complex), value.GenerateJobSlug_Complex());
        ValidateString(problems, nameof(value.GenerateJobSlug_Long), value.GenerateJobSlug_Long());
        ValidateString(problems, nameof(value.EscapeJobDescription_Clean), value.EscapeJobDescription_Clean());
        ValidateString(problems, nameof(value.EscapeJobDescription_Special), value.EscapeJobDescription_Special());
        ValidateString(problems, nameof(value.TruncateJobDescription), value.TruncateJobDescription());
        ValidateString(problems, nameof(value.MaskHandlerType), value.MaskHandlerType());
        ValidateString(problems, nameof(value.CreateJobIdentifier), value.CreateJobIdentifier());
        ValidateString(problems, nameof(value.FormatJobStatus), value.FormatJobStatus());

        // Validate enum parsing outputs
        ValidateJobPriority(problems, nameof(value.ParseJobPriority_High), value.ParseJobPriority_High());
        ValidateJobPriority(problems, nameof(value.ParseJobPriority_Normal), value.ParseJobPriority_Normal());
        ValidateJobPriority(problems, nameof(value.ParseJobPriority_Low), value.ParseJobPriority_Low());
        ValidateJobPriority(problems, nameof(value.ParseJobPriority_Default), value.ParseJobPriority_Default());

        return problems;
    }

    /// <summary>
    /// Checks if the JobManagementBenchmarks instance is valid.
    /// </summary>
    /// <param name="value">The instance to check.</param>
    /// <returns>True if valid; false otherwise.</returns>
    public static bool IsValid(this JobManagementBenchmarks value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures the JobManagementBenchmarks instance is valid, throwing an exception
    /// with detailed validation problems if any are found.
    /// </summary>
    /// <param name="value">The instance to validate.</param>
    /// <exception cref="ArgumentException">Thrown when validation fails with problem details.</exception>
    public static void EnsureValid(this JobManagementBenchmarks value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"JobManagementBenchmarks validation failed:{Environment.NewLine}  - " +
                string.Join(Environment.NewLine + "  - ", problems));
        }
    }

    private static void ValidateString(List<string> problems, string memberName, string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            problems.Add($"{memberName} returned null or empty string");
        }
        else if (value.Length > 1000)
        {
            problems.Add($"{memberName} returned string longer than 1000 characters ({value.Length})");
        }
    }

    private static void ValidateJobPriority(List<string> problems, string memberName, JobPriority value)
    {
        // JobPriority enum values are 0-3 (Low=0, Normal=1, High=2, Critical=3)
        // Validate that the parsed value is within expected range
        if (value < JobPriority.Low || value > JobPriority.Critical)
        {
            problems.Add($"{memberName} returned out-of-range JobPriority value: {value}");
        }
    }
}
