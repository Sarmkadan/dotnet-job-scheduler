namespace JobScheduler.Core.Domain.Entities;

/// <summary>
/// Extension methods for <see cref="ExecutionMetrics"/>.
/// </summary>
public static class ExecutionMetricsExtensions
{
    /// <summary>
    /// Determines if the execution metrics indicate a consistently reliable job.
    /// </summary>
    /// <param name="metrics">The execution metrics to evaluate.</param>
    /// <param name="minimumSuccessRatePercent">The minimum success rate percentage required.</param>
    /// <param name="minimumExecutions">The minimum number of executions required.</param>
    /// <returns>True if the job is consistently reliable; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="metrics"/> is null.</exception>
    public static bool IsConsistentlyReliable(this ExecutionMetrics metrics, double minimumSuccessRatePercent = 90.0, int minimumExecutions = 10)
    {
        ArgumentNullException.ThrowIfNull(metrics);

        return metrics.IsReliable(minimumSuccessRatePercent) && metrics.TotalExecutions >= minimumExecutions;
    }

    /// <summary>
    /// Calculates the average execution duration in seconds.
    /// </summary>
    /// <param name="metrics">The execution metrics to evaluate.</param>
    /// <returns>The average execution duration in seconds.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="metrics"/> is null.</exception>
    public static double GetAverageExecutionDurationInSeconds(this ExecutionMetrics metrics)
    {
        ArgumentNullException.ThrowIfNull(metrics);

        return metrics.AverageDurationMs / 1000.0;
    }

    /// <summary>
    /// Determines if the job has a high failure rate.
    /// </summary>
    /// <param name="metrics">The execution metrics to evaluate.</param>
    /// <param name="thresholdFailureRate">The threshold failure rate.</param>
    /// <returns>True if the job has a high failure rate; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="metrics"/> is null.</exception>
    public static bool HasHighFailureRate(this ExecutionMetrics metrics, double thresholdFailureRate = 0.1)
    {
        ArgumentNullException.ThrowIfNull(metrics);

        return 1 - metrics.SuccessRate >= thresholdFailureRate;
    }
}
