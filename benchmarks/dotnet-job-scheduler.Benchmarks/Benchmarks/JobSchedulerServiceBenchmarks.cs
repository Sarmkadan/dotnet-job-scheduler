#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ====================================================================

using BenchmarkDotNet.Attributes;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JobScheduler.Benchmarks;

/// <summary>
/// Measures core JobSchedulerService operations that are called frequently:
/// - Job creation and validation
/// - Schedule evaluation and next execution time calculation
/// - Bulk job execution processing
/// </summary>
[MemoryDiagnoser]
public sealed class JobSchedulerServiceBenchmarks
{
    private IServiceProvider? _serviceProvider;
    private JobSchedulerService? _schedulerService;
    private List<Job>? _testJobs;

    // Test job definitions
    private const string ValidCronExpression = "0 9 * * *"; // Daily at 9 AM
    private const string InvalidCronExpression = "invalid cron";
    private const string DuplicateJobName = "DuplicateJob";

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();

        // Configure minimal services for testing
        services.AddLogging(configure => configure.AddConsole().SetMinimumLevel(LogLevel.Error));
        services.AddSingleton<CronExpressionService>();
        services.AddSingleton<RetryService>();
        services.AddSingleton<ConcurrencyManager>();

        // Mock repositories
        services.AddSingleton<IJobRepository>(new MockJobRepository());
        services.AddSingleton<IExecutionRepository>(new MockExecutionRepository());
        services.AddSingleton<JobExecutorService>();

        _serviceProvider = services.BuildServiceProvider();
        _schedulerService = _serviceProvider.GetRequiredService<JobSchedulerService>();

        // Create test jobs for bulk operations
        _testJobs = new List<Job>();
        for (int i = 0; i < 100; i++)
        {
            _testJobs.Add(new Job
            {
                Name = $"TestJob{i:D3}",
                Description = $"Test job {i}",
                CronExpression = ValidCronExpression,
                HandlerType = "TestHandler, TestAssembly",
                Priority = JobPriority.Normal,
                MaxRetries = 3,
                ExecutionTimeoutSeconds = 300,
                IsActive = true
            });
        }
    }

    [Benchmark]
    public async Task CreateJob_Valid()
    {
        var job = new Job
        {
            Name = "ValidJob" + Guid.NewGuid().ToString()[..8],
            Description = "A valid test job",
            CronExpression = ValidCronExpression,
            HandlerType = "TestHandler, TestAssembly",
            Priority = JobPriority.Normal,
            MaxRetries = 3,
            ExecutionTimeoutSeconds = 300,
            IsActive = true
        };

        await _schedulerService!.CreateJobAsync(job, "test-user");
    }

    [Benchmark]
    public async Task CreateJob_InvalidCron()
    {
        var job = new Job
        {
            Name = "InvalidCronJob" + Guid.NewGuid().ToString()[..8],
            Description = "Job with invalid cron",
            CronExpression = InvalidCronExpression,
            HandlerType = "TestHandler, TestAssembly",
            Priority = JobPriority.Normal,
            MaxRetries = 3,
            ExecutionTimeoutSeconds = 300,
            IsActive = true
        };

        try
        {
            await _schedulerService!.CreateJobAsync(job, "test-user");
        }
        catch (CronExpressionException)
        {
            // Expected
        }
    }

    [Benchmark]
    public async Task CreateJob_DuplicateName()
    {
        var job = new Job
        {
            Name = DuplicateJobName,
            Description = "Duplicate job name",
            CronExpression = ValidCronExpression,
            HandlerType = "TestHandler, TestAssembly",
            Priority = JobPriority.Normal,
            MaxRetries = 3,
            ExecutionTimeoutSeconds = 300,
            IsActive = true
        };

        try
        {
            await _schedulerService!.CreateJobAsync(job, "test-user");
        }
        catch (JobValidationException)
        {
            // Expected
        }
    }

    [Benchmark]
    public async Task GetScheduledJobsForExecution()
    {
        // Simulate getting scheduled jobs for execution
        var dueJobs = await _serviceProvider!.GetRequiredService<IJobRepository>().GetScheduledJobsForExecutionAsync();
        _ = dueJobs.Count();
    }

    [Benchmark]
    public async Task ExecuteDueJobs_EmptyQueue()
    {
        var executions = await _schedulerService!.ExecuteDueJobsAsync();
        _ = executions.Count();
    }

    [Benchmark]
    public async Task ExecuteDueJobs_WithJobs()
    {
        // Add some test jobs first
        foreach (var job in _testJobs!.Take(10))
        {
            await _schedulerService!.CreateJobAsync(job, "test-user");
        }

        var executions = await _schedulerService!.ExecuteDueJobsAsync();
        _ = executions.Count();
    }

    [Benchmark]
    public async Task SuspendJob()
    {
        // Create a job first
        var job = new Job
        {
            Name = "SuspendableJob" + Guid.NewGuid().ToString()[..8],
            Description = "Job to suspend",
            CronExpression = ValidCronExpression,
            HandlerType = "TestHandler, TestAssembly",
            Priority = JobPriority.Normal,
            MaxRetries = 3,
            ExecutionTimeoutSeconds = 300,
            IsActive = true
        };

        var createdJob = await _schedulerService!.CreateJobAsync(job, "test-user");

        await _schedulerService.SuspendJobAsync(createdJob.Id, "Testing suspension");
    }

    [Benchmark]
    public async Task ResumeJob()
    {
        // Create and suspend a job first
        var job = new Job
        {
            Name = "ResumableJob" + Guid.NewGuid().ToString()[..8],
            Description = "Job to resume",
            CronExpression = ValidCronExpression,
            HandlerType = "TestHandler, TestAssembly",
            Priority = JobPriority.Normal,
            MaxRetries = 3,
            ExecutionTimeoutSeconds = 300,
            IsActive = true
        };

        var createdJob = await _schedulerService!.CreateJobAsync(job, "test-user");
        await _schedulerService.SuspendJobAsync(createdJob.Id, "Testing suspension");

        await _schedulerService.ResumeJobAsync(createdJob.Id);
    }

    [Benchmark]
    public async Task GetSchedulerStatistics()
    {
        var stats = await _serviceProvider!.GetRequiredService<JobSchedulerService>().GetSchedulerStatisticsAsync();
        _ = stats.TotalJobs;
    }
}

