// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using JobScheduler.Core.Constants;
using JobScheduler.Core.Data.Repositories;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Exceptions;

namespace JobScheduler.Core.Services;

/// <summary>
/// Service for executing job handlers and managing execution lifecycle.
/// Handles timeouts, error capture, and execution state management.
/// </summary>
public class JobExecutorService
{
    private readonly IJobRepository _jobRepository;
    private readonly IExecutionRepository _executionRepository;
    private readonly ConcurrencyManager _concurrencyManager;
    private readonly ILogger<JobExecutorService>? _logger;

    public JobExecutorService(
        IJobRepository jobRepository,
        IExecutionRepository executionRepository,
        ConcurrencyManager concurrencyManager,
        ILogger<JobExecutorService>? logger = null)
    {
        _jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
        _executionRepository = executionRepository ?? throw new ArgumentNullException(nameof(executionRepository));
        _concurrencyManager = concurrencyManager ?? throw new ArgumentNullException(nameof(concurrencyManager));
        _logger = logger;
    }

    /// <summary>
    /// Executes a job with timeout, error handling, and state management.
    /// </summary>
    public async Task<JobExecution> ExecuteJobAsync(Job job, CancellationToken cancellationToken = default)
    {
        if (job == null)
            throw new ArgumentNullException(nameof(job));

        var execution = new JobExecution
        {
            Id = Guid.NewGuid(),
            JobId = job.Id,
            Status = ExecutionStatus.Running,
            ExecutorName = Environment.MachineName,
            AttemptNumber = 1
        };

        try
        {
            // Check concurrency limits
            await _concurrencyManager.EnsureCanExecuteAsync(job);
            _concurrencyManager.IncrementConcurrencyCount(job.Id);

            // Create execution record
            await _executionRepository.AddAsync(execution);
            await _executionRepository.SaveChangesAsync();

            _logger?.LogInformation("Starting execution {ExecutionId} for job {JobId} ({JobName})",
                execution.Id, job.Id, job.Name);

            // Execute with timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(job.ExecutionTimeoutSeconds));

            var stopwatch = Stopwatch.StartNew();
            var jobTask = ExecuteJobHandlerAsync(job, execution, cts.Token);

            try
            {
                var result = await jobTask;
                stopwatch.Stop();
                execution.SetOutput(result);
                execution.MarkAsCompleted(ExecutionStatus.Success);

                _logger?.LogInformation("Job {JobId} execution {ExecutionId} completed successfully in {Duration}ms",
                    job.Id, execution.Id, stopwatch.ElapsedMilliseconds);
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                execution.MarkAsCompleted(ExecutionStatus.TimedOut);
                execution.ErrorMessage = $"Execution timed out after {job.ExecutionTimeoutSeconds} seconds";

                _logger?.LogWarning("Job {JobId} execution {ExecutionId} timed out after {Duration}ms",
                    job.Id, execution.Id, stopwatch.ElapsedMilliseconds);
            }
        }
        catch (ConcurrencyException ex)
        {
            _logger?.LogWarning("Job {JobId} rejected due to concurrency limits: {Message}", job.Id, ex.Message);
            execution.Status = ExecutionStatus.Skipped;
            execution.ErrorMessage = "Execution skipped due to concurrency limits";
        }
        catch (Exception ex)
        {
            execution.MarkAsFailed(ex.Message, ex.StackTrace, true);

            _logger?.LogError(ex, "Job {JobId} execution {ExecutionId} failed with error: {Message}",
                job.Id, execution.Id, ex.Message);
        }
        finally
        {
            _concurrencyManager.DecrementConcurrencyCount(job.Id);

            // Update job metrics
            job.UpdateExecutionMetrics(execution.Status == ExecutionStatus.Success);
            job.LastExecutedAt = DateTime.UtcNow;

            // Save execution record
            _executionRepository.Update(execution);
            _jobRepository.Update(job);
            await _executionRepository.SaveChangesAsync();
        }

        return execution;
    }

    /// <summary>
    /// Executes the actual job handler based on handler type.
    /// Override or extend this to support custom job handlers.
    /// </summary>
    protected virtual async Task<string> ExecuteJobHandlerAsync(Job job, JobExecution execution, CancellationToken cancellationToken)
    {
        // This is a placeholder implementation
        // In a real system, this would instantiate and execute the handler type
        // based on job.HandlerType, passing job.HandlerParameters

        await Task.Delay(100, cancellationToken);

        return $"Job {job.Name} executed successfully";
    }

    /// <summary>
    /// Validates if a job can be executed immediately.
    /// </summary>
    public async Task<(bool CanExecute, string? Reason)> ValidateJobForExecutionAsync(Job job)
    {
        if (job == null)
            return (false, "Job is null");

        if (!job.IsActive)
            return (false, "Job is not active");

        if (job.Status == JobStatus.Suspended)
            return (false, "Job is suspended");

        if (job.Status == JobStatus.Cancelled)
            return (false, "Job is cancelled");

        if (job.Status == JobStatus.FailedPermanently)
            return (false, "Job has failed permanently");

        if (!await _concurrencyManager.CanExecuteAsync(job))
            return (false, "Concurrency limit reached");

        if (string.IsNullOrWhiteSpace(job.HandlerType))
            return (false, "No handler type specified");

        return (true, null);
    }

    /// <summary>
    /// Gets execution statistics for a job.
    /// </summary>
    public async Task<ExecutionStatistics> GetExecutionStatisticsAsync(Guid jobId)
    {
        var executions = await _executionRepository.GetExecutionsByJobAsync(jobId);
        var job = await _jobRepository.GetByIdAsync(jobId);

        if (job == null)
            throw new JobNotFoundException(jobId);

        var successfulExecutions = executions.Where(e => e.Status == ExecutionStatus.Success).ToList();

        return new ExecutionStatistics
        {
            JobId = jobId,
            TotalExecutions = executions.Count(),
            SuccessfulExecutions = successfulExecutions.Count,
            FailedExecutions = executions.Count(e => e.Status == ExecutionStatus.Failed),
            TimedOutExecutions = executions.Count(e => e.Status == ExecutionStatus.TimedOut),
            SkippedExecutions = executions.Count(e => e.Status == ExecutionStatus.Skipped),
            AverageDurationMs = successfulExecutions.Any() ? (long)successfulExecutions.Average(e => e.DurationMilliseconds) : 0,
            SuccessRate = job.GetSuccessRate()
        };
    }
}

/// <summary>
/// Statistics about job execution performance.
/// </summary>
public class ExecutionStatistics
{
    public Guid JobId { get; set; }
    public int TotalExecutions { get; set; }
    public int SuccessfulExecutions { get; set; }
    public int FailedExecutions { get; set; }
    public int TimedOutExecutions { get; set; }
    public int SkippedExecutions { get; set; }
    public long AverageDurationMs { get; set; }
    public double SuccessRate { get; set; }
}
