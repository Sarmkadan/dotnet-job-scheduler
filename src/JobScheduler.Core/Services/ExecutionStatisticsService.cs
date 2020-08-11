#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using JobScheduler.Core.Data.Repositories;
using Microsoft.Extensions.Logging;

namespace JobScheduler.Core.Services;

/// <summary>
/// Computes advanced execution statistics and analytics for jobs.
/// Provides detailed performance analysis including percentiles and trends.
/// WHY: Comprehensive statistics enable data-driven performance optimization.
/// </summary>
public class ExecutionStatisticsService
{
    private readonly IExecutionRepository _executionRepository;
    private readonly IJobRepository _jobRepository;
    private readonly ILogger<ExecutionStatisticsService> _logger;

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
    public async Task<List<PerformanceTrendPoint>> GetPerformanceTrendAsync(Guid jobId, int days = 7)
    {
        try
        {
            var executions = await _executionRepository.GetByJobIdAsync(jobId);
            var cutoff = DateTime.UtcNow.AddDays(-days);

            var trend = executions
                .Where(e => e.StartedAt > cutoff)
                .GroupBy(e => e.StartedAt?.Date)
                .OrderBy(g => g.Key)
                .Select(g => new PerformanceTrendPoint
                {
                    Date = g.Key ?? DateTime.UtcNow.Date,
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

public class PerformanceTrendPoint
{
    public DateTime Date { get; set; }
    public int ExecutionCount { get; set; }
    public long AverageExecutionTimeMs { get; set; }
    public double SuccessRate { get; set; }
    public long MaxExecutionTimeMs { get; set; }
}

public class ExecutionAnomalyReport
{
    public Guid ExecutionId { get; set; }
    public DateTime? Timestamp { get; set; }
    public long ExecutionTimeMs { get; set; }
    public long ExpectedTimeMs { get; set; }
    public double DeviationFactor { get; set; }
    public string AnomalyType { get; set; } = string.Empty;
}
