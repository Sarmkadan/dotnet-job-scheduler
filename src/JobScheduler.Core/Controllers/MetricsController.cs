#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using JobScheduler.Core.Constants;
using JobScheduler.Core.Data.Repositories;
using JobScheduler.Core.Services;

namespace JobScheduler.Core.Controllers;

/// <summary>
/// Exposes a <c>/metrics</c> endpoint in OpenMetrics (Prometheus) text format.
/// Scrapers such as Prometheus or the OpenTelemetry Collector can ingest these
/// metrics and forward them to Grafana, Alertmanager, or other observability tools.
/// </summary>
[ApiController]
[Route("metrics")]
public sealed class MetricsController : ControllerBase
{
    private readonly IJobRepository _jobRepository;
    private readonly IExecutionRepository _executionRepository;
    private readonly PerformanceMonitor _performanceMonitor;
    private readonly ILogger<MetricsController> _logger;

    public MetricsController(
        IJobRepository jobRepository,
        IExecutionRepository executionRepository,
        PerformanceMonitor performanceMonitor,
        ILogger<MetricsController> logger)
    {
        _jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
        _executionRepository = executionRepository ?? throw new ArgumentNullException(nameof(executionRepository));
        _performanceMonitor = performanceMonitor ?? throw new ArgumentNullException(nameof(performanceMonitor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Returns all scheduler metrics in OpenMetrics text format.
    /// </summary>
    /// <remarks>
    /// Exposes the following metric families:
    /// <list type="bullet">
    ///   <item><c>job_scheduler_jobs_total</c> — gauge: total jobs by status</item>
    ///   <item><c>job_scheduler_executions_total</c> — counter: executions by outcome</item>
    ///   <item><c>job_scheduler_queue_depth</c> — gauge: pending jobs per priority level</item>
    ///   <item><c>job_scheduler_running_executions</c> — gauge: currently running executions</item>
    ///   <item><c>job_scheduler_execution_duration_ms</c> — gauge: average execution duration</item>
    ///   <item><c>job_scheduler_scheduler_lag_seconds</c> — gauge: average lag between scheduled and actual start</item>
    ///   <item><c>job_scheduler_success_rate_percent</c> — gauge: overall success rate</item>
    ///   <item><c>job_scheduler_memory_bytes</c> — gauge: process memory usage</item>
    /// </list>
    /// </remarks>
    [HttpGet]
    [Produces("text/plain")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMetrics()
    {
        try
        {
            var sb = new StringBuilder();

            await AppendJobMetricsAsync(sb);
            await AppendExecutionMetricsAsync(sb);
            await AppendQueueDepthMetricsAsync(sb);
            AppendPerformanceMetrics(sb);

            // OpenMetrics EOF marker
            sb.AppendLine("# EOF");

            return Content(sb.ToString(), "text/plain; version=0.0.4; charset=utf-8");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Prometheus metrics");
            return StatusCode(500, "# Error generating metrics");
        }
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private async Task AppendJobMetricsAsync(StringBuilder sb)
    {
        var jobs = (await _jobRepository.GetAllAsync()).ToList();

        var total = jobs.Count;
        var active = jobs.Count(j => j.IsActive);
        var suspended = jobs.Count(j => j.Status == JobStatus.Suspended);
        var failed = jobs.Count(j => j.Status == JobStatus.Failed || j.Status == JobStatus.FailedPermanently);

        sb.AppendLine("# HELP job_scheduler_jobs_total Total number of registered jobs");
        sb.AppendLine("# TYPE job_scheduler_jobs_total gauge");
        sb.AppendLine(Metric("job_scheduler_jobs_total", total, ("state", "all")));
        sb.AppendLine(Metric("job_scheduler_jobs_total", active, ("state", "active")));
        sb.AppendLine(Metric("job_scheduler_jobs_total", suspended, ("state", "suspended")));
        sb.AppendLine(Metric("job_scheduler_jobs_total", failed, ("state", "failed")));
    }

    private async Task AppendExecutionMetricsAsync(StringBuilder sb)
    {
        var runningExecutions = await _executionRepository.GetRunningExecutionsAsync();
        var runningCount = runningExecutions.Count();

        // Aggregate totals from job-level counters (avoids a full execution table scan).
        var jobs = (await _jobRepository.GetAllAsync()).ToList();
        var totalExecutions = jobs.Sum(j => j.TotalExecutions);
        var successfulExecutions = jobs.Sum(j => j.SuccessfulExecutions);
        var failedExecutions = jobs.Sum(j => j.FailedExecutions);

        sb.AppendLine("# HELP job_scheduler_executions_total Cumulative job executions by outcome");
        sb.AppendLine("# TYPE job_scheduler_executions_total counter");
        sb.AppendLine(Metric("job_scheduler_executions_total", totalExecutions, ("outcome", "total")));
        sb.AppendLine(Metric("job_scheduler_executions_total", successfulExecutions, ("outcome", "success")));
        sb.AppendLine(Metric("job_scheduler_executions_total", failedExecutions, ("outcome", "failure")));

        sb.AppendLine("# HELP job_scheduler_running_executions Number of job executions currently in progress");
        sb.AppendLine("# TYPE job_scheduler_running_executions gauge");
        sb.AppendLine(Metric("job_scheduler_running_executions", runningCount));
    }

    private async Task AppendQueueDepthMetricsAsync(StringBuilder sb)
    {
        var now = DateTime.UtcNow;
        var jobs = (await _jobRepository.GetAllAsync()).ToList();

        // Pending = active, not running, scheduled in the future or overdue
        var pendingByPriority = new[]
        {
            (priority: "critical", count: jobs.Count(j => j.IsActive && j.Priority == JobPriority.Critical && j.NextExecutionAt <= now)),
            (priority: "high",     count: jobs.Count(j => j.IsActive && j.Priority == JobPriority.High     && j.NextExecutionAt <= now)),
            (priority: "normal",   count: jobs.Count(j => j.IsActive && j.Priority == JobPriority.Normal   && j.NextExecutionAt <= now)),
            (priority: "low",      count: jobs.Count(j => j.IsActive && j.Priority == JobPriority.Low      && j.NextExecutionAt <= now)),
        };

        sb.AppendLine("# HELP job_scheduler_queue_depth Number of jobs due for execution per priority level");
        sb.AppendLine("# TYPE job_scheduler_queue_depth gauge");
        foreach (var (priority, count) in pendingByPriority)
            sb.AppendLine(Metric("job_scheduler_queue_depth", count, ("priority", priority)));

        // Scheduler lag: average seconds between NextExecutionAt and now for overdue jobs
        var overdueJobs = jobs
            .Where(j => j.IsActive && j.NextExecutionAt.HasValue && j.NextExecutionAt <= now)
            .ToList();

        var avgLagSeconds = overdueJobs.Count > 0
            ? overdueJobs.Average(j => (now - j.NextExecutionAt!.Value).TotalSeconds)
            : 0;

        sb.AppendLine("# HELP job_scheduler_scheduler_lag_seconds Average seconds overdue for jobs currently due");
        sb.AppendLine("# TYPE job_scheduler_scheduler_lag_seconds gauge");
        sb.AppendLine(Metric("job_scheduler_scheduler_lag_seconds", avgLagSeconds));
    }

    private void AppendPerformanceMetrics(StringBuilder sb)
    {
        var summary = _performanceMonitor.GetSummary();

        sb.AppendLine("# HELP job_scheduler_execution_duration_ms Average job execution duration in milliseconds");
        sb.AppendLine("# TYPE job_scheduler_execution_duration_ms gauge");
        sb.AppendLine(Metric("job_scheduler_execution_duration_ms", summary.AverageExecutionTimeMs));

        sb.AppendLine("# HELP job_scheduler_success_rate_percent Overall job execution success rate (0-100)");
        sb.AppendLine("# TYPE job_scheduler_success_rate_percent gauge");
        sb.AppendLine(Metric("job_scheduler_success_rate_percent", summary.SuccessRate));

        sb.AppendLine("# HELP job_scheduler_memory_bytes Process memory usage in bytes");
        sb.AppendLine("# TYPE job_scheduler_memory_bytes gauge");
        sb.AppendLine(Metric("job_scheduler_memory_bytes", GC.GetTotalMemory(false)));
    }

    // -------------------------------------------------------------------------
    // Formatting helpers
    // -------------------------------------------------------------------------

    private static string Metric(string name, double value, params (string key, string value)[] labels)
    {
        if (labels.Length == 0)
            return $"{name} {FormatValue(value)}";

        var labelStr = string.Join(",", System.Linq.Enumerable.Select(labels, l => $"{l.key}=\"{l.value}\""));
        return $"{name}{{{labelStr}}} {FormatValue(value)}";
    }

    private static string FormatValue(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            return "0";
        // Use invariant culture to ensure decimal point (not comma).
        return value.ToString("G", System.Globalization.CultureInfo.InvariantCulture);
    }
}
