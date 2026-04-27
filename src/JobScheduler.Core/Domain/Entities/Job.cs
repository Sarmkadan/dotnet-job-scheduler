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
}
