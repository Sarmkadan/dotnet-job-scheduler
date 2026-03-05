#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using JobScheduler.Core.Services;

namespace JobScheduler.Core.Controllers;

/// <summary>
/// Provides aggregated system metrics and dashboard data for monitoring the job scheduler.
/// Exposes overall health, performance, and queue statistics.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class DashboardController : ControllerBase
{
    private readonly JobSchedulerService _schedulerService;
    private readonly PerformanceMonitor _performanceMonitor;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        JobSchedulerService schedulerService,
        PerformanceMonitor performanceMonitor,
        ILogger<DashboardController> logger)
    {
        _schedulerService = schedulerService ?? throw new ArgumentNullException(nameof(schedulerService));
        _performanceMonitor = performanceMonitor ?? throw new ArgumentNullException(nameof(performanceMonitor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Returns comprehensive system overview including job counts, execution status, and performance metrics.
    /// This is the primary endpoint for dashboard visualization.
    /// </summary>
    [HttpGet("overview")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardOverview>> GetOverview()
    {
        try
        {
            var systemStats = await _schedulerService.GetSystemStatisticsAsync();
            var runningCount = await _schedulerService.GetRunningJobCountAsync();
            var failedCount = await _schedulerService.GetFailedJobCountAsync();

            var overview = new DashboardOverview
            {
                TotalJobs = systemStats.TotalJobs,
                ActiveJobs = systemStats.ActiveJobs,
                RunningExecutions = runningCount,
                FailedJobsLast24Hours = failedCount,
                AverageSuccessRate = systemStats.AverageSuccessRate,
                TotalExecutions = systemStats.TotalExecutions,
                SuccessfulExecutions = systemStats.SuccessfulExecutions,
                AverageExecutionTimeMs = systemStats.AverageExecutionTimeMs,
                LastUpdatedAt = DateTime.UtcNow
            };

            return Ok(overview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard overview");
            return StatusCode(500, new { error = "Failed to retrieve dashboard overview" });
        }
    }

    /// <summary>
    /// Retrieves detailed queue status including pending, running, and failed jobs.
    /// Useful for understanding current system load and bottlenecks.
    /// </summary>
    [HttpGet("queue-status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<QueueStatusResponse>> GetQueueStatus()
    {
        try
        {
            var status = await _schedulerService.GetQueueStatusAsync();

            return Ok(new QueueStatusResponse
            {
                PendingJobs = status.PendingCount,
                RunningJobs = status.RunningCount,
                FailedJobs = status.FailedCount,
                CompletedJobs = status.CompletedCount,
                SuspendedJobs = status.SuspendedCount,
                TotalQueued = status.PendingCount + status.RunningCount,
                QueueUtilization = CalculateUtilization(status),
                EstimatedTimeToEmpty = EstimateTimeToEmpty(status)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving queue status");
            return StatusCode(500, new { error = "Failed to retrieve queue status" });
        }
    }

    /// <summary>
    /// Provides job priority distribution showing how jobs are balanced across priority levels.
    /// Helps identify if system is overloaded with high-priority jobs.
    /// </summary>
    [HttpGet("priority-distribution")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PriorityDistributionResponse>> GetPriorityDistribution()
    {
        try
        {
            var distribution = await _schedulerService.GetJobPriorityDistributionAsync();

            return Ok(new PriorityDistributionResponse
            {
                CriticalJobs = distribution["Critical"],
                HighJobs = distribution["High"],
                NormalJobs = distribution["Normal"],
                LowJobs = distribution["Low"],
                TotalJobs = distribution.Values.Sum()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving priority distribution");
            return StatusCode(500, new { error = "Failed to retrieve priority distribution" });
        }
    }

    /// <summary>
    /// Returns time-series performance data for visualization on dashboards.
    /// Data is aggregated by hour for the last 24 hours.
    /// </summary>
    [HttpGet("performance-timeline")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PerformanceTimelinePoint>>> GetPerformanceTimeline(
        [FromQuery] int hours = 24)
    {
        try
        {
            var data = await _performanceMonitor.GetPerformanceTimelineAsync(DateTime.UtcNow.AddHours(-hours));

            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving performance timeline");
            return StatusCode(500, new { error = "Failed to retrieve performance timeline" });
        }
    }

    /// <summary>
    /// Lists top 10 slowest jobs by average execution time.
    /// Useful for identifying performance bottlenecks.
    /// </summary>
    [HttpGet("slowest-jobs")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SlowestJobResponse>>> GetSlowestJobs()
    {
        try
        {
            var slowest = await _schedulerService.GetSlowestJobsAsync(10);

            var responses = slowest.Select(j => new SlowestJobResponse
            {
                JobId = j.Id,
                JobName = j.Name,
                AverageExecutionTimeMs = j.AverageExecutionTimeMs,
                MaxExecutionTimeMs = j.MaxExecutionTimeMs,
                ExecutionCount = j.TotalExecutions
            }).ToList();

            return Ok(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving slowest jobs");
            return StatusCode(500, new { error = "Failed to retrieve slowest jobs" });
        }
    }

    /// <summary>
    /// Lists top 10 most frequently failing jobs.
    /// Helps identify problematic jobs requiring attention.
    /// </summary>
    [HttpGet("most-failing-jobs")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<FailingJobResponse>>> GetMostFailingJobs()
    {
        try
        {
            var failing = await _schedulerService.GetMostFailingJobsAsync(10);

            var responses = failing.Select(j => new FailingJobResponse
            {
                JobId = j.Id,
                JobName = j.Name,
                FailureRate = j.GetSuccessRate() == 0 ? 100 : (100 - j.GetSuccessRate()),
                FailedCount = j.FailedExecutions,
                SuccessRate = j.GetSuccessRate()
            }).ToList();

            return Ok(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving failing jobs");
            return StatusCode(500, new { error = "Failed to retrieve failing jobs" });
        }
    }

    /// <summary>
    /// Generates a health check report with detailed system diagnostics.
    /// Returns actionable insights for system administration.
    /// </summary>
    [HttpGet("health-report")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<HealthReportResponse>> GetHealthReport()
    {
        try
        {
            var report = new HealthReportResponse
            {
                Timestamp = DateTime.UtcNow,
                DatabaseConnected = await _schedulerService.IsDatabaseConnectedAsync(),
                MemoryUsageMb = GC.TotalMemory(false) / 1024 / 1024,
                ProcessorUtilization = _performanceMonitor.GetCpuUtilization(),
                Warnings = await GenerateSystemWarnings(),
                IsHealthy = true
            };

            report.IsHealthy = !report.Warnings.Any(w => w.Severity == "Critical");

            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating health report");
            return StatusCode(500, new { error = "Failed to generate health report" });
        }
    }

    private double CalculateUtilization(QueueStatus status)
    {
        var total = status.PendingCount + status.RunningCount + status.FailedCount +
                   status.CompletedCount + status.SuspendedCount;
        return total == 0 ? 0 : ((double)(status.PendingCount + status.RunningCount) / total) * 100;
    }

    private TimeSpan? EstimateTimeToEmpty(QueueStatus status)
    {
        if (status.RunningCount == 0 && status.PendingCount == 0)
            return TimeSpan.Zero;

        var avgTime = _performanceMonitor.GetAverageExecutionTimeMs();
        return avgTime > 0 ? TimeSpan.FromMilliseconds(status.PendingCount * avgTime) : null;
    }

    private async Task<List<HealthWarning>> GenerateSystemWarnings()
    {
        var warnings = new List<HealthWarning>();
        var status = await _schedulerService.GetQueueStatusAsync();

        if (status.FailedCount > 100)
            warnings.Add(new HealthWarning
            {
                Severity = "Warning",
                Message = $"High number of failed jobs ({status.FailedCount})"
            });

        var avgTime = _performanceMonitor.GetAverageExecutionTimeMs();
        if (avgTime > 5000)
            warnings.Add(new HealthWarning
            {
                Severity = "Warning",
                Message = $"Average execution time is high ({avgTime}ms)"
            });

        return warnings;
    }
}

public sealed class DashboardOverview
{
    public int TotalJobs { get; set; }
    public int ActiveJobs { get; set; }
    public int RunningExecutions { get; set; }
    public int FailedJobsLast24Hours { get; set; }
    public double AverageSuccessRate { get; set; }
    public int TotalExecutions { get; set; }
    public int SuccessfulExecutions { get; set; }
    public long AverageExecutionTimeMs { get; set; }
    public DateTime LastUpdatedAt { get; set; }
}

public sealed class QueueStatusResponse
{
    public int PendingJobs { get; set; }
    public int RunningJobs { get; set; }
    public int FailedJobs { get; set; }
    public int CompletedJobs { get; set; }
    public int SuspendedJobs { get; set; }
    public int TotalQueued { get; set; }
    public double QueueUtilization { get; set; }
    public TimeSpan? EstimatedTimeToEmpty { get; set; }
}

public sealed class PriorityDistributionResponse
{
    public int CriticalJobs { get; set; }
    public int HighJobs { get; set; }
    public int NormalJobs { get; set; }
    public int LowJobs { get; set; }
    public int TotalJobs { get; set; }
}

public sealed class PerformanceTimelinePoint
{
    public DateTime Timestamp { get; set; }
    public int ExecutionCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public long AverageExecutionTimeMs { get; set; }
}

public sealed class SlowestJobResponse
{
    public Guid JobId { get; set; }
    public string JobName { get; set; } = string.Empty;
    public long AverageExecutionTimeMs { get; set; }
    public long MaxExecutionTimeMs { get; set; }
    public int ExecutionCount { get; set; }
}

public sealed class FailingJobResponse
{
    public Guid JobId { get; set; }
    public string JobName { get; set; } = string.Empty;
    public double FailureRate { get; set; }
    public int FailedCount { get; set; }
    public double SuccessRate { get; set; }
}

public sealed class HealthReportResponse
{
    public DateTime Timestamp { get; set; }
    public bool DatabaseConnected { get; set; }
    public long MemoryUsageMb { get; set; }
    public double ProcessorUtilization { get; set; }
    public List<HealthWarning> Warnings { get; set; } = new();
    public bool IsHealthy { get; set; }
}

public sealed class HealthWarning
{
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public sealed class QueueStatus
{
    public int PendingCount { get; set; }
    public int RunningCount { get; set; }
    public int FailedCount { get; set; }
    public int CompletedCount { get; set; }
    public int SuspendedCount { get; set; }
}
