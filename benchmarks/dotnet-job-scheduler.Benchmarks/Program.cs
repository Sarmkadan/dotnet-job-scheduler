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
]);
