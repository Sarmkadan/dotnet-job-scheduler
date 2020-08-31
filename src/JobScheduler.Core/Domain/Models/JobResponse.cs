// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using JobScheduler.Core.Constants;

namespace JobScheduler.Core.Domain.Models;

/// <summary>
/// Response model for job data in API responses.
/// Serializable representation of job information.
/// </summary>
public class JobResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CronExpression { get; set; } = string.Empty;
    public JobPriority Priority { get; set; }
    public JobStatus Status { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastExecutedAt { get; set; }
    public DateTime? NextExecutionAt { get; set; }
    public int TotalExecutions { get; set; }
    public int SuccessfulExecutions { get; set; }
    public int FailedExecutions { get; set; }
    public double SuccessRate { get; set; }
    public int MaxRetries { get; set; }
    public int MaxConcurrentExecutions { get; set; }

    public static JobResponse FromJob(Job job)
    {
        return new JobResponse
        {
            Id = job.Id,
            Name = job.Name,
            Description = job.Description,
            CronExpression = job.CronExpression,
            Priority = job.Priority,
            Status = job.Status,
            IsActive = job.IsActive,
            CreatedAt = job.CreatedAt,
            UpdatedAt = job.UpdatedAt,
            LastExecutedAt = job.LastExecutedAt,
            NextExecutionAt = job.NextExecutionAt,
            TotalExecutions = job.TotalExecutions,
            SuccessfulExecutions = job.SuccessfulExecutions,
            FailedExecutions = job.FailedExecutions,
            SuccessRate = job.GetSuccessRate(),
            MaxRetries = job.MaxRetries,
            MaxConcurrentExecutions = job.MaxConcurrentExecutions
        };
    }
}
