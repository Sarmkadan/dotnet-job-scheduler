using System.Diagnostics.CodeAnalysis;

namespace JobScheduler.Benchmarks;

public static class JobSchedulerServiceBenchmarksValidation
{
    /// <summary>
    /// Validates the <see cref="JobSchedulerServiceBenchmarks"/> instance for common issues.
    /// </summary>
    /// <param name="value">The benchmark instance to validate.</param>
    /// <returns>List of validation errors; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "value is validated by ArgumentNullException.ThrowIfNull")]
    public static IReadOnlyList<string> Validate(this JobSchedulerServiceBenchmarks value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

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
    /// Checks if the <see cref="JobSchedulerServiceBenchmarks"/> instance is valid.
    /// </summary>
    /// <param name="value">The benchmark instance to check.</param>
    /// <returns>True if valid; false otherwise.</returns>
    public static bool IsValid(this JobSchedulerServiceBenchmarks value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures the <see cref="JobSchedulerServiceBenchmarks"/> instance is valid, throwing an exception if not.
    /// </summary>
    /// <param name="value">The benchmark instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is invalid.</exception>
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
        ArgumentNullException.ThrowIfNull(errors);
        ArgumentException.ThrowIfNullOrEmpty(methodName);

        if (method is null)
        {
            errors.Add($"Benchmark method {methodName} is null");
        }
        else if (method.Method.ReturnType != typeof(void)
            && !method.Method.ReturnType.IsGenericType
            && !method.Method.ReturnType.IsAssignableTo(typeof(System.Threading.Tasks.Task)))
        {
            errors.Add($"Benchmark method {methodName} has unexpected return type: {method.Method.ReturnType.Name}");
        }
    }
}