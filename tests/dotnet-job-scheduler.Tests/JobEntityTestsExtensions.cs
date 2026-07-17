#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Constants;

namespace DotnetJobScheduler.Tests;

/// <summary>
/// Extension methods for <see cref="JobEntityTests"/> that provide additional testing utilities
/// for job entity validation and metrics calculation.
/// </summary>
public static class JobEntityTestsExtensions
{
    /// <summary>
    /// Creates a job with minimal valid configuration for testing purposes.
    /// </summary>
    /// <param name="_">The <see cref="JobEntityTests"/> instance.</param>
    /// <param name="name">The job name.</param>
    /// <param name="handlerType">The handler type string.</param>
    /// <param name="maxRetries">Maximum retry count (default: 3).</param>
    /// <param name="executionTimeoutSeconds">Execution timeout in seconds (default: 300).</param>
    /// <param name="maxConcurrentExecutions">Maximum concurrent executions (default: 1).</param>
    /// <returns>A configured <see cref="Job"/> entity ready for testing.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> or <paramref name="handlerType"/> is null or empty.</exception>
    public static Job CreateMinimalValidJob(
        this JobEntityTests _,
        string name = "test-job",
        string handlerType = "TestHandler, TestAssembly",
        int maxRetries = 3,
        int executionTimeoutSeconds = 300,
        int maxConcurrentExecutions = 1)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(handlerType);