/// <summary>
/// Mock repository for benchmarking without database dependency
/// </summary>
internal sealed class MockJobRepository : IJobRepository
{
    private readonly List<Job> _jobs = new();
    private int _nextId = 1;

    public Task<Job?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var job = _jobs.FirstOrDefault(j => j.Id == id);
        return Task.FromResult(job);
    }

    public Task<Job?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var job = _jobs.FirstOrDefault(j => j.Name == name);
        return Task.FromResult(job);
    }

    public Task<List<Job>> GetActiveJobsAsync(CancellationToken cancellationToken = default)
    {
        var activeJobs = _jobs.Where(j => j.IsActive && j.Status == JobStatus.Scheduled).ToList();
        return Task.FromResult(activeJobs);
    }

    public Task<List<Job>> GetScheduledJobsForExecutionAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var dueJobs = _jobs
            .Where(j => j.IsActive && j.Status == JobStatus.Scheduled && j.NextExecutionAt <= now)
            .ToList();
        return Task.FromResult(dueJobs);
    }

    public Task<Job> AddAsync(Job entity, CancellationToken cancellationToken = default)
    {
        entity.Id = _nextId++;
        entity.CreatedAt = DateTime.UtcNow;
        _jobs.Add(entity);
        return Task.FromResult(entity);
    }

    public Task UpdateAsync(Job entity, CancellationToken cancellationToken = default)
    {
        var existing = _jobs.FirstOrDefault(j => j.Id == entity.Id);
        if (existing != null)
        {
            existing.Name = entity.Name;
            existing.Description = entity.Description;
            existing.CronExpression = entity.CronExpression;
            existing.HandlerType = entity.HandlerType;
            existing.Status = entity.Status;
            existing.Priority = entity.Priority;
            existing.IsActive = entity.IsActive;
            existing.MaxRetries = entity.MaxRetries;
            existing.RetryBackoffSeconds = entity.RetryBackoffSeconds;
            existing.ExecutionTimeoutSeconds = entity.ExecutionTimeoutSeconds;
            existing.ModifiedAt = DateTime.UtcNow;
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var job = _jobs.FirstOrDefault(j => j.Id == id);
        if (job != null)
        {
            _jobs.Remove(job);
        }
        return Task.CompletedTask;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(1);
    }

    public Task<JobQueryResult> QueryAsync(JobQuery query, CancellationToken cancellationToken = default)
    {
        var results = _jobs.AsQueryable();

        if (!string.IsNullOrEmpty(query.Name))
            results = results.Where(j => j.Name.Contains(query.Name));

        if (query.Status.HasValue)
            results = results.Where(j => j.Status == query.Status.Value);

        if (query.Priority.HasValue)
            results = results.Where(j => j.Priority == query.Priority.Value);

        if (query.IsActive.HasValue)
            results = results.Where(j => j.IsActive == query.IsActive.Value);

        var items = results
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        var total = results.Count();
        return Task.FromResult(new JobQueryResult(items, total));
    }
}

