// ... (rest of README.md content)

## ConcurrencyManagerBenchmarks

The `ConcurrencyManagerBenchmarks` class measures performance of ConcurrencyManager operations that enforce execution limits, including global concurrency tracking, per-job concurrency limits, execution slot acquisition and release, and concurrency limit enforcement.

Example usage:
```csharp
// Create a new instance of ConcurrencyManagerBenchmarks
var benchmarks = new ConcurrencyManagerBenchmarks();

// Setup the benchmarks
benchmarks.Setup();

// Measure global concurrency limit not reached
var canExecuteGlobalLimitNotReached = await benchmarks.CanExecuteJob_GlobalLimitNotReached();
Console.WriteLine($"Can execute job globally: {canExecuteGlobalLimitNotReached}");

// Measure global concurrency limit reached
var canExecuteGlobalLimitReached = await benchmarks.CanExecuteJob_GlobalLimitReached();
Console.WriteLine($"Can execute job globally (limit reached): {canExecuteGlobalLimitReached}");

// Measure per-job concurrency limit
var canExecutePerJobLimit = await benchmarks.CanExecuteJob_PerJobLimit();
Console.WriteLine($"Can execute job per-job limit: {canExecutePerJobLimit}");

// Track execution start
benchmarks.TrackExecutionStart();

// Track execution end
benchmarks.TrackExecutionEnd();

// Get current concurrency count
var concurrencyCount = benchmarks.GetCurrentConcurrencyCount();
Console.WriteLine($"Concurrency count: {concurrencyCount}");

// Get global concurrency count
var globalConcurrencyCount = benchmarks.GetGlobalConcurrencyCount();
Console.WriteLine($"Global concurrency count: {globalConcurrencyCount}");

// Reset concurrency manager
var concurrencyStats = benchmarks.Reset();
Console.WriteLine($"Concurrency stats: {string.Join(", ", concurrencyStats.Select(x => $"{x.Key}: {x.Value}"))}");
```
```