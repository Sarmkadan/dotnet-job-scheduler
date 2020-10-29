#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using JobScheduler.Core.Constants;
using JobScheduler.Core.Domain.Entities;

namespace JobScheduler.Core.Data.Repositories;

/// <summary>
/// Provides validation helpers for <see cref="ExecutionRepository"/> to ensure repository
/// instances and their method parameters are valid before operations.
/// </summary>
public static class ExecutionRepositoryValidation
{
    /// <summary>
    /// Validates an ExecutionRepository instance and returns any problems found.
    /// </summary>
    /// <param name="value">The repository instance to validate</param>
    /// <returns>List of validation problems; empty if valid</returns>
    /// <exception cref="ArgumentNullException">Thrown if value is null</exception>
    public static IReadOnlyList<string> Validate(this ExecutionRepository? value)
    {
        var problems = new List<string>();

        if (value is null)
        {
            problems.Add("ExecutionRepository instance cannot be null.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified ExecutionRepository instance is valid.
    /// </summary>
    /// <param name="value">The repository instance to check</param>
    /// <returns>True if valid; false otherwise</returns>
    public static bool IsValid(this ExecutionRepository? value)
    {
        return value is not null && value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that the specified ExecutionRepository instance is valid.
    /// </summary>
    /// <param name="value">The repository instance to validate</param>
    /// <exception cref="ArgumentException">Thrown if value is null or invalid, containing the list of problems</exception>
    public static void EnsureValid(this ExecutionRepository? value)
    {
        if (value is null)
        {
            throw new ArgumentException("ExecutionRepository instance cannot be null.");
        }

        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"ExecutionRepository is invalid. Problems:\n{string.Join("\n", problems)}");
        }
    }

    /// <summary>
    /// Validates parameters for GetLatestExecutionAsync method.
    /// </summary>
    /// <param name="jobId">The job identifier</param>
    /// <returns>List of validation problems; empty if valid</returns>
    public static IReadOnlyList<string> Validate(this Guid jobId)
    {
        var problems = new List<string>();

        if (jobId == Guid.Empty)
        {
            problems.Add("Job ID cannot be empty.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified jobId is valid for GetLatestExecutionAsync.
    /// </summary>
    /// <param name="jobId">The job identifier to check</param>
    /// <returns>True if valid; false otherwise</returns>
    public static bool IsValid(this Guid jobId)
    {
        return jobId != Guid.Empty;
    }

    /// <summary>
    /// Ensures that the specified jobId is valid for GetLatestExecutionAsync.
    /// </summary>
    /// <param name="jobId">The job identifier to validate</param>
    /// <exception cref="ArgumentException">Thrown if jobId is empty</exception>
    public static void EnsureValid(this Guid jobId)
    {
        if (jobId == Guid.Empty)
        {
            throw new ArgumentException("Job ID cannot be empty.");
        }
    }

    /// <summary>
    /// Validates parameters for GetExecutionsByStatusAsync method.
    /// </summary>
    /// <param name="status">The execution status to filter by</param>
    /// <returns>List of validation problems; empty if valid</returns>
    public static IReadOnlyList<string> Validate(this ExecutionStatus status)
    {
        // All ExecutionStatus enum values are valid by design
        return Array.Empty<string>();
    }

    /// <summary>
    /// Determines whether the specified status is valid for GetExecutionsByStatusAsync.
    /// </summary>
    /// <param name="status">The status to check</param>
    /// <returns>Always true for ExecutionStatus enum values</returns>
    public static bool IsValid(this ExecutionStatus status)
    {
        return true;
    }

    /// <summary>
    /// Validates parameters for GetExecutionsByJobAndStatusAsync method.
    /// </summary>
    /// <param name="jobId">The job identifier</param>
    /// <param name="status">The execution status to filter by</param>
    /// <returns>List of validation problems; empty if valid</returns>
    public static IReadOnlyList<string> Validate(this Guid jobId, ExecutionStatus status)
    {
        var problems = new List<string>();

        if (jobId == Guid.Empty)
        {
            problems.Add("Job ID cannot be empty.");
        }

        // status is always valid as it's an enum
        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified jobId and status are valid for GetExecutionsByJobAndStatusAsync.
    /// </summary>
    /// <param name="jobId">The job identifier to check</param>
    /// <param name="status">The status to check</param>
    /// <returns>True if valid; false otherwise</returns>
    public static bool IsValid(this Guid jobId, ExecutionStatus status)
    {
        return jobId != Guid.Empty;
    }

    /// <summary>
    /// Ensures that the specified jobId and status are valid for GetExecutionsByJobAndStatusAsync.
    /// </summary>
    /// <param name="jobId">The job identifier to validate</param>
    /// <param name="status">The status to validate</param>
    /// <exception cref="ArgumentException">Thrown if parameters are invalid</exception>
    public static void EnsureValid(this Guid jobId, ExecutionStatus status)
    {
        if (jobId == Guid.Empty)
        {
            throw new ArgumentException("Job ID cannot be empty.");
        }
    }

    /// <summary>
    /// Validates parameters for GetCurrentlyRunningCountAsync method.
    /// </summary>
    /// <param name="jobId">The job identifier</param>
    /// <returns>List of validation problems; empty if valid</returns>
    public static IReadOnlyList<string> ValidateForCurrentlyRunningCount(this Guid jobId)
    {
        var problems = new List<string>();

        if (jobId == Guid.Empty)
        {
            problems.Add("Job ID cannot be empty.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified jobId is valid for GetCurrentlyRunningCountAsync.
    /// </summary>
    /// <param name="jobId">The job identifier to check</param>
    /// <returns>True if valid; false otherwise</returns>
    public static bool IsValidForCurrentlyRunningCount(this Guid jobId)
    {
        return jobId != Guid.Empty;
    }

    /// <summary>
    /// Ensures that the specified jobId is valid for GetCurrentlyRunningCountAsync.
    /// </summary>
    /// <param name="jobId">The job identifier to validate</param>
    /// <exception cref="ArgumentException">Thrown if jobId is empty</exception>
    public static void EnsureValidForCurrentlyRunningCount(this Guid jobId)
    {
        if (jobId == Guid.Empty)
        {
            throw new ArgumentException("Job ID cannot be empty.");
        }
    }

    /// <summary>
    /// Validates parameters for GetAverageExecutionTimeAsync method.
    /// </summary>
    /// <param name="jobId">The job identifier</param>
    /// <param name="lastN">Optional limit on number of recent executions to consider</param>
    /// <returns>List of validation problems; empty if valid</returns>
    public static IReadOnlyList<string> Validate(this Guid jobId, int? lastN = null)
    {
        var problems = new List<string>();

        if (jobId == Guid.Empty)
        {
            problems.Add("Job ID cannot be empty.");
        }

        if (lastN.HasValue)
        {
            if (lastN.Value <= 0)
            {
                problems.Add("lastN must be a positive integer if specified.");
            }
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified jobId and lastN are valid for GetAverageExecutionTimeAsync.
    /// </summary>
    /// <param name="jobId">The job identifier to check</param>
    /// <param name="lastN">Optional limit on number of recent executions</param>
    /// <returns>True if valid; false otherwise</returns>
    public static bool IsValid(this Guid jobId, int? lastN = null)
    {
        if (jobId == Guid.Empty)
            return false;

        if (lastN.HasValue && lastN.Value <= 0)
            return false;

        return true;
    }

    /// <summary>
    /// Ensures that the specified jobId and lastN are valid for GetAverageExecutionTimeAsync.
    /// </summary>
    /// <param name="jobId">The job identifier to validate</param>
    /// <param name="lastN">Optional limit on number of recent executions</param>
    /// <exception cref="ArgumentException">Thrown if parameters are invalid</exception>
    public static void EnsureValid(this Guid jobId, int? lastN = null)
    {
        if (jobId == Guid.Empty)
        {
            throw new ArgumentException("Job ID cannot be empty.");
        }

        if (lastN.HasValue && lastN.Value <= 0)
        {
            throw new ArgumentException("lastN must be a positive integer if specified.");
        }
    }

    /// <summary>
    /// Validates parameters for GetExecutionsByDateRangeAsync method.
    /// </summary>
    /// <param name="startDate">The start date of the range (inclusive)</param>
    /// <param name="endDate">The end date of the range (inclusive)</param>
    /// <returns>List of validation problems; empty if valid</returns>
    public static IReadOnlyList<string> Validate(this DateTime startDate, DateTime endDate)
    {
        var problems = new List<string>();

        if (startDate == default)
        {
            problems.Add("Start date cannot be default (Unix epoch).");
        }

        if (endDate == default)
        {
            problems.Add("End date cannot be default (Unix epoch).");
        }

        if (startDate > endDate)
        {
            problems.Add("Start date must be less than or equal to end date.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified date range is valid for GetExecutionsByDateRangeAsync.
    /// </summary>
    /// <param name="startDate">The start date to check</param>
    /// <param name="endDate">The end date to check</param>
    /// <returns>True if valid; false otherwise</returns>
    public static bool IsValid(this DateTime startDate, DateTime endDate)
    {
        return startDate != default
            && endDate != default
            && startDate <= endDate;
    }

    /// <summary>
    /// Ensures that the specified date range is valid for GetExecutionsByDateRangeAsync.
    /// </summary>
    /// <param name="startDate">The start date to validate</param>
    /// <param name="endDate">The end date to validate</param>
    /// <exception cref="ArgumentException">Thrown if date range is invalid</exception>
    public static void EnsureValid(this DateTime startDate, DateTime endDate)
    {
        if (startDate == default)
        {
            throw new ArgumentException("Start date cannot be default (Unix epoch).");
        }

        if (endDate == default)
        {
            throw new ArgumentException("End date cannot be default (Unix epoch).");
        }

        if (startDate > endDate)
        {
            throw new ArgumentException("Start date must be less than or equal to end date.");
        }
    }

    /// <summary>
    /// Validates parameters for GetByJobIdAsync method.
    /// </summary>
    /// <param name="jobId">The job identifier</param>
    /// <returns>List of validation problems; empty if valid</returns>
    public static IReadOnlyList<string> ValidateForGetByJobId(this Guid jobId)
    {
        var problems = new List<string>();

        if (jobId == Guid.Empty)
        {
            problems.Add("Job ID cannot be empty.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified jobId is valid for GetByJobIdAsync.
    /// </summary>
    /// <param name="jobId">The job identifier to check</param>
    /// <returns>True if valid; false otherwise</returns>
    public static bool IsValidForGetByJobId(this Guid jobId)
    {
        return jobId != Guid.Empty;
    }

    /// <summary>
    /// Ensures that the specified jobId is valid for GetByJobIdAsync.
    /// </summary>
    /// <param name="jobId">The job identifier to validate</param>
    /// <exception cref="ArgumentException">Thrown if jobId is empty</exception>
    public static void EnsureValidForGetByJobId(this Guid jobId)
    {
        if (jobId == Guid.Empty)
        {
            throw new ArgumentException("Job ID cannot be empty.");
        }
    }
}