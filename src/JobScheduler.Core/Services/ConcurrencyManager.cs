#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using JobScheduler.Core.Constants;
using JobScheduler.Core.Data.Repositories;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Exceptions;

namespace JobScheduler.Core.Services;

/// <summary>
/// Manages concurrent job execution limits and ensures concurrency constraints.
/// Prevents overloading system with too many concurrent executions.
/// </summary>
public class ConcurrencyManager
{
    private readonly IExecutionRepository _executionRepository;
    private readonly ConcurrentDictionary<Guid, int> _jobConcurrencyCache;
    private readonly int _maxGlobalConcurrency;
    private int _globalRunningCount;
    private readonly ILogger<ConcurrencyManager>? _logger;

    /// <summary>
    /// Creates a concurrency manager with the default global limit and no logger.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="executionRepository"/> is null.</exception>
    public ConcurrencyManager(IExecutionRepository executionRepository)
        : this(executionRepository, SchedulerConstants.DefaultMaxConcurrentJobs, null)
    {
    }

    /// <summary>
    /// Creates a concurrency manager.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="executionRepository"/> is null.</exception>
    public ConcurrencyManager(IExecutionRepository executionRepository, int maxGlobalConcurrency = SchedulerConstants.DefaultMaxConcurrentJobs, ILogger<ConcurrencyManager>? logger = null)
    {
        _executionRepository = executionRepository ?? throw new ArgumentNullException(nameof(executionRepository));
        _maxGlobalConcurrency = maxGlobalConcurrency;
        _jobConcurrencyCache = new ConcurrentDictionary<Guid, int>();
        _globalRunningCount = 0;
        _logger = logger;
        _logger?.LogInformation("ConcurrencyManager initialized with max global concurrency: {MaxGlobalConcurrency}", maxGlobalConcurrency);
    }

    /// <summary>
    /// Checks if a job can execute based on concurrency constraints.
    /// </summary>
    public virtual async Task<bool> CanExecuteAsync(Job job)
    {
        if (job is null)
        {
            _logger?.LogError("CanExecuteAsync called with null job");
            throw new ArgumentNullException(nameof(job));
        }

        _logger?.LogDebug("Checking concurrency limits for job {JobId} (max: {MaxConcurrent})", job.Id, job.MaxConcurrentExecutions);

        // Check global concurrency limit
        var globalCount = await _executionRepository.GetConcurrentRunningCountAsync();
        _logger?.LogDebug("Current global concurrency: {GlobalCount}/{MaxGlobalConcurrency}", globalCount, _maxGlobalConcurrency);
        if (globalCount >= _maxGlobalConcurrency)
        {
            _logger?.LogWarning("Job {JobId} blocked: global concurrency limit reached ({GlobalCount}/{MaxGlobalConcurrency})", job.Id, globalCount, _maxGlobalConcurrency);
            return false;
        }

        // Check job-specific concurrency limit
        var jobSpecificCount = await _executionRepository.GetCurrentlyRunningCountAsync(job.Id);
        _logger?.LogDebug("Current job concurrency for {JobId}: {JobCount}/{MaxConcurrent}", job.Id, jobSpecificCount, job.MaxConcurrentExecutions);
        if (jobSpecificCount >= job.MaxConcurrentExecutions)
        {
            _logger?.LogWarning("Job {JobId} blocked: job-specific concurrency limit reached ({JobCount}/{MaxConcurrent})", job.Id, jobSpecificCount, job.MaxConcurrentExecutions);
            return false;
        }

        _logger?.LogDebug("Job {JobId} can execute: concurrency checks passed", job.Id);
        return true;
    }

    /// <summary>
    /// Ensures a job can execute; throws if it cannot due to concurrency limits.
    /// </summary>
    public virtual async Task EnsureCanExecuteAsync(Job job)
    {
        if (!await CanExecuteAsync(job))
        {
            var currentCount = await _executionRepository.GetCurrentlyRunningCountAsync(job.Id);
            _logger?.LogError("Concurrency limit exceeded for job {JobId}: {CurrentCount}/{MaxConcurrent}", job.Id, currentCount, job.MaxConcurrentExecutions);
            throw new ConcurrencyException(job.Id, currentCount, job.MaxConcurrentExecutions);
        }

        _logger?.LogDebug("Job {JobId} concurrency check passed", job.Id);
    }

    /// <summary>
    /// Increments the concurrency counter for a job.
    /// </summary>
    public virtual void IncrementConcurrencyCount(Guid jobId)
    {
        var newCount = _jobConcurrencyCache.AddOrUpdate(jobId, 1, (_, current) => current + 1);
        Interlocked.Increment(ref _globalRunningCount);
        _logger?.LogDebug("Incremented concurrency count for job {JobId}: {NewCount}", jobId, newCount);
    }

    /// <summary>
    /// Decrements the concurrency counter for a job.
    /// </summary>
    public virtual void DecrementConcurrencyCount(Guid jobId)
    {
        var oldCount = _jobConcurrencyCache.AddOrUpdate(jobId, 0, (_, current) => Math.Max(0, current - 1));

        // Compare-and-swap keeps the global counter from going negative when several
        // executions finish at the same time (a plain check-then-decrement races).
        int observed;
        do
        {
            observed = Volatile.Read(ref _globalRunningCount);
            if (observed == 0)
                break;
        }
        while (Interlocked.CompareExchange(ref _globalRunningCount, observed - 1, observed) != observed);

        _logger?.LogDebug("Decremented concurrency count for job {JobId}: was {OldCount}", jobId, oldCount);
    }

    /// <summary>
    /// Gets the current concurrency count for a job.
    /// </summary>
    public virtual int GetJobConcurrencyCount(Guid jobId)
    {
        return _jobConcurrencyCache.TryGetValue(jobId, out var count) ? count : 0;
    }

    /// <summary>
    /// Gets the global concurrency count.
    /// </summary>
    public virtual int GetGlobalConcurrencyCount()
    {
        return Volatile.Read(ref _globalRunningCount);
    }

    /// <summary>
    /// Clears cache and synchronizes with database state.
    /// Should be called periodically or on system startup.
    /// </summary>
    public virtual async Task SynchronizeWithDatabaseAsync()
    {
        _jobConcurrencyCache.Clear();

        var runningExecutions = (await _executionRepository.GetRunningExecutionsAsync()).ToList();

        foreach (var group in runningExecutions.GroupBy(e => e.JobId))
        {
            _jobConcurrencyCache[group.Key] = group.Count();
        }

        // The repository count is authoritative: it also covers executions started by other nodes.
        Volatile.Write(ref _globalRunningCount, await _executionRepository.GetConcurrentRunningCountAsync());
    }

    /// <summary>
    /// Gets statistics about current concurrency state.
    /// </summary>
    public virtual Dictionary<string, int> GetConcurrencyStats()
    {
        var stats = new Dictionary<string, int>
        {
            { "GlobalRunning", Volatile.Read(ref _globalRunningCount) },
            { "GlobalLimit", _maxGlobalConcurrency },
            { "JobsWithExecutions", _jobConcurrencyCache.Count(x => x.Value > 0) },
            { "TotalCachedJobs", _jobConcurrencyCache.Count }
        };

        _logger?.LogDebug("Concurrency stats: {Stats}", string.Join(", ", stats.Select(kv => $"{kv.Key}={kv.Value}")));
        return stats;
    }
}
