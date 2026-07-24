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
using JobScheduler.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JobScheduler.Benchmarks;

/// <summary>
/// Measures ConcurrencyManager operations that enforce execution limits:
/// - Global concurrency tracking
/// - Per-job concurrency limits
/// - Execution slot acquisition and release
/// - Concurrency limit enforcement
/// </summary>
[MemoryDiagnoser]
public sealed class ConcurrencyManagerBenchmarks
{
    private IServiceProvider? _serviceProvider;
    private ConcurrencyManager? _concurrencyManager;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddLogging(configure => configure.AddConsole().SetMinimumLevel(LogLevel.Error));

        // Mock repositories
        services.AddSingleton<IJobRepository>(new ConcurrencyMockJobRepository());
        services.AddSingleton<IExecutionRepository>(new MockExecutionRepository());

        _serviceProvider = services.BuildServiceProvider();
        _concurrencyManager = new ConcurrencyManager(_serviceProvider.GetRequiredService<IExecutionRepository>());
    }

    [Benchmark]
    public async Task<bool> CanExecuteJob_GlobalLimitNotReached()
    {
        var job = new Job
        {
            Name = "TestJob1",
            MaxConcurrentExecutions = 1,
            IsActive = true
        };

        return await _concurrencyManager!.CanExecuteAsync(job);
    }

    [Benchmark]
    public async Task<bool> CanExecuteJob_GlobalLimitReached()
    {
        var job = new Job
        {
            Name = "TestJob2",
            MaxConcurrentExecutions = 1,
            IsActive = true
        };

        // Simulate global limit reached
        _concurrencyManager!.IncrementConcurrencyCount(job.Id);
        _concurrencyManager!.IncrementConcurrencyCount(job.Id); // Should exceed limit

        return await _concurrencyManager.CanExecuteAsync(job);
    }

    [Benchmark]
    public async Task<bool> CanExecuteJob_PerJobLimit()
    {
        var job = new Job
        {
            Name = "TestJob3",
            MaxConcurrentExecutions = 2,
            IsActive = true
        };

        // Track some executions for this job
        _concurrencyManager!.IncrementConcurrencyCount(job.Id);
        _concurrencyManager!.IncrementConcurrencyCount(job.Id);

        return await _concurrencyManager.CanExecuteAsync(job);
    }

    [Benchmark]
    public void TrackExecutionStart()
    {
        var jobId = Guid.NewGuid();
        _concurrencyManager!.IncrementConcurrencyCount(jobId);
    }

    [Benchmark]
    public void TrackExecutionEnd()
    {
        var jobId = Guid.NewGuid();
        _concurrencyManager!.IncrementConcurrencyCount(jobId);
        _concurrencyManager!.DecrementConcurrencyCount(jobId);
    }

    [Benchmark]
    public int GetCurrentConcurrencyCount()
    {
        var jobId = Guid.NewGuid();
        _concurrencyManager!.IncrementConcurrencyCount(jobId);
        _concurrencyManager!.IncrementConcurrencyCount(jobId);

        var count = _concurrencyManager.GetJobConcurrencyCount(jobId);
        _concurrencyManager!.DecrementConcurrencyCount(jobId);
        _concurrencyManager!.DecrementConcurrencyCount(jobId);

        return count;
    }

    [Benchmark]
    public int GetGlobalConcurrencyCount()
    {
        var jobId1 = Guid.NewGuid();
        var jobId2 = Guid.NewGuid();
        var jobId3 = Guid.NewGuid();

        _concurrencyManager!.IncrementConcurrencyCount(jobId1);
        _concurrencyManager!.IncrementConcurrencyCount(jobId2);
        _concurrencyManager!.IncrementConcurrencyCount(jobId3);

        var count = _concurrencyManager.GetGlobalConcurrencyCount();

        _concurrencyManager!.DecrementConcurrencyCount(jobId1);
        _concurrencyManager!.DecrementConcurrencyCount(jobId2);
        _concurrencyManager!.DecrementConcurrencyCount(jobId3);

        return count;
    }

    [Benchmark]
    public Dictionary<string, int> Reset()
    {
        _concurrencyManager!.IncrementConcurrencyCount(Guid.NewGuid());
        _concurrencyManager!.IncrementConcurrencyCount(Guid.NewGuid());

        return _concurrencyManager!.GetConcurrencyStats();
    }
}

/// <summary>
/// Mock repository for ConcurrencyManager benchmarks
/// </summary>
internal sealed class ConcurrencyMockJobRepository : IJobRepository
{
    private readonly Dictionary<Guid, Job> _jobs = new();

    public Task<Job?> GetByIdAsync(Guid id)
    {
        _jobs.TryGetValue(id, out var job);
        return Task.FromResult(job);
    }

    public Task<IEnumerable<Job>> GetAllAsync()
    {
        return Task.FromResult<IEnumerable<Job>>(_jobs.Values.ToList());
    }

