#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ====================================================================

using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;
using JobScheduler.Core.Constants;
using JobScheduler.Core.Data.Repositories;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Exceptions;
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

    public Task<Job?> GetByIdAsync(Guid id)
    {
        var job = _jobs.FirstOrDefault(j => j.Id == id);
        return Task.FromResult(job);
    }

    public Task<IEnumerable<Job>> GetAllAsync()
    {
        return Task.FromResult<IEnumerable<Job>>(_jobs.ToList());
    }

    public Task<IEnumerable<Job>> FindAsync(Expression<Func<Job, bool>> predicate)
    {
        return Task.FromResult(_jobs.AsQueryable().Where(predicate).AsEnumerable());
    }

    public Task<Job?> FirstOrDefaultAsync(Expression<Func<Job, bool>> predicate)
    {
        return Task.FromResult(_jobs.AsQueryable().FirstOrDefault(predicate));
    }

    public Task<int> CountAsync(Expression<Func<Job, bool>>? predicate = null)
    {
        var count = predicate is null ? _jobs.Count : _jobs.AsQueryable().Count(predicate);
        return Task.FromResult(count);
    }

    public Task AddAsync(Job entity)
    {
        if (entity.Id == Guid.Empty)
            entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;
        _jobs.Add(entity);
        return Task.CompletedTask;
    }

    public Task AddRangeAsync(IEnumerable<Job> entities)
    {
        foreach (var entity in entities)
        {
            if (entity.Id == Guid.Empty)
                entity.Id = Guid.NewGuid();
            entity.CreatedAt = DateTime.UtcNow;
            _jobs.Add(entity);
        }
        return Task.CompletedTask;
    }

    public void Update(Job entity)
    {
        var existing = _jobs.FirstOrDefault(j => j.Id == entity.Id);
        if (existing is not null)
        {
            var index = _jobs.IndexOf(existing);
            _jobs[index] = entity;
        }
    }

    public void UpdateRange(IEnumerable<Job> entities)
    {
        foreach (var entity in entities)
            Update(entity);
    }

    public void Remove(Job entity)
    {
        _jobs.RemoveAll(j => j.Id == entity.Id);
    }

    public void RemoveRange(IEnumerable<Job> entities)
    {
        foreach (var entity in entities)
            Remove(entity);
    }

    public Task<bool> AnyAsync(Expression<Func<Job, bool>> predicate)
    {
        return Task.FromResult(_jobs.AsQueryable().Any(predicate));
    }

    public Task SaveChangesAsync() => Task.CompletedTask;

    public Task<Job?> GetByNameAsync(string name)
    {
        var job = _jobs.FirstOrDefault(j => j.Name == name);
        return Task.FromResult(job);
    }

    public Task<IEnumerable<Job>> GetActiveJobsAsync()
    {
        return Task.FromResult<IEnumerable<Job>>(_jobs.Where(j => j.IsActive).ToList());
    }

    public Task<IEnumerable<Job>> GetJobsByStatusAsync(JobStatus status)
    {
        return Task.FromResult<IEnumerable<Job>>(_jobs.Where(j => j.Status == status).ToList());
    }

    public Task<IEnumerable<Job>> GetJobsByPriorityAsync(JobPriority priority)
    {
        return Task.FromResult<IEnumerable<Job>>(_jobs.Where(j => j.Priority == priority).ToList());
    }

    public Task<IEnumerable<Job>> GetScheduledJobsForExecutionAsync()
    {
        var now = DateTime.UtcNow;
        var dueJobs = _jobs
            .Where(j => j.IsActive && j.Status == JobStatus.Scheduled && j.NextExecutionAt <= now)
            .ToList();
        return Task.FromResult<IEnumerable<Job>>(dueJobs);
    }

    public Task<IEnumerable<Job>> GetFailedJobsAsync()
    {
        return Task.FromResult<IEnumerable<Job>>(
            _jobs.Where(j => j.Status == JobStatus.Failed || j.Status == JobStatus.FailedPermanently).ToList());
    }

    public Task<IEnumerable<Job>> GetLongRunningJobsAsync(int thresholdSeconds)
    {
        var now = DateTime.UtcNow;
        var longRunning = _jobs
            .Where(j => j.Status == JobStatus.Running && j.LastExecutedAt.HasValue &&
                        (now - j.LastExecutedAt.Value).TotalSeconds > thresholdSeconds)
            .ToList();
        return Task.FromResult<IEnumerable<Job>>(longRunning);
    }

    public Task<IEnumerable<Job>> GetJobsWithoutRecentExecutionAsync(int minutesThreshold)
    {
        var threshold = DateTime.UtcNow.AddMinutes(-minutesThreshold);
        var jobs = _jobs
            .Where(j => j.IsActive && (!j.LastExecutedAt.HasValue || j.LastExecutedAt < threshold))
            .ToList();
        return Task.FromResult<IEnumerable<Job>>(jobs);
    }

    public Task<JobQueryResult> QueryAsync(JobQuery query)
    {
        var results = _jobs.AsEnumerable();

        if (!string.IsNullOrEmpty(query.Name))
            results = results.Where(j => j.Name.Contains(query.Name));

        if (query.Status.HasValue)
            results = results.Where(j => j.Status == query.Status.Value);

        if (query.Priority.HasValue)
            results = results.Where(j => j.Priority == query.Priority.Value);

        if (query.IsActive.HasValue)
            results = results.Where(j => j.IsActive == query.IsActive.Value);

        var materialized = results.ToList();
        var items = materialized
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        return Task.FromResult(new JobQueryResult(items, materialized.Count));
    }
}

