#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using JobScheduler.Core.Data.Repositories;
using JobScheduler.Core.Domain.Models;
using Microsoft.Extensions.Logging;

namespace JobScheduler.Core.Services;

/// <summary>
/// Computes advanced execution statistics and analytics for jobs.
/// Provides detailed performance analysis including percentiles and trends.
/// WHY: Comprehensive statistics enable data-driven performance optimization.
/// </summary>
public sealed class ExecutionStatisticsService
{
    private readonly IExecutionRepository _executionRepository;
    private readonly IJobRepository _jobRepository;
    private readonly ILogger<ExecutionStatisticsService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionStatisticsService"/> class.
    /// </summary>
    /// <param name="executionRepository">The execution repository.</param>
    /// <param name="jobRepository">The job repository.</param>
    /// <param name="logger">The logger.</param>
    public ExecutionStatisticsService(
        IExecutionRepository executionRepository,
        IJobRepository jobRepository,
        ILogger<ExecutionStatisticsService> logger)
    {
        _executionRepository = executionRepository ?? throw new ArgumentNullException(nameof(executionRepository));
        _jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets comprehensive execution statistics for a specific job.
    /// Includes success rates, execution time metrics, and trends.
    /// </summary>
    /// <param name="jobId">The job identifier.</param>
    /// <returns>Execution statistics for the job, or null if the job is not found.</returns>
    public async Task<ExecutionStatsResponse?> GetJobExecutionStatsAsync(Guid jobId)
    {
        try
        {
            var job = await _jobRepository.GetByIdAsync(jobId);
            if (job is null)
                return null;

            var executions = await _executionRepository.GetByJobIdAsync(jobId);
            if (!executions.Any())
            {
                return new ExecutionStatsResponse
                {
                    JobId = jobId,
                    TotalExecutions = 0,
                    SuccessfulExecutions = 0,
                    FailedExecutions = 0,
                    SuccessRate = 100
                };
            }

            var successful = executions.Count(e => e.Status.ToString() == "Completed");
            var times = executions.Select(e => e.ExecutionTimeMs).OrderBy(t => t).ToList();

            return new ExecutionStatsResponse
            {
                JobId = jobId,
                TotalExecutions = executions.Count,
                SuccessfulExecutions = successful,
                FailedExecutions = executions.Count - successful,
                SuccessRate = (double)successful / executions.Count * 100,
                AverageExecutionTimeMs = (long)times.Average(),
                MinExecutionTimeMs = times.FirstOrDefault(),
                MaxExecutionTimeMs = times.LastOrDefault(),
                LastExecutionAt = executions.Max(e => e.StartedAt)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating execution statistics for job: {JobId}", jobId);
            return null;
        }
    }

    /// <summary>
    /// Analyzes performance characteristics including percentiles and anomaly detection.
    /// </summary>
    /// <param name="jobId">The job identifier.</param>
    /// <returns>Performance analysis for the job, or null if no executions are found.</returns>
    public async Task<PerformanceAnalysisResponse?> GetJobPerformanceAnalysisAsync(Guid jobId)
    {
        try
        {
            var executions = await _executionRepository.GetByJobIdAsync(jobId);
            if (!executions.Any())
                return null;

            var times = executions
                .Select(e => e.ExecutionTimeMs)
                .OrderBy(t => t)
                .ToList();

            var analysis = new PerformanceAnalysisResponse
            {
                JobId = jobId,
                AverageExecutionTimeMs = (long)times.Average(),
                MedianExecutionTimeMs = GetPercentile(times, 50),
                P95ExecutionTimeMs = GetPercentile(times, 95),
                P99ExecutionTimeMs = GetPercentile(times, 99),
                SlowestExecutionTimeMs = times.Max(),
                FastestExecutionTimeMs = times.Min()
            };

            // Find when slowest and fastest executions occurred
            var slowest = executions.OrderByDescending(e => e.ExecutionTimeMs).FirstOrDefault();
            var fastest = executions.OrderBy(e => e.ExecutionTimeMs).FirstOrDefault();

            analysis.SlowestExecutionAt = slowest?.StartedAt;
            analysis.FastestExecutionAt = fastest?.StartedAt;

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing job performance: {JobId}", jobId);
            return null;
        }
    }

    /// <summary>
    /// Generates a trend report showing performance changes over time.
    /// Useful for identifying performance degradation.
    /// </summary>
    /// <param name="jobId">The job identifier.</param>
    /// <param name="days">The number of days to look back for trend data (default is 7).</param>
    /// <returns>A list of performance trend points.</returns>
    public async Task<List<PerformanceTrendPoint>> GetPerformanceTrendAsync(Guid jobId, int days = 7)
    {
        try
        {
            var executions = await _executionRepository.GetByJobIdAsync(jobId);
            var cutoff = DateTime.UtcNow.AddDays(-days);

            var trend = executions
                .Where(e => e.StartedAt > cutoff)
                .GroupBy(e => e.StartedAt.Date)
                .OrderBy(g => g.Key)
                .Select(g => new PerformanceTrendPoint
                {
                    Date = g.Key,
                    ExecutionCount = g.Count(),
                    AverageExecutionTimeMs = (long)g.Average(e => e.ExecutionTimeMs),
                    SuccessRate = (double)g.Count(e => e.Status.ToString() == "Completed") / g.Count() * 100,
                    MaxExecutionTimeMs = g.Max(e => e.ExecutionTimeMs)
                })
                .ToList();

            return trend;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating performance trend: {JobId}", jobId);
            return new();
        }
    }

    /// <summary>
    /// Detects anomalous execution times using standard deviation.
    /// Helps identify performance issues or resource constraints.
    /// </summary>
    /// <param name="jobId">The job identifier.</param>
    /// <returns>A list of execution anomaly reports.</returns>
    public async Task<List<ExecutionAnomalyReport>> DetectExecutionAnomaliesAsync(Guid jobId)
    {
        try
        {
            var executions = await _executionRepository.GetByJobIdAsync(jobId);
            if (executions.Count < 5) // Need minimum data for meaningful analysis
                return new();

            var times = executions.Select(e => e.ExecutionTimeMs).ToList();
            var mean = times.Average();
            var stdDev = Math.Sqrt(times.Average(t => Math.Pow(t - mean, 2)));

            var anomalies = new List<ExecutionAnomalyReport>();

            // Executions more than 2 standard deviations away are anomalies
            foreach (var execution in executions)
            {
                var deviation = Math.Abs(execution.ExecutionTimeMs - mean) / stdDev;
                if (deviation > 2)
                {
                    anomalies.Add(new ExecutionAnomalyReport
                    {
                        ExecutionId = execution.Id,
                        Timestamp = execution.StartedAt,
                        ExecutionTimeMs = execution.ExecutionTimeMs,
                        ExpectedTimeMs = (long)mean,
                        DeviationFactor = deviation,
                        AnomalyType = execution.ExecutionTimeMs > mean ? "SlowExecution" : "FastExecution"
                    });
                }
            }

            return anomalies.OrderByDescending(a => a.DeviationFactor).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting execution anomalies: {JobId}", jobId);
            return new();
        }
    }

    /// <summary>
    /// Calculates the value at the specified percentile in a list of values.
    /// </summary>
    /// <param name="values">The list of values.</param>
    /// <param name="percentile">The percentile to calculate (0-100).</param>
    /// <returns>The value at the specified percentile.</returns>
    private long GetPercentile(List<long> values, int percentile)
    {
        if (values.Count == 0)
            return 0;

        var index = (percentile / 100.0) * (values.Count - 1);
        var lower = (int)Math.Floor(index);
        var upper = (int)Math.Ceiling(index);

        if (lower == upper)
            return values[lower];

        var weight = index - lower;
        return (long)(values[lower] * (1 - weight) + values[upper] * weight);
    }
}

/// <summary>
/// Represents a data point in a performance trend report.
/// </summary>
public sealed class PerformanceTrendPoint
{
    /// <summary>
    /// Gets or sets the date for this trend point.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Gets or sets the number of executions on this date.
    /// </summary>
    public int ExecutionCount { get; set; }

    /// <summary>
    /// Gets or sets the average execution time in milliseconds for this date.
    /// </summary>
    public long AverageExecutionTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the success rate percentage for this date.
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Gets or sets the maximum execution time in milliseconds for this date.
    /// </summary>
    public long MaxExecutionTimeMs { get; set; }
}

/// <summary>
/// Represents an execution anomaly detected in job performance analysis.
/// </summary>
public sealed class ExecutionAnomalyReport
{
    /// <summary>
    /// Gets or sets the unique identifier of the execution.
    /// </summary>
    public Guid ExecutionId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the execution occurred.
    /// </summary>
    public DateTime? Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the actual execution time in milliseconds.
    /// </summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the expected execution time in milliseconds.
    /// </summary>
    public long ExpectedTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the deviation factor (how many standard deviations from the mean).
    /// </summary>
    public double DeviationFactor { get; set; }

    /// <summary>
    /// Gets or sets the type of anomaly (SlowExecution or FastExecution).
    /// </summary>
    public string AnomalyType { get; set; } = string.Empty;
}
