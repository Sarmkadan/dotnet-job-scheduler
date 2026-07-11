#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Threading.Tasks;
using JobScheduler.Core.Domain.Entities;

namespace JobScheduler.Core.Services;

/// <summary>
/// Extension methods for <see cref="RetryService"/> providing fluent retry configuration and common retry patterns.
/// </summary>
public static class RetryServiceExtensions
{
	/// <summary>
	/// Creates a retry execution with custom executor name.
	/// </summary>
	/// <param name="retryService">The retry service instance.</param>
	/// <param name="job">The job being retried.</param>
	/// <param name="failedExecution">The failed execution to retry.</param>
	/// <param name="executorName">Custom executor name for the retry execution.</param>
	/// <returns>A new job execution configured for retry.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="retryService"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentNullException"><paramref name="job"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentNullException"><paramref name="failedExecution"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentException"><paramref name="executorName"/> is <see langword="null"/>, empty, or consists only of whitespace.</exception>
	public static JobExecution CreateRetryExecution(
		this RetryService retryService,
		Job job,
		JobExecution failedExecution,
		string executorName)
	{
		if (retryService is null)
			throw new ArgumentNullException(nameof(retryService));
		if (job is null)
			throw new ArgumentNullException(nameof(job));
		if (failedExecution is null)
			throw new ArgumentNullException(nameof(failedExecution));
		if (string.IsNullOrWhiteSpace(executorName))
			throw new ArgumentException("Executor name cannot be null or whitespace.", nameof(executorName));

		var retryExecution = retryService.CreateRetryExecution(job, failedExecution);
		retryExecution.ExecutorName = executorName;

		return retryExecution;
	}

	/// <summary>
	/// Calculates the next retry time with a minimum delay threshold.
	/// Ensures retries don't happen too frequently even with small configured delays.
	/// </summary>
	/// <param name="retryService">The retry service instance.</param>
	/// <param name="job">The job being retried.</param>
	/// <param name="failedExecution">The failed execution.</param>
	/// <param name="minimumDelaySeconds">Minimum delay in seconds (default: 5).</param>
	/// <returns>The calculated next retry time.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="retryService"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentNullException"><paramref name="job"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentNullException"><paramref name="failedExecution"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="minimumDelaySeconds"/> is less than 1.</exception>
	public static DateTime CalculateNextRetryTime(
		this RetryService retryService,
		Job job,
		JobExecution failedExecution,
		int minimumDelaySeconds = 5)
	{
		if (retryService is null)
			throw new ArgumentNullException(nameof(retryService));
		if (job is null)
			throw new ArgumentNullException(nameof(job));
		if (failedExecution is null)
			throw new ArgumentNullException(nameof(failedExecution));
		if (minimumDelaySeconds < 1)
			throw new ArgumentOutOfRangeException(nameof(minimumDelaySeconds), "Minimum delay must be at least 1 second.");

		var nextRetryTime = retryService.CalculateNextRetryTime(job, failedExecution);
		var calculatedDelay = nextRetryTime - failedExecution.CompletedAt!.Value;

		// Ensure minimum delay is respected
		if (calculatedDelay.TotalSeconds < minimumDelaySeconds)
		{
			nextRetryTime = failedExecution.CompletedAt.Value.AddSeconds(minimumDelaySeconds);
		}

		return nextRetryTime;
	}

	/// <summary>
	/// Checks if retry budget is exceeded with custom time window.
	/// </summary>
	/// <param name="retryService">The retry service instance.</param>
	/// <param name="jobId">The job identifier.</param>
	/// <param name="retryBudgetCount">Maximum allowed failures in time window (default: 5).</param>
	/// <param name="timeWindowMinutes">Time window in minutes (default: 5).</param>
	/// <returns>True if retry budget is exceeded; otherwise false.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="retryService"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="timeWindowMinutes"/> is less than 1.</exception>
	public static async Task<bool> IsRetryBudgetExceededAsync(
		this RetryService retryService,
		Guid jobId,
		int retryBudgetCount = 5,
		int timeWindowMinutes = 5)
	{
		if (retryService is null)
			throw new ArgumentNullException(nameof(retryService));
		if (timeWindowMinutes < 1)
			throw new ArgumentOutOfRangeException(nameof(timeWindowMinutes), "Time window must be at least 1 minute.");

		return await retryService.IsRetryBudgetExceededAsync(jobId, retryBudgetCount, timeWindowMinutes);
	}

	/// <summary>
	/// Formats a retry message with additional context information.
	/// </summary>
	/// <param name="retryService">The retry service instance.</param>
	/// <param name="attemptNumber">The retry attempt number.</param>
	/// <param name="delay">The delay before retry.</param>
	/// <param name="serverName">The server name.</param>
	/// <param name="jobId">The job identifier for additional context.</param>
	/// <returns>A formatted retry message with job context.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="retryService"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentException"><paramref name="serverName"/> is <see langword="null"/>, empty, or consists only of whitespace.</exception>
	public static string FormatRetryMessage(
		this RetryService retryService,
		int attemptNumber,
		TimeSpan delay,
		string serverName,
		Guid jobId)
	{
		if (retryService is null)
			throw new ArgumentNullException(nameof(retryService));
		if (string.IsNullOrWhiteSpace(serverName))
			throw new ArgumentException("Server name cannot be null or whitespace.", nameof(serverName));

		return $"Job {jobId} - Retry attempt {attemptNumber} scheduled in {delay.TotalSeconds:F0}s on server '{serverName}'.";
	}
}