    public Task<IEnumerable<Job>> FindAsync(Expression<Func<Job, bool>> predicate)
    {
        return Task.FromResult(_jobs.Values.AsQueryable().Where(predicate).AsEnumerable());
    }

    public Task<Job?> FirstOrDefaultAsync(Expression<Func<Job, bool>> predicate)
    {
        return Task.FromResult(_jobs.Values.AsQueryable().FirstOrDefault(predicate));
    }

    public Task<int> CountAsync(Expression<Func<Job, bool>>? predicate = null)
    {
        var count = predicate is null ? _jobs.Count : _jobs.Values.AsQueryable().Count(predicate);
        return Task.FromResult(count);
    }

    public Task<Job?> GetByNameAsync(string name)
    {
        var job = _jobs.Values.FirstOrDefault(j => j.Name == name);
        return Task.FromResult(job);
    }

    public Task<IEnumerable<Job>> GetActiveJobsAsync()
    {
        var activeJobs = _jobs.Values.Where(j => j.IsActive).ToList();
        return Task.FromResult<IEnumerable<Job>>(activeJobs);
    }

    public Task<IEnumerable<Job>> GetJobsByStatusAsync(JobStatus status)
    {
        return Task.FromResult<IEnumerable<Job>>(_jobs.Values.Where(j => j.Status == status).ToList());
    }

    public Task<IEnumerable<Job>> GetJobsByPriorityAsync(JobPriority priority)
    {
        return Task.FromResult<IEnumerable<Job>>(_jobs.Values.Where(j => j.Priority == priority).ToList());
    }

    public Task<IEnumerable<Job>> GetScheduledJobsForExecutionAsync()
    {
        var dueJobs = _jobs.Values
            .Where(j => j.IsActive && j.Status == JobStatus.Scheduled)
            .ToList();
        return Task.FromResult<IEnumerable<Job>>(dueJobs);
    }

    public Task<IEnumerable<Job>> GetFailedJobsAsync()
    {
        return Task.FromResult<IEnumerable<Job>>(
            _jobs.Values.Where(j => j.Status == JobStatus.Failed || j.Status == JobStatus.FailedPermanently).ToList());
    }

    public Task<IEnumerable<Job>> GetLongRunningJobsAsync(int thresholdSeconds)
    {
        var now = DateTime.UtcNow;
        var longRunning = _jobs.Values
            .Where(j => j.Status == JobStatus.Running && j.LastExecutedAt.HasValue &&
                        (now - j.LastExecutedAt.Value).TotalSeconds > thresholdSeconds)
            .ToList();
        return Task.FromResult<IEnumerable<Job>>(longRunning);
    }

    public Task<IEnumerable<Job>> GetJobsWithoutRecentExecutionAsync(int minutesThreshold)
    {
        var threshold = DateTime.UtcNow.AddMinutes(-minutesThreshold);
        var jobs = _jobs.Values
            .Where(j => j.IsActive && (!j.LastExecutedAt.HasValue || j.LastExecutedAt < threshold))
            .ToList();
        return Task.FromResult<IEnumerable<Job>>(jobs);
    }

    public Task AddAsync(Job entity)
    {
        if (entity.Id == Guid.Empty)
            entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;
        _jobs[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public Task AddRangeAsync(IEnumerable<Job> entities)
    {
        foreach (var entity in entities)
        {
            if (entity.Id == Guid.Empty)
                entity.Id = Guid.NewGuid();
            entity.CreatedAt = DateTime.UtcNow;
            _jobs[entity.Id] = entity;
        }
        return Task.CompletedTask;
    }

    public void Update(Job entity)
    {
        if (_jobs.ContainsKey(entity.Id))
        {
            _jobs[entity.Id] = entity;
        }
    }

    public void UpdateRange(IEnumerable<Job> entities)
    {
        foreach (var entity in entities)
            Update(entity);
    }

    public void Remove(Job entity)
    {
        _jobs.Remove(entity.Id);
    }

    public void RemoveRange(IEnumerable<Job> entities)
    {
        foreach (var entity in entities)
            Remove(entity);
    }

    public Task<bool> AnyAsync(Expression<Func<Job, bool>> predicate)
    {
        return Task.FromResult(_jobs.Values.AsQueryable().Any(predicate));
    }

    public Task SaveChangesAsync() => Task.CompletedTask;

    public Task<JobQueryResult> QueryAsync(JobQuery query)
    {
        var results = _jobs.Values.ToList();
        return Task.FromResult(new JobQueryResult(results, results.Count));
    }
public Task<IEnumerable<Job>> GetMisfiredJobsAsync()
{
    var now = DateTime.UtcNow;
    var misfiredJobs = _jobs.Values
        .Where(j => j.IsActive && j.Status != JobStatus.Suspended && j.Status != JobStatus.Cancelled &&
                     j.NextExecutionAt.HasValue && j.NextExecutionAt < now.AddSeconds(-60))
        .ToList();
    return Task.FromResult<IEnumerable<Job>>(misfiredJobs);
}
}