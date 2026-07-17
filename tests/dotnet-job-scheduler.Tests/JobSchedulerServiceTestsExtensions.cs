#nullable enable

using FluentAssertions;
using JobScheduler.Core.Constants;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Exceptions;

namespace DotnetJobScheduler.Tests;

public static class JobSchedulerServiceTestsExtensions
{
    /// <summary>
    /// Creates a test job with the specified name and returns it.
    /// </summary>
    /// <param name="name">The job name (default: "test-job-{Guid.NewGuid()}")</param>
    /// <returns>A valid test job instance.</returns>
    /// <exception cref="ArgumentException">Thrown when name is null or whitespace.</exception>
    public static Job CreateTestJob(this JobSchedulerServiceTests _, string name = "test-job")
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        return new Job
        {
            Id = Guid.NewGuid(),
            Name = name,
            CronExpression = "0 9 * * *",
            HandlerType = "TestApp.Jobs.TestJob, TestApp",
            MaxRetries = 3,
            ExecutionTimeoutSeconds = 300,
            MaxConcurrentExecutions = 1,
            Status = JobStatus.Scheduled
        };
    }

    /// <summary>
    /// Creates a test job execution with the specified parameters.
    /// </summary>
    /// <param name="jobId">The job ID.</param>
    /// <param name="status">The execution status (default: Success).</param>
    /// <param name="attemptNumber">The attempt number (default: 1).</param>
    /// <returns>A test job execution instance.</returns>
    /// <exception cref="ArgumentException">Thrown when jobId is empty.</exception>
    public static JobExecution CreateTestExecution(
        this JobSchedulerServiceTests _,
        Guid jobId,
        ExecutionStatus status = ExecutionStatus.Success,
        int attemptNumber = 1)
    {
        if (jobId == Guid.Empty)
        {
            throw new ArgumentException("Job ID cannot be empty", nameof(jobId));
        }

        return new JobExecution
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            Status = status,
            AttemptNumber = attemptNumber,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = status == ExecutionStatus.Success ? DateTime.UtcNow : null
        };
    }

    /// <summary>
    /// Asserts that a job has the expected basic properties.
    /// </summary>
    /// <param name="job">The job to assert.</param>
    /// <param name="expectedName">The expected job name.</param>
    /// <param name="expectedStatus">The expected job status (default: Scheduled).</param>
    /// <param name="expectedCron">The expected cron expression.</param>
    /// <exception cref="ArgumentNullException">Thrown when job is null.</exception>
    /// <exception cref="ArgumentException">Thrown when expectedName is null or whitespace.</exception>
    public static void ShouldHaveBasicJobProperties(
        this JobSchedulerServiceTests _,
        Job job,
        string expectedName,
        JobStatus expectedStatus = JobStatus.Scheduled,
        string? expectedCron = null)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentException.ThrowIfNullOrEmpty(expectedName);

        job.Should().NotBeNull();
        job.Name.Should().Be(expectedName);
        job.Status.Should().Be(expectedStatus);

        if (expectedCron is not null)
        {
            job.CronExpression.Should().Be(expectedCron);
        }

        job.Id.Should().NotBe(Guid.Empty);
        job.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Creates a collection of test jobs with sequential names.
    /// </summary>
    /// <param name="count">Number of jobs to create.</param>
    /// <param name="baseName">Base name for jobs (default: "test-job").</param>
    /// <returns>Read-only list of created jobs.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when count is less than 1.</exception>
    public static IReadOnlyList<Job> CreateTestJobs(
        this JobSchedulerServiceTests _,
        int count,
        string baseName = "test-job")
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        ArgumentException.ThrowIfNullOrEmpty(baseName);

        var jobs = new List<Job>(count);
        for (var i = 0; i < count; i++)
        {
            jobs.Add(new Job
            {
                Id = Guid.NewGuid(),
                Name = $"{baseName}-{i + 1}",
                CronExpression = "0 9 * * *",
                HandlerType = "TestApp.Jobs.TestJob, TestApp",
                MaxRetries = 3,
                ExecutionTimeoutSeconds = 300,
                MaxConcurrentExecutions = 1,
                Status = JobStatus.Scheduled
            });
        }

        return jobs.AsReadOnly();
    }
}