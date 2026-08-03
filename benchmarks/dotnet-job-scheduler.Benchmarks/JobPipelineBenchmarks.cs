using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Services;
using JobScheduler.Core.Exceptions;

namespace dotnet_job_scheduler.Benchmarks
{
    [MemoryDiagnoser]
    public class JobPipelineBenchmarks
    {
        // Parameters for AddSteps benchmark
        [Params(100, 1000, 10000)]
        public int AddStepsSize;

        // Parameters for ValidateAsync benchmark
        [Params(100, 1000, 10000)]
        public int ValidateAsyncSize;

        // Parameter to control whether the graph is valid or cyclic
        [Params(true, false)]
        public bool IsValidGraph;

        private JobPipeline _pipeline;
        private MockJobDependencyService _dependencyService;

        [GlobalSetup]
        public void Setup()
        {
            _pipeline = new JobPipeline();
            DependencyGraphValidationResult result;
            if (IsValidGraph)
            {
                result = new DependencyGraphValidationResult { IsValid = true, Message = "Dependency graph is a valid DAG." };
            }
            else
            {
                // Create a simple cycle: A -> B -> C -> A
                var cycleNodes = new List<Guid> { Guid.Parse("11111111-1111-1111-1111-111111111111"),
                                                  Guid.Parse("22222222-2222-2222-2222-222222222222"),
                                                  Guid.Parse("33333333-3333-3333-3333-333333333333") };
                result = new DependencyGraphValidationResult { IsValid = false, CycleNodes = cycleNodes, Message = "Cycle detected involving 3 job(s): ..." };
            }
            _dependencyService = new MockJobDependencyService(ValidateAsyncSize, result);
        }

        [Benchmark]
        public void AddSteps_Benchmark()
        {
            var pipeline = new JobPipeline();
            for (int i = 0; i < AddStepsSize; i++)
            {
                pipeline.Steps.Add(new JobPipelineStep { JobId = Guid.NewGuid() });
            }
        }

        [Benchmark]
        public Task ValidateAsync_Benchmark()
        {
            return _pipeline.ValidateAsync(_dependencyService);
        }

        [Benchmark]
        public void CreatePopulatedPipeline_Benchmark()
        {
            var pipeline = new JobPipeline
            {
                Id = Guid.NewGuid(),
                Name = "Test Pipeline",
                Description = "This is a test pipeline for benchmarking",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = "benchmark_user"
            };

            // Add some steps
            for (int i = 0; i < 10; i++)
            {
                pipeline.Steps.Add(new JobPipelineStep
                {
                    JobId = Guid.NewGuid(),
                    StepOrder = i,
                    StopOnFailure = true
                });
            }
        }

        [Benchmark]
        public void AccessSteps_Benchmark()
        {
            // Just access the Steps property multiple times
            for (int i = 0; i < 1000; i++)
            {
                var count = _pipeline.Steps.Count;
                var isReadOnly = ((System.Collections.IList)_pipeline.Steps).IsReadOnly;
                var isFixedSize = ((System.Collections.IList)_pipeline.Steps).IsFixedSize;
            }
        }

        [Benchmark]
        public void ClearSteps_Benchmark()
        {
            // Fill up the pipeline first
            for (int i = 0; i < 1000; i++)
            {
                _pipeline.Steps.Add(new JobPipelineStep { JobId = Guid.NewGuid() });
            }

            // Then clear it
            _pipeline.Steps.Clear();
        }

        private class MockJobDependencyService : IJobDependencyService
        {
            private readonly int _size;
            private readonly DependencyGraphValidationResult _result;

            public MockJobDependencyService(int size, DependencyGraphValidationResult result)
            {
                _size = size;
                _result = result;
            }

            public Task<DependencyGraphValidationResult> ValidateGraphAsync(CancellationToken cancellationToken = default)
            {
                // Simulate work by doing a loop of size _size
                for (int i = 0; i < _size; i++)
                {
                    // Do nothing, just to burn cycles
                }

                return Task.FromResult(_result);
            }

            public Task AddDependencyAsync(Guid jobId, Guid dependsOnJobId, string? createdBy = null, CancellationToken cancellationToken = default)
            {
                // Simulate work
                for (int i = 0; i < _size; i++) { }
                return Task.CompletedTask;
            }

            public Task RemoveDependencyAsync(Guid jobId, Guid dependsOnJobId, CancellationToken cancellationToken = default)
            {
                // Simulate work
                for (int i = 0; i < _size; i++) { }
                return Task.CompletedTask;
            }

            public Task<IReadOnlyList<Job>> GetDependenciesAsync(Guid jobId, CancellationToken cancellationToken = default)
            {
                // Simulate work and return empty list
                for (int i = 0; i < _size; i++) { }
                return Task.FromResult<IReadOnlyList<Job>>(new List<Job>());
            }

            public Task<IReadOnlyList<Job>> GetDependentsAsync(Guid jobId, CancellationToken cancellationToken = default)
            {
                // Simulate work and return empty list
                for (int i = 0; i < _size; i++) { }
                return Task.FromResult<IReadOnlyList<Job>>(new List<Job>());
            }

            public Task<IReadOnlyList<Job>> GetTopologicalOrderAsync(CancellationToken cancellationToken = default)
            {
                // Simulate work and return empty list
                for (int i = 0; i < _size; i++) { }
                return Task.FromResult<IReadOnlyList<Job>>(new List<Job>());
            }
        }
    }
}