internal sealed class MockExecutionRepository : IExecutionRepository
{
    private readonly List<JobExecution> _executions = new();
    private int _nextId = 1;

    public Task<JobExecution?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var execution = _executions.FirstOrDefault(e => e.Id == id);
        return Task.FromResult(execution);
    }

    public Task<List<JobExecution>> GetByJobIdAsync(int jobId, CancellationToken cancellationToken = default)
    {
        var executions = _executions.Where(e => e.JobId == jobId).ToList();
        return Task.FromResult(executions);
    }

    public Task<JobExecution> AddAsync(JobExecution entity, CancellationToken cancellationToken = default)
    {
        entity.Id = _nextId++;
        entity.ExecutedAt = DateTime.UtcNow;
        _executions.Add(entity);
        return Task.FromResult(entity);
    }

    public Task UpdateAsync(JobExecution entity, CancellationToken cancellationToken = default)
    {
        var existing = _executions.FirstOrDefault(e => e.Id == entity.Id);
        if (existing != null)
        {
            existing.Status = entity.Status;
            existing.CompletedAt = entity.CompletedAt;
            existing.Duration = entity.Duration;
            existing.Result = entity.Result;
            existing.ErrorMessage = entity.ErrorMessage;
            existing.RetryAttempt = entity.RetryAttempt;
            existing.ServerName = entity.ServerName;
            existing.MemoryUsageMb = entity.MemoryUsageMb;
            existing.CpuUsagePercent = entity.CpuUsagePercent;
        }
        return Task.CompletedTask;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(1);
    }

    public Task<ExecutionMetrics> GetMetricsAsync(int? jobId, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default)
    {
        var filtered = _executions.AsQueryable();

        if (jobId.HasValue)
            filtered = filtered.Where(e => e.JobId == jobId.Value);

        if (startDate.HasValue)
            filtered = filtered.Where(e => e.ExecutedAt >= startDate.Value);

        if (endDate.HasValue)
            filtered = filtered.Where(e => e.ExecutedAt <= endDate.Value);

        var executions = filtered.ToList();
        var successful = executions.Count(e => e.Status == ExecutionStatus.Completed);
        var failed = executions.Count(e => e.Status == ExecutionStatus.Failed);
        var totalDuration = executions.Where(e => e.Duration.HasValue).Sum(e => e.Duration!.Value.TotalMilliseconds);

        return Task.FromResult(new ExecutionMetrics
        {
            TotalExecutions = executions.Count,
            SuccessfulCount = successful,
            FailedCount = failed,
            AverageDurationMs = executions.Count > 0 ? totalDuration / executions.Count : 0,
            MaxDurationMs = executions.Count > 0 ? (int)executions.Max(e => e.Duration?.TotalMilliseconds ?? 0) : 0,
            MinDurationMs = executions.Count > 0 ? (int)executions.Min(e => e.Duration?.TotalMilliseconds ?? 0) : 0
        });
    }
}