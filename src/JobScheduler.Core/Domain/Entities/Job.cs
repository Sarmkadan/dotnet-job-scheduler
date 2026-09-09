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
    /// <summary>
    /// Unique identifier for the job.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Name of the job.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the job.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Cron expression that defines the job's schedule.
    /// </summary>
    public string CronExpression { get; set; } = string.Empty;

    /// <summary>
    /// Optional IANA or Windows timezone ID (e.g. "America/New_York", "Eastern Standard Time").
    /// When set, the cron expression is evaluated in this timezone so that schedules like
    /// "0 9 * * *" fire at 09:00 local time regardless of DST transitions.
    /// When null or empty the cron expression is evaluated in UTC.
    /// </summary>
    public string? TimeZoneId { get; set; }

    /// <summary>
    /// Priority level of the job.
    /// </summary>
    public JobPriority Priority { get; set; } = JobPriority.Normal;

    /// <summary>
    /// Current status of the job.
    /// </summary>
    public JobStatus Status { get; set; } = JobStatus.Pending;

    /// <summary>
    /// Type of the handler that will execute this job.
    /// </summary>
    public string HandlerType { get; set; } = string.Empty;

    /// <summary>
    /// Parameters to pass to the job handler.
    /// </summary>
    public string? HandlerParameters { get; set; }

    /// <summary>
    /// Indicates whether the job is currently active and eligible for scheduling.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Policy to handle misfire situations when a job execution is missed.
    /// </summary>
    public MisfirePolicy MisfirePolicy { get; set; } = MisfirePolicy.SkipToNext;

    /// <summary>
    /// Maximum number of concurrent executions allowed for this job.
    /// </summary>
    public int MaxConcurrentExecutions { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value indicating whether concurrent execution of the same job is disallowed.
        /// When true, only one instance of this job can run at any time, even if MaxConcurrentExecutions > 1.
        /// This is useful for jobs that should not overlap with themselves (e.g., database migrations, file operations).
        /// </summary>
        public bool DisallowConcurrentExecution { get; set; } = false;

    /// <summary>
    /// Maximum number of retry attempts for failed job executions.
    /// </summary>
    public int MaxRetries { get; set; } = SchedulerConstants.DefaultMaxRetries;

    /// <summary>
    /// Backoff time in seconds between retry attempts.
    /// </summary>
    public int RetryBackoffSeconds { get; set; } = SchedulerConstants.DefaultRetryBackoffSeconds;

    /// <summary>
    /// Maximum execution time in seconds before the job is considered timed out.
    /// </summary>
    public int ExecutionTimeoutSeconds { get; set; } = SchedulerConstants.DefaultExecutionTimeoutSeconds;

    /// <summary>
    /// Date and time when the job was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date and time when the job was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Date and time when the job was last executed.
    /// </summary>
    public DateTime? LastExecutedAt { get; set; }

    /// <summary>
    /// Date and time when the job is scheduled to execute next.
    /// </summary>
    public DateTime? NextExecutionAt { get; set; }

    /// <summary>
    /// Total number of times the job has been executed.
    /// </summary>
    public int TotalExecutions { get; set; }

    /// <summary>
    /// Number of times the job has executed successfully.
    /// </summary>
    public int SuccessfulExecutions { get; set; }

    /// <summary>
    /// Number of times the job has failed execution.
    /// </summary>
    public int FailedExecutions { get; set; }

    /// <summary>
    /// User who created the job.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// User who last updated the job.
    /// </summary>
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Collection of job execution records.
    /// </summary>
    public virtual List<JobExecution> Executions { get; set; } = new();

    /// <summary>
    /// Collection of job schedule history records.
    /// </summary>
    public virtual List<JobScheduleHistory> ScheduleHistories { get; set; } = new();

    /// <summary>
    /// Retry policy configuration for this job. Controls retry behavior on execution failures.
    /// </summary>
    public virtual RetryPolicy? RetryPolicy { get; set; }

    /// <summary>
    /// Returns a string representation of the job.
    /// </summary>
    /// <returns>A string representation of the job.</returns>
    public override string ToString() => $"Job {{ Id = {Id}, Name = {Name}, Description = {Description}, CronExpression = {CronExpression}, TimeZoneId = {TimeZoneId}, Priority = {Priority} }}";

    /// <summary>
    /// Validates the job configuration before scheduling.
    /// </summary>
    /// <returns>True if the job configuration is valid for scheduling; otherwise, false.</returns>
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

        if (DisallowConcurrentExecution && MaxConcurrentExecutions > 1)
        {
            MaxConcurrentExecutions = 1;
        }

        return true;
    }

    /// <summary>
    /// Updates the job's execution metrics after an execution attempt.
    /// </summary>
    /// <param name="success">Whether the job execution was successful.</param>
    public void UpdateExecutionMetrics(bool success)
    {
        TotalExecutions++;
        LastExecutedAt = DateTime.UtcNow;

        if (success)
            SuccessfulExecutions++;
        else
            FailedExecutions++;
    }

    /// <summary>
    /// Calculates the success rate of the job as a percentage.
    /// </summary>
    /// <returns>The success rate percentage (0-100). Returns 0 if no executions have occurred.</returns>
    public double GetSuccessRate()
    {
        return TotalExecutions == 0 ? 0 : (double)SuccessfulExecutions / TotalExecutions * 100;
    }

    /// <summary>
    /// Marks the job as updated with the current timestamp.
    /// </summary>
    /// <param name="updatedBy">The user who updated the job (optional).</param>
    public void MarkAsUpdated(string? updatedBy = null)
    {
        UpdatedAt = DateTime.UtcNow;
        if (updatedBy is not null)
            UpdatedBy = updatedBy;
    }

        /// <summary>
        /// Determines whether the job can execute based on its active status, suspension state, and concurrency limits.
        /// </summary>
        /// <param name="currentConcurrentCount">The current number of concurrent executions of this job.</param>
        /// <returns><c>true</c> if the job can execute; otherwise, <c>false</c>.</returns>
        public bool CanExecuteNow(int currentConcurrentCount)
        {
            // If DisallowConcurrentExecution is true, only allow execution if no instances are currently running
            if (DisallowConcurrentExecution)
            {
                return IsActive && Status != JobStatus.Suspended && Status != JobStatus.Cancelled &&
                       currentConcurrentCount == 0;
            }

            return IsActive && Status != JobStatus.Suspended && Status != JobStatus.Cancelled &&
                   currentConcurrentCount < MaxConcurrentExecutions;
        }

    /// <summary>
    /// Gets the effective retry policy for this job, falling back to default values if not configured.
    /// </summary>
    /// <returns>The effective retry policy to use for this job.</returns>
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
    /// <param name="now">The current date and time.</param>
    /// <param name="agingRateMinutesPerLevel">The number of minutes overdue required to increase the priority score by one level.</param>
    /// <returns>The calculated effective priority score.</returns>
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
