#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.ComponentModel.DataAnnotations;
using JobScheduler.Core.Constants;

namespace JobScheduler.Core.Domain.Models;

/// <summary>
/// Request model for creating a new scheduled job.
/// Contains validation and job configuration from client requests.
/// </summary>
public class CreateJobRequest
{
    [Required]
    [StringLength(256, MinimumLength = 3)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 5)]
    public string CronExpression { get; set; } = string.Empty;

    [Required]
    [StringLength(512)]
    public string HandlerType { get; set; } = string.Empty;

    public string? HandlerParameters { get; set; }

    public JobPriority Priority { get; set; } = JobPriority.Normal;

    [Range(1, 1000)]
    public int MaxConcurrentExecutions { get; set; } = 1;

    [Range(0, 100)]
    public int MaxRetries { get; set; } = SchedulerConstants.DefaultMaxRetries;

    [Range(1, 3600)]
    public int RetryBackoffSeconds { get; set; } = SchedulerConstants.DefaultRetryBackoffSeconds;

    [Range(10, 86400)]
    public int ExecutionTimeoutSeconds { get; set; } = SchedulerConstants.DefaultExecutionTimeoutSeconds;

    public bool IsActive { get; set; } = true;

    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Name) &&
               !string.IsNullOrWhiteSpace(CronExpression) &&
               !string.IsNullOrWhiteSpace(HandlerType) &&
               MaxRetries >= 0 &&
               ExecutionTimeoutSeconds > 0 &&
               MaxConcurrentExecutions > 0;
    }

    public Job ToJob()
    {
        return new Job
        {
            Name = Name,
            Description = Description ?? string.Empty,
            CronExpression = CronExpression,
            HandlerType = HandlerType,
            HandlerParameters = HandlerParameters,
            Priority = Priority,
            MaxConcurrentExecutions = MaxConcurrentExecutions,
            MaxRetries = MaxRetries,
            RetryBackoffSeconds = RetryBackoffSeconds,
            ExecutionTimeoutSeconds = ExecutionTimeoutSeconds,
            IsActive = IsActive
        };
    }
}
