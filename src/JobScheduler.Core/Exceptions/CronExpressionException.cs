#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;

namespace JobScheduler.Core.Exceptions;

/// <summary>
/// Thrown when a cron expression is invalid or cannot be parsed.
/// </summary>
public sealed class CronExpressionException : JobSchedulerException
{
    /// <summary>Gets or sets the cron expression that failed validation.</summary>
    public string CronExpression { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CronExpressionException"/> class.
    /// </summary>
    /// <param name="cronExpression">The invalid cron expression.</param>
    /// <param name="message">The error message.</param>
    public CronExpressionException(string cronExpression, string message)
        : base(string.IsNullOrEmpty(cronExpression)
            ? $"Invalid cron expression '': {message}"
            : $"Invalid cron expression '{cronExpression}': {message}",
            "INVALID_CRON_EXPRESSION")
    {
        CronExpression = cronExpression;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CronExpressionException"/> class with an inner exception.
    /// </summary>
    /// <param name="cronExpression">The invalid cron expression.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public CronExpressionException(string cronExpression, string message, Exception innerException)
        : base(string.IsNullOrEmpty(cronExpression)
            ? $"Invalid cron expression '': {message}"
            : $"Invalid cron expression '{cronExpression}': {message}",
            "INVALID_CRON_EXPRESSION",
            innerException)
    {
        CronExpression = cronExpression;
    }
}