internal sealed class MockExecutionRepository : IExecutionRepository
{
    private readonly List<JobExecution> _executions = new();

    public Task<JobExecution?> GetByIdAsync(Guid id)
    {
        var execution = _executions.FirstOrDefault(e => e.Id == id);
        return Task.FromResult(execution);
    }

    public Task<IEnumerable<JobExecution>> GetAllAsync()
    {
        return Task.FromResult<IEnumerable<JobExecution>>(_executions.ToList());
    }

    public Task<IEnumerable<JobExecution>> FindAsync(Expression<Func<JobExecution, bool>> predicate)
    {
        return Task.FromResult(_executions.AsQueryable().Where(predicate).AsEnumerable());
    }

    public Task<JobExecution?> FirstOrDefaultAsync(Expression<Func<JobExecution, bool>> predicate)
    {
        return Task.FromResult(_executions.AsQueryable().FirstOrDefault(predicate));
    }

    public Task<int> CountAsync(Expression<Func<JobExecution, bool>>? predicate = null)
    {
        var count = predicate is null ? _executions.Count : _executions.AsQueryable().Count(predicate);
        return Task.FromResult(count);
    }

    public Task AddAsync(JobExecution entity)
    {
        if (entity.Id == Guid.Empty)
            entity.Id = Guid.NewGuid();
        _executions.Add(entity);
        return Task.CompletedTask;
    }

    public Task AddRangeAsync(IEnumerable<JobExecution> entities)
    {
        foreach (var entity in entities)
        {
            if (entity.Id == Guid.Empty)
                entity.Id = Guid.NewGuid();
            _executions.Add(entity);
        }
        return Task.CompletedTask;
    }

    public void Update(JobExecution entity)
    {
        var existing = _executions.FirstOrDefault(e => e.Id == entity.Id);
        if (existing is not null)
        {
            var index = _executions.IndexOf(existing);
            _executions[index] = entity;
        }
    }

    public void UpdateRange(IEnumerable<JobExecution> entities)
    {
        foreach (var entity in entities)
            Update(entity);
    }

    public void Remove(JobExecution entity)
    {
        _executions.RemoveAll(e => e.Id == entity.Id);
    }

    public void RemoveRange(IEnumerable<JobExecution> entities)
    {
        foreach (var entity in entities)
            Remove(entity);
    }

    public Task<bool> AnyAsync(Expression<Func<JobExecution, bool>> predicate)
    {
        return Task.FromResult(_executions.AsQueryable().Any(predicate));
    }

    public Task SaveChangesAsync() => Task.CompletedTask;

    public Task<JobExecution?> GetLatestExecutionAsync(Guid jobId)
    {
        var execution = _executions
            .Where(e => e.JobId == jobId)
            .OrderByDescending(e => e.StartedAt)
            .FirstOrDefault();
        return Task.FromResult(execution);
    }

    public Task<IEnumerable<JobExecution>> GetExecutionsByJobAsync(Guid jobId)
    {
        return Task.FromResult<IEnumerable<JobExecution>>(
            _executions.Where(e => e.JobId == jobId).OrderByDescending(e => e.StartedAt).ToList());
    }

