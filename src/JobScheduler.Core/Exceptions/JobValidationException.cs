#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;

namespace JobScheduler.Core.Exceptions;

/// <summary>
/// Thrown when job configuration or data fails validation.
/// </summary>
public sealed class JobValidationException : JobSchedulerException
{
    /// <summary>Gets or sets the name of the property that failed validation.</summary>
    public string? PropertyName { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="JobValidationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public JobValidationException(string message)
        : base(message, "JOB_VALIDATION_ERROR")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JobValidationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="propertyName">The name of the property that failed validation.</param>
    public JobValidationException(string message, string propertyName)
        : base(message, "JOB_VALIDATION_ERROR")
    {
        PropertyName = propertyName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JobValidationException"/> class with an inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public JobValidationException(string message, Exception innerException)
        : base(message, "JOB_VALIDATION_ERROR", innerException)
    {
    }
}