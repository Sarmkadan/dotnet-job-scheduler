#nullable enable

using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace JobScheduler.Core.Controllers;

/// <summary>
/// Provides extension methods for <see cref="DashboardController"/> to enhance dashboard functionality
/// with additional convenience methods and data transformations.
/// </summary>
public static class DashboardControllerExtensions
{
    /// <summary>
    /// Calculates the system health score (0-100) based on current system metrics.
    /// A score below 70 indicates potential issues requiring attention.
    /// </summary>
    /// <param name="controller">The dashboard controller instance.</param>
    /// <returns>A health score between 0 and 100.</returns>
    /// <exception cref="ArgumentNullException">Thrown when controller is null.</exception>
    public static async Task<int> CalculateHealthScoreAsync(this DashboardController controller)
    {
        ArgumentNullException.ThrowIfNull(controller);

        try
        {
            var overview = await controller.GetOverview();
            if (overview.Result is not OkObjectResult okResult || okResult.Value is not DashboardOverview overviewData)
                return 0;

            var healthReport = await controller.GetHealthReport();
            if (healthReport.Result is not OkObjectResult healthResult || healthResult.Value is not HealthReportResponse healthData)
                return 0;

            var queueStatus = await controller.GetQueueStatus();
            if (queueStatus.Result is not OkObjectResult queueResult || queueResult.Value is not QueueStatusResponse queueData)
                return 0;

            // Calculate health score components
            var successRateScore = (int)Math.Round(overviewData.AverageSuccessRate * 100);
            var failedJobsPenalty = Math.Min(30, queueData.FailedJobs * 2);
            var systemWarningsPenalty = healthData.Warnings.Count * 10;
            var queueUtilizationPenalty = (int)Math.Min(20, queueData.QueueUtilization / 5);

            var healthScore = successRateScore - failedJobsPenalty - systemWarningsPenalty - queueUtilizationPenalty;
            healthScore = Math.Max(0, Math.Min(100, healthScore));

            return healthScore;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Gets a simplified health status summary for quick dashboard indicators.
    /// </summary>
    /// <param name="controller">The dashboard controller instance.</param>
    /// <returns>A tuple containing (Status: "Good", "Warning", or "Critical", Color: "green", "yellow", or "red").</returns>
    /// <exception cref="ArgumentNullException">Thrown when controller is null.</exception>
    public static async Task<(string Status, string Color)> GetHealthStatusAsync(this DashboardController controller)
    {
        ArgumentNullException.ThrowIfNull(controller);

        var healthScore = await controller.CalculateHealthScoreAsync();
        var healthReport = await controller.GetHealthReport();

        if (healthReport.Result is not OkObjectResult healthResult || healthResult.Value is not HealthReportResponse healthData)
            return ("Critical", "red");

        if (healthScore >= 80 && healthData.IsHealthy)
            return ("Good", "green");

        if (healthScore >= 50)
            return ("Warning", "yellow");

        return ("Critical", "red");
    }

    /// <summary>
    /// Gets job execution statistics formatted for display with human-readable units.
    /// </summary>
    /// <param name="controller">The dashboard controller instance.</param>
    /// <returns>A dictionary containing formatted display values for key metrics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when controller is null.</exception>
    public static async Task<IReadOnlyDictionary<string, string>> GetFormattedStatisticsAsync(this DashboardController controller)
    {
        ArgumentNullException.ThrowIfNull(controller);

        var overview = await controller.GetOverview();
        var queueStatus = await controller.GetQueueStatus();
        var healthReport = await controller.GetHealthReport();

        var stats = new Dictionary<string, string>(StringComparer.Ordinal);

        if (overview.Result is OkObjectResult okOverview && okOverview.Value is DashboardOverview overviewData)
        {
            stats["Total Jobs"] = overviewData.TotalJobs.ToString(CultureInfo.InvariantCulture);
            stats["Active Jobs"] = overviewData.ActiveJobs.ToString(CultureInfo.InvariantCulture);
            stats["Running"] = overviewData.RunningExecutions.ToString(CultureInfo.InvariantCulture);
            stats["Failed (24h)"] = overviewData.FailedJobsLast24Hours.ToString(CultureInfo.InvariantCulture);
            stats["Success Rate"] = $"{overviewData.AverageSuccessRate:P1}";
            stats["Avg Execution"] = $"{overviewData.AverageExecutionTimeMs:N0}ms";
            stats["Last Updated"] = overviewData.LastUpdatedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

        if (queueStatus.Result is OkObjectResult okQueue && okQueue.Value is QueueStatusResponse queueData)
        {
            stats["Pending"] = queueData.PendingJobs.ToString(CultureInfo.InvariantCulture);
            stats["Queue Util"] = $"{queueData.QueueUtilization:N1}%";

            if (queueData.EstimatedTimeToEmpty.HasValue)
            {
                var timeToEmpty = queueData.EstimatedTimeToEmpty.Value;
                stats["Time to Empty"] = timeToEmpty.TotalHours >= 1
                    ? $"{timeToEmpty.TotalHours:N1}h"
                    : $"{timeToEmpty.TotalMinutes:N0}m";
            }
        }

        if (healthReport.Result is OkObjectResult okHealth && okHealth.Value is HealthReportResponse healthData)
        {
            stats["Memory Usage"] = $"{healthData.MemoryUsageMb:N0}MB";
            stats["CPU Usage"] = $"{healthData.ProcessorUtilization:N1}%";
            stats["Database"] = healthData.DatabaseConnected ? "Connected" : "Disconnected";
        }

        return stats;
    }

    /// <summary>
    /// Gets the top N jobs by failure rate with additional context.
    /// </summary>
    /// <param name="controller">The dashboard controller instance.</param>
    /// <param name="count">Number of jobs to return (default: 5).</param>
    /// <returns>A list of job failure analysis objects with additional metrics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when controller is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when count is less than 1.</exception>
    public static async Task<IReadOnlyList<JobFailureAnalysis>> GetTopFailingJobsWithAnalysisAsync(
        this DashboardController controller,
        int count = 5)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);

        var failingJobs = await controller.GetMostFailingJobs();

        if (failingJobs.Result is not OkObjectResult okResult || okResult.Value is not List<FailingJobResponse> failingJobData)
            return Array.Empty<JobFailureAnalysis>();

        var slowestJobs = await controller.GetSlowestJobs();
        var slowestJobData = slowestJobs.Result is OkObjectResult slowestOkResult
            ? slowestOkResult.Value as List<SlowestJobResponse>
            : null;

        var analysisList = new List<JobFailureAnalysis>();

        foreach (var job in failingJobData.Take(count))
        {
            var isSlow = slowestJobData?.Any(s => s.JobId == job.JobId) ?? false;
            var failureImpact = job.FailedCount * (job.FailureRate / 100);

            analysisList.Add(new JobFailureAnalysis
            {
                JobId = job.JobId,
                JobName = job.JobName,
                FailureRate = job.FailureRate,
                FailedCount = job.FailedCount,
                SuccessRate = job.SuccessRate,
                IsSlow = isSlow,
                FailureImpactScore = failureImpact,
                Status = failureImpact > 50 ? "High Impact" : failureImpact > 20 ? "Medium Impact" : "Low Impact"
            });
        }

        return analysisList.AsReadOnly();
    }
}

/// <summary>
/// Represents a detailed analysis of job failures with additional context.
/// </summary>
public sealed class JobFailureAnalysis
{
    /// <summary>Gets the unique identifier of the job.</summary>
    public Guid JobId { get; init; }

    /// <summary>Gets the name of the job.</summary>
    public string JobName { get; init; } = string.Empty;

    /// <summary>Gets the failure rate percentage (0-100).</summary>
    public double FailureRate { get; init; }

    /// <summary>Gets the total number of failed executions.</summary>
    public int FailedCount { get; init; }

    /// <summary>Gets the success rate percentage (0-100).</summary>
    public double SuccessRate { get; init; }

    /// <summary>Gets whether the job is also among the slowest jobs.</summary>
    public bool IsSlow { get; init; }

    /// <summary>Gets a calculated impact score based on failure count and rate.</summary>
    public double FailureImpactScore { get; init; }

    /// <summary>Gets a human-readable status based on the failure impact.</summary>
    public string Status { get; init; } = string.Empty;
}