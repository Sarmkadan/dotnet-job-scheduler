#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.ComponentModel.DataAnnotations;
using JobScheduler.Core.Constants;
using JobScheduler.Core.Domain.Entities;

namespace JobScheduler.Core.Domain.Models;

/// <summary>
/// Request model for creating a new scheduled job.
/// Contains validation and job configuration from client requests.
/// </summary>
public sealed class CreateJobRequest
{
    /// <summary>
    /// The name of the job. Required and must be between 3 and 256 characters.
    /// </summary>
    [Required]
    [StringLength(256, MinimumLength = 3)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the job. Maximum length 1000 characters.
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// The cron expression that defines the job schedule. Required and must be between 5 and 100 characters.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 5)]
    public string CronExpression { get; set; } = string.Empty;

    /// <summary>
    /// Optional IANA or Windows timezone ID (e.g. "America/New_York", "Eastern Standard Time").
    /// When provided, the cron expression fires at local time in that timezone.
    /// </summary>
    [StringLength(100)]
    public string? TimeZoneId { get; set; }

    /// <summary>
    /// The type of the handler that will process the job. Required and must be between 1 and 512 characters.
    /// </summary>
    [Required]
    [StringLength(512)]
    public string HandlerType { get; set; } = string.Empty;

    /// <summary>
    /// Optional parameters to pass to the handler. JSON string format.
    /// </summary>
    public string? HandlerParameters { get; set; }

    /// <summary>
    /// The priority of the job.
    /// </summary>
    public JobPriority Priority { get; set; } = JobPriority.Normal;

    /// <summary>
    /// The maximum number of concurrent executions allowed for this job. Must be between 1 and 1000.
    /// </summary>
    [Range(1, 1000)]
    public int MaxConcurrentExecutions { get; set; } = 1;

    /// <summary>
    /// The maximum number of retry attempts if the job fails. Must be between 0 and 100.
    /// </summary>
    [Range(0, 100)]
    public int MaxRetries { get; set; } = SchedulerConstants.DefaultMaxRetries;

    /// <summary>
    /// The backoff seconds between retry attempts. Must be between 1 and 3600.
    /// </summary>
    [Range(1, 3600)]
    public int RetryBackoffSeconds { get; set; } = SchedulerConstants.DefaultRetryBackoffSeconds;

    [Range(10, 86400)]
    public int ExecutionTimeoutSeconds { get; set; } = SchedulerConstants.DefaultExecutionTimeoutSeconds;

    /// <summary>
    /// Indicates whether the job is active and should be scheduled. Defaults to true.
    /// </summary>
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
            TimeZoneId = TimeZoneId,
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

    /// <summary>
    /// Returns a string representation of the CreateJobRequest object.
    /// </summary>
    /// <returns>A string containing the property values of the CreateJobRequest.</returns>
    public override string ToString()
    {
        return $"CreateJobRequest {{ Name = {Name}, Description = {Description}, CronExpression = {CronExpression}, TimeZoneId = {TimeZoneId}, HandlerType = {HandlerType}, HandlerParameters = {HandlerParameters} }}";
    }
}
