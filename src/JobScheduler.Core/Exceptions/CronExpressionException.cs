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
    public string CronExpression { get; set; }

    public CronExpressionException(string cronExpression, string message)
        : base($"Invalid cron expression '{cronExpression}': {message}", "INVALID_CRON_EXPRESSION")
    {
        CronExpression = cronExpression;
    }

    public CronExpressionException(string cronExpression, string message, Exception innerException)
        : base($"Invalid cron expression '{cronExpression}': {message}", "INVALID_CRON_EXPRESSION", innerException)
    {
        CronExpression = cronExpression;
    }
}
