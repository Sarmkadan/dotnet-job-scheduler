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
    /// <summary>
    /// The default number of hours included in the performance timeline.
    /// </summary>
    private const int DefaultTimelineHours = 24;

    /// <summary>
    /// The number of jobs included in top-job dashboard lists.
    /// </summary>
    private const int TopJobsCount = 10;

    /// <summary>
    /// The failed-job count above which a health warning is generated.
    /// </summary>
    private const int FailedJobsWarningThreshold = 100;

    /// <summary>
    /// The average execution time in milliseconds above which a health warning is generated.
    /// </summary>
    private const int AverageExecutionTimeWarningThresholdMs = 5000;

    private readonly JobSchedulerService _schedulerService;
    private readonly PerformanceMonitor _performanceMonitor;
    private readonly ILogger<DashboardController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DashboardController"/> class.
    /// </summary>
    /// <param name="schedulerService">The job scheduler service used to retrieve system data.</param>
    /// <param name="performanceMonitor">The performance monitor used to retrieve performance metrics.</param>
    /// <param name="logger">The logger used for logging controller operations.</param>
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
        [FromQuery] int hours = DefaultTimelineHours)
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
            var slowest = await _schedulerService.GetSlowestJobsAsync(TopJobsCount);

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
            var failing = await _schedulerService.GetMostFailingJobsAsync(TopJobsCount);

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
                MemoryUsageMb = GC.GetTotalMemory(false) / 1024 / 1024,
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

        if (status.FailedCount > FailedJobsWarningThreshold)
            warnings.Add(new HealthWarning
            {
                Severity = "Warning",
                Message = $"High number of failed jobs ({status.FailedCount})"
            });

        var avgTime = _performanceMonitor.GetAverageExecutionTimeMs();
        if (avgTime > AverageExecutionTimeWarningThresholdMs)
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
    /// <summary>
    /// Gets or sets the total number of jobs in the system.
    /// </summary>
    public int TotalJobs { get; set; }

    /// <summary>
    /// Gets or sets the number of active jobs in the system.
    /// </summary>
    public int ActiveJobs { get; set; }

    /// <summary>
    /// Gets or sets the number of currently running job executions.
    /// </summary>
    public int RunningExecutions { get; set; }

    /// <summary>
    /// Gets or sets the number of failed jobs in the last 24 hours.
    /// </summary>
    public int FailedJobsLast24Hours { get; set; }

    /// <summary>
    /// Gets or sets the average success rate of job executions (percentage).
    /// </summary>
    public double AverageSuccessRate { get; set; }

    /// <summary>
    /// Gets or sets the total number of job executions.
    /// </summary>
    public int TotalExecutions { get; set; }

    /// <summary>
    /// Gets or sets the number of successful job executions.
    /// </summary>
    public int SuccessfulExecutions { get; set; }

    /// <summary>
    /// Gets or sets the average execution time in milliseconds.
    /// </summary>
    public long AverageExecutionTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the overview was last updated.
    /// </summary>
    public DateTime LastUpdatedAt { get; set; }

    /// <summary>
    /// Returns a string summarizing the key dashboard overview metrics.
    /// </summary>
    public override string ToString()
    {
        return $"DashboardOverview {{ TotalJobs = {TotalJobs}, ActiveJobs = {ActiveJobs}, RunningExecutions = {RunningExecutions}, FailedJobsLast24Hours = {FailedJobsLast24Hours}, AverageSuccessRate = {AverageSuccessRate}, TotalExecutions = {TotalExecutions}, SuccessfulExecutions = {SuccessfulExecutions}, AverageExecutionTimeMs = {AverageExecutionTimeMs}, LastUpdatedAt = {LastUpdatedAt} }}";
    }
}

public sealed class QueueStatusResponse
    {
        /// <summary>
        /// Gets or sets the number of pending jobs in the queue.
        /// </summary>
        public int PendingJobs { get; set; }

        /// <summary>
        /// Gets or sets the number of currently running jobs.
        /// </summary>
        public int RunningJobs { get; set; }

        /// <summary>
        /// Gets or sets the number of failed jobs.
        /// </summary>
        public int FailedJobs { get; set; }

        /// <summary>
        /// Gets or sets the number of completed jobs.
        /// </summary>
        public int CompletedJobs { get; set; }

        /// <summary>
        /// Gets or sets the number of suspended jobs.
        /// </summary>
        public int SuspendedJobs { get; set; }

        /// <summary>
        /// Gets or sets the total number of queued jobs (pending + running).
        /// </summary>
        public int TotalQueued { get; set; }

        /// <summary>
        /// Gets or sets the queue utilization percentage.
        /// </summary>
        public double QueueUtilization { get; set; }

        /// <summary>
        /// Gets or sets the estimated time to empty the queue based on current processing rate.
        /// </summary>
        public TimeSpan? EstimatedTimeToEmpty { get; set; }

        /// <summary>
        /// Returns a string summarizing the key queue status metrics.
        /// </summary>
        public override string ToString()
        {
            return $"QueueStatusResponse {{ PendingJobs = {PendingJobs}, RunningJobs = {RunningJobs}, FailedJobs = {FailedJobs}, CompletedJobs = {CompletedJobs}, SuspendedJobs = {SuspendedJobs}, TotalQueued = {TotalQueued}, QueueUtilization = {QueueUtilization}, EstimatedTimeToEmpty = {EstimatedTimeToEmpty} }}";
        }
    }

public sealed class PriorityDistributionResponse
{
    /// <summary>
    /// Gets or sets the number of critical priority jobs.
    /// </summary>
    public int CriticalJobs { get; set; }

    /// <summary>
    /// Gets or sets the number of high priority jobs.
    /// </summary>
    public int HighJobs { get; set; }

