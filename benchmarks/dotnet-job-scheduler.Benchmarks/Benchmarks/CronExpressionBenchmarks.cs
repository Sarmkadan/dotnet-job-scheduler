// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using BenchmarkDotNet.Attributes;
using JobScheduler.Core.Services;

namespace JobScheduler.Benchmarks;

/// <summary>
/// Measures cron expression parsing and schedule evaluation throughput.
/// These operations sit on the hot path: they execute for every scheduled job
/// on every scheduler tick.
/// </summary>
[MemoryDiagnoser]
public class CronExpressionBenchmarks
{
    private CronExpressionService _service = null!;

    private const string EveryMinute  = "* * * * *";
    private const string DailyAt9Am   = "0 9 * * *";
    private const string WeekdaysAt8  = "0 8 * * 1-5";
    private const string FirstOfMonth = "0 0 1 * *";

    [GlobalSetup]
    public void Setup() => _service = new CronExpressionService();

    /// <summary>Validates syntax only — no schedule object kept.</summary>
    [Benchmark]
    public bool IsValidCronExpression_Simple() =>
        _service.IsValidCronExpression(EveryMinute);

    /// <summary>
    /// First call for each expression exercises the NCronTab parser;
    /// subsequent calls return the cached CrontabSchedule.
    /// </summary>
    [Benchmark]
    public DateTime GetNextExecutionTime_EveryMinute() =>
        _service.GetNextExecutionTime(EveryMinute);

    [Benchmark]
    public DateTime GetNextExecutionTime_Daily() =>
        _service.GetNextExecutionTime(DailyAt9Am);

    [Benchmark]
    public DateTime GetNextExecutionTime_Weekdays() =>
        _service.GetNextExecutionTime(WeekdaysAt8);

    /// <summary>Computes 10 upcoming occurrences from today.</summary>
    [Benchmark]
    public IEnumerable<DateTime> GetNextExecutionTimes_10() =>
        _service.GetNextExecutionTimes(DailyAt9Am, 10);

    /// <summary>
    /// Checks whether a job would fire within 60 s of a given instant.
    /// Used by the scheduler tick loop.
    /// </summary>
    [Benchmark]
    public bool ShouldExecuteAt_Miss() =>
        _service.ShouldExecuteAt(FirstOfMonth, DateTime.UtcNow);

    [Benchmark]
    public string GetCronDescription_Simple() =>
        _service.GetCronDescription(EveryMinute);
}
