using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JobScheduler.Core.Constants;
using JobScheduler.Core.Data.Repositories;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using Xunit;

namespace JobScheduler.Core.Tests;

/// <summary>
/// Tests for diagnosability improvements: DebuggerDisplay/ToString on entities,
/// ExecutionTracker.GetSnapshot, and JobSchedulerHealthCheck.
/// </summary>
public class DiagnosabilityTests
{
    // ---------------------------------------------------------------
    // ToString / DebuggerDisplay format
    // ---------------------------------------------------------------

    [Fact]
    public void Job_ToString_ContainsCronAndNextExecution()
    {
        var next = new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);
        var job = new Job
        {
            Id = Guid.NewGuid(),
            CronExpression = "0 * * * *",
            NextExecutionAt = next
        };

        var result = job.ToString();

        Assert.Contains("[0 * * * *]", result);
        Assert.Contains("next=", result);
        Assert.Contains(job.Id.ToString(), result);
    }

    [Fact]
    public void JobExecution_ToString_ContainsAttemptAndStatus()
    {
        var exec = new JobExecution
        {
            JobId = Guid.NewGuid(),
            AttemptNumber = 2,
            Status = ExecutionStatus.Failed,
            DurationMilliseconds = 1500
        };

        var result = exec.ToString();

        Assert.Contains("#2", result);
        Assert.Contains("Failed", result);
        Assert.Contains("1500ms", result);
    }

    [Fact]
    public void RetryPolicy_ToString_ContainsStrategyAndLimits()
    {
        var policy = new RetryPolicy
        {
            Strategy = BackoffStrategy.Exponential,
            MaxRetries = 5,
            InitialBackoffSeconds = 10,
            MaxBackoffSeconds = 300
        };

        var result = policy.ToString();

        Assert.Contains("Exponential", result);
        Assert.Contains("max=5", result);
        Assert.Contains("init=10s", result);
        Assert.Contains("cap=300s", result);
    }

    [Fact]
    public void CronScheduleInfo_ToString_ContainsExpressionAndNext()
    {
        var next = new DateTime(2026, 10, 21, 9, 0, 0, DateTimeKind.Utc);
        var info = new CronScheduleInfo("0 9 * * *", next, "At 09:00");

        var result = info.ToString();

        Assert.Contains("0 9 * * *", result);
        Assert.Contains("At 09:00", result);
    }

    // ---------------------------------------------------------------
    // ExecutionTracker.GetSnapshot
    // ---------------------------------------------------------------

    [Fact]
    public async Task GetSnapshot_ReturnsPerJobStats()
    {
        var jobId = Guid.NewGuid();
        var job = new Job { Id = jobId, Name = "TestJob", CronExpression = "0 * * * *" };

        var executions = new List<JobExecution>
        {
            new() { JobId = jobId, Status = ExecutionStatus.Success, DurationMilliseconds = 100, StartedAt = DateTime.UtcNow.AddMinutes(-10) },
            new() { JobId = jobId, Status = ExecutionStatus.Success, DurationMilliseconds = 200, StartedAt = DateTime.UtcNow.AddMinutes(-5) },
            new() { JobId = jobId, Status = ExecutionStatus.Failed, DurationMilliseconds = 50, StartedAt = DateTime.UtcNow.AddMinutes(-1) },
        };

        var jobRepo = new Mock<IJobRepository>();
        jobRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new[] { job });

        var execRepo = new Mock<IExecutionRepository>();
        // GetExecutionsByJobAsync returns newest first
        execRepo.Setup(r => r.GetExecutionsByJobAsync(jobId))
            .ReturnsAsync(executions.OrderByDescending(e => e.StartedAt));

        var tracker = new ExecutionTracker(jobRepo.Object, execRepo.Object);

        var snapshot = await tracker.GetSnapshotAsync();

        Assert.Single(snapshot);
        Assert.True(snapshot.ContainsKey(jobId));

        var stats = snapshot[jobId];
        Assert.Equal("TestJob", stats.JobName);
        Assert.Equal(2, stats.SuccessCount);
        Assert.Equal(1, stats.FailureCount);
        Assert.Equal(150, stats.AverageDurationMs); // (100+200)/2
        Assert.Equal(0, stats.CurrentlyRunning);
        Assert.NotNull(stats.LastRunAt);
        Assert.Equal(ExecutionStatus.Failed, stats.LastStatus); // newest first
    }

    [Fact]
    public async Task GetSnapshot_EmptyJobs_ReturnsEmptyDict()
    {
        var jobRepo = new Mock<IJobRepository>();
        jobRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(Array.Empty<Job>());

        var execRepo = new Mock<IExecutionRepository>();

        var tracker = new ExecutionTracker(jobRepo.Object, execRepo.Object);
        var snapshot = await tracker.GetSnapshotAsync();

        Assert.Empty(snapshot);
    }

    // ---------------------------------------------------------------
    // JobSchedulerHealthCheck
    // ---------------------------------------------------------------

    [Fact]
    public async Task HealthCheck_Unhealthy_WhenNoHeartbeat()
    {
        JobSchedulerHealthCheck.LastHeartbeat = DateTime.MinValue;

        var check = CreateHealthCheck();
        var result = await check.CheckHealthAsync(CreateContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("not running", result.Description);
    }

    [Fact]
    public async Task HealthCheck_Unhealthy_WhenHeartbeatStale()
    {
        // Set heartbeat to 10 seconds ago - with 1000ms poll, threshold is 2s
        JobSchedulerHealthCheck.LastHeartbeat = DateTime.UtcNow.AddSeconds(-10);

        var check = CreateHealthCheck(pollIntervalMs: 1000);
        var result = await check.CheckHealthAsync(CreateContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task HealthCheck_Unhealthy_WhenJobOverdue()
    {
        JobSchedulerHealthCheck.LastHeartbeat = DateTime.UtcNow;

        var overdueJob = new Job
        {
            Id = Guid.NewGuid(),
            Name = "OverdueJob",
            IsActive = true,
            Status = JobStatus.Scheduled,
            NextExecutionAt = DateTime.UtcNow.AddSeconds(-30) // 30s overdue
        };

        var jobRepo = new Mock<IJobRepository>();
        jobRepo.Setup(r => r.GetActiveJobsAsync()).ReturnsAsync(new[] { overdueJob });

        var execRepo = new Mock<IExecutionRepository>();

        var check = new JobSchedulerHealthCheck(jobRepo.Object, execRepo.Object, pollIntervalMs: 1000);
        var result = await check.CheckHealthAsync(CreateContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("overdue", result.Description);
    }

    [Fact]
    public async Task HealthCheck_Degraded_WhenLastRunFailed()
    {
        JobSchedulerHealthCheck.LastHeartbeat = DateTime.UtcNow;

        var jobId = Guid.NewGuid();
        var job = new Job
        {
            Id = jobId,
            Name = "FailingJob",
            IsActive = true,
            Status = JobStatus.Scheduled,
            NextExecutionAt = DateTime.UtcNow.AddMinutes(5)
        };

        var failedExec = new JobExecution
        {
            JobId = jobId,
            Status = ExecutionStatus.Failed,
            StartedAt = DateTime.UtcNow.AddMinutes(-1)
        };

        var jobRepo = new Mock<IJobRepository>();
        jobRepo.Setup(r => r.GetActiveJobsAsync()).ReturnsAsync(new[] { job });

        var execRepo = new Mock<IExecutionRepository>();
        execRepo.Setup(r => r.GetLatestExecutionAsync(jobId)).ReturnsAsync(failedExec);

        var check = new JobSchedulerHealthCheck(jobRepo.Object, execRepo.Object, pollIntervalMs: 1000);
        var result = await check.CheckHealthAsync(CreateContext());

        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Contains("failed", result.Description);
    }

    [Fact]
    public async Task HealthCheck_Healthy_WhenEverythingNominal()
    {
        JobSchedulerHealthCheck.LastHeartbeat = DateTime.UtcNow;

        var jobId = Guid.NewGuid();
        var job = new Job
        {
            Id = jobId,
            Name = "GoodJob",
            IsActive = true,
            Status = JobStatus.Scheduled,
            NextExecutionAt = DateTime.UtcNow.AddMinutes(5)
        };

        var successExec = new JobExecution
        {
            JobId = jobId,
            Status = ExecutionStatus.Success,
            StartedAt = DateTime.UtcNow.AddMinutes(-1)
        };

        var jobRepo = new Mock<IJobRepository>();
        jobRepo.Setup(r => r.GetActiveJobsAsync()).ReturnsAsync(new[] { job });

        var execRepo = new Mock<IExecutionRepository>();
        execRepo.Setup(r => r.GetLatestExecutionAsync(jobId)).ReturnsAsync(successExec);

        var check = new JobSchedulerHealthCheck(jobRepo.Object, execRepo.Object, pollIntervalMs: 1000);
        var result = await check.CheckHealthAsync(CreateContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public void RecordHeartbeat_UpdatesTimestamp()
    {
        JobSchedulerHealthCheck.LastHeartbeat = DateTime.MinValue;

        var before = DateTime.UtcNow;
        JobSchedulerHealthCheck.RecordHeartbeat();
        var after = DateTime.UtcNow;

        Assert.True(JobSchedulerHealthCheck.LastHeartbeat >= before);
        Assert.True(JobSchedulerHealthCheck.LastHeartbeat <= after);
    }

    [Fact]
    public void JobExecutionSnapshot_ToString_ContainsAllFields()
    {
        var snapshot = new JobExecutionSnapshot
        {
            JobId = Guid.NewGuid(),
            JobName = "MyJob",
            LastRunAt = DateTime.UtcNow,
            LastStatus = ExecutionStatus.Success,
            SuccessCount = 10,
            FailureCount = 2,
            AverageDurationMs = 350,
            CurrentlyRunning = 1
        };

        var str = snapshot.ToString();

        Assert.Contains("MyJob", str);
        Assert.Contains("ok=10", str);
        Assert.Contains("fail=2", str);
        Assert.Contains("running=1", str);
    }

    // ---------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------

    private static JobSchedulerHealthCheck CreateHealthCheck(int pollIntervalMs = 1000)
    {
        var jobRepo = new Mock<IJobRepository>();
        jobRepo.Setup(r => r.GetActiveJobsAsync()).ReturnsAsync(Array.Empty<Job>());

        var execRepo = new Mock<IExecutionRepository>();

        return new JobSchedulerHealthCheck(jobRepo.Object, execRepo.Object, pollIntervalMs);
    }

    private static HealthCheckContext CreateContext()
    {
        return new HealthCheckContext
        {
            Registration = new HealthCheckRegistration(
                "job-scheduler",
                new Mock<IHealthCheck>().Object,
                null,
                null)
        };
    }
}
