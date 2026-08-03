using BenchmarkDotNet.Attributes;
using JobScheduler.Core.Domain.Entities;

namespace JobScheduler.Benchmarks;

[MemoryDiagnoser]
public class JobDependencyBenchmarks
{
    private JobDependency _jobDependency;
    private string _json;

    [Params(10, 100)]
    public int Count { get; set; }

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

    [Benchmark]
    public string Serialize() => _jobDependency.ToJson();

    [Benchmark]
    public string SerializeIndented() => _jobDependency.ToJson(true);

    [Benchmark]
    public JobDependency? Deserialize() => JobDependencyJsonExtensions.FromJson(_json);

    [Benchmark]
    public bool TryDeserialize() => JobDependencyJsonExtensions.TryFromJson(_json, out _);

    [Benchmark]
    public void SerializeMany()
    {
        for (int i = 0; i < Count; i++)
        {
            _jobDependency.ToJson();
        }
    }
}
