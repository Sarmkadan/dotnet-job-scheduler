#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using JobScheduler.Core.Constants;
using JobScheduler.Core.Data.Repositories;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Exceptions;

namespace JobScheduler.Core.Services;

/// <summary>
/// Central orchestrator for the job scheduler system. Coordinates job scheduling,
/// execution, retry logic, concurrency management, and lifecycle transitions.
/// </summary>
/// <remarks>
/// <para>
/// The scheduler supports multiple scheduling modes:
/// <list type="bullet">
///   <item>Cron-based recurring schedules (via <see cref="CronExpressionService"/>)</item>
///   <item>One-time delayed execution</item>
///   <item>Immediate execution</item>
///   <item>Dependency-based execution chains</item>
/// </list>
/// </para>
/// <para>
/// Concurrency is managed per job via <see cref="ConcurrencyManager"/> - each job can define
/// a maximum number of parallel executions. Failed jobs are retried according to the
/// configured <see cref="RetryService"/> policy (fixed delay, exponential backoff, or none).
/// </para>
/// </remarks>
public sealed class JobSchedulerService
{
    private readonly IJobRepository _jobRepository;
    private readonly IExecutionRepository _executionRepository;
    private readonly JobExecutorService _executorService;
    private readonly CronExpressionService _cronService;
    private readonly RetryService _retryService;
    private readonly ConcurrencyManager _concurrencyManager;
    private readonly ILogger<JobSchedulerService>? _logger;

    public JobSchedulerService(
        IJobRepository jobRepository,
        IExecutionRepository executionRepository,
        JobExecutorService executorService,
        CronExpressionService cronService,
        RetryService retryService,
        ConcurrencyManager concurrencyManager,
        ILogger<JobSchedulerService>? logger = null)
    {
        _jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
        _executionRepository = executionRepository ?? throw new ArgumentNullException(nameof(executionRepository));
        _executorService = executorService ?? throw new ArgumentNullException(nameof(executorService));
        _cronService = cronService ?? throw new ArgumentNullException(nameof(cronService));
        _retryService = retryService ?? throw new ArgumentNullException(nameof(retryService));
        _concurrencyManager = concurrencyManager ?? throw new ArgumentNullException(nameof(concurrencyManager));
        _logger = logger;
    }

    /// <summary>
    /// Creates and schedules a new job.
    /// </summary>
    public async Task<Job> CreateJobAsync(Job job, string? createdBy = null)
    {
        if (job is null)
            throw new ArgumentNullException(nameof(job));

        // Fix: Add validation for job.Name to prevent null, empty, or whitespace values.
        if (string.IsNullOrWhiteSpace(job.Name))
            throw new ArgumentException("Job name cannot be null or whitespace.", nameof(job.Name));

        if (!job.IsValidForScheduling())
            throw new JobValidationException("Job configuration is invalid");

        if (!_cronService.IsValidCronExpression(job.CronExpression))
            throw new CronExpressionException(job.CronExpression, "Invalid cron expression");

        var existingJob = await _jobRepository.GetByNameAsync(job.Name);
        if (existingJob is not null)
            throw new JobValidationException($"Job with name '{job.Name}' already exists", nameof(job.Name));

        job.CreatedBy = createdBy;
        job.Status = JobStatus.Scheduled;
        job.NextExecutionAt = _cronService.GetNextExecutionTime(job.CronExpression);

        await _jobRepository.AddAsync(job);
        await _jobRepository.SaveChangesAsync();

        _logger?.LogInformation("Job {JobId} ({JobName}) created and scheduled for {NextExecution}",
            job.Id, job.Name, job.NextExecutionAt);

        return job;
    }

