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
using JobScheduler.Core.Constants;
using JobScheduler.Core.Data.Repositories;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Exceptions;

namespace JobScheduler.Core.Services;

/// <summary>
/// Manages concurrent job execution limits and ensures concurrency constraints.
/// Prevents overloading system with too many concurrent executions.
/// </summary>
public sealed class ConcurrencyManager
{
    private readonly IExecutionRepository _executionRepository;
    private readonly ConcurrentDictionary<Guid, int> _jobConcurrencyCache;
    private readonly int _maxGlobalConcurrency;
    private int _globalRunningCount;

    public ConcurrencyManager(IExecutionRepository executionRepository, int maxGlobalConcurrency = SchedulerConstants.DefaultMaxConcurrentJobs)
    {
        _executionRepository = executionRepository ?? throw new ArgumentNullException(nameof(executionRepository));
        _maxGlobalConcurrency = maxGlobalConcurrency;
        _jobConcurrencyCache = new ConcurrentDictionary<Guid, int>();
        _globalRunningCount = 0;
    }

    /// <summary>
    /// Checks if a job can execute based on concurrency constraints.
    /// </summary>
    public async Task<bool> CanExecuteAsync(Job job)
    {
        if (job is null)
            throw new ArgumentNullException(nameof(job));

        // Check global concurrency limit
        var globalCount = await _executionRepository.GetConcurrentRunningCountAsync();
        if (globalCount >= _maxGlobalConcurrency)
            return false;

        // Check job-specific concurrency limit
        var jobSpecificCount = await _executionRepository.GetCurrentlyRunningCountAsync(job.Id);
        if (jobSpecificCount >= job.MaxConcurrentExecutions)
            return false;

        return true;
    }

    /// <summary>
    /// Ensures a job can execute; throws if it cannot due to concurrency limits.
    /// </summary>
    public async Task EnsureCanExecuteAsync(Job job)
    {
        if (!await CanExecuteAsync(job))
        {
            var currentCount = await _executionRepository.GetCurrentlyRunningCountAsync(job.Id);
            throw new ConcurrencyException(job.Id, currentCount, job.MaxConcurrentExecutions);
        }
    }

    /// <summary>
    /// Increments the concurrency counter for a job.
    /// </summary>
    public void IncrementConcurrencyCount(Guid jobId)
    {
        _jobConcurrencyCache.AddOrUpdate(jobId, 1, (_, current) => current + 1);
        Interlocked.Increment(ref _globalRunningCount);
    }

    /// <summary>
    /// Decrements the concurrency counter for a job.
    /// </summary>
    public void DecrementConcurrencyCount(Guid jobId)
    {
        _jobConcurrencyCache.AddOrUpdate(jobId, 0, (_, current) => Math.Max(0, current - 1));

        if (_globalRunningCount > 0)
            Interlocked.Decrement(ref _globalRunningCount);
    }

    /// <summary>
    /// Gets the current concurrency count for a job.
    /// </summary>
    public int GetJobConcurrencyCount(Guid jobId)
    {
        return _jobConcurrencyCache.TryGetValue(jobId, out var count) ? count : 0;
    }

    /// <summary>
    /// Gets the global concurrency count.
    /// </summary>
    public int GetGlobalConcurrencyCount()
    {
        return _globalRunningCount;
    }

    /// <summary>
    /// Clears cache and synchronizes with database state.
    /// Should be called periodically or on system startup.
    /// </summary>
    public async Task SynchronizeWithDatabaseAsync()
    {
        _jobConcurrencyCache.Clear();

        var runningExecutions = await _executionRepository.GetRunningExecutionsAsync();
        var grouped = runningExecutions.GroupBy(e => e.JobId);

        foreach (var group in grouped)
        {
            _jobConcurrencyCache[group.Key] = group.Count();
        }

        _globalRunningCount = runningExecutions.Count();
    }

    /// <summary>
    /// Gets statistics about current concurrency state.
    /// </summary>
    public Dictionary<string, int> GetConcurrencyStats()
    {
        return new Dictionary<string, int>
        {
            { "GlobalRunning", _globalRunningCount },
            { "GlobalLimit", _maxGlobalConcurrency },
            { "JobsWithExecutions", _jobConcurrencyCache.Count(x => x.Value > 0) },
            { "TotalCachedJobs", _jobConcurrencyCache.Count }
        };
    }
}
