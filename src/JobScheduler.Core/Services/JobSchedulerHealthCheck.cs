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
using JobScheduler.Core.Constants;
using JobScheduler.Core.Data.Repositories;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace JobScheduler.Core.Services;

/// <summary>
/// Health check that reports the scheduler's operational status.
/// <list type="bullet">
///   <item><description><see cref="HealthStatus.Unhealthy"/>: the scheduler loop is not running or a job is overdue by more than 2 polling intervals.</description></item>
///   <item><description><see cref="HealthStatus.Degraded"/>: any job's last execution failed.</description></item>
///   <item><description><see cref="HealthStatus.Healthy"/>: everything is nominal.</description></item>
/// </list>
/// </summary>
public sealed class JobSchedulerHealthCheck : IHealthCheck
{
    private readonly IJobRepository _jobRepository;
    private readonly IExecutionRepository _executionRepository;
    private readonly int _pollIntervalMs;
    private readonly ILogger<JobSchedulerHealthCheck>? _logger;

    private static DateTime _lastLoopHeartbeat = DateTime.MinValue;
    private static readonly object _heartbeatLock = new();

    /// <summary>
    /// Records a heartbeat from the scheduler loop.
    /// Call this method at the end of each scheduling cycle.
    /// </summary>
    public static void RecordHeartbeat()
    {
        lock (_heartbeatLock)
        {
            _lastLoopHeartbeat = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Gets or sets the timestamp of the last recorded heartbeat.
    /// Exposed for testing and diagnostic tooling.
    /// </summary>
    public static DateTime LastHeartbeat
    {
        get { lock (_heartbeatLock) { return _lastLoopHeartbeat; } }
        set { lock (_heartbeatLock) { _lastLoopHeartbeat = value; } }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JobSchedulerHealthCheck"/> class.
    /// </summary>
    /// <param name="jobRepository">The job repository.</param>
    /// <param name="executionRepository">The execution repository.</param>
    /// <param name="pollIntervalMs">The scheduler's polling interval in milliseconds.</param>
    /// <param name="logger">Optional logger.</param>
    public JobSchedulerHealthCheck(
        IJobRepository jobRepository,
        IExecutionRepository executionRepository,
        int pollIntervalMs = SchedulerConstants.QueuePollIntervalMs,
        ILogger<JobSchedulerHealthCheck>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(jobRepository);
        ArgumentNullException.ThrowIfNull(executionRepository);

        _jobRepository = jobRepository;
        _executionRepository = executionRepository;
        _pollIntervalMs = pollIntervalMs;
        _logger = logger;
    }

    /// <summary>
    /// Runs the health check.
    /// </summary>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>();
        var now = DateTime.UtcNow;

        // 1. Check scheduler loop heartbeat
        DateTime heartbeat;
        lock (_heartbeatLock)
        {
            heartbeat = _lastLoopHeartbeat;
        }

        data["LastHeartbeat"] = heartbeat == DateTime.MinValue ? "never" : heartbeat.ToString("O");

        if (heartbeat == DateTime.MinValue)
        {
            data["Reason"] = "Scheduler loop has never sent a heartbeat";
            _logger?.LogWarning("Health check: scheduler loop has never sent a heartbeat");
            return HealthCheckResult.Unhealthy("Scheduler loop is not running.", data: data);
        }

        var heartbeatAge = now - heartbeat;
        var maxHeartbeatAge = TimeSpan.FromMilliseconds(_pollIntervalMs * 2.0);
        data["HeartbeatAgeMs"] = (long)heartbeatAge.TotalMilliseconds;

        if (heartbeatAge > maxHeartbeatAge)
        {
            data["Reason"] = $"Heartbeat is {heartbeatAge.TotalSeconds:F1}s old, threshold is {maxHeartbeatAge.TotalSeconds:F1}s";
            _logger?.LogWarning("Health check: scheduler loop heartbeat is stale ({AgeMs}ms)", (long)heartbeatAge.TotalMilliseconds);
            return HealthCheckResult.Unhealthy("Scheduler loop is not running.", data: data);
        }

        // 2. Check for overdue jobs
        var activeJobs = (await _jobRepository.GetActiveJobsAsync()).ToList();
        var overdueThreshold = TimeSpan.FromMilliseconds(_pollIntervalMs * 2.0);
        var overdueJobs = activeJobs
            .Where(j => j.NextExecutionAt.HasValue && (now - j.NextExecutionAt.Value) > overdueThreshold)
            .ToList();

        if (overdueJobs.Count > 0)
        {
            data["OverdueJobCount"] = overdueJobs.Count;
            data["OverdueJobs"] = string.Join(", ", overdueJobs.Select(j => $"{j.Name} (overdue {(now - j.NextExecutionAt!.Value).TotalSeconds:F0}s)"));
            _logger?.LogWarning("Health check: {Count} overdue jobs detected", overdueJobs.Count);
            return HealthCheckResult.Unhealthy(
                $"{overdueJobs.Count} job(s) overdue by more than 2 polling intervals.",
                data: data);
        }

        // 3. Check for recent failures
        var jobsWithLastFailure = new List<string>();
        foreach (var job in activeJobs)
        {
            var latest = await _executionRepository.GetLatestExecutionAsync(job.Id);
            if (latest is not null && latest.Status == ExecutionStatus.Failed)
            {
                jobsWithLastFailure.Add(job.Name);
            }
        }

        if (jobsWithLastFailure.Count > 0)
        {
            data["FailedJobCount"] = jobsWithLastFailure.Count;
            data["FailedJobs"] = string.Join(", ", jobsWithLastFailure);
            _logger?.LogInformation("Health check: {Count} jobs with last-run failure", jobsWithLastFailure.Count);
            return HealthCheckResult.Degraded(
                $"{jobsWithLastFailure.Count} job(s) last execution failed.",
                data: data);
        }

        data["ActiveJobCount"] = activeJobs.Count;
        return HealthCheckResult.Healthy("All jobs operating normally.", data: data);
    }
}
