#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using JobScheduler.Core.Constants;
using JobScheduler.Core.Data.Repositories;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Events;
using JobScheduler.Core.Exceptions;

namespace JobScheduler.Core.Services;

/// <summary>
/// Pluggable handler abstraction for executing a job's actual work.
/// Used by callers (e.g. benchmarks, custom hosts) that want to supply their
/// own execution logic instead of relying on <see cref="JobExecutorService"/>'s
/// default handler-type dispatch.
/// </summary>
public interface IJobHandler
{
    /// <summary>Executes the job and returns a result/output string.</summary>
    Task<string> ExecuteAsync(Job job, CancellationToken cancellationToken);
}

/// <summary>
/// Service for executing job handlers and managing execution lifecycle.
/// Handles timeouts, error capture, and execution state management.
/// </summary>
public class JobExecutorService
{
    private readonly IJobRepository _jobRepository;
    private readonly IExecutionRepository _executionRepository;
    private readonly ConcurrencyManager _concurrencyManager;
    private readonly IEventPublisher? _eventPublisher;
    private readonly ILogger<JobExecutorService>? _logger;

    /// <summary>
    /// Creates an executor service without a logger.
    /// </summary>
    /// <exception cref="ArgumentNullException">A dependency is null.</exception>
    public JobExecutorService(
        IJobRepository jobRepository,
        IExecutionRepository executionRepository,
        ConcurrencyManager concurrencyManager)
        : this(jobRepository, executionRepository, concurrencyManager, null, null)
    {
    }

    /// <summary>
    /// Creates an executor service.
    /// </summary>
    /// <exception cref="ArgumentNullException">A dependency is null.</exception>
    public JobExecutorService(
        IJobRepository jobRepository,
        IExecutionRepository executionRepository,
        ConcurrencyManager concurrencyManager,
        ILogger<JobExecutorService>? logger)
        : this(jobRepository, executionRepository, concurrencyManager, logger, null)
    {
    }

    /// <summary>
    /// Creates an executor service.
    /// </summary>
    /// <exception cref="ArgumentNullException">A dependency is null.</exception>
    public JobExecutorService(
        IJobRepository jobRepository,
        IExecutionRepository executionRepository,
        ConcurrencyManager concurrencyManager,
        ILogger<JobExecutorService>? logger,
        IEventPublisher? eventPublisher)
    {
        _jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
        _executionRepository = executionRepository ?? throw new ArgumentNullException(nameof(executionRepository));
        _concurrencyManager = concurrencyManager ?? throw new ArgumentNullException(nameof(concurrencyManager));
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    /// <summary>
    /// Executes a job with timeout, error handling, and state management.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="job"/> is null.</exception>
    /// <exception cref="ConcurrencyException">The job cannot run because a concurrency limit is reached.</exception>
    public virtual async Task<JobExecution> ExecuteJobAsync(Job job, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);

        // Concurrency is checked before an execution record exists: persisting a record for a run
        // that never starts would corrupt the history and the success-rate metrics.
        await _concurrencyManager.EnsureCanExecuteAsync(job);

        return await RunWithRetryAsync(job, handler: null, Environment.MachineName, cancellationToken);
    }

    /// <summary>
    /// Executes a job using an explicitly supplied <see cref="IJobHandler"/> instead of the
    /// default handler-type dispatch. Useful for tests and benchmarks that want to inject
    /// custom execution logic without registering a handler type.
    /// </summary>
    public virtual async Task<JobExecution> ExecuteJobAsync(Job job, IJobHandler handler, string executorName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentException.ThrowIfNullOrWhiteSpace(executorName);

        await _concurrencyManager.EnsureCanExecuteAsync(job);

        return await RunWithRetryAsync(job, handler, executorName, cancellationToken);
    }

