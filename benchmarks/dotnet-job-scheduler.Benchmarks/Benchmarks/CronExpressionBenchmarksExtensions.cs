#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using BenchmarkDotNet.Running;
using JobScheduler.Core.Services;

namespace JobScheduler.Benchmarks;

/// <summary>
/// Extension methods for <see cref="CronExpressionBenchmarks"/> that provide additional benchmark scenarios
/// and performance measurements for cron expression operations.
/// </summary>
public static class CronExpressionBenchmarksExtensions
{
    /// <summary>
    /// Benchmarks parallel execution of multiple cron expression validations.
    /// Tests the overhead of concurrent validation requests which can occur in web scenarios.
    /// </summary>
    public static bool IsValidCronExpression_Parallel(this CronExpressionBenchmarks benchmarks, int iterations = 1000)
    {
        var service = new CronExpressionService();
        var expressions = new[] { "* * * * *", "0 9 * * *", "0 8 * * 1-5", "0 0 1 * *", "*/5 * * * *" };

        bool result = true;
        for (int i = 0; i < iterations; i++)
        {
            foreach (var expr in expressions)
            {
                result &= service.IsValidCronExpression(expr);
            }
        }

        return result;
    }

    /// <summary>
    /// Benchmarks getting next execution times for multiple expressions simultaneously.
    /// Measures the overhead of creating multiple schedule objects in parallel.
    /// </summary>
    public static DateTime GetNextExecutionTime_MultipleExpressions(this CronExpressionBenchmarks benchmarks)
    {
        var service = new CronExpressionService();
        var expressions = new[] { "* * * * *", "0 9 * * *", "0 8 * * 1-5", "0 0 1 * *" };

        DateTime nextTime = DateTime.UtcNow;
        foreach (var expr in expressions)
        {
            nextTime = service.GetNextExecutionTime(expr);
        }

        return nextTime;
    }

    /// <summary>
    /// Benchmarks the generation of multiple upcoming execution times for different expressions.
    /// Tests the performance of batch operations which are common in reporting scenarios.
    /// </summary>
    public static IEnumerable<DateTime> GetNextExecutionTimes_Multiple(this CronExpressionBenchmarks benchmarks, int count = 20)
    {
        var service = new CronExpressionService();
        var expressions = new[] { "* * * * *", "0 9 * * *", "0 8 * * 1-5", "0 0 1 * *" };

        var results = new List<DateTime>();
        foreach (var expr in expressions)
        {
            results.AddRange(service.GetNextExecutionTimes(expr, count));
        }

        return results;
    }

    /// <summary>
    /// Benchmarks the ShouldExecuteAt check with various time ranges.
    /// Tests the scheduler's decision logic under different temporal conditions.
    /// </summary>
    public static bool ShouldExecuteAt_RangeCheck(this CronExpressionBenchmarks benchmarks)
    {
        var service = new CronExpressionService();
        var expressions = new[] { "* * * * *", "0 9 * * *", "0 8 * * 1-5", "0 0 1 * *", "*/15 * * * *" };
        var testTimes = new[] {
            DateTime.UtcNow.AddMinutes(-5),
            DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(1)
        };

        bool result = false;
        foreach (var expr in expressions)
        {
            foreach (var testTime in testTimes)
            {
                result |= service.ShouldExecuteAt(expr, testTime);
            }
        }

        return result;
    }
}