    /// <summary>
    /// Processes scheduled jobs that are due for execution.
    /// </summary>
    public async Task<IEnumerable<JobExecution>> ExecuteDueJobsAsync(CancellationToken cancellationToken = default)
    {
        var dueJobs = await _jobRepository.GetScheduledJobsForExecutionAsync();
        var executions = new List<JobExecution>();

        foreach (var job in dueJobs)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                var execution = await _executorService.ExecuteJobAsync(job, cancellationToken);
                executions.Add(execution);

                // Schedule next execution
                if (job.IsActive && job.Status != JobStatus.FailedPermanently)
                {
                    job.NextExecutionAt = _cronService.GetNextExecutionTime(job.CronExpression, DateTime.UtcNow);
                    _jobRepository.Update(job);
                    await _jobRepository.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error executing job {JobId}", job.Id);
            }
        }

        return executions;
    }

    /// <summary>
    /// Processes failed executions that are eligible for retry.
    /// </summary>
    public async Task<IEnumerable<JobExecution>> ProcessRetriesAsync()
    {
        var failedExecutions = await _executionRepository.GetFailedExecutionsRequiringRetryAsync();
        var retryExecutions = new List<JobExecution>();

        foreach (var failedExecution in failedExecutions)
        {
            var job = await _jobRepository.GetByIdAsync(failedExecution.JobId);
            if (job is null)
                continue;

            if (!await _retryService.ShouldRetryAsync(job, failedExecution))
            {
                job.Status = JobStatus.FailedPermanently;
                _jobRepository.Update(job);
                await _jobRepository.SaveChangesAsync();
                continue;
            }

            var nextRetryTime = _retryService.CalculateNextRetryTime(job, failedExecution);
            if (nextRetryTime > DateTime.UtcNow)
                continue;

            var retryExecution = _retryService.CreateRetryExecution(job, failedExecution);
            await _executionRepository.AddAsync(retryExecution);
            await _executionRepository.SaveChangesAsync();

            retryExecutions.Add(retryExecution);
        }

        return retryExecutions;
    }

    /// <summary>
    /// Updates a job's schedule (cron expression).
    /// </summary>
    public async Task<Job> UpdateJobScheduleAsync(Guid jobId, string newCronExpression, string? changedBy = null)
    {
        var job = await _jobRepository.GetByIdAsync(jobId);
        if (job is null)
            throw new JobNotFoundException(jobId);

        // Fix: Add validation for newCronExpression to prevent null, empty, or whitespace values.
        if (string.IsNullOrWhiteSpace(newCronExpression))
            throw new ArgumentException("New cron expression cannot be null or whitespace.", nameof(newCronExpression));

        if (!_cronService.IsValidCronExpression(newCronExpression))
            throw new CronExpressionException(newCronExpression, "Invalid cron expression");

        var oldCron = job.CronExpression;
        job.CronExpression = newCronExpression;
        job.NextExecutionAt = _cronService.GetNextExecutionTime(newCronExpression);
        job.MarkAsUpdated(changedBy);

        _jobRepository.Update(job);
        await _jobRepository.SaveChangesAsync();

        // Record the change
        var history = JobScheduleHistory.CreateCronChange(jobId, oldCron, newCronExpression, changedBy);
        // Note: Add to context and save if using history tracking

        _logger?.LogInformation("Job {JobId} schedule updated from '{OldCron}' to '{NewCron}'", jobId, oldCron, newCronExpression);

        return job;
    }

    /// <summary>
    /// Suspends a job to prevent further execution.
    /// </summary>
    public async Task<Job> SuspendJobAsync(Guid jobId, string? reason = null, string? suspendedBy = null)
    {
        var job = await _jobRepository.GetByIdAsync(jobId);
        if (job is null)
            throw new JobNotFoundException(jobId);

        // Fix: Validate reason to ensure it's not an empty or whitespace string if provided.
        if (!string.IsNullOrEmpty(reason) && string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason cannot be an empty or whitespace string if provided.", nameof(reason));

        var oldStatus = job.Status;
        job.Status = JobStatus.Suspended;
        job.MarkAsUpdated(suspendedBy);

        _jobRepository.Update(job);
        await _jobRepository.SaveChangesAsync();

        _logger?.LogInformation("Job {JobId} suspended (reason: {Reason})", jobId, reason ?? "Not specified");

        return job;
    }

    /// <summary>
    /// Resumes a suspended job.
    /// </summary>
    public async Task<Job> ResumeJobAsync(Guid jobId, string? resumedBy = null)
    {
        var job = await _jobRepository.GetByIdAsync(jobId);
        if (job is null)
            throw new JobNotFoundException(jobId);

        // Fix: Validate resumedBy to ensure it's not an empty or whitespace string if provided.
        if (!string.IsNullOrEmpty(resumedBy) && string.IsNullOrWhiteSpace(resumedBy))
            throw new ArgumentException("ResumedBy cannot be an empty or whitespace string if provided.", nameof(resumedBy));

        job.Status = JobStatus.Scheduled;
        job.NextExecutionAt = _cronService.GetNextExecutionTime(job.CronExpression);
        job.MarkAsUpdated(resumedBy);

        _jobRepository.Update(job);
        await _jobRepository.SaveChangesAsync();

        _logger?.LogInformation("Job {JobId} resumed", jobId);

        return job;
    }

    /// <summary>
    /// Deletes a job and all associated executions.
    /// </summary>
    public async Task DeleteJobAsync(Guid jobId)
    {
        var job = await _jobRepository.GetByIdAsync(jobId);
        if (job is null)
            throw new JobNotFoundException(jobId);

        _jobRepository.Remove(job);
        await _jobRepository.SaveChangesAsync();

        _logger?.LogInformation("Job {JobId} deleted", jobId);
    }

    /// <summary>
    /// Gets detailed information about a job.
    /// </summary>
    public async Task<JobDetailsDto> GetJobDetailsAsync(Guid jobId)
    {
        var job = await _jobRepository.GetByIdAsync(jobId);
        if (job is null)
            throw new JobNotFoundException(jobId);

        var executions = await _executionRepository.GetExecutionsByJobAsync(jobId);
        var stats = await _executorService.GetExecutionStatisticsAsync(jobId);

        return new JobDetailsDto
        {
            Job = job,
            ExecutionStatistics = stats,
            LastExecution = executions.FirstOrDefault(),
            TotalExecutions = executions.Count(),
            NextScheduledTime = job.NextExecutionAt
        };
    }

    /// <summary>
    /// Gets system-wide scheduler statistics.
    /// </summary>
    public async Task<SchedulerStatisticsDto> GetSchedulerStatisticsAsync()
    {
        var allJobs = await _jobRepository.GetAllAsync();
        var runningExecutions = await _executionRepository.GetRunningExecutionsAsync();
        var concurrencyStats = _concurrencyManager.GetConcurrencyStats();

        return new SchedulerStatisticsDto
        {
            TotalJobs = allJobs.Count(),
            ActiveJobs = allJobs.Count(j => j.IsActive),
            RunningExecutions = runningExecutions.Count(),
            TotalExecutions = allJobs.Sum(j => j.TotalExecutions),
            SuccessfulExecutions = allJobs.Sum(j => j.SuccessfulExecutions),
            FailedExecutions = allJobs.Sum(j => j.FailedExecutions),
            AverageSuccessRate = allJobs.Any() ? allJobs.Average(j => j.GetSuccessRate()) : 0,
            ConcurrencyStats = concurrencyStats
        };
    }
}

public sealed class JobDetailsDto
{
    public Job Job { get; set; } = null!;
    public ExecutionStatistics ExecutionStatistics { get; set; } = null!;
    public JobExecution? LastExecution { get; set; }
    public int TotalExecutions { get; set; }
    public DateTime? NextScheduledTime { get; set; }
}

public sealed class SchedulerStatisticsDto
{
    public int TotalJobs { get; set; }
    public int ActiveJobs { get; set; }
    public int RunningExecutions { get; set; }
    public int TotalExecutions { get; set; }
    public int SuccessfulExecutions { get; set; }
    public int FailedExecutions { get; set; }
    public double AverageSuccessRate { get; set; }
    public Dictionary<string, int> ConcurrencyStats { get; set; } = new();
}