    /// <summary>
    /// Shared execution pipeline with retry logic: records the attempt, runs the handler under a timeout,
    /// classifies the outcome, publishes events, and always releases the concurrency slot.
    /// </summary>
    private async Task<JobExecution> RunWithRetryAsync(Job job, IJobHandler? handler, string executorName, CancellationToken cancellationToken)
    {
        var retryPolicy = job.GetEffectiveRetryPolicy();
        JobExecution? finalExecution = null;
        int attemptNumber = 0;
        bool willRetry = false;

        // Track if we should publish events
        var shouldPublishEvents = _eventPublisher != null;

        do
        {
            attemptNumber++;
            willRetry = false;

            var execution = new JobExecution
            {
                Id = Guid.NewGuid(),
                JobId = job.Id,
                Status = ExecutionStatus.Running,
                ExecutorName = executorName,
                AttemptNumber = attemptNumber
            };

            _concurrencyManager.IncrementConcurrencyCount(job.Id);

            try
            {
                await _executionRepository.AddAsync(execution);
                await _executionRepository.SaveChangesAsync();

                _logger?.LogInformation("Starting execution {ExecutionId} for job {JobId} ({JobName}) - Attempt {Attempt}",
                    execution.Id, job.Id, job.Name, attemptNumber);

                if (shouldPublishEvents)
                {
                    await _eventPublisher!.PublishAsync(new JobExecutionStartedEvent
                    {
                        JobId = job.Id,
                        ExecutionId = execution.Id,
                        JobName = job.Name
                    });
                }

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(job.ExecutionTimeoutSeconds));

                var stopwatch = Stopwatch.StartNew();

                try
                {
                    var result = handler is null
                        ? await ExecuteJobHandlerAsync(job, execution, cts.Token)
                        : await handler.ExecuteAsync(job, cts.Token);

                    stopwatch.Stop();
                    execution.SetOutput(result);
                    execution.MarkAsCompleted(ExecutionStatus.Success);

                    _logger?.LogInformation("Job {JobId} execution {ExecutionId} completed successfully in {Duration}ms",
                        job.Id, execution.Id, stopwatch.ElapsedMilliseconds);

                    if (shouldPublishEvents)
                    {
                        await _eventPublisher!.PublishAsync(new JobExecutionCompletedEvent
                        {
                            JobId = job.Id,
                            ExecutionId = execution.Id,
                            JobName = job.Name,
                            Success = true,
                            ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                            ErrorMessage = null
                        });
                    }

                    finalExecution = execution;
                    break; // Success - exit retry loop
                }
                catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
                {
                    stopwatch.Stop();
                    execution.MarkAsCompleted(ExecutionStatus.Cancelled);
                    execution.ErrorMessage = "Execution cancelled by caller";

                    _logger?.LogWarning(ex, "Job {JobId} execution {ExecutionId} was cancelled after {Duration}ms",
                        job.Id, execution.Id, stopwatch.ElapsedMilliseconds);

                    if (shouldPublishEvents)
                    {
                        // Check if cancellation should be retried based on policy
                        bool shouldRetry = retryPolicy.RetryOnCancellation && attemptNumber <= retryPolicy.MaxRetries;
                        willRetry = shouldRetry;

                        await _eventPublisher!.PublishAsync(new JobExecutionFailedEvent
                        {
                            JobId = job.Id,
                            ExecutionId = execution.Id,
                            JobName = job.Name,
                            ErrorMessage = execution.ErrorMessage ?? "Execution cancelled",
                            RetryAttempt = attemptNumber,
                            WillRetry = willRetry
                        });
                    }

                    finalExecution = execution;
                    break;
                }
                catch (OperationCanceledException ex)
                {
                    // This is a timeout-based cancellation (not graceful shutdown)
                    stopwatch.Stop();
                    execution.MarkAsCompleted(ExecutionStatus.TimedOut);
                    execution.ErrorMessage = $"Execution timed out after {job.ExecutionTimeoutSeconds} seconds";

                    _logger?.LogWarning(ex, "Job {JobId} execution {ExecutionId} timed out after {Duration}ms",
                        job.Id, execution.Id, stopwatch.ElapsedMilliseconds);

                    if (shouldPublishEvents)
                    {
                        var timeoutRetryable = retryPolicy.RetryOnTimeout;
                        willRetry = timeoutRetryable && attemptNumber <= retryPolicy.MaxRetries;

                        await _eventPublisher!.PublishAsync(new JobExecutionFailedEvent
                        {
                            JobId = job.Id,
                            ExecutionId = execution.Id,
                            JobName = job.Name,
                            ErrorMessage = execution.ErrorMessage,
                            RetryAttempt = attemptNumber,
                            WillRetry = willRetry
                        });
                    }

                    finalExecution = execution;
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    execution.MarkAsFailed(ex.Message, ex.StackTrace, true);

                    _logger?.LogError(ex, "Job {JobId} execution {ExecutionId} failed with error: {Message}",
                        job.Id, execution.Id, ex.Message);

                    if (shouldPublishEvents)
                    {
                        // Determine if this exception type should be retried
                        bool shouldRetry = retryPolicy.ShouldRetryOnException(ex.GetType().FullName ?? ex.GetType().Name);
                        willRetry = shouldRetry && attemptNumber <= retryPolicy.MaxRetries;

                        await _eventPublisher!.PublishAsync(new JobExecutionFailedEvent
                        {
                            JobId = job.Id,
                            ExecutionId = execution.Id,
                            JobName = job.Name,
                            ErrorMessage = ex.Message,
                            RetryAttempt = attemptNumber,
                            WillRetry = willRetry
                        });
                    }
                }
            }
            finally
            {
                _concurrencyManager.DecrementConcurrencyCount(job.Id);

                job.UpdateExecutionMetrics(finalExecution?.Status == ExecutionStatus.Success);
                job.LastExecutedAt = DateTime.UtcNow;

                _executionRepository.Update(execution);
                _jobRepository.Update(job);
                await _executionRepository.SaveChangesAsync();
            }

            // If execution failed and we should retry, wait for the backoff period
            if (finalExecution?.Status == ExecutionStatus.Failed && willRetry && attemptNumber <= retryPolicy.MaxRetries)
            {
                int backoffSeconds = retryPolicy.CalculateBackoffDelay(attemptNumber);
                _logger?.LogInformation("Job {JobId} will retry in {BackoffSeconds}s (attempt {Attempt}/{MaxAttempts})",
                    job.Id, backoffSeconds, attemptNumber, retryPolicy.MaxRetries);

                await Task.Delay(TimeSpan.FromSeconds(backoffSeconds), cancellationToken);
            }

        } while (willRetry && attemptNumber <= retryPolicy.MaxRetries);

