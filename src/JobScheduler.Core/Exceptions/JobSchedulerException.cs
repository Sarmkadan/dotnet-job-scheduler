#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;

namespace JobScheduler.Core.Exceptions;

/// <summary>
/// Base exception for all job scheduler-related errors.
/// </summary>
public class JobSchedulerException : Exception
{
    /// <summary>Gets or sets the error code.</summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="JobSchedulerException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public JobSchedulerException(string message)
        : base(string.IsNullOrEmpty(message) ? "An error occurred in the job scheduler." : message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JobSchedulerException"/> class with an error code.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="errorCode">The error code.</param>
    public JobSchedulerException(string message, string errorCode)
        : base(string.IsNullOrEmpty(message) ? "An error occurred in the job scheduler." : message)
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JobSchedulerException"/> class with an inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public JobSchedulerException(string message, Exception innerException)
        : base(string.IsNullOrEmpty(message) ? "An error occurred in the job scheduler." : message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JobSchedulerException"/> class with a message and error code.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="errorCode">The error code.</param>
    /// <param name="innerException">The inner exception.</param>
    public JobSchedulerException(string message, string errorCode, Exception innerException)
        : base(string.IsNullOrEmpty(message) ? "An error occurred in the job scheduler." : message, innerException)
    {
        ErrorCode = errorCode;
    }
}