    public Task<IEnumerable<JobExecution>> GetExecutionsByStatusAsync(ExecutionStatus status)
    {
        return Task.FromResult<IEnumerable<JobExecution>>(_executions.Where(e => e.Status == status).ToList());
    }

    public Task<IEnumerable<JobExecution>> GetExecutionsByJobAndStatusAsync(Guid jobId, ExecutionStatus status)
    {
        return Task.FromResult<IEnumerable<JobExecution>>(
            _executions.Where(e => e.JobId == jobId && e.Status == status).ToList());
    }

    public Task<int> GetCurrentlyRunningCountAsync(Guid jobId)
    {
        return Task.FromResult(_executions.Count(e => e.JobId == jobId && e.Status == ExecutionStatus.Running));
    }

    public Task<int> GetConcurrentRunningCountAsync()
    {
        return Task.FromResult(_executions.Count(e => e.Status == ExecutionStatus.Running));
    }

    public Task<IEnumerable<JobExecution>> GetRunningExecutionsAsync()
    {
        return Task.FromResult<IEnumerable<JobExecution>>(
            _executions.Where(e => e.Status == ExecutionStatus.Running).ToList());
    }

    public Task<IEnumerable<JobExecution>> GetFailedExecutionsRequiringRetryAsync()
    {
        return Task.FromResult<IEnumerable<JobExecution>>(
            _executions.Where(e => e.Status == ExecutionStatus.Failed && e.IsRetryable).ToList());
    }

    public Task<IEnumerable<JobExecution>> GetExecutionsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return Task.FromResult<IEnumerable<JobExecution>>(
            _executions.Where(e => e.StartedAt >= startDate && e.StartedAt <= endDate).ToList());
    }

    public Task<long> GetAverageExecutionTimeAsync(Guid jobId, int? lastN = null)
    {
        IEnumerable<JobExecution> query = _executions.Where(e => e.JobId == jobId && e.Status == ExecutionStatus.Success);
        if (lastN.HasValue)
            query = query.OrderByDescending(e => e.StartedAt).Take(lastN.Value);

        var list = query.ToList();
        var average = list.Count > 0 ? (long)list.Average(e => e.DurationMilliseconds) : 0;
        return Task.FromResult(average);
    }

    public Task<List<JobExecution>> GetByJobIdAsync(Guid jobId)
    {
        return Task.FromResult(_executions.Where(e => e.JobId == jobId).ToList());
    }

    public Task<ExecutionMetrics> GetMetricsAsync(Guid? jobId, DateTime? startDate, DateTime? endDate)
    {
        var filtered = _executions.AsEnumerable();

        if (jobId.HasValue)
            filtered = filtered.Where(e => e.JobId == jobId.Value);

        if (startDate.HasValue)
            filtered = filtered.Where(e => e.StartedAt >= startDate.Value);

        if (endDate.HasValue)
            filtered = filtered.Where(e => e.StartedAt <= endDate.Value);

        var executions = filtered.ToList();
        var successful = executions.Count(e => e.Status == ExecutionStatus.Success);
        var failed = executions.Count(e => e.Status == ExecutionStatus.Failed);

        return Task.FromResult(new ExecutionMetrics
        {
            TotalExecutions = executions.Count,
            SuccessfulExecutions = successful,
            FailedExecutions = failed,
            AverageDurationMs = executions.Count > 0 ? (long)executions.Average(e => e.DurationMilliseconds) : 0,
            MaxDurationMs = executions.Count > 0 ? executions.Max(e => e.DurationMilliseconds) : 0,
            MinDurationMs = executions.Count > 0 ? executions.Min(e => e.DurationMilliseconds) : 0
        });
    }
}

/// <summary>
/// Minimal in-memory query filter used by benchmark mock repositories.
/// </summary>
internal sealed class JobQuery
{
    public string? Name { get; set; }
    public JobStatus? Status { get; set; }
    public JobPriority? Priority { get; set; }
    public bool? IsActive { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Paged result returned by <see cref="MockJobRepository.QueryAsync"/>.
/// </summary>
internal sealed class JobQueryResult
{
    public List<Job> Items { get; }
    public int TotalCount { get; }

    public JobQueryResult(List<Job> items, int totalCount)
    {
        Items = items;
        TotalCount = totalCount;
    }
}