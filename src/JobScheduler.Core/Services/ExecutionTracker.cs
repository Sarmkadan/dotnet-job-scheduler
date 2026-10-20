#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using JobScheduler.Core.Constants;
using JobScheduler.Core.Data.Repositories;
using JobScheduler.Core.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace JobScheduler.Core.Services;

/// <summary>
/// Tracks job executions and provides per-job diagnostic snapshots.
/// </summary>
public sealed class ExecutionTracker
{
    private readonly IJobRepository _jobRepository;
    private readonly IExecutionRepository _executionRepository;
    private readonly ILogger<ExecutionTracker>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionTracker"/> class.
    /// </summary>
    public ExecutionTracker(
        IJobRepository jobRepository,
        IExecutionRepository executionRepository,
        ILogger<ExecutionTracker>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(jobRepository);
        ArgumentNullException.ThrowIfNull(executionRepository);

        _jobRepository = jobRepository;
        _executionRepository = executionRepository;
        _logger = logger;
    }

    /// <summary>
    /// Returns a point-in-time snapshot of per-job execution statistics.
    /// </summary>
    /// <returns>A dictionary keyed by job ID with stats for each job.</returns>
    public async Task<Dictionary<Guid, JobExecutionSnapshot>> GetSnapshotAsync()
    {
        var jobs = (await _jobRepository.GetAllAsync()).ToList();
        var result = new Dictionary<Guid, JobExecutionSnapshot>(jobs.Count);

        foreach (var job in jobs)
        {
            var executions = (await _executionRepository.GetExecutionsByJobAsync(job.Id)).ToList();
            var running = executions.Where(e => e.Status == ExecutionStatus.Running).ToList();
            var successful = executions.Where(e => e.Status == ExecutionStatus.Success).ToList();
            var failed = executions.Where(e => e.Status == ExecutionStatus.Failed).ToList();
            var last = executions.FirstOrDefault();

            result[job.Id] = new JobExecutionSnapshot
            {
                JobId = job.Id,
                JobName = job.Name,
                LastRunAt = last?.StartedAt,
                LastStatus = last?.Status,
                SuccessCount = successful.Count,
                FailureCount = failed.Count,
                AverageDurationMs = successful.Count > 0
                    ? (long)successful.Average(e => e.DurationMilliseconds)
                    : 0,
                CurrentlyRunning = running.Count
            };
        }

        _logger?.LogDebug("Execution snapshot captured for {Count} jobs", result.Count);
        return result;
    }
}

/// <summary>
/// Point-in-time execution statistics for a single job.
/// </summary>
[DebuggerDisplay("{JobName}: {SuccessCount}ok/{FailureCount}fail running={CurrentlyRunning}")]
public sealed class JobExecutionSnapshot
{
    /// <summary>Identifier of the job.</summary>
    public Guid JobId { get; set; }

    /// <summary>Name of the job.</summary>
    public string JobName { get; set; } = string.Empty;

    /// <summary>Timestamp of the most recent execution start, or null if never run.</summary>
    public DateTime? LastRunAt { get; set; }

    /// <summary>Status of the most recent execution, or null if never run.</summary>
    public ExecutionStatus? LastStatus { get; set; }

    /// <summary>Total number of successful executions.</summary>
    public int SuccessCount { get; set; }

    /// <summary>Total number of failed executions.</summary>
    public int FailureCount { get; set; }

    /// <summary>Average duration in milliseconds of successful executions.</summary>
    public long AverageDurationMs { get; set; }

    /// <summary>Number of currently running executions for this job.</summary>
    public int CurrentlyRunning { get; set; }

    /// <summary>
    /// Returns a diagnostic string summarising this snapshot.
    /// </summary>
    public override string ToString() =>
        $"{JobName}: last={LastRunAt:O} status={LastStatus} ok={SuccessCount} fail={FailureCount} avg={AverageDurationMs}ms running={CurrentlyRunning}";
}
