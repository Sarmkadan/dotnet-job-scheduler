#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using JobScheduler.Core.Constants;

namespace JobScheduler.Core.Domain.Entities;

/// <summary>
/// Represents a scheduled job in the distributed job scheduler system.
/// Contains job configuration, scheduling rules, and retry policies.
/// </summary>
public class Job
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string CronExpression { get; set; } = string.Empty;

    /// <summary>
    /// Optional IANA or Windows timezone ID (e.g. "America/New_York", "Eastern Standard Time").
    /// When set, the cron expression is evaluated in this timezone so that schedules like
    /// "0 9 * * *" fire at 09:00 local time regardless of DST transitions.
    /// When null or empty the cron expression is evaluated in UTC.
    /// </summary>
    public string? TimeZoneId { get; set; }

    public JobPriority Priority { get; set; } = JobPriority.Normal;

    public JobStatus Status { get; set; } = JobStatus.Pending;

    public string HandlerType { get; set; } = string.Empty;

    public string? HandlerParameters { get; set; }

    public bool IsActive { get; set; } = true;

    public int MaxConcurrentExecutions { get; set; } = 1;

    public int MaxRetries { get; set; } = SchedulerConstants.DefaultMaxRetries;

    public int RetryBackoffSeconds { get; set; } = SchedulerConstants.DefaultRetryBackoffSeconds;

    public int ExecutionTimeoutSeconds { get; set; } = SchedulerConstants.DefaultExecutionTimeoutSeconds;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? LastExecutedAt { get; set; }

    public DateTime? NextExecutionAt { get; set; }

    public int TotalExecutions { get; set; }

    public int SuccessfulExecutions { get; set; }

    public int FailedExecutions { get; set; }

    public string? CreatedBy { get; set; }

    public string? UpdatedBy { get; set; }

    public virtual List<JobExecution> Executions { get; set; } = new();

    public virtual List<JobScheduleHistory> ScheduleHistories { get; set; } = new();

/// <summary>
/// Retry policy configuration for this job. Controls retry behavior on execution failures.
/// </summary>
public virtual RetryPolicy? RetryPolicy { get; set; }

    /// <summary>
    /// Validates the job configuration before scheduling.
    /// Throws ValidationException if validation fails.
    /// </summary>
    public bool IsValidForScheduling()
    {
        if (string.IsNullOrWhiteSpace(Name) || Name.Length > SchedulerConstants.MaxJobNameLength)
            return false;

        if (string.IsNullOrWhiteSpace(CronExpression) || CronExpression.Length > SchedulerConstants.MaxCronExpressionLength)
            return false;

        if (string.IsNullOrWhiteSpace(HandlerType))
            return false;

        if (MaxRetries < 0 || MaxRetries > 100)
            return false;

        if (ExecutionTimeoutSeconds <= 0 || ExecutionTimeoutSeconds > 86400)
            return false;

        if (MaxConcurrentExecutions <= 0 || MaxConcurrentExecutions > SchedulerConstants.MaxJobsPerPriority)
            return false;

        return true;
    }

    public void UpdateExecutionMetrics(bool success)
    {
        TotalExecutions++;
        LastExecutedAt = DateTime.UtcNow;

        if (success)
            SuccessfulExecutions++;
        else
            FailedExecutions++;
    }

    public double GetSuccessRate()
    {
        return TotalExecutions == 0 ? 0 : (double)SuccessfulExecutions / TotalExecutions * 100;
    }

    public void MarkAsUpdated(string? updatedBy = null)
    {
        UpdatedAt = DateTime.UtcNow;
        if (updatedBy is not null)
            UpdatedBy = updatedBy;
    }

    public bool CanExecuteNow(int currentConcurrentCount)
    {
        return IsActive && Status != JobStatus.Suspended && Status != JobStatus.Cancelled &&
               currentConcurrentCount < MaxConcurrentExecutions;
    }

/// <summary>
/// Gets the effective retry policy for this job, falling back to default values if not configured.
/// </summary>
public RetryPolicy GetEffectiveRetryPolicy()
{
    if (RetryPolicy != null && RetryPolicy.IsValid())
    {
        return RetryPolicy;
    }

    // Return a default policy based on job's simple retry properties
    return new RetryPolicy
    {
        JobId = Id,
        MaxRetries = MaxRetries,
        InitialBackoffSeconds = RetryBackoffSeconds,
        MaxBackoffSeconds = SchedulerConstants.DefaultMaxRetryBackoffSeconds,
        Strategy = BackoffStrategy.Exponential,
        BackoffMultiplier = SchedulerConstants.RetryBackoffMultiplier,
        RetryOnTimeout = true,
        RetryOnCancellation = false,
        RetryableExceptions = null
    };
}

    /// <summary>
    /// Returns an effective priority score that incorporates an aging bonus so that
    /// long-waiting low-priority jobs are eventually dequeued under sustained
    /// high-priority load.  Each <paramref name="agingRateMinutesPerLevel"/> minutes
    /// overdue raises the effective score by one priority level.  The bonus is capped
    /// so that a Low job can reach at most the Critical tier.
    /// </summary>
    public double CalculateEffectivePriority(DateTime now, double agingRateMinutesPerLevel = 5.0)
    {
        var overdueMinutes = NextExecutionAt.HasValue
            ? Math.Max(0, (now - NextExecutionAt.Value).TotalMinutes)
            : 0;

        var agingBonus = agingRateMinutesPerLevel > 0
            ? overdueMinutes / agingRateMinutesPerLevel
            : 0;

        // Cap the bonus so a Low job can age up to Critical at most.
        var maxBonus = (double)JobPriority.Critical - (int)Priority;
        agingBonus = Math.Min(agingBonus, maxBonus);

        return (int)Priority + agingBonus;
    }
}
