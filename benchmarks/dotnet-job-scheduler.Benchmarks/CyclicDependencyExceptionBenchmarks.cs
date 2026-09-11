using BenchmarkDotNet.Attributes;
using JobScheduler.Core.Exceptions;

namespace JobScheduler.Benchmarks;

[MemoryDiagnoser]
/// <summary>
/// Benchmarks for measuring the performance of CyclicDependencyException creation.
/// </summary>
public class CyclicDependencyExceptionBenchmarks
{
    private Guid _jobId;
    private Guid _dependsOnJobId;
    private Exception? _innerException;

    [Params(10, 100, 1000)]
    public int Count { get; set; }

    [GlobalSetup]
    /// <summary>
    /// Initializes test data for each benchmark iteration.
    /// </summary>
    public void Setup()
    {
        _jobId = Guid.NewGuid();
        _dependsOnJobId = Guid.NewGuid();
        _innerException = new Exception("Inner exception");
    }

    [Benchmark]
    /// <summary>
    /// Measures the time to create a CyclicDependencyException with job IDs only.
    /// </summary>
    public CyclicDependencyException CreateSimple()
    {
        return new CyclicDependencyException(_jobId, _dependsOnJobId);
    }

    [Benchmark]
    public CyclicDependencyException CreateWithInner()
    {
        return new CyclicDependencyException(_jobId, _dependsOnJobId, _innerException);
    }

    /// <summary>
/// Measures the time to create multiple CyclicDependencyException instances (as specified by Count).
/// </summary>
[Benchmark]
    public void CreateMany()
    {
        for (int i = 0; i < Count; i++)
        {
            var ex = new CyclicDependencyException(_jobId, _dependsOnJobId);
        }
    }
}
