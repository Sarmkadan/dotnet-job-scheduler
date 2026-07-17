#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using JobScheduler.Core.Domain.Models;

namespace JobScheduler.Core.Services;

/// <summary>
/// Provides validation helpers for <see cref="JobHistoryService"/> and related types.
/// </summary>
public static class JobHistoryServiceValidation
{
    /// <summary>
    /// Validates a <see cref="PagedResult{T}"/> instance.
    /// </summary>
    /// <param name="value">The paged result to validate.</param>
    /// <returns>A list of human-readable validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this PagedResult<ExecutionResponse>? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        if (value.Items is null)
            problems.Add("Items collection cannot be null.");
        else if (value.Items.Count > value.TotalCount)
            problems.Add("Items count exceeds TotalCount.");

        if (value.TotalCount < 0)
            problems.Add("TotalCount cannot be negative.");
        else if (value.TotalCount > 0 && value.PageSize == 0)
            problems.Add("PageSize must be positive when TotalCount is greater than 0.");

        if (value.PageNumber < 1)
            problems.Add("PageNumber must be at least 1.");

        if (value.PageSize < 0)
            problems.Add("PageSize cannot be negative.");

        if (value.TotalPages < 0)
            problems.Add("TotalPages cannot be negative.");

        if (value.PageNumber > value.TotalPages + 1)
            problems.Add("PageNumber exceeds TotalPages + 1.");

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates a <see cref="PagedResult{T}"/> instance.
    /// </summary>
    /// <param name="value">The paged result to validate.</param>
    /// <returns>A list of human-readable validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this PagedResult<JobExecutionSummary>? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        if (value.Items is null)
            problems.Add("Items collection cannot be null.");
        else if (value.Items.Count > value.TotalCount)
            problems.Add("Items count exceeds TotalCount.");

        if (value.TotalCount < 0)
            problems.Add("TotalCount cannot be negative.");
        else if (value.TotalCount > 0 && value.PageSize == 0)
            problems.Add("PageSize must be positive when TotalCount is greater than 0.");

        if (value.PageNumber < 1)
            problems.Add("PageNumber must be at least 1.");

        if (value.PageSize < 0)
            problems.Add("PageSize cannot be negative.");

        if (value.TotalPages < 0)
            problems.Add("TotalPages cannot be negative.");

        if (value.PageNumber > value.TotalPages + 1)
            problems.Add("PageNumber exceeds TotalPages + 1.");

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates a <see cref="JobExecutionSummary"/> instance.
    /// </summary>
    /// <param name="value">The job execution summary to validate.</param>
    /// <returns>A list of human-readable validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this JobExecutionSummary? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        if (value.TotalExecutions < 0)
            problems.Add("TotalExecutions cannot be negative.");

        if (value.SuccessCount < 0)
            problems.Add("SuccessCount cannot be negative.");

        if (value.FailureCount < 0)
            problems.Add("FailureCount cannot be negative.");

        if (value.TimedOutCount < 0)
            problems.Add("TimedOutCount cannot be negative.");

        if (value.CancelledCount < 0)
            problems.Add("CancelledCount cannot be negative.");

        if (value.AverageDurationMs < 0)
            problems.Add("AverageDurationMs cannot be negative.");

        if (value.MinDurationMs < 0)
            problems.Add("MinDurationMs cannot be negative.");

        if (value.MaxDurationMs < 0)
            problems.Add("MaxDurationMs cannot be negative.");

        if (value.MinDurationMs > value.MaxDurationMs)
            problems.Add("MinDurationMs cannot exceed MaxDurationMs.");

        if (value.LastExecutedAt.HasValue && value.LastExecutedAt.Value > DateTime.UtcNow)
            problems.Add("LastExecutedAt cannot be in the future.");

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates a <see cref="ExecutionResponse"/> instance.
    /// </summary>
    /// <param name="value">The execution response to validate.</param>
    /// <returns>A list of human-readable validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this ExecutionResponse? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        if (value.Id == Guid.Empty)
            problems.Add("Id cannot be empty.");

        if (value.JobId == Guid.Empty)
            problems.Add("JobId cannot be empty.");