    /// <summary>
    /// Gets or sets the number of normal priority jobs.
    /// </summary>
    public int NormalJobs { get; set; }

    /// <summary>
    /// Gets or sets the number of low priority jobs.
    /// </summary>
    public int LowJobs { get; set; }

    /// <summary>
    /// Gets or sets the total number of jobs across all priority levels.
    /// </summary>
    public int TotalJobs { get; set; }

    /// <summary>
    /// Returns a string summarizing the job priority distribution.
    /// </summary>
    public override string ToString()
    {
        return $"PriorityDistributionResponse {{ CriticalJobs = {CriticalJobs}, HighJobs = {HighJobs}, NormalJobs = {NormalJobs}, LowJobs = {LowJobs}, TotalJobs = {TotalJobs} }}";
    }
}

public sealed class PerformanceTimelinePoint
{
    /// <summary>
    /// Gets or sets the timestamp of the performance data point.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the total number of job executions during this time period.
    /// </summary>
    public int ExecutionCount { get; set; }

    /// <summary>
    /// Gets or sets the number of successful job executions during this time period.
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Gets or sets the number of failed job executions during this time period.
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Gets or sets the average execution time in milliseconds for jobs during this time period.
    /// </summary>
    public long AverageExecutionTimeMs { get; set; }

    /// <summary>
    /// Returns a string summarizing the performance timeline point.
    /// </summary>
    public override string ToString()
    {
        return $"PerformanceTimelinePoint {{ Timestamp = {Timestamp}, ExecutionCount = {ExecutionCount}, SuccessCount = {SuccessCount}, FailureCount = {FailureCount}, AverageExecutionTimeMs = {AverageExecutionTimeMs} }}";
    }
}

public sealed class SlowestJobResponse
{
    /// <summary>
    /// Gets or sets the unique identifier of the job.
    /// </summary>
    public Guid JobId { get; set; }

    /// <summary>
    /// Gets or sets the name of the job.
    /// </summary>
    public string JobName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the average execution time in milliseconds.
    /// </summary>
    public long AverageExecutionTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the maximum execution time in milliseconds.
    /// </summary>
    public long MaxExecutionTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the total number of executions for this job.
    /// </summary>
    public int ExecutionCount { get; set; }

    /// <summary>
    /// Returns a string summarizing the slowest job metrics.
    /// </summary>
    public override string ToString()
    {
        return $"SlowestJobResponse {{ JobId = {JobId}, JobName = {JobName}, AverageExecutionTimeMs = {AverageExecutionTimeMs}, MaxExecutionTimeMs = {MaxExecutionTimeMs}, ExecutionCount = {ExecutionCount} }}";
    }
}

public sealed class FailingJobResponse
{
    /// <summary>
    /// Gets or sets the unique identifier of the job.
    /// </summary>
    public Guid JobId { get; set; }
    /// <summary>
    /// Gets or sets the name of the job.
    /// </summary>
    public string JobName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the failure rate percentage.
    /// </summary>
    public double FailureRate { get; set; }
    /// <summary>
    /// Gets or sets the number of failed executions.
    /// </summary>
    public int FailedCount { get; set; }
    /// <summary>
    /// Gets or sets the success rate percentage.
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Returns a string summarizing the failing job metrics.
    /// </summary>
    public override string ToString()
    {
        return $"FailingJobResponse {{ JobId = {JobId}, JobName = {JobName}, FailureRate = {FailureRate}, FailedCount = {FailedCount}, SuccessRate = {SuccessRate} }}";
    }
}

public sealed class HealthReportResponse
{
    /// <summary>
    /// Gets or sets the timestamp when the report was generated.
    /// </summary>
    public DateTime Timestamp { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether the database is connected.
    /// </summary>
    public bool DatabaseConnected { get; set; }
    /// <summary>
    /// Gets or sets the current memory usage in megabytes.
    /// </summary>
    public long MemoryUsageMb { get; set; }
    /// <summary>
    /// Gets or sets the current processor utilization percentage.
    /// </summary>
    public double ProcessorUtilization { get; set; }
    /// <summary>
    /// Gets or sets the list of health warnings.
    /// </summary>
    public List<HealthWarning> Warnings { get; set; } = new();
    /// <summary>
    /// Gets or sets a value indicating whether the system is healthy.
    /// </summary>
    public bool IsHealthy { get; set; }

    /// <summary>
    /// Returns a string summarizing the health report.
    /// </summary>
    public override string ToString()
    {
        return $"HealthReportResponse {{ Timestamp = {Timestamp}, DatabaseConnected = {DatabaseConnected}, MemoryUsageMb = {MemoryUsageMb}, ProcessorUtilization = {ProcessorUtilization}, Warnings = {Warnings.Count}, IsHealthy = {IsHealthy} }}";
    }
}

public sealed class HealthWarning
{
    /// <summary>
    /// Gets or sets the severity level of the warning.
    /// </summary>
    public string Severity { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the warning message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Returns a string summarizing the health warning.
    /// </summary>
    public override string ToString()
    {
        return $"HealthWarning {{ Severity = {Severity}, Message = {Message} }}";
    }
}

public sealed class QueueStatus
{
    /// <summary>
    /// Gets or sets the number of pending jobs.
    /// </summary>
    public int PendingCount { get; set; }
    /// <summary>
    /// Gets or sets the number of running jobs.
    /// </summary>
    public int RunningCount { get; set; }
    /// <summary>
    /// Gets or sets the number of failed jobs.
    /// </summary>
    public int FailedCount { get; set; }
    /// <summary>
    /// Gets or sets the number of completed jobs.
    /// </summary>
    public int CompletedCount { get; set; }
    /// <summary>
    /// Gets or sets the number of suspended jobs.
    /// </summary>
    public int SuspendedCount { get; set; }
}
