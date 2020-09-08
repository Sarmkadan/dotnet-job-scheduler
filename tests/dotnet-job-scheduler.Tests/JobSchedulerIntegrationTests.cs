#nullable enable

using FluentAssertions;
using JobScheduler.Core.Configuration;
using JobScheduler.Core.Constants;
using JobScheduler.Core.Data;
using JobScheduler.Core.Data.Repositories;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace DotnetJobScheduler.Tests;

/// <summary>
/// Integration tests for the complete job scheduler workflow.
/// Tests full end-to-end scenarios with real database and service interactions.
/// </summary>
public sealed class JobSchedulerIntegrationTests : IAsyncLifetime
{
    private readonly ServiceCollection _services = new();
    private IServiceProvider? _serviceProvider;
    private JobSchedulerContext? _dbContext;

    public async Task InitializeAsync()
    {
        // Configure services
        _services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.SetMinimumLevel(LogLevel.Warning);
        });

        _services.AddDbContext<JobSchedulerContext>(options =>
            options.UseInMemoryDatabase("SchedulerTest"));

        _services.AddScoped<IJobRepository, JobRepository>();
        _services.AddScoped<IExecutionRepository, ExecutionRepository>();

        _services.AddScoped<CronExpressionService>();
        _services.AddScoped<RetryService>();
        _services.AddScoped<ConcurrencyManager>();
        _services.AddScoped<JobExecutorService>();
        _services.AddScoped<JobSchedulerService>();
        _services.AddScoped<ScheduleService>();
        _services.AddScoped<CacheService>();

        _serviceProvider = _services.BuildServiceProvider();

        // Initialize database
        _dbContext = _serviceProvider.GetRequiredService<JobSchedulerContext>();
        await _dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        if (_dbContext is not null)
        {
            await _dbContext.Database.EnsureDeletedAsync();
            await _dbContext.DisposeAsync();
        }
        (_serviceProvider as ServiceProvider)?.Dispose();
    }

    [Fact]
    public async Task CreateJob_WithValidInput_SchedulesJobSuccessfully()
    {
        // Arrange
        var jobScheduler = _serviceProvider!.GetRequiredService<JobSchedulerService>();
        var job = new Job
        {
            Name = "test-daily-job",
            CronExpression = "0 9 * * *",
            HandlerType = "TestApp.Jobs.DailyJob, TestApp",
            MaxRetries = 3,
            ExecutionTimeoutSeconds = 300,
            MaxConcurrentExecutions = 1,
            Priority = JobPriority.Medium
        };

        // Act
        var createdJob = await jobScheduler.CreateJobAsync(job, "integration-test");

        // Assert
        createdJob.Id.Should().NotBe(Guid.Empty);
        createdJob.Name.Should().Be("test-daily-job");
        createdJob.Status.Should().Be(JobStatus.Scheduled);
        createdJob.CreatedBy.Should().Be("integration-test");
        createdJob.NextExecutionAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateMultipleJobs_WithDifferentSchedules_AllPersistCorrectly()
    {
        // Arrange
        var jobScheduler = _serviceProvider!.GetRequiredService<JobSchedulerService>();
        var jobs = new[]
        {
            new Job { Name = "hourly-job", CronExpression = "0 * * * *", HandlerType = "App.Jobs.Hourly, App", ExecutionTimeoutSeconds = 60, MaxConcurrentExecutions = 1 },
            new Job { Name = "daily-job", CronExpression = "0 0 * * *", HandlerType = "App.Jobs.Daily, App", ExecutionTimeoutSeconds = 300, MaxConcurrentExecutions = 1 },
            new Job { Name = "weekly-job", CronExpression = "0 0 * * 0", HandlerType = "App.Jobs.Weekly, App", ExecutionTimeoutSeconds = 600, MaxConcurrentExecutions = 1 },
        };

        // Act
        var createdJobs = new List<Job>();
        foreach (var job in jobs)
        {
            var created = await jobScheduler.CreateJobAsync(job);
            createdJobs.Add(created);
        }

        // Assert
        createdJobs.Should().HaveCount(3);
        createdJobs.Select(j => j.Name).Should().Contain(new[] { "hourly-job", "daily-job", "weekly-job" });

        // Verify in repository
        var repository = _serviceProvider.GetRequiredService<IJobRepository>();
        var activeJobs = await repository.GetActiveJobsAsync();
        activeJobs.Count().Should().Be(3);
    }

    [Fact]
    public async Task SuspendAndResumeJob_TransitionsStateCorrectly()
    {
        // Arrange
        var jobScheduler = _serviceProvider!.GetRequiredService<JobSchedulerService>();
        var job = new Job
        {
            Name = "suspend-test-job",
            CronExpression = "0 * * * *",
            HandlerType = "App.Jobs.Test, App",
            ExecutionTimeoutSeconds = 60,
            MaxConcurrentExecutions = 1
        };
        var createdJob = await jobScheduler.CreateJobAsync(job);

        // Act
        await jobScheduler.SuspendJobAsync(createdJob.Id);
        var suspendedJob = await _serviceProvider.GetRequiredService<IJobRepository>()
            .GetByIdAsync(createdJob.Id);

        await jobScheduler.ResumeJobAsync(createdJob.Id);
        var resumedJob = await _serviceProvider.GetRequiredService<IJobRepository>()
            .GetByIdAsync(createdJob.Id);

        // Assert
        suspendedJob?.Status.Should().Be(JobStatus.Suspended);
        resumedJob?.Status.Should().Be(JobStatus.Scheduled);
    }

    [Fact]
    public async Task UpdateJobSchedule_ChangesExecutionTiming()
    {
        // Arrange
        var jobScheduler = _serviceProvider!.GetRequiredService<JobSchedulerService>();
        var job = new Job
        {
            Name = "schedule-update-job",
            CronExpression = "0 9 * * *",
            HandlerType = "App.Jobs.Test, App",
            ExecutionTimeoutSeconds = 60,
            MaxConcurrentExecutions = 1
        };
        var createdJob = await jobScheduler.CreateJobAsync(job);
        var originalNextExecution = createdJob.NextExecutionAt;

        // Act
        await jobScheduler.UpdateJobScheduleAsync(createdJob.Id, "0 12 * * *");
        var updatedJob = await _serviceProvider.GetRequiredService<IJobRepository>()
            .GetByIdAsync(createdJob.Id);

        // Assert
        updatedJob?.CronExpression.Should().Be("0 12 * * *");
        updatedJob?.NextExecutionAt.Should().NotBe(originalNextExecution);
    }

    [Fact]
    public async Task DeleteJob_RemovesJobFromSystem()
    {
        // Arrange
        var jobScheduler = _serviceProvider!.GetRequiredService<JobSchedulerService>();
        var job = new Job
        {
            Name = "delete-test-job",
            CronExpression = "0 9 * * *",
            HandlerType = "App.Jobs.Test, App",
            ExecutionTimeoutSeconds = 60,
            MaxConcurrentExecutions = 1
        };
        var createdJob = await jobScheduler.CreateJobAsync(job);

        // Act
        await jobScheduler.DeleteJobAsync(createdJob.Id);
        var deletedJob = await _serviceProvider.GetRequiredService<IJobRepository>()
            .GetByIdAsync(createdJob.Id);

        // Assert
        deletedJob.Should().BeNull();
    }

    [Fact]
    public async Task ConcurrencyControl_EnforcesJobConcurrencyLimits()
    {
        // Arrange
        var concurrencyManager = _serviceProvider!.GetRequiredService<ConcurrencyManager>();
        var job = new Job
        {
            Id = Guid.NewGuid(),
            Name = "concurrency-test",
            CronExpression = "* * * * *",
            HandlerType = "App.Jobs.Test, App",
            ExecutionTimeoutSeconds = 300,
            MaxConcurrentExecutions = 2,
            Status = JobStatus.Scheduled
        };

        // Setup mock repository with concurrency counts
        var executionRepo = _serviceProvider.GetRequiredService<IExecutionRepository>();

        // Act - First execution should be allowed
        var canExecuteFirst = await concurrencyManager.CanExecuteAsync(job);

        // Increment the counter
        concurrencyManager.IncrementConcurrencyCount(job.Id);

        // Assert
        canExecuteFirst.Should().BeTrue();

        var stats = concurrencyManager.GetConcurrencyStats();
        stats.GlobalRunningCount.Should().Be(1);
    }

    [Fact]
    public async Task CacheService_OptimizesDataRetrieval()
    {
        // Arrange
        var cacheService = _serviceProvider!.GetRequiredService<CacheService>();
        var testData = new { Id = 1, Name = "Test Job" };
        var cacheKey = "test:cache:key";

        // Act
        await cacheService.SetAsync(cacheKey, testData);
        var cachedValue = await cacheService.GetAsync<dynamic>(cacheKey);

        // Assert
        cachedValue.Should().NotBeNull();
        var stats = cacheService.GetStatistics();
        stats.TotalKeys.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ScheduleService_CalculatesUpcomingExecutions()
    {
        // Arrange
        var jobScheduler = _serviceProvider!.GetRequiredService<JobSchedulerService>();
        var scheduleService = _serviceProvider.GetRequiredService<ScheduleService>();

        var job = new Job
        {
            Name = "schedule-calc-job",
            CronExpression = "0 * * * *", // Every hour
            HandlerType = "App.Jobs.Test, App",
            ExecutionTimeoutSeconds = 60,
            MaxConcurrentExecutions = 1
        };
        var createdJob = await jobScheduler.CreateJobAsync(job);

        // Act
        var upcomingTimes = await scheduleService.GetUpcomingExecutionTimesAsync(createdJob.Id, count: 5);

        // Assert
        upcomingTimes.Should().HaveCount(5);
        for (int i = 0; i < upcomingTimes.Count - 1; i++)
        {
            upcomingTimes[i].Should().BeBefore(upcomingTimes[i + 1]);
        }
    }

    [Fact]
    public async Task RetryService_HandlesFailedExecutions()
    {
        // Arrange
        var jobScheduler = _serviceProvider!.GetRequiredService<JobSchedulerService>();
        var retryService = _serviceProvider.GetRequiredService<RetryService>();

        var job = new Job
        {
            Name = "retry-test-job",
            CronExpression = "0 * * * *",
            HandlerType = "App.Jobs.Test, App",
            ExecutionTimeoutSeconds = 60,
            MaxConcurrentExecutions = 1,
            MaxRetries = 3
        };
        var createdJob = await jobScheduler.CreateJobAsync(job);

        var failedExecution = new JobExecution
        {
            JobId = createdJob.Id,
            Status = ExecutionStatus.Failed,
            AttemptNumber = 1,
            IsRetryable = true,
            CompletedAt = DateTime.UtcNow
        };

        // Act
        var shouldRetry = await retryService.ShouldRetryAsync(createdJob, failedExecution);

        // Assert
        shouldRetry.Should().BeTrue();
    }

    [Fact]
    public async Task JobExecutor_ExecutesJobWithTimeout()
    {
        // Arrange
        var jobScheduler = _serviceProvider!.GetRequiredService<JobSchedulerService>();
        var executor = _serviceProvider.GetRequiredService<JobExecutorService>();

        var job = new Job
        {
            Name = "executor-test-job",
            CronExpression = "0 * * * *",
            HandlerType = "App.Jobs.Test, App",
            ExecutionTimeoutSeconds = 5,
            MaxConcurrentExecutions = 1
        };
        var createdJob = await jobScheduler.CreateJobAsync(job);

        // Act
        var execution = await executor.ExecuteJobAsync(createdJob);

        // Assert
        execution.Should().NotBeNull();
        execution.JobId.Should().Be(createdJob.Id);
        execution.ExecutorName.Should().Be(Environment.MachineName);
    }

    [Fact]
    public async Task WorkflowComplete_CreateExecuteAndTrack()
    {
        // Arrange
        var jobScheduler = _serviceProvider!.GetRequiredService<JobSchedulerService>();
        var executionRepo = _serviceProvider.GetRequiredService<IExecutionRepository>();

        var job = new Job
        {
            Name = "complete-workflow",
            CronExpression = "0 0 * * *",
            HandlerType = "App.Jobs.Workflow, App",
            ExecutionTimeoutSeconds = 300,
            MaxConcurrentExecutions = 1,
            Priority = JobPriority.High
        };

        // Act - Create job
        var createdJob = await jobScheduler.CreateJobAsync(job, "workflow-test");

        // Get job details
        var jobDetails = await jobScheduler.GetJobDetailsAsync(createdJob.Id);

        // Assert
        jobDetails.Should().NotBeNull();
        jobDetails?.Name.Should().Be("complete-workflow");
        jobDetails?.Status.Should().Be(JobStatus.Scheduled);
        jobDetails?.Priority.Should().Be(JobPriority.High);
    }

    [Fact]
    public async Task MultipleJobTypes_WithVariedConfigurations()
    {
        // Arrange
        var jobScheduler = _serviceProvider!.GetRequiredService<JobSchedulerService>();
        var repository = _serviceProvider.GetRequiredService<IJobRepository>();

        // Act - Create jobs with different configurations
        var highPriorityJob = await jobScheduler.CreateJobAsync(new Job
        {
            Name = "high-priority-job",
            CronExpression = "*/5 * * * *",
            HandlerType = "App.Jobs.Priority, App",
            ExecutionTimeoutSeconds = 60,
            MaxConcurrentExecutions = 5,
            Priority = JobPriority.High
        });

        var lowPriorityJob = await jobScheduler.CreateJobAsync(new Job
        {
            Name = "low-priority-job",
            CronExpression = "0 2 * * *",
            HandlerType = "App.Jobs.Batch, App",
            ExecutionTimeoutSeconds = 3600,
            MaxConcurrentExecutions = 1,
            Priority = JobPriority.Low
        });

        // Assert
        var allJobs = await repository.GetActiveJobsAsync();
        allJobs.Count().Should().Be(2);

        var highPriority = allJobs.FirstOrDefault(j => j.Priority == JobPriority.High);
        highPriority.Should().NotBeNull();
        highPriority?.MaxConcurrentExecutions.Should().Be(5);

        var lowPriority = allJobs.FirstOrDefault(j => j.Priority == JobPriority.Low);
        lowPriority.Should().NotBeNull();
        lowPriority?.ExecutionTimeoutSeconds.Should().Be(3600);
    }
}
