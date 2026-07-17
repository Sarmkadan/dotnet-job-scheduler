#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics.CodeAnalysis;

namespace JobScheduler.Core.Domain.Entities;

/// <summary>
/// Provides validation helpers for <see cref="JobPipeline"/> entities.
/// Validates all public members according to business rules and data integrity constraints.
/// </summary>
public static class JobPipelineValidation
{
    /// <summary>
    /// Validates the specified <see cref="JobPipeline"/> instance and returns a list of human-readable problems.
    /// </summary>
    /// <param name="value">The pipeline instance to validate.</param>
    /// <returns>An empty list if valid; otherwise a list of validation error messages.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this JobPipeline? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate Name
        if (string.IsNullOrWhiteSpace(value.Name))
        {
            errors.Add("Pipeline name cannot be null, empty, or whitespace.");
        }
        else if (value.Name.Length > 255)
        {
            errors.Add("Pipeline name cannot exceed 255 characters.");
        }

        // Validate Description
        if (!string.IsNullOrEmpty(value.Description) && value.Description.Length > 1024)
        {
            errors.Add("Pipeline description cannot exceed 1024 characters.");
        }

        // Validate IsActive (no specific constraints)

        // Validate CreatedAt
        if (value.CreatedAt == default)
        {
            errors.Add("Pipeline created timestamp must be set to a non-default DateTime value.");
        }
        else if (value.CreatedAt > DateTime.UtcNow.AddMinutes(5))
        {
            errors.Add("Pipeline created timestamp cannot be in the future.");
        }

        // Validate UpdatedAt
        if (value.UpdatedAt.HasValue)
        {
            if (value.UpdatedAt.Value == default)
            {
                errors.Add("Pipeline updated timestamp must be set to a non-default DateTime value when present.");
            }
            else if (value.UpdatedAt.Value > DateTime.UtcNow.AddMinutes(5))
            {
                errors.Add("Pipeline updated timestamp cannot be in the future.");
            }
            else if (value.UpdatedAt.Value < value.CreatedAt)
            {
                errors.Add("Pipeline updated timestamp cannot be earlier than created timestamp.");
            }
        }

        // Validate CreatedBy
        if (value.CreatedBy is not null)
        {
            if (string.IsNullOrWhiteSpace(value.CreatedBy))
            {
                errors.Add("Pipeline created by identifier cannot be empty or whitespace when set.");
            }
            else if (value.CreatedBy.Length > 128)
            {
                errors.Add("Pipeline created by identifier cannot exceed 128 characters.");
            }
        }

        // Validate Steps collection
        if (value.Steps is null)
        {
            errors.Add("Pipeline steps collection cannot be null.");
        }
        else
        {
            // Validate each step
            foreach (var step in value.Steps)
            {
                if (step is null)
                {
                    errors.Add("Pipeline contains a null step.");
                    continue;
                }

                // Validate JobPipelineStep properties
                if (step.Id == default)
                {
                    errors.Add("Pipeline step ID must be set to a non-default Guid value.");
                }

                if (step.PipelineId == default)
                {
                    errors.Add("Pipeline step PipelineId must be set to a non-default Guid value.");
                }

                if (step.JobId == default)
                {
                    errors.Add("Pipeline step JobId must be set to a non-default Guid value.");
                }

                if (step.StepOrder < 0 || step.StepOrder > 9999)
                {
                    errors.Add("Pipeline step StepOrder must be between 0 and 9999 inclusive.");
                }
            }
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="JobPipeline"/> instance is valid.
    /// </summary>
    /// <param name="value">The pipeline instance to check.</param>
    /// <returns>True if valid; otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static bool IsValid(this JobPipeline? value)
        => value is not null && value.Validate().Count == 0;

    /// <summary>
    /// Ensures that the specified <see cref="JobPipeline"/> instance is valid, throwing an <see cref="ArgumentException"/>
    /// with a detailed message listing all validation problems if it is not.
    /// </summary>
    /// <param name="value">The pipeline instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is invalid, with a message listing all problems.</exception>
    public static void EnsureValid(this JobPipeline? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();

        if (errors.Count == 0)
        {
            return;
        }

        throw new ArgumentException(
            $"Pipeline validation failed:{Environment.NewLine}- {
                string.Join($"{Environment.NewLine}- ", errors)
            }",
            nameof(value)
        );
    }
}