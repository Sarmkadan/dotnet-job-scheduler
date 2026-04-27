#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace JobScheduler.Core.Services;

/// <summary>
/// Monitors scheduler performance metrics including execution times, throughput, and resource usage.
/// WHY: Performance monitoring is essential for capacity planning and bottleneck identification.
/// </summary>
public class PerformanceMonitor
{
    private readonly ILogger<PerformanceMonitor> _logger;
    private readonly ConcurrentQueue<PerformanceMetric> _metrics;
    private readonly int _maxMetricsRetained = 10000; // Keep last 10000 metrics in memory

    public PerformanceMonitor(ILogger<PerformanceMonitor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _metrics = new ConcurrentQueue<PerformanceMetric>();
    }

    /// <summary>
    /// Records execution time for a job.
    /// Metrics are stored in-memory for analysis.
    /// </summary>
    public void RecordExecutionTime(Guid jobId, string jobName, long elapsedMs, bool success)
    {
        var metric = new PerformanceMetric
        {
            JobId = jobId,
            JobName = jobName,
            ExecutionTimeMs = elapsedMs,
            Success = success,
            Timestamp = DateTime.UtcNow
        };

        _metrics.Enqueue(metric);

        // Trim old metrics if queue grows too large
        while (_metrics.Count > _maxMetricsRetained)
        {
            _metrics.TryDequeue(out _);
        }

        _logger.LogDebug("Recorded execution time for {JobName}: {ElapsedMs}ms (Success: {Success})",
            jobName, elapsedMs, success);
    }

    /// <summary>
    /// Gets average execution time for a specific job.
    /// </summary>
    public long GetAverageExecutionTime(Guid jobId)
    {
        var jobMetrics = _metrics.Where(m => m.JobId == jobId).ToList();
        return jobMetrics.Any() ? (long)jobMetrics.Average(m => m.ExecutionTimeMs) : 0;
    }

    /// <summary>
    /// Gets average execution time across all jobs.
    /// Used for dashboard and diagnostics.
    /// </summary>
    public long GetAverageExecutionTimeMs()
    {
        var allMetrics = _metrics.ToList();
        return allMetrics.Any() ? (long)allMetrics.Average(m => m.ExecutionTimeMs) : 0;
    }

    /// <summary>
    /// Gets throughput (executions per minute) for the scheduler.
    /// </summary>
    public double GetThroughputPerMinute()
    {
        var now = DateTime.UtcNow;
        var oneMinuteAgo = now.AddMinutes(-1);
        var recentMetrics = _metrics.Where(m => m.Timestamp > oneMinuteAgo).Count();
        return recentMetrics;
    }

    /// <summary>
    /// Gets success rate (percentage of successful executions).
    /// </summary>
    public double GetSuccessRate()
    {
        var allMetrics = _metrics.ToList();
        if (!allMetrics.Any())
            return 100;

        var successCount = allMetrics.Count(m => m.Success);
        return (double)successCount / allMetrics.Count * 100;
    }

    /// <summary>
    /// Gets success rate for a specific job.
    /// </summary>
    public double GetSuccessRate(Guid jobId)
    {
        var jobMetrics = _metrics.Where(m => m.JobId == jobId).ToList();
        if (!jobMetrics.Any())
            return 100;

        var successCount = jobMetrics.Count(m => m.Success);
        return (double)successCount / jobMetrics.Count * 100;
    }

    /// <summary>
    /// Gets percentile execution time for a job.
    /// P99 is commonly used for SLA monitoring.
    /// </summary>
    public long GetPercentileExecutionTime(Guid jobId, double percentile)
    {
        if (percentile < 0 || percentile > 100)
            throw new ArgumentException("Percentile must be between 0 and 100", nameof(percentile));

        var jobMetrics = _metrics.Where(m => m.JobId == jobId).OrderBy(m => m.ExecutionTimeMs).ToList();
        if (!jobMetrics.Any())
            return 0;

        var index = (int)((percentile / 100.0) * jobMetrics.Count);
        return jobMetrics[Math.Min(index, jobMetrics.Count - 1)].ExecutionTimeMs;
    }

