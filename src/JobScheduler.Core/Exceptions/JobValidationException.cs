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
public class JobValidationException : JobSchedulerException
{
    public string? PropertyName { get; set; }

    public JobValidationException(string message) : base(message, "JOB_VALIDATION_ERROR") { }

    public JobValidationException(string message, string propertyName)
        : base(message, "JOB_VALIDATION_ERROR")
    {
        PropertyName = propertyName;
    }

    public JobValidationException(string message, Exception innerException)
        : base(message, "JOB_VALIDATION_ERROR", innerException) { }
}
