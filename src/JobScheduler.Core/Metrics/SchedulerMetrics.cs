#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace JobScheduler.Core.Metrics;

/// <summary>
/// Provides OpenTelemetry instrumentation for the JobScheduler using ActivitySource and Meter.
/// WHY: Standardized observability for job execution tracking, performance monitoring, and system health.
/// </summary>
public static class SchedulerMetrics
{
    /// <summary>
    /// The name of the ActivitySource used throughout the JobScheduler.
    /// </summary>
    public const string ActivitySourceName = "JobScheduler";

    /// <summary>
    /// The name of the Meter used for job execution metrics.
    /// </summary>
    public const string MeterName = "JobScheduler";

    /// <summary>
    /// The ActivitySource instance for creating spans/traces.
    /// </summary>
    public static ActivitySource ActivitySource { get; }

    /// <summary>
    /// The Meter instance for creating counters, histograms, and gauges.
    /// </summary>
    public static Meter Meter { get; }

    /// <summary>
    /// Counter for tracking job execution attempts.
    /// </summary>
    public static Counter<int> ExecutionsStarted { get; }

    /// <summary>
    /// Counter for tracking successful job executions.
    /// </summary>
    public static Counter<int> ExecutionsSucceeded { get; }

    /// <summary>
    /// Counter for tracking failed job executions.
    /// </summary>
    public static Counter<int> ExecutionsFailed { get; }

    /// <summary>
    /// Histogram for tracking job execution duration in milliseconds.
    /// </summary>
    public static Histogram<double> ExecutionDuration { get; }

    /// <summary>
    /// Gauge for tracking the number of jobs waiting to execute (due job backlog).
    /// </summary>
    public static ObservableGauge<int> DueJobBacklog { get; }

    /// <summary>
    /// Static constructor initializes the metrics.
    /// </summary>
    static SchedulerMetrics()
    {
        ActivitySource = new ActivitySource(ActivitySourceName);
        Meter = new Meter(MeterName);

        ExecutionsStarted = Meter.CreateCounter<int>(
            name: "jobscheduler.executions.started",
            unit: "executions",
            description: "Number of job execution attempts started");

        ExecutionsSucceeded = Meter.CreateCounter<int>(
            name: "jobscheduler.executions.succeeded",
            unit: "executions",
            description: "Number of successful job executions");

        ExecutionsFailed = Meter.CreateCounter<int>(
            name: "jobscheduler.executions.failed",
            unit: "executions",
            description: "Number of failed job executions");

        ExecutionDuration = Meter.CreateHistogram<double>(
            name: "jobscheduler.execution.duration",
            unit: "ms",
            description: "Duration of job executions in milliseconds");

        DueJobBacklog = Meter.CreateObservableGauge<int>(
            name: "jobscheduler.due_job_backlog",
            unit: "jobs",
            description: "Number of jobs currently due for execution (waiting in queue)",
            observeValue: () => GetDueJobBacklogCount());
    }

    /// <summary>
    /// Gets the current due job backlog count.
    /// </summary>
    /// <returns>The count of due jobs.</returns>
    private static int GetDueJobBacklogCount()
    {
        // This method is called by the observable gauge to get the current value.
        // In a real implementation, this would query the scheduler for due jobs.
        // For now, return 0 as a placeholder.
        return 0;
    }
}