    /// <summary>
    /// Gets CPU utilization percentage (0-100).
    /// Approximation based on process CPU usage.
    /// </summary>
    public double GetCpuUtilization()
    {
        try
        {
            using (var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total"))
            {
                cpuCounter.NextValue(); // First call returns 0
                System.Threading.Thread.Sleep(100);
                return cpuCounter.NextValue();
            }
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Gets memory usage in megabytes.
    /// </summary>
    public long GetMemoryUsageMb()
    {
        return GC.TotalMemory(false) / 1024 / 1024;
    }

    /// <summary>
    /// Gets timeline of performance data aggregated by hour.
    /// Used for dashboard visualization.
    /// </summary>
    public async Task<List<PerformanceTimelinePoint>> GetPerformanceTimelineAsync(DateTime from)
    {
        var metrics = _metrics.Where(m => m.Timestamp >= from).ToList();
        var timeline = new List<PerformanceTimelinePoint>();

        var grouped = metrics
            .GroupBy(m => new DateTime(m.Timestamp.Year, m.Timestamp.Month, m.Timestamp.Day, m.Timestamp.Hour, 0, 0))
            .OrderBy(g => g.Key);

        foreach (var group in grouped)
        {
            timeline.Add(new PerformanceTimelinePoint
            {
                Timestamp = group.Key,
                ExecutionCount = group.Count(),
                SuccessCount = group.Count(m => m.Success),
                FailureCount = group.Count(m => !m.Success),
                AverageExecutionTimeMs = (long)group.Average(m => m.ExecutionTimeMs)
            });
        }

        return timeline;
    }

    /// <summary>
    /// Clears all recorded metrics.
    /// Used during reset or to free memory.
    /// </summary>
    public void ClearMetrics()
    {
        while (_metrics.TryDequeue(out _)) { }
        _logger.LogInformation("Cleared all performance metrics");
    }

    /// <summary>
    /// Gets metric statistics summary.
    /// </summary>
    public MetricsSummary GetSummary()
    {
        var allMetrics = _metrics.ToList();

        return new MetricsSummary
        {
            TotalExecutions = allMetrics.Count,
            SuccessfulExecutions = allMetrics.Count(m => m.Success),
            FailedExecutions = allMetrics.Count(m => !m.Success),
            AverageExecutionTimeMs = allMetrics.Any() ? (long)allMetrics.Average(m => m.ExecutionTimeMs) : 0,
            MinExecutionTimeMs = allMetrics.Any() ? allMetrics.Min(m => m.ExecutionTimeMs) : 0,
            MaxExecutionTimeMs = allMetrics.Any() ? allMetrics.Max(m => m.ExecutionTimeMs) : 0,
            MemoryUsageMb = GetMemoryUsageMb()
        };
    }
}

public class PerformanceMetric
{
    public Guid JobId { get; set; }
    public string JobName { get; set; } = string.Empty;
    public long ExecutionTimeMs { get; set; }
    public bool Success { get; set; }
    public DateTime Timestamp { get; set; }
}

public class MetricsSummary
{
    public int TotalExecutions { get; set; }
    public int SuccessfulExecutions { get; set; }
    public int FailedExecutions { get; set; }
    public long AverageExecutionTimeMs { get; set; }
    public long MinExecutionTimeMs { get; set; }
    public long MaxExecutionTimeMs { get; set; }
    public long MemoryUsageMb { get; set; }

    public double SuccessRate => TotalExecutions == 0 ? 0 : (double)SuccessfulExecutions / TotalExecutions * 100;
}

public class PerformanceTimelinePoint
{
    public DateTime Timestamp { get; set; }
    public int ExecutionCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public long AverageExecutionTimeMs { get; set; }
}