        if (string.IsNullOrWhiteSpace(value.Status))
            problems.Add("Status cannot be null or whitespace.");

        if (value.StartedAt == default)
            problems.Add("StartedAt cannot be default (Unix epoch).");

        if (value.StartedAt > DateTime.UtcNow)
            problems.Add("StartedAt cannot be in the future.");

        if (value.CompletedAt.HasValue && value.CompletedAt.Value < value.StartedAt)
            problems.Add("CompletedAt cannot be before StartedAt.");

        if (value.CompletedAt.HasValue && value.CompletedAt.Value > DateTime.UtcNow)
            problems.Add("CompletedAt cannot be in the future.");

        if (value.DurationMilliseconds < 0)
            problems.Add("DurationMilliseconds cannot be negative.");

        if (value.AttemptNumber < 0)
            problems.Add("AttemptNumber cannot be negative.");

        if (value.ExecutionTimeMs < 0)
            problems.Add("ExecutionTimeMs cannot be negative.");

        if (value.RetryAttempt < 0)
            problems.Add("RetryAttempt cannot be negative.");

        if (value.CreatedAt == default)
            problems.Add("CreatedAt cannot be default (Unix epoch).");

        if (value.CreatedAt > DateTime.UtcNow)
            problems.Add("CreatedAt cannot be in the future.");

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="PagedResult{T}"/> is valid.
    /// </summary>
    /// <param name="value">The paged result to check.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this PagedResult<ExecutionResponse>? value) =>
        value?.Validate() is null || value.Validate().Count == 0;

    /// <summary>
    /// Determines whether the specified <see cref="PagedResult{T}"/> is valid.
    /// </summary>
    /// <param name="value">The paged result to check.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this PagedResult<JobExecutionSummary>? value) =>
        value?.Validate() is null || value.Validate().Count == 0;

    /// <summary>
    /// Determines whether the specified <see cref="JobExecutionSummary"/> is valid.
    /// </summary>
    /// <param name="value">The job execution summary to check.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this JobExecutionSummary? value) =>
        value?.Validate() is null || value.Validate().Count == 0;

    /// <summary>
    /// Determines whether the specified <see cref="ExecutionResponse"/> is valid.
    /// </summary>
    /// <param name="value">The execution response to check.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this ExecutionResponse? value) =>
        value?.Validate() is null || value.Validate().Count == 0;

    /// <summary>
    /// Ensures that the specified <see cref="PagedResult{T}"/> is valid, throwing an exception if not.
    /// </summary>
    /// <param name="value">The paged result to validate.</param>
    /// <exception cref="ArgumentException">Thrown when the paged result is invalid.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static void EnsureValid(this PagedResult<ExecutionResponse>? value)
    {
        var problems = value.Validate();
        if (problems.Count > 0)
            throw new ArgumentException(string.Join(" ", problems));
    }

    /// <summary>
    /// Ensures that the specified <see cref="PagedResult{T}"/> is valid, throwing an exception if not.
    /// </summary>
    /// <param name="value">The paged result to validate.</param>
    /// <exception cref="ArgumentException">Thrown when the paged result is invalid.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static void EnsureValid(this PagedResult<JobExecutionSummary>? value)
    {
        var problems = value.Validate();
        if (problems.Count > 0)
            throw new ArgumentException(string.Join(" ", problems));
    }

    /// <summary>
    /// Ensures that the specified <see cref="JobExecutionSummary"/> is valid, throwing an exception if not.
    /// </summary>
    /// <param name="value">The job execution summary to validate.</param>
    /// <exception cref="ArgumentException">Thrown when the summary is invalid.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static void EnsureValid(this JobExecutionSummary? value)
    {
        var problems = value.Validate();
        if (problems.Count > 0)
            throw new ArgumentException(string.Join(" ", problems));
    }

    /// <summary>
    /// Ensures that the specified <see cref="ExecutionResponse"/> is valid, throwing an exception if not.
    /// </summary>
    /// <param name="value">The execution response to validate.</param>
    /// <exception cref="ArgumentException">Thrown when the response is invalid.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static void EnsureValid(this ExecutionResponse? value)
    {
        var problems = value.Validate();
        if (problems.Count > 0)
            throw new ArgumentException(string.Join(" ", problems));
    }
}