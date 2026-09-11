#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using JobScheduler.Core.Constants;
using JobScheduler.Core.Domain.Entities;

namespace JobScheduler.Core.Domain.Models;

/// <summary>
/// Response model for job data in API responses.
/// Serializable representation of job information.
/// </summary>
public sealed class JobResponse
{
    /// <summary>
    /// Unique identifier for the job.
    /// </summary>
    public Guid Id { get; set; }
    /// <summary>
    /// The name of the job.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// A description of the job.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// The cron expression that defines the job's schedule.
    /// </summary>
    public string CronExpression { get; set; } = string.Empty;
    /// <summary>
    /// The time zone in which the cron expression is evaluated.
    /// </summary>
    public string? TimeZoneId { get; set; }
    /// <summary>
    /// The priority level of the job (e.g., Low, Medium, High).
    /// </summary>
    public string Priority { get; set; } = string.Empty;
    /// <summary>
    /// The current status of the job (e.g., Pending, Running, Completed).
    /// </summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>
    /// The type of handler that processes the job.
    /// </summary>
    public string HandlerType { get; set; } = string.Empty;
    /// <summary>
    /// The maximum time in seconds the job is allowed to run.
    /// </summary>
    public int ExecutionTimeoutSeconds { get; set; }
    /// <summary>
    /// Indicates whether the job is active and should be scheduled.
    /// </summary>
    public bool IsActive { get; set; }
    /// <summary>
    /// The date and time when the job was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>
    /// The date and time when the job was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    /// <summary>
    /// The date and time when the job was last executed.
    /// </summary>
    public DateTime? LastExecutedAt { get; set; }
    /// <summary>
    /// The date and time when the job is next scheduled to execute.
    /// </summary>
    public DateTime? NextExecutionAt { get; set; }
    /// <summary>
    /// The total number of times the job has been executed.
    /// </summary>
    public int TotalExecutions { get; set; }
    /// <summary>
    /// The number of times the job has executed successfully.
    /// </summary>
    public int SuccessfulExecutions { get; set; }
    /// <summary>
    /// The number of times the job has failed to execute.
    /// </summary>
    public int FailedExecutions { get; set; }
    /// <summary>
    /// The percentage of successful executions (0-100).
    /// </summary>
    public double SuccessRate { get; set; }
    /// <summary>
    /// The maximum number of retry attempts for failed executions.
    /// </summary>
    public int MaxRetries { get; set; }
    /// <summary>
    /// The maximum number of instances of the job that can run concurrently.
    /// </summary>
    public int MaxConcurrentExecutions { get; set; }

    /// <summary>
    /// Creates a JobResponse from a Job entity.
    /// </summary>
    /// <param name="job">The Job entity to convert.</param>
    /// <returns>A JobResponse representing the job.</returns>
    public static JobResponse FromJob(Job job)
    {
        return new JobResponse
        {
            Id = job.Id,
            Name = job.Name,
            Description = job.Description,
            CronExpression = job.CronExpression,
            TimeZoneId = job.TimeZoneId,
            Priority = job.Priority.ToString(),
            Status = job.Status.ToString(),
            HandlerType = job.HandlerType,
            ExecutionTimeoutSeconds = job.ExecutionTimeoutSeconds,
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
    /// <summary>
    /// Returns a string representation of the job response.
    /// </summary>
    /// <returns>A string summarizing Id, Name, Status, and NextExecution.</returns>
    public override string ToString()
    {
        return $"Job {Id}: {Name} - {Status}, Next: {NextExecutionAt?.ToString("o") ?? "None"}";
    }
}