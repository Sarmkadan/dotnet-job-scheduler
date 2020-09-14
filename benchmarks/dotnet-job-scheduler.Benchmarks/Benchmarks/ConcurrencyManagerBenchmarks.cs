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

        // Mock repository
        services.AddSingleton<IJobRepository>(new MockJobRepository());

        _serviceProvider = services.BuildServiceProvider();
        _concurrencyManager = new ConcurrencyManager(_serviceProvider.GetRequiredService<IJobRepository>());
    }

    [Benchmark]
    public bool CanExecuteJob_GlobalLimitNotReached()
    {
        var job = new Job
        {
            Id = 1,
            Name = "TestJob1",
            MaxConcurrentExecutions = 1,
            IsActive = true
        };

        return _concurrencyManager!.CanExecuteJob(job);
    }

    [Benchmark]
    public bool CanExecuteJob_GlobalLimitReached()
    {
        var job = new Job
        {
            Id = 2,
            Name = "TestJob2",
            MaxConcurrentExecutions = 1,
            IsActive = true
        };

        // Simulate global limit reached
        _concurrencyManager!.TrackExecutionStart(job.Id);
        _concurrencyManager!.TrackExecutionStart(job.Id); // Should exceed limit

        return _concurrencyManager.CanExecuteJob(job);
    }

    [Benchmark]
    public bool CanExecuteJob_PerJobLimit()
    {
        var job = new Job
        {
            Id = 3,
            Name = "TestJob3",
            MaxConcurrentExecutions = 2,
            IsActive = true
        };

        // Track some executions for this job
        _concurrencyManager!.TrackExecutionStart(job.Id);
        _concurrencyManager!.TrackExecutionStart(job.Id);

        return _concurrencyManager.CanExecuteJob(job);
    }

    [Benchmark]
    public void TrackExecutionStart()
    {
        var jobId = 100;
        _concurrencyManager!.TrackExecutionStart(jobId);
    }

    [Benchmark]
    public void TrackExecutionEnd()
    {
        var jobId = 200;
        _concurrencyManager!.TrackExecutionStart(jobId);
        _concurrencyManager!.TrackExecutionEnd(jobId);
    }

    [Benchmark]
    public int GetCurrentConcurrencyCount()
    {
        var jobId = 300;
        _concurrencyManager!.TrackExecutionStart(jobId);
        _concurrencyManager!.TrackExecutionStart(jobId);

        var count = _concurrencyManager.GetCurrentConcurrencyCount(jobId);
        _concurrencyManager!.TrackExecutionEnd(jobId);
        _concurrencyManager!.TrackExecutionEnd(jobId);

        return count;
    }

    [Benchmark]
    public int GetGlobalConcurrencyCount()
    {
        _concurrencyManager!.TrackExecutionStart(1);
        _concurrencyManager!.TrackExecutionStart(2);
        _concurrencyManager!.TrackExecutionStart(3);

        var count = _concurrencyManager.GetGlobalConcurrencyCount();

        _concurrencyManager!.TrackExecutionEnd(1);
        _concurrencyManager!.TrackExecutionEnd(2);
        _concurrencyManager!.TrackExecutionEnd(3);

        return count;
    }

    [Benchmark]
    public void Reset()
    {
        _concurrencyManager!.TrackExecutionStart(1);
        _concurrencyManager!.TrackExecutionStart(2);

        _concurrencyManager!.Reset();
    }
}

/// <summary>
/// Mock repository for ConcurrencyManager benchmarks
/// </summary>
internal sealed class ConcurrencyMockJobRepository : IJobRepository
{
    private readonly Dictionary<int, Job> _jobs = new();

    public Task<Job?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        _jobs.TryGetValue(id, out var job);
        return Task.FromResult(job);
    }

    public Task<Job?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var job = _jobs.Values.FirstOrDefault(j => j.Name == name);
        return Task.FromResult(job);
    }

    public Task<List<Job>> GetActiveJobsAsync(CancellationToken cancellationToken = default)
    {
        var activeJobs = _jobs.Values.Where(j => j.IsActive).ToList();
        return Task.FromResult(activeJobs);
    }

    public Task<List<Job>> GetScheduledJobsForExecutionAsync(CancellationToken cancellationToken = default)
    {
        var dueJobs = _jobs.Values
            .Where(j => j.IsActive && j.Status == JobStatus.Scheduled)
            .ToList();
        return Task.FromResult(dueJobs);
    }

    public Task<Job> AddAsync(Job entity, CancellationToken cancellationToken = default)
    {
        entity.Id = _jobs.Count + 1;
        entity.CreatedAt = DateTime.UtcNow;
        _jobs[entity.Id] = entity;
        return Task.FromResult(entity);
    }

    public Task UpdateAsync(Job entity, CancellationToken cancellationToken = default)
    {
        if (_jobs.ContainsKey(entity.Id))
        {
            _jobs[entity.Id] = entity;
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        _jobs.Remove(id);
        return Task.CompletedTask;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(1);
    }

    public Task<JobQueryResult> QueryAsync(JobQuery query, CancellationToken cancellationToken = default)
    {
        var results = _jobs.Values.AsQueryable();
        var items = results.ToList();
        return Task.FromResult(new JobQueryResult(items, items.Count));
    }
}