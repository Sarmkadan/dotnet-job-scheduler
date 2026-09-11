#nullable enable

/// <summary>
/// The Program class contains the entry point for the BenchmarkDotNet runner.
/// </summary>
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using BenchmarkDotNet.Running;
using JobScheduler.Benchmarks;

/// <summary>
/// The Program class contains the entry point for the BenchmarkDotNet runner.
/// </summary>
class Program
{
    /// <summary>
    /// The Main method configures and invokes the BenchmarkDotNet runner.
    /// </summary>
    static void Main()
    {
        BenchmarkRunner.Run(
        [
            typeof(CronExpressionBenchmarks),
            typeof(StringProcessingBenchmarks),
            typeof(CsvProcessingBenchmarks),
            typeof(JobManagementBenchmarks),
            typeof(JobSchedulerServiceBenchmarks),
            typeof(JobExecutorServiceBenchmarks),
            typeof(ConcurrencyManagerBenchmarks),
            typeof(RetryServiceBenchmarks),
            typeof(JobPipelineServiceBenchmarks),
            typeof(CacheServiceBenchmarks),
        ]);
    }
}
