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
/// Measures JobPipelineService operations for job chain/pipeline support:
/// - Pipeline creation and validation
/// - Pipeline execution flow control
/// - Dependency chain resolution
/// - Pipeline status tracking
/// </summary>
[MemoryDiagnoser]
public sealed class JobPipelineServiceBenchmarks
{
    private IServiceProvider? _serviceProvider;
    private JobPipelineService? _pipelineService;
    private MockJobRepository? _jobRepository;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddLogging(configure => configure.AddConsole().SetMinimumLevel(LogLevel.Error));

        // Mock repositories
        _jobRepository = new MockJobRepository();
        services.AddSingleton<IJobRepository>(_jobRepository);
        services.AddSingleton<IExecutionRepository>(new MockExecutionRepository());
        services.AddSingleton<JobDependencyService>();

        _serviceProvider = services.BuildServiceProvider();
        _pipelineService = _serviceProvider.GetRequiredService<JobPipelineService>();

        // Create test jobs for pipelines
        for (int i = 0; i < 20; i++)
        {
            _jobRepository.AddAsync(new Job
            {
                Name = $"PipelineJob{i:D2}",
                Description = $"Job for pipeline {i}",
                CronExpression = "0 9 * * *",
                HandlerType = "TestHandler, TestAssembly",
                Priority = JobPriority.Normal,
                MaxRetries = 3,
                ExecutionTimeoutSeconds = 300,
                IsActive = true
            }).GetAwaiter().GetResult();
        }
        _jobRepository.SaveChangesAsync().GetAwaiter().GetResult();
    }

    [Benchmark]
    public async Task CreatePipeline_3Steps()
    {
        var pipeline = await _pipelineService!.CreatePipelineAsync(new CreatePipelineRequest
        {
            Name = "ETL-Pipeline-Test",
            Description = "ETL pipeline with 3 steps",
            Steps = new List<PipelineStepRequest>
            {
                new() { JobId = 1, StopOnFailure = true },
                new() { JobId = 2, StopOnFailure = true },
                new() { JobId = 3, StopOnFailure = false }
            }
        }, "test-user");

        _ = pipeline.Id;
    }

    [Benchmark]
    public async Task CreatePipeline_10Steps()
    {
        var steps = new List<PipelineStepRequest>();
        for (int i = 1; i <= 10; i++)
        {
            steps.Add(new PipelineStepRequest { JobId = i, StopOnFailure = true });
        }

        var pipeline = await _pipelineService!.CreatePipelineAsync(new CreatePipelineRequest
        {
            Name = "Large-Pipeline-Test",
            Description = "Pipeline with 10 steps",
            Steps = steps
        }, "test-user");

        _ = pipeline.Id;
    }

    [Benchmark]
    public async Task GetPipelineStatus()
    {
        // Create a pipeline first
        var pipeline = await _pipelineService!.CreatePipelineAsync(new CreatePipelineRequest
        {
            Name = "Status-Pipeline",
            Description = "Pipeline for status testing",
            Steps = new List<PipelineStepRequest>
            {
                new() { JobId = 1, StopOnFailure = true },
                new() { JobId = 2, StopOnFailure = true },
                new() { JobId = 3, StopOnFailure = false }
            }
        }, "test-user");

        var status = await _pipelineService!.GetPipelineStatusAsync(pipeline.Id);
        _ = status.StepStatuses.Count;
    }

    [Benchmark]
    public async Task GetAllPipelines()
    {
        var pipelines = await _pipelineService!.GetAllPipelinesAsync();
        _ = pipelines.Count;
    }

    [Benchmark]
    public async Task GetPipelineById()
    {
        var pipeline = await _pipelineService!.GetPipelineByIdAsync(1);
        _ = pipeline?.Id;
    }

    [Benchmark]
    public async Task DeletePipeline()
    {
        // Create a pipeline first
        var pipeline = await _pipelineService!.CreatePipelineAsync(new CreatePipelineRequest
        {
            Name = "Delete-Pipeline",
            Description = "Pipeline to delete",
            Steps = new List<PipelineStepRequest> { new() { JobId = 1, StopOnFailure = true } }
        }, "test-user");

        await _pipelineService!.DeletePipelineAsync(pipeline.Id);
    }

    [Benchmark]
    public async Task CheckPipelineReadyStatus()
    {
        // Create a pipeline first
        var pipeline = await _pipelineService!.CreatePipelineAsync(new CreatePipelineRequest
        {
            Name = "Ready-Pipeline",
            Description = "Pipeline for ready check",
            Steps = new List<PipelineStepRequest>
            {
                new() { JobId = 1, StopOnFailure = true },
                new() { JobId = 2, StopOnFailure = true }
            }
        }, "test-user");

        var isReady = await _pipelineService!.IsPipelineReadyAsync(pipeline.Id);
        _ = isReady;
    }
}

/// <summary>
/// Mock JobDependencyService for pipeline benchmarks
/// </summary>
internal sealed class MockJobDependencyService : IJobDependencyService
{
    private readonly Dictionary<int, List<int>> _dependencies = new();

    public Task AddDependencyAsync(int dependentJobId, int prerequisiteJobId, CancellationToken cancellationToken = default)
    {
        if (!_dependencies.ContainsKey(dependentJobId))
        {
            _dependencies[dependentJobId] = new List<int>();
        }
        _dependencies[dependentJobId].Add(prerequisiteJobId);
        return Task.CompletedTask;
    }

    public Task<List<int>> GetPrerequisitesAsync(int jobId, CancellationToken cancellationToken = default)
    {
        if (_dependencies.TryGetValue(jobId, out var prerequisites))
        {
            return Task.FromResult(prerequisites);
        }
        return Task.FromResult(new List<int>());
    }

    public Task<bool> HasPrerequisitesAsync(int jobId, CancellationToken cancellationToken = default)
    {
        var hasDeps = _dependencies.ContainsKey(jobId) && _dependencies[jobId].Count > 0;
        return Task.FromResult(hasDeps);
    }

    public Task RemoveDependenciesAsync(int jobId, CancellationToken cancellationToken = default)
    {
        _dependencies.Remove(jobId);
        return Task.CompletedTask;
    }
}