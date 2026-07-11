using System.Globalization;

namespace JobScheduler.Benchmarks;

public static class JobSchedulerServiceBenchmarksValidation
{
    /// <summary>
    /// Validates the JobSchedulerServiceBenchmarks instance for common issues.
    /// </summary>
    /// <param name="value">The benchmark instance to validate</param>
    /// <returns>List of validation errors; empty if valid</returns>
    /// <exception cref="ArgumentNullException">Thrown if value is null</exception>
    public static IReadOnlyList<string> Validate(this JobSchedulerServiceBenchmarks value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate Setup method
        if (value.Setup == null)
        {
            errors.Add("Setup method is null");
        }

        // Validate benchmark methods
        ValidateBenchmarkMethod(value.CreateJob_Valid, nameof(value.CreateJob_Valid), errors);
        ValidateBenchmarkMethod(value.CreateJob_InvalidCron, nameof(value.CreateJob_InvalidCron), errors);
        ValidateBenchmarkMethod(value.CreateJob_DuplicateName, nameof(value.CreateJob_DuplicateName), errors);
        ValidateBenchmarkMethod(value.GetScheduledJobsForExecution, nameof(value.GetScheduledJobsForExecution), errors);
        ValidateBenchmarkMethod(value.ExecuteDueJobs_EmptyQueue, nameof(value.ExecuteDueJobs_EmptyQueue), errors);
        ValidateBenchmarkMethod(value.ExecuteDueJobs_WithJobs, nameof(value.ExecuteDueJobs_WithJobs), errors);
        ValidateBenchmarkMethod(value.SuspendJob, nameof(value.SuspendJob), errors);
        ValidateBenchmarkMethod(value.ResumeJob, nameof(value.ResumeJob), errors);
        ValidateBenchmarkMethod(value.GetSchedulerStatistics, nameof(value.GetSchedulerStatistics), errors);

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Checks if the JobSchedulerServiceBenchmarks instance is valid.
    /// </summary>
    /// <param name="value">The benchmark instance to check</param>
    /// <returns>True if valid; false otherwise</returns>
    public static bool IsValid(this JobSchedulerServiceBenchmarks value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures the JobSchedulerServiceBenchmarks instance is valid, throwing an exception if not.
    /// </summary>
    /// <param name="value">The benchmark instance to validate</param>
    /// <exception cref="ArgumentException">Thrown if value is invalid</exception>
    public static void EnsureValid(this JobSchedulerServiceBenchmarks value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"JobSchedulerServiceBenchmarks is invalid:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", errors)}");
        }
    }

    private static void ValidateBenchmarkMethod(Delegate? method, string methodName, List<string> errors)
    {
        if (method == null)
        {
            errors.Add($"Benchmark method {methodName} is null");
        }
        else if (method.Method.ReturnType != typeof(void) &&
                 !method.Method.ReturnType.IsGenericType)
        {
            errors.Add($"Benchmark method {methodName} has unexpected return type: {method.Method.ReturnType.Name}");
        }
    }
}