#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using BenchmarkDotNet.Running;
using JobScheduler.Benchmarks;

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
]);
