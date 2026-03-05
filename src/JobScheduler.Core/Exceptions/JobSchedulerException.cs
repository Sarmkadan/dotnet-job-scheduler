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
    public string? ErrorCode { get; set; }

    public JobSchedulerException(string message) : base(message) { }

    public JobSchedulerException(string message, string errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }

    public JobSchedulerException(string message, Exception innerException) : base(message, innerException) { }

    public JobSchedulerException(string message, string errorCode, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}
