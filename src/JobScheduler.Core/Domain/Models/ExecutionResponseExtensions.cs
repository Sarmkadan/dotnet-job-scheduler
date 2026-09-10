#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using JobScheduler.Core.Constants;

namespace JobScheduler.Core.Domain.Models;

/// <summary>
/// Extension methods for <see cref="ExecutionResponse"/>.
/// </summary>
public static class ExecutionResponseExtensions
{
    /// <summary>
    /// Determines whether the execution response indicates a successful execution.
    /// </summary>
    /// <param name="response">The execution response to evaluate.</param>
    /// <returns><c>true</c> if the status is <see cref="ExecutionStatus.Success"/>; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="response"/> is <c>null</c>.</exception>
    public static bool IsSuccessful(this ExecutionResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return string.Equals(response.Status, ExecutionStatus.Success.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether the execution response status is terminal.
    /// </summary>
    /// <param name="response">The execution response to evaluate.</param>
    /// <returns><c>true</c> if the status is terminal; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="response"/> is <c>null</c>.</exception>
    public static bool IsTerminal(this ExecutionResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        if (!Enum.TryParse<ExecutionStatus>(response.Status, out var status))
        {
            return false;
        }

        return status.IsTerminal();
    }

    /// <summary>
    /// Gets the duration of the execution as a <see cref="TimeSpan"/>.
    /// </summary>
    /// <param name="response">The execution response.</param>
    /// <returns>A <see cref="TimeSpan"/> representing the duration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="response"/> is <c>null</c>.</exception>
    public static TimeSpan GetDuration(this ExecutionResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return TimeSpan.FromMilliseconds(response.DurationMilliseconds);
    }

    /// <summary>
    /// Determines whether the execution response represents a retry attempt.
    /// </summary>
    /// <param name="response">The execution response to evaluate.</param>
    /// <returns><c>true</c> if <see cref="ExecutionResponse.RetryAttempt"/> is greater than zero; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="response"/> is <c>null</c>.</exception>
    public static bool IsRetry(this ExecutionResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return response.RetryAttempt > 0;
    }

    /// <summary>
    /// Summarizes a collection of execution responses by status.
    /// </summary>
    /// <param name="responses">The collection of execution responses.</param>
    /// <returns>A dictionary where the key is the status string and the value is the count of responses with that status.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="responses"/> is <c>null</c>.</exception>
    public static IDictionary<string, int> Summarize(this IEnumerable<ExecutionResponse> responses)
    {
        ArgumentNullException.ThrowIfNull(responses);
        return responses
            .GroupBy(r => r.Status)
            .ToDictionary(g => g.Key, g => g.Count());
    }
}