        // If all retries exhausted and execution failed, publish exhausted event
        if (finalExecution?.Status == ExecutionStatus.Failed && attemptNumber > retryPolicy.MaxRetries)
        {
            if (shouldPublishEvents)
            {
                await _eventPublisher!.PublishAsync(new JobExecutionExhaustedEvent
                {
                    JobId = job.Id,
                    ExecutionId = finalExecution.Id,
                    JobName = job.Name,
                    ErrorMessage = finalExecution.ErrorMessage ?? "All retry attempts exhausted",
                    TotalAttempts = attemptNumber,
                    MaxRetries = retryPolicy.MaxRetries
                });
            }
        }

        return finalExecution ?? throw new ExecutionException("Job execution failed to produce a result", Guid.Empty, job.Id);
    }

    /// <summary>
    /// Resolves <see cref="Job.HandlerType"/> to a type implementing <see cref="IJobHandler"/>
    /// instantiates it and executes it. Override to plug in a container-based handler factory.
    /// </summary>
    /// <exception cref="ExecutionException">
    /// The handler type is missing, cannot be resolved, does not implement <see cref="IJobHandler"/>
    /// or cannot be instantiated.
    /// </exception>
    protected virtual async Task<string> ExecuteJobHandlerAsync(Job job, JobExecution execution, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(execution);

        var handler = CreateHandler(job, execution);
        var output = await handler.ExecuteAsync(job, cancellationToken).ConfigureAwait(false);

        return output ?? string.Empty;
    }

    /// <summary>
    /// Resolves and instantiates the <see cref="IJobHandler"/> named by <see cref="Job.HandlerType"/>.
    /// The name may be an assembly-qualified name or a plain full name, in which case every loaded
    /// assembly is searched. A handler may declare a parameterless constructor or a constructor
    /// taking the raw <see cref="Job.HandlerParameters"/> string.
    /// </summary>
    private IJobHandler CreateHandler(Job job, JobExecution execution)
    {
        if (string.IsNullOrWhiteSpace(job.HandlerType))
            throw new ExecutionException("Job has no handler type configured", execution.Id, job.Id);

        var handlerType = ResolveHandlerType(job.HandlerType)
            ?? throw new ExecutionException($"Handler type '{job.HandlerType}' could not be resolved", execution.Id, job.Id);

        if (!typeof(IJobHandler).IsAssignableFrom(handlerType) || handlerType.IsAbstract || handlerType.IsInterface)
            throw new ExecutionException($"Handler type '{job.HandlerType}' does not implement {nameof(IJobHandler)}", execution.Id, job.Id);

        try
        {
            var withParameters = handlerType.GetConstructor([typeof(string)]);
            var instance = withParameters is not null
                ? withParameters.Invoke([job.HandlerParameters ?? string.Empty])
                : Activator.CreateInstance(handlerType);

            return instance as IJobHandler
                ?? throw new ExecutionException($"Handler type '{job.HandlerType}' could not be instantiated", execution.Id, job.Id);
        }
        catch (Exception ex) when (ex is not ExecutionException)
        {
            throw new ExecutionException($"Handler type '{job.HandlerType}' could not be instantiated", execution.Id, job.Id, ex);
        }
    }

    /// <summary>
    /// Resolves a type name against the assembly-qualified name first, then against the
    /// full names of the types in every assembly loaded into the current domain.
    /// </summary>
    private static Type? ResolveHandlerType(string handlerType)
    {
        var direct = Type.GetType(handlerType, throwOnError: false, ignoreCase: false);
        if (direct is not null)
            return direct;

        // The name may carry an assembly part that is not loadable here; match on the type name alone.
        var typeName = handlerType.Split(',', 2)[0].Trim();

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var candidate = assembly.GetType(typeName, throwOnError: false, ignoreCase: false);
            if (candidate is not null)
                return candidate;
        }

        return null;
    }

    /// <summary>
    /// Validates if a job can be executed immediately.
    /// </summary>
    public virtual async Task<(bool CanExecute, string? Reason)> ValidateJobForExecutionAsync(Job job)
    {
        if (job is null)
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
    public virtual async Task<ExecutionStatistics> GetExecutionStatisticsAsync(Guid jobId)
    {
        var executions = await _executionRepository.GetExecutionsByJobAsync(jobId);
        var job = await _jobRepository.GetByIdAsync(jobId);

        if (job is null)
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
public sealed class ExecutionStatistics
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