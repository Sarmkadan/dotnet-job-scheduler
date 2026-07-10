# CronExpressionBenchmarks

Provides a suite of micro-benchmarks for evaluating the performance of core `CronExpression` operations. This class measures the throughput and latency of parsing, validation, next-execution-time calculation, batch retrieval, execution-time matching, and description generation under representative workloads.

## API

### `public void Setup`
Prepares the benchmark environment before each iteration. Initializes any required state, such as pre-parsed `CronExpression` instances or reference timestamps, ensuring that setup overhead is excluded from measurement.

### `public bool IsValidCronExpression_Simple`
Benchmarks the validation of a straightforward cron expression string. Returns `true` if the expression is syntactically and semantically valid; otherwise `false`. Does not throw.

### `public DateTime GetNextExecutionTime_EveryMinute`
Computes the next execution time for a cron expression configured to fire every minute, starting from a fixed reference instant. Returns the resulting `DateTime`. Throws if the underlying expression is invalid or if the calculation overflows the supported time range.

### `public DateTime GetNextExecutionTime_Daily`
Computes the next execution time for a daily schedule from a fixed reference instant. Returns the resulting `DateTime`. Throws under the same conditions as `GetNextExecutionTime_EveryMinute`.

### `public DateTime GetNextExecutionTime_Weekdays`
Computes the next execution time for a weekday-only schedule from a fixed reference instant. Returns the resulting `DateTime`. Throws under the same conditions as `GetNextExecutionTime_EveryMinute`.

### `public IEnumerable<DateTime> GetNextExecutionTimes_10`
Retrieves the next ten consecutive execution times from a fixed reference instant. Returns a lazy enumeration of `DateTime` values. Throws if the expression is invalid or if any intermediate calculation fails.

### `public bool ShouldExecuteAt_Miss`
Determines whether the cron expression would trigger at a specific instant that intentionally falls outside the expected schedule. Returns `true` if the expression matches; otherwise `false`. Does not throw.

### `public string GetCronDescription_Simple`
Generates a human-readable description for a simple cron expression. Returns the description as a `string`. Throws if the expression cannot be parsed or described.

## Usage

```csharp
// Benchmarking next-execution-time retrieval for a daily schedule
var benchmarks = new CronExpressionBenchmarks();
benchmarks.Setup();
DateTime nextDaily = benchmarks.GetNextExecutionTime_Daily();
Console.WriteLine($"Next daily execution: {nextDaily}");
```

```csharp
// Validating a cron expression and obtaining its description
var benchmarks = new CronExpressionBenchmarks();
benchmarks.Setup();
bool valid = benchmarks.IsValidCronExpression_Simple();
if (valid)
{
    string description = benchmarks.GetCronDescription_Simple();
    Console.WriteLine($"Expression description: {description}");
}
```

## Notes

- All benchmark methods assume that `Setup` has been called first; behavior is undefined if invoked without prior setup.
- The `DateTime`-returning methods use a fixed reference instant, typically `DateTime.UtcNow` captured during setup, to ensure repeatable measurements.
- `GetNextExecutionTimes_10` returns a lazy sequence; benchmark timing includes enumeration of all ten elements.
- `ShouldExecuteAt_Miss` is designed to measure the cost of a negative match and does not mutate any shared state.
- This class is intended for diagnostic and profiling scenarios only. It is not thread-safe for concurrent execution of individual benchmark methods, as internal state may be overwritten by `Setup` between iterations.
