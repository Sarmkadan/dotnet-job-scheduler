#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Diagnostics.CodeAnalysis;

namespace JobScheduler.Core.Services;

/// <summary>
/// Provides validation extension methods for <see cref="DependencyGraphValidationResult"/> instances.
/// </summary>
public static class DependencyGraphValidationResultValidation
{
    /// <summary>
    /// Validates the <see cref="DependencyGraphValidationResult"/> instance and returns a list of human-readable problems.
    /// </summary>
    /// <param name="value">The validation result to check.</param>
    /// <returns>An empty list if valid; otherwise, a list of error messages.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<string> Validate(this DependencyGraphValidationResult value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate IsValid is consistent with CycleNodes
        if (value.IsValid && value.CycleNodes.Count > 0)
        {
            errors.Add("IsValid is true but CycleNodes contains items. A valid graph must have an empty CycleNodes collection.");
        }

        if (!value.IsValid && value.CycleNodes.Count == 0)
        {
            errors.Add("IsValid is false but CycleNodes is empty. An invalid graph must have at least one cycle node.");
        }

        // Validate CycleNodes content when present
        if (value.CycleNodes.Count > 0)
        {
            if (value.CycleNodes.Any(g => g == Guid.Empty))
            {
                errors.Add("CycleNodes contains Guid.Empty values, which are not valid job identifiers.");
            }

            // Check for duplicates in cycle nodes
            var distinctCount = value.CycleNodes.Distinct().Count();
            if (distinctCount != value.CycleNodes.Count)
            {
                errors.Add("CycleNodes contains duplicate job IDs, which is not valid for a cycle path.");
            }
        }

        // Validate Message is not null or whitespace when IsValid is false
        if (!value.IsValid && string.IsNullOrWhiteSpace(value.Message))
        {
            errors.Add("Message must not be null or whitespace when IsValid is false.");
        }

        // Validate Message is not null or whitespace when IsValid is true (should have a meaningful message)
        if (value.IsValid && string.IsNullOrWhiteSpace(value.Message))
        {
            errors.Add("Message must not be null or whitespace even for valid graphs.");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the <see cref="DependencyGraphValidationResult"/> represents a valid dependency graph.
    /// </summary>
    /// <param name="value">The validation result to check.</param>
    /// <returns><see langword="true"/> if the graph is valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static bool IsValid(this DependencyGraphValidationResult value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.IsValid;
    }

    /// <summary>
    /// Ensures that the <see cref="DependencyGraphValidationResult"/> represents a valid dependency graph.
    /// Throws an <see cref="ArgumentException"/> listing all validation problems if the result is invalid.
    /// </summary>
    /// <param name="value">The validation result to check.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The validation result indicates an invalid graph with detailed error messages.</exception>
    public static void EnsureValid(this DependencyGraphValidationResult value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        if (errors.Count == 0)
        {
            return;
        }

        throw new ArgumentException(
            $"Dependency graph validation failed:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", errors)}",
            nameof(value));
    }
}