        return new Job
        {
            Name = name,
            CronExpression = "0 0 * * *",
            HandlerType = handlerType,
            MaxRetries = maxRetries,
            ExecutionTimeoutSeconds = executionTimeoutSeconds,
            MaxConcurrentExecutions = maxConcurrentExecutions,
            Status = JobStatus.Scheduled
        };
    }

    /// <summary>
    /// Creates a job with invalid configuration (missing handler type) for negative testing.
    /// </summary>
    /// <param name="_">The <see cref="JobEntityTests"/> instance.</param>
    /// <param name="name">The job name.</param>
    /// <returns>A <see cref="Job"/> with empty handler type to test validation failures.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or empty.</exception>
    public static Job CreateInvalidHandlerJob(
        this JobEntityTests _,
        string name = "invalid-job")
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        return new Job
        {
            Name = name,
            CronExpression = "0 0 * * *",
            HandlerType = string.Empty,
            MaxRetries = 3,
            ExecutionTimeoutSeconds = 300,
            MaxConcurrentExecutions = 1,
            Status = JobStatus.Scheduled
        };
    }

    /// <summary>
    /// Creates a job with execution metrics already populated for testing success rate calculations.
    /// </summary>
    /// <param name="_">The <see cref="JobEntityTests"/> instance.</param>
    /// <param name="successCount">The number of successful executions.</param>
    /// <param name="failureCount">The number of failed executions.</param>
    /// <param name="name">The job name.</param>
    /// <returns>A <see cref="Job"/> with pre-populated execution metrics.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or empty, or <paramref name="successCount"/>/<paramref name="failureCount"/> is negative.</exception>
    public static Job CreateJobWithMetrics(
        this JobEntityTests _,
        int successCount,
        int failureCount,
        string name = "metrics-job")
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        if (successCount < 0)
        {
            throw new ArgumentException("Success count cannot be negative", nameof(successCount));
        }

        if (failureCount < 0)
        {
            throw new ArgumentException("Failure count cannot be negative", nameof(failureCount));
        }

        var job = new Job
        {
            Name = name,
            CronExpression = "0 0 * * *",
            HandlerType = "TestHandler, TestAssembly",
            MaxRetries = 3,
            ExecutionTimeoutSeconds = 300,
            MaxConcurrentExecutions = 1,
            Status = JobStatus.Scheduled
        };

        for (var i = 0; i < successCount; i++)
        {
            job.UpdateExecutionMetrics(success: true);
        }

        for (var i = 0; i < failureCount; i++)
        {
            job.UpdateExecutionMetrics(success: false);
        }

        return job;
    }

    /// <summary>
    /// Creates a job with suspended status for testing execution blocking.
    /// </summary>
    /// <param name="_">The <see cref="JobEntityTests"/> instance.</param>
    /// <param name="name">The job name.</param>
    /// <returns>A <see cref="Job"/> that should not execute.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or empty.</exception>
    public static Job CreateSuspendedJob(
        this JobEntityTests _,
        string name = "suspended-job")
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        return new Job
        {
            Name = name,
            CronExpression = "0 0 * * *",
            HandlerType = "TestHandler, TestAssembly",
            MaxRetries = 3,
            ExecutionTimeoutSeconds = 300,
            MaxConcurrentExecutions = 1,
            Status = JobStatus.Suspended
        };
    }

    /// <summary>
    /// Creates a job with concurrency limit for testing concurrent execution limits.
    /// </summary>
    /// <param name="_">The <see cref="JobEntityTests"/> instance.</param>
    /// <param name="maxConcurrentExecutions">Maximum concurrent execution slots.</param>
    /// <param name="currentConcurrentCount">Current number of active executions.</param>
    /// <param name="name">The job name.</param>
    /// <returns>A job configured for concurrency testing.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or empty, or <paramref name="maxConcurrentExecutions"/>/<paramref name="currentConcurrentCount"/> is negative.</exception>
    public static Job CreateConcurrentJob(
        this JobEntityTests _,
        int maxConcurrentExecutions,
        int currentConcurrentCount,
        string name = "concurrent-job")
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        if (maxConcurrentExecutions < 0)
        {
            throw new ArgumentException("Max concurrent executions cannot be negative", nameof(maxConcurrentExecutions));
        }

        if (currentConcurrentCount < 0)
        {
            throw new ArgumentException("Current concurrent count cannot be negative", nameof(currentConcurrentCount));
        }

        return new Job
        {
            Name = name,
            CronExpression = "0 0 * * *",
            HandlerType = "TestHandler, TestAssembly",
            MaxRetries = 3,
            ExecutionTimeoutSeconds = 300,
            MaxConcurrentExecutions = maxConcurrentExecutions,
            Status = JobStatus.Scheduled
        };
    }

    /// <summary>
    /// Gets the execution metrics summary as a formatted string.
    /// </summary>
    /// <param name="_">The <see cref="JobEntityTests"/> instance.</param>
    /// <param name="job">The <see cref="Job"/> entity to summarize.</param>
    /// <returns>A formatted <see cref="string"/> showing execution metrics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="job"/> is null.</exception>
    public static string GetExecutionMetricsSummary(this JobEntityTests _, Job job)
    {
        ArgumentNullException.ThrowIfNull(job);

        return $"Executions: {job.TotalExecutions}, Success: {job.SuccessfulExecutions}, Failure: {job.FailedExecutions}, Success Rate: {job.GetSuccessRate():P1}";
    }

    /// <summary>
    /// Determines whether the job configuration is valid for scheduling with detailed validation errors.
    /// </summary>
    /// <param name="_">The <see cref="JobEntityTests"/> instance.</param>
    /// <param name="job">The <see cref="Job"/> entity to validate.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> of validation error messages, or empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="job"/> is null.</exception>
    public static IEnumerable<string> GetValidationErrors(this JobEntityTests _, Job job)
    {
        ArgumentNullException.ThrowIfNull(job);

        var errors = new List<string>();

        if (string.IsNullOrEmpty(job.HandlerType))
        {
            errors.Add("HandlerType is required");
        }

        if (job.ExecutionTimeoutSeconds <= 0)
        {
            errors.Add("ExecutionTimeoutSeconds must be greater than zero");
        }

        if (job.MaxRetries < 0)
        {
            errors.Add("MaxRetries cannot be negative");
        }

        if (job.MaxConcurrentExecutions <= 0)
        {
            errors.Add("MaxConcurrentExecutions must be greater than zero");
        }

        if (string.IsNullOrEmpty(job.CronExpression))
        {
            errors.Add("CronExpression is required");
        }

        return errors.AsReadOnly();
    }
}