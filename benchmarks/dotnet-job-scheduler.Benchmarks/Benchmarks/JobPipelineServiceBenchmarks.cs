#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ====================================================================

using BenchmarkDotNet.Attributes;
using JobScheduler.Core.Constants;
using JobScheduler.Core.Data.Repositories;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Domain.Models;
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
    private List<Guid> _testJobIds = new();

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddLogging(configure => configure.AddConsole().SetMinimumLevel(LogLevel.Error));

        // Mock repositories
        _jobRepository = new MockJobRepository();
        services.AddSingleton<IJobRepository>(_jobRepository);
        services.AddSingleton<IExecutionRepository>(new MockExecutionRepository());
        services.AddSingleton<IJobDependencyService, MockJobDependencyService>();

        _serviceProvider = services.BuildServiceProvider();
        _pipelineService = _serviceProvider.GetRequiredService<JobPipelineService>();

        // Create test jobs for pipelines
        _testJobIds = new List<Guid>();
        for (int i = 0; i < 20; i++)
        {
            var job = new Job
            {
                Name = $"PipelineJob{i:D2}",
                Description = $"Job for pipeline {i}",
                CronExpression = "0 9 * * *",
                HandlerType = "TestHandler, TestAssembly",
                Priority = JobPriority.Normal,
                MaxRetries = 3,
                ExecutionTimeoutSeconds = 300,
                IsActive = true
            };

            _jobRepository.AddAsync(job).GetAwaiter().GetResult();
            _testJobIds.Add(job.Id);
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
                new() { JobId = _testJobIds[0], StopOnFailure = true },
                new() { JobId = _testJobIds[1], StopOnFailure = true },
                new() { JobId = _testJobIds[2], StopOnFailure = false }
            }
        }, "test-user");

        _ = pipeline.Id;
    }

    [Benchmark]
    public async Task CreatePipeline_10Steps()
    {
        var steps = new List<PipelineStepRequest>();
        for (int i = 0; i < 10; i++)
        {
            steps.Add(new PipelineStepRequest { JobId = _testJobIds[i], StopOnFailure = true });
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
                new() { JobId = _testJobIds[0], StopOnFailure = true },
                new() { JobId = _testJobIds[1], StopOnFailure = true },
                new() { JobId = _testJobIds[2], StopOnFailure = false }
            }
        }, "test-user");

        var status = await _pipelineService!.GetPipelineStatusAsync(pipeline.Id);
        _ = status?.StepStatuses.Count;
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
        var pipeline = await _pipelineService!.GetPipelineAsync(_testJobIds[0]);
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
            Steps = new List<PipelineStepRequest>
            {
                new() { JobId = _testJobIds[0], StopOnFailure = true },
                new() { JobId = _testJobIds[1], StopOnFailure = true }
            }
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
                new() { JobId = _testJobIds[0], StopOnFailure = true },
                new() { JobId = _testJobIds[1], StopOnFailure = true }
            }
        }, "test-user");

        var status = await _pipelineService!.GetPipelineStatusAsync(pipeline.Id);
        var isReady = status?.StepStatuses.Any(s => s.IsReady) ?? false;
        _ = isReady;
    }
}

/// <summary>
/// Mock JobDependencyService for pipeline benchmarks
/// </summary>
internal sealed class MockJobDependencyService : IJobDependencyService
{
    private readonly Dictionary<Guid, List<Guid>> _dependencies = new();

    public Task AddDependencyAsync(Guid jobId, Guid dependsOnJobId, string? createdBy = null,
        CancellationToken cancellationToken = default)
    {
        if (!_dependencies.TryGetValue(jobId, out var list))
        {
            list = new List<Guid>();
            _dependencies[jobId] = list;
        }
        list.Add(dependsOnJobId);
        return Task.CompletedTask;
    }

    public Task RemoveDependencyAsync(Guid jobId, Guid dependsOnJobId, CancellationToken cancellationToken = default)
    {
        if (_dependencies.TryGetValue(jobId, out var list))
            list.Remove(dependsOnJobId);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Job>> GetDependenciesAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Job>>(Array.Empty<Job>());
    }

    public Task<IReadOnlyList<Job>> GetDependentsAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Job>>(Array.Empty<Job>());
    }

    public Task<IReadOnlyList<Job>> GetTopologicalOrderAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Job>>(Array.Empty<Job>());
    }

    public Task<DependencyGraphValidationResult> ValidateGraphAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new DependencyGraphValidationResult { IsValid = true, Message = "Mock graph is valid." });
    }
}
