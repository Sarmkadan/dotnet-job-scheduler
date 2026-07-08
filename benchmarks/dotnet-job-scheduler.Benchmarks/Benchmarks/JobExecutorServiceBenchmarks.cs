#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ====================================================================

using BenchmarkDotNet.Attributes;
using JobScheduler.Core.Constants;
using JobScheduler.Core.Data.Repositories;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JobScheduler.Benchmarks;

/// <summary>
/// Measures JobExecutorService operations that handle actual job execution:
/// - Job execution with timeout
/// - Error handling and retry logic
/// - Execution metrics collection
/// - Concurrency control enforcement
/// </summary>
[MemoryDiagnoser]
public sealed class JobExecutorServiceBenchmarks
{
    private IServiceProvider? _serviceProvider;
    private JobExecutorService? _executorService;
    private MockJobHandler? _successHandler;
    private MockFailingHandler? _failingHandler;
    private MockSlowHandler? _slowHandler;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddLogging(configure => configure.AddConsole().SetMinimumLevel(LogLevel.Error));
        services.AddSingleton<RetryService>();
        services.AddSingleton<ConcurrencyManager>();
        services.AddSingleton<PerformanceMonitor>();
        services.AddSingleton<CronExpressionService>();
        services.AddSingleton<CacheService>();

        // Mock repositories
        services.AddSingleton<IJobRepository>(new MockJobRepository());
        services.AddSingleton<IExecutionRepository>(new MockExecutionRepository());

        _serviceProvider = services.BuildServiceProvider();
        _executorService = _serviceProvider.GetRequiredService<JobExecutorService>();

        // Create mock handlers
        _successHandler = new MockJobHandler("Success", TimeSpan.Zero);
        _failingHandler = new MockFailingHandler();
        _slowHandler = new MockSlowHandler(TimeSpan.FromSeconds(10));
    }

    [Benchmark]
    public async Task ExecuteJob_Successful()
    {
        var job = new Job
        {
            Name = "SuccessfulJob",
            Description = "Job that executes successfully",
            HandlerType = "MockJobHandler, Benchmarks",
            Priority = JobPriority.Normal,
            MaxRetries = 3,
            ExecutionTimeoutSeconds = 30,
            IsActive = true,
            Status = JobStatus.Scheduled
        };

        var execution = await _executorService!.ExecuteJobAsync(job, _successHandler, "test-server", default);
        _ = execution.Status;
    }

    [Benchmark]
    public async Task ExecuteJob_Failing()
    {
        var job = new Job
        {
            Name = "FailingJob",
            Description = "Job that fails",
            HandlerType = "MockFailingHandler, Benchmarks",
            Priority = JobPriority.Normal,
            MaxRetries = 3,
            ExecutionTimeoutSeconds = 30,
            IsActive = true,
            Status = JobStatus.Scheduled
        };

        var execution = await _executorService!.ExecuteJobAsync(job, _failingHandler, "test-server", default);
        _ = execution.Status;
    }

    [Benchmark]
    public async Task ExecuteJob_Timeout()
    {
        var job = new Job
        {
            Name = "TimeoutJob",
            Description = "Job that times out",
            HandlerType = "MockSlowHandler, Benchmarks",
            Priority = JobPriority.Normal,
            MaxRetries = 3,
            ExecutionTimeoutSeconds = 1, // Very short timeout
            IsActive = true,
            Status = JobStatus.Scheduled
        };

        var execution = await _executorService!.ExecuteJobAsync(job, _slowHandler, "test-server", default);
        _ = execution.Status;
    }

    [Benchmark]
    public async Task ExecuteJob_WithConcurrencyLimit()
    {
        var job = new Job
        {
            Name = "ConcurrentJob",
            Description = "Job with concurrency limit",
            HandlerType = "MockJobHandler, Benchmarks",
            Priority = JobPriority.Normal,
            MaxRetries = 3,
            ExecutionTimeoutSeconds = 30,
            MaxConcurrentExecutions = 1, // Limit to 1 concurrent execution
            IsActive = true,
            Status = JobStatus.Scheduled
        };

        var execution = await _executorService!.ExecuteJobAsync(job, _successHandler, "test-server", default);
        _ = execution.Status;
    }

    [Benchmark]
    public async Task ExecuteJob_WithPriority()
    {
        var highPriorityJob = new Job
        {
            Name = "HighPriorityJob",
            Description = "High priority job",
            HandlerType = "MockJobHandler, Benchmarks",
            Priority = JobPriority.High,
            MaxRetries = 3,
            ExecutionTimeoutSeconds = 30,
            IsActive = true,
            Status = JobStatus.Scheduled
        };

        var execution = await _executorService!.ExecuteJobAsync(highPriorityJob, _successHandler, "test-server", default);
        _ = execution.Status;
    }

    [Benchmark]
    public async Task ExecuteJob_WithMetricsCollection()
    {
        var job = new Job
        {
            Name = "MetricsJob",
            Description = "Job with metrics collection",
            HandlerType = "MockJobHandler, Benchmarks",
            Priority = JobPriority.Normal,
            MaxRetries = 3,
            ExecutionTimeoutSeconds = 30,
            IsActive = true,
            Status = JobStatus.Scheduled
        };

        var execution = await _executorService!.ExecuteJobAsync(job, _successHandler, "test-server", default);
        _ = execution.MemoryUsageMb;
        _ = execution.CpuUsagePercent;
    }
}

/// <summary>
/// Mock job handler that succeeds immediately
/// </summary>
internal sealed class MockJobHandler : IJobHandler
{
    private readonly string _result;
    private readonly TimeSpan _delay;

    public MockJobHandler(string result, TimeSpan delay)
    {
        _result = result;
        _delay = delay;
    }

    public async Task<string> ExecuteAsync(Job job, CancellationToken cancellationToken)
    {
        await Task.Delay(_delay, cancellationToken);
        return _result;
    }
}

/// <summary>
/// Mock job handler that always fails
/// </summary>
internal sealed class MockFailingHandler : IJobHandler
{
    public async Task<string> ExecuteAsync(Job job, CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken);
        throw new InvalidOperationException("Job failed as expected");
    }
}

/// <summary>
/// Mock job handler that runs slowly
/// </summary>
internal sealed class MockSlowHandler : IJobHandler
{
    private readonly TimeSpan _delay;

    public MockSlowHandler(TimeSpan delay)
    {
        _delay = delay;
    }

    public async Task<string> ExecuteAsync(Job job, CancellationToken cancellationToken)
    {
        await Task.Delay(_delay, cancellationToken);
        return "Completed";
    }
}