#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using JobScheduler.Core.Domain.Entities;

namespace JobScheduler.Core.Services;

/// <summary>
/// Provides extension methods for <see cref="DependencyGraphValidationResult"/> to simplify
/// common validation scenarios and result handling.
/// </summary>
public static class DependencyGraphValidationResultExtensions
{
    /// <summary>
    /// Determines whether the validation result indicates a valid dependency graph (no cycles detected).
    /// </summary>
    /// <param name="result">The validation result to check.</param>
    /// <returns>True if the graph is valid; otherwise false.</returns>
    public static bool IsGraphValid(this DependencyGraphValidationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.IsValid;
    }

    /// <summary>
    /// Gets a formatted error message from the validation result, or returns an empty string if the graph is valid.
    /// </summary>
    /// <param name="result">The validation result.</param>
    /// <returns>A formatted error message if invalid; otherwise empty string.</returns>
    public static string GetErrorMessage(this DependencyGraphValidationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.IsValid ? string.Empty : result.Message;
    }

    /// <summary>
    /// Determines whether the validation result indicates a cycle was detected.
    /// </summary>
    /// <param name="result">The validation result to check.</param>
    /// <returns>True if a cycle was detected; otherwise false.</returns>
    public static bool HasCycle(this DependencyGraphValidationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return !result.IsValid && result.CycleNodes.Count > 0;
    }

    /// <summary>
    /// Gets the number of jobs involved in the detected cycle, or 0 if no cycle exists.
    /// </summary>
    /// <param name="result">The validation result.</param>
    /// <returns>The count of jobs in the cycle, or 0.</returns>
    public static int CycleCount(this DependencyGraphValidationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.CycleNodes.Count;
    }

    /// <summary>
    /// Gets a formatted string representation of the cycle nodes for logging or display purposes.
    /// </summary>
    /// <param name="result">The validation result.</param>
    /// <returns>A formatted string showing the cycle nodes in order, or empty string if no cycle.</returns>
    public static string FormatCycle(this DependencyGraphValidationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.HasCycle()
            ? $"Cycle: {string.Join(" → ", result.CycleNodes)}"
            : string.Empty;
    }

    /// <summary>
    /// Creates a new validation result that combines this result with another validation result.
    /// If both results are valid, returns a valid result. If either has a cycle, returns the cycle result.
    /// </summary>
    /// <param name="result">The current validation result.</param>
    /// <param name="other">Another validation result to combine with.</param>
    /// <returns>A combined validation result.</returns>
    public static DependencyGraphValidationResult CombineWith(this DependencyGraphValidationResult result, DependencyGraphValidationResult other)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(other);

        if (result.IsValid && other.IsValid)
        {
            return DependencyGraphValidationResult.Valid();
        }

        if (result.IsValid)
        {
            return other;
        }

        if (other.IsValid)
        {
            return result;
        }

        // Both have cycles - return the one with more nodes in the cycle
        return result.CycleNodes.Count >= other.CycleNodes.Count ? result : other;
    }
}