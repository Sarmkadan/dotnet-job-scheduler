using BenchmarkDotNet.Attributes;
using JobScheduler.Core.Domain.Entities;

namespace JobScheduler.Benchmarks;

/// <summary>
/// Contains benchmarks for measuring the performance of JobDependency serialization and deserialization operations.
/// </summary>
[MemoryDiagnoser]
public class JobDependencyBenchmarks
{
    private JobDependency _jobDependency;
    private string _json;

    [Params(10, 100)]
    public int Count { get; set; }

    /// <summary>
    /// Initializes the test data for the benchmarks.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _jobDependency = new JobDependency
        {
            JobId = Guid.NewGuid(),
            DependsOnJobId = Guid.NewGuid(),
            CreatedBy = "BenchmarkUser"
        };
        _json = _jobDependency.ToJson();
    }

    /// <summary>
    /// Measures the performance of serializing a JobDependency to JSON.
    /// </summary>
    [Benchmark]
    public string Serialize() => _jobDependency.ToJson();

    /// <summary>
    /// Measures the performance of serializing a JobDependency to indented JSON.
    /// </summary>
    [Benchmark]
    public string SerializeIndented() => _jobDependency.ToJson(true);

    /// <summary>
    /// Measures the performance of deserializing a JSON string into a JobDependency object.
    /// </summary>
    [Benchmark]
    public JobDependency? Deserialize() => JobDependencyJsonExtensions.FromJson(_json);

    /// <summary>
    /// Measures the performance of attempting to deserialize a JSON string into a JobDependency object.
    /// </summary>
    [Benchmark]
    public bool TryDeserialize() => JobDependencyJsonExtensions.TryFromJson(_json, out _);

    /// <summary>
    /// Measures the performance of serializing multiple JobDependency objects in a loop.
    /// </summary>
    [Benchmark]
    public void SerializeMany()
    {
        for (int i = 0; i < Count; i++)
        {
            _jobDependency.ToJson();
        }
    }
}
