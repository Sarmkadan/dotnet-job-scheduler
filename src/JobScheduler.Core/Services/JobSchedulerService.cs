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
using JobScheduler.Core.Controllers;
using JobScheduler.Core.Data.Repositories;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Domain.Models;
using JobScheduler.Core.Exceptions;

namespace JobScheduler.Core.Services;

/// <summary>
/// Central orchestrator for the job scheduler system.
/// Manages job scheduling, execution, retries, and monitoring.
/// </summary>
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
        job.NextExecutionAt = string.IsNullOrWhiteSpace(job.TimeZoneId)
            ? _cronService.GetNextExecutionTime(job.CronExpression)
            : _cronService.GetNextExecutionTimeInZone(job.CronExpression, job.TimeZoneId);

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
    var misfiredJobs = await _jobRepository.GetMisfiredJobsAsync();
    var executions = new List<JobExecution>();

    // Process regular due jobs first
    foreach (var job in dueJobs)
    {
        if (cancellationToken.IsCancellationRequested)
            break;

        try
        {
            // Ask before executing: the executor throws when a limit is hit, and a job that is
            // merely saturated is not an error worth logging on every scheduler tick.
            if (!await _concurrencyManager.CanExecuteAsync(job))
            {
                _logger?.LogDebug("Job {JobId} skipped: concurrency limit reached", job.Id);
                continue;
            }

            var execution = await _executorService.ExecuteJobAsync(job, cancellationToken);
            executions.Add(execution);

            // Schedule next execution
            if (job.IsActive && job.Status != JobStatus.FailedPermanently)
            {
                job.NextExecutionAt = string.IsNullOrWhiteSpace(job.TimeZoneId)
                    ? _cronService.GetNextExecutionTime(job.CronExpression, DateTime.UtcNow)
                    : _cronService.GetNextExecutionTimeInZone(job.CronExpression, job.TimeZoneId, DateTime.UtcNow);
                _jobRepository.Update(job);
                await _jobRepository.SaveChangesAsync();
            }
        }
        catch (ConcurrencyException ex)
        {
            // ConcurrencyException means another scheduler instance claimed this job.
            // This is expected in a distributed environment and should not be treated as an error.
            _logger?.LogDebug(ex, "Job {JobId} skipped: another scheduler instance claimed the job", job.Id);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error executing job {JobId}", job.Id);
        }
    }

    // Process misfired jobs with misfire policy
    foreach (var job in misfiredJobs)
    {
        if (cancellationToken.IsCancellationRequested)
            break;

        try
        {
            if (!await _concurrencyManager.CanExecuteAsync(job))
            {
                _logger?.LogDebug("Job {JobId} skipped: concurrency limit reached", job.Id);
                continue;
            }

            // Apply misfire policy
            switch (job.MisfirePolicy)
            {
                case MisfirePolicy.FireOnceNow:
                    _logger?.LogInformation("Job {JobId} ({JobName}) misfired but will execute once now (MisfirePolicy.FireOnceNow)", job.Id, job.Name);
                    var execution = await _executorService.ExecuteJobAsync(job, cancellationToken);
                    executions.Add(execution);
                    break;

                case MisfirePolicy.SkipToNext:
                    _logger?.LogInformation("Job {JobId} ({JobName}) misfired - skipping to next execution (MisfirePolicy.SkipToNext)", job.Id, job.Name);
                    // Update next execution time to the next scheduled time from the original NextExecutionAt
                    if (job.NextExecutionAt.HasValue)
                    {
                        job.NextExecutionAt = string.IsNullOrWhiteSpace(job.TimeZoneId)
                            ? _cronService.GetNextExecutionTime(job.CronExpression, job.NextExecutionAt.Value)
                            : _cronService.GetNextExecutionTimeInZone(job.CronExpression, job.TimeZoneId, job.NextExecutionAt.Value);
                        _jobRepository.Update(job);
                        await _jobRepository.SaveChangesAsync();
                    }
                    break;

                case MisfirePolicy.FireAll:
                    _logger?.LogInformation("Job {JobId} ({JobName}) misfired - will execute all missed occurrences (MisfirePolicy.FireAll)", job.Id, job.Name);
                    // For FireAll, we execute once now and schedule the next occurrence
                    var fireAllExecution = await _executorService.ExecuteJobAsync(job, cancellationToken);
                    executions.Add(fireAllExecution);
                    break;
            }
        }
        catch (ConcurrencyException ex)
        {
            _logger?.LogDebug(ex, "Job {JobId} skipped: another scheduler instance claimed the job", job.Id);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling misfired job {JobId}", job.Id);
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
        job.NextExecutionAt = string.IsNullOrWhiteSpace(job.TimeZoneId)
            ? _cronService.GetNextExecutionTime(job.CronExpression)
            : _cronService.GetNextExecutionTimeInZone(job.CronExpression, job.TimeZoneId);
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

        var allExecutions = await _executionRepository.GetAllAsync();
        var successfulExecutions = allExecutions.Where(e => e.Status == ExecutionStatus.Success).ToList();

        return new SchedulerStatisticsDto
        {
            TotalJobs = allJobs.Count(),
            ActiveJobs = allJobs.Count(j => j.IsActive),
            RunningExecutions = runningExecutions.Count(),
            TotalExecutions = allJobs.Sum(j => j.TotalExecutions),
            SuccessfulExecutions = allJobs.Sum(j => j.SuccessfulExecutions),
            FailedExecutions = allJobs.Sum(j => j.FailedExecutions),
            AverageSuccessRate = allJobs.Any() ? allJobs.Average(j => j.GetSuccessRate()) : 0,
            AverageExecutionTimeMs = successfulExecutions.Any() ? (long)successfulExecutions.Average(e => e.DurationMilliseconds) : 0,
            ConcurrencyStats = concurrencyStats
        };
    }

    /// <summary>
    /// Alias for <see cref="GetSchedulerStatisticsAsync"/> used by dashboard and health endpoints.
    /// </summary>
    public Task<SchedulerStatisticsDto> GetSystemStatisticsAsync() => GetSchedulerStatisticsAsync();

    /// <summary>
    /// Returns the most recent execution records for a job, newest first.
    /// </summary>
    /// <param name="jobId">The job identifier.</param>
    /// <param name="limit">Maximum number of records to return. Defaults to 20.</param>
    public async Task<IEnumerable<JobExecution>> GetExecutionHistoryAsync(Guid jobId, int limit = 20)
    {
        if (limit <= 0)
            limit = 20;

        var job = await _jobRepository.GetByIdAsync(jobId);
        if (job is null)
            throw new JobNotFoundException(jobId);

        var executions = await _executionRepository.GetExecutionsByJobAsync(jobId);
        return executions.Take(limit);
    }

    /// <summary>
    /// Retrieves a single job by ID, or null if it does not exist.
    /// </summary>
    public Task<Job?> GetJobByIdAsync(Guid jobId) => _jobRepository.GetByIdAsync(jobId);

    /// <summary>
    /// Retrieves a page of jobs, optionally filtered by status.
    /// </summary>
    public async Task<IEnumerable<Job>> GetJobsAsync(JobStatus? status, int pageNumber = 1, int pageSize = 10)
    {
        var jobs = status.HasValue
            ? await _jobRepository.GetJobsByStatusAsync(status.Value)
            : await _jobRepository.GetAllAsync();

        if (pageNumber < 1)
            pageNumber = 1;
        if (pageSize < 1)
            pageSize = 10;

        return jobs
            .OrderByDescending(j => j.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);
    }

    /// <summary>
    /// Counts jobs, optionally filtered by status.
    /// </summary>
    public async Task<int> GetTotalJobCountAsync(JobStatus? status)
    {
        var jobs = status.HasValue
            ? await _jobRepository.GetJobsByStatusAsync(status.Value)
            : await _jobRepository.GetAllAsync();

        return jobs.Count();
    }

    /// <summary>
    /// Updates an existing job's configuration from a create/update request.
    /// Returns null if the job does not exist.
    /// </summary>
    public async Task<Job?> UpdateJobAsync(Guid jobId, CreateJobRequest request, string? updatedBy = null)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var job = await _jobRepository.GetByIdAsync(jobId);
        if (job is null)
            return null;

        if (!_cronService.IsValidCronExpression(request.CronExpression))
            throw new CronExpressionException(request.CronExpression, "Invalid cron expression");

        job.Name = request.Name;
        job.Description = request.Description ?? string.Empty;
        job.CronExpression = request.CronExpression;
        job.TimeZoneId = request.TimeZoneId;
        job.HandlerType = request.HandlerType;
        job.HandlerParameters = request.HandlerParameters;
        job.Priority = request.Priority;
        job.MaxRetries = request.MaxRetries;
        job.RetryBackoffSeconds = request.RetryBackoffSeconds;
        job.ExecutionTimeoutSeconds = request.ExecutionTimeoutSeconds;
        job.MaxConcurrentExecutions = request.MaxConcurrentExecutions;
        job.IsActive = request.IsActive;

        if (!job.IsValidForScheduling())
            throw new JobValidationException("Job configuration is invalid");

        job.NextExecutionAt = string.IsNullOrWhiteSpace(job.TimeZoneId)
            ? _cronService.GetNextExecutionTime(job.CronExpression)
            : _cronService.GetNextExecutionTimeInZone(job.CronExpression, job.TimeZoneId);

        job.MarkAsUpdated(updatedBy);

        _jobRepository.Update(job);
        await _jobRepository.SaveChangesAsync();

        _logger?.LogInformation("Job {JobId} updated", jobId);

        return job;
    }

    /// <summary>
    /// Deletes a job and reports whether it existed.
    /// </summary>
    public async Task<bool> DeleteJobAsync(Guid jobId, string? deletedBy)
    {
        var job = await _jobRepository.GetByIdAsync(jobId);
        if (job is null)
            return false;

        _jobRepository.Remove(job);
        await _jobRepository.SaveChangesAsync();

        _logger?.LogInformation("Job {JobId} deleted by {DeletedBy}", jobId, deletedBy ?? "unknown");

        return true;
    }

    /// <summary>
    /// Immediately executes a job outside of its normal cron schedule.
    /// Returns null if the job does not exist.
    /// </summary>
    public async Task<JobExecution?> TriggerJobExecutionAsync(Guid jobId)
    {
        var job = await _jobRepository.GetByIdAsync(jobId);
        if (job is null)
            return null;

        return await _executorService.ExecuteJobAsync(job);
    }

    /// <summary>
    /// Retrieves a page of executions for a job, or null if the job does not exist.
    /// </summary>
    public async Task<IEnumerable<JobExecution>?> GetJobExecutionsAsync(Guid jobId, int pageNumber = 1, int pageSize = 20)
    {
        var job = await _jobRepository.GetByIdAsync(jobId);
        if (job is null)
            return null;

        if (pageNumber < 1)
            pageNumber = 1;
        if (pageSize < 1)
            pageSize = 20;

        var executions = await _executionRepository.GetExecutionsByJobAsync(jobId);
        return executions.Skip((pageNumber - 1) * pageSize).Take(pageSize);
    }

    /// <summary>
    /// Counts executions recorded for a job.
    /// </summary>
    public async Task<int> GetJobExecutionCountAsync(Guid jobId)
    {
        var executions = await _executionRepository.GetExecutionsByJobAsync(jobId);
        return executions.Count();
    }

    /// <summary>
    /// Retrieves a single execution by ID, or null if it does not exist.
    /// </summary>
    public Task<JobExecution?> GetExecutionByIdAsync(Guid executionId) => _executionRepository.GetByIdAsync(executionId);

    /// <summary>
    /// Retrieves the most recent failed executions across all jobs, since the given cutoff.
    /// </summary>
    public async Task<IEnumerable<JobExecution>> GetRecentFailedExecutionsAsync(DateTime cutoffDate, int limit = 50)
    {
        var failed = await _executionRepository.GetExecutionsByStatusAsync(ExecutionStatus.Failed);
        return failed
            .Where(e => e.StartedAt >= cutoffDate)
            .OrderByDescending(e => e.StartedAt)
            .Take(limit);
    }

    /// <summary>
    /// Deletes execution records older than the given cutoff date.
    /// Returns the number of deleted records.
    /// </summary>
    public async Task<int> DeleteExecutionsOlderThanAsync(DateTime cutoffDate)
    {
        var oldExecutions = (await _executionRepository.FindAsync(e => e.StartedAt < cutoffDate)).ToList();
        if (oldExecutions.Count == 0)
            return 0;

        _executionRepository.RemoveRange(oldExecutions);
        await _executionRepository.SaveChangesAsync();

        return oldExecutions.Count;
    }

    /// <summary>
    /// Counts jobs currently in the Running state, used for live dashboard metrics.
    /// </summary>
    public Task<int> GetRunningJobCountAsync() => _executionRepository.GetConcurrentRunningCountAsync();

    /// <summary>
    /// Counts jobs in a failed state (Failed or FailedPermanently).
    /// </summary>
    public async Task<int> GetFailedJobCountAsync()
    {
        var failedJobs = await _jobRepository.GetFailedJobsAsync();
        return failedJobs.Count();
    }

    /// <summary>
    /// Returns a snapshot of job counts grouped by lifecycle state.
    /// </summary>
    public async Task<QueueStatus> GetQueueStatusAsync()
    {
        var allJobs = (await _jobRepository.GetAllAsync()).ToList();

        return new QueueStatus
        {
            PendingCount = allJobs.Count(j => j.Status == JobStatus.Pending || j.Status == JobStatus.Scheduled),
            RunningCount = allJobs.Count(j => j.Status == JobStatus.Running),
            FailedCount = allJobs.Count(j => j.Status == JobStatus.Failed || j.Status == JobStatus.FailedPermanently),
            CompletedCount = allJobs.Count(j => j.Status == JobStatus.Completed),
            SuspendedCount = allJobs.Count(j => j.Status == JobStatus.Suspended)
        };
    }

    /// <summary>
    /// Returns the number of jobs at each priority level.
    /// </summary>
    public async Task<Dictionary<string, int>> GetJobPriorityDistributionAsync()
    {
        var allJobs = await _jobRepository.GetAllAsync();

        return new Dictionary<string, int>
        {
            ["Critical"] = allJobs.Count(j => j.Priority == JobPriority.Critical),
            ["High"] = allJobs.Count(j => j.Priority == JobPriority.High),
            ["Normal"] = allJobs.Count(j => j.Priority == JobPriority.Normal),
            ["Low"] = allJobs.Count(j => j.Priority == JobPriority.Low)
        };
    }

    /// <summary>
    /// Returns the jobs with the highest average execution time.
    /// </summary>
    public async Task<List<JobPerformanceSummary>> GetSlowestJobsAsync(int count = 10)
    {
        var allJobs = await _jobRepository.GetAllAsync();
        var allExecutions = await _executionRepository.GetAllAsync();
        var executionsByJob = allExecutions.GroupBy(e => e.JobId).ToDictionary(g => g.Key, g => g.ToList());

        var summaries = allJobs.Select(job =>
        {
            executionsByJob.TryGetValue(job.Id, out var jobExecutions);
            jobExecutions ??= new List<JobExecution>();

            return new JobPerformanceSummary
            {
                Id = job.Id,
                Name = job.Name,
                AverageExecutionTimeMs = jobExecutions.Count > 0 ? (long)jobExecutions.Average(e => e.DurationMilliseconds) : 0,
                MaxExecutionTimeMs = jobExecutions.Count > 0 ? jobExecutions.Max(e => e.DurationMilliseconds) : 0,
                TotalExecutions = job.TotalExecutions
            };
        });

        return summaries
            .OrderByDescending(s => s.AverageExecutionTimeMs)
            .Take(count)
            .ToList();
    }

    /// <summary>
    /// Returns the jobs with the highest number of failed executions.
    /// </summary>
    public async Task<List<Job>> GetMostFailingJobsAsync(int count = 10)
    {
        var allJobs = await _jobRepository.GetAllAsync();
        return allJobs
            .OrderByDescending(j => j.FailedExecutions)
            .Take(count)
            .ToList();
    }

    /// <summary>
    /// Verifies database connectivity by issuing a lightweight query.
    /// </summary>
    public async Task<bool> IsDatabaseConnectedAsync()
    {
        try
        {
            await _jobRepository.CountAsync();
            return true;
        }
        catch
        {
            return false;
        }
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
    public long AverageExecutionTimeMs { get; set; }
    public Dictionary<string, int> ConcurrencyStats { get; set; } = new();
}

/// <summary>
/// Aggregated performance data for a single job, used by dashboard reporting.
/// </summary>
public sealed class JobPerformanceSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public long AverageExecutionTimeMs { get; set; }
    public long MaxExecutionTimeMs { get; set; }
    public int TotalExecutions { get; set; }
}
