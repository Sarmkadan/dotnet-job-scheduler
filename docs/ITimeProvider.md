# ITimeProvider

`ITimeProvider` abstracts clock, delay, and time-zone operations so production code can use the system clock while tests can control time deterministically. It is defined in `src/JobScheduler.Core/Abstractions/ITimeProvider.cs` together with `SystemTimeProvider` and `TestTimeProvider`.

## Interface members

| Member | Description |
| --- | --- |
| `DateTimeOffset UtcNow { get; }` | Gets the current UTC date and time. |
| `DateTimeOffset Now { get; }` | Gets the current local date and time. |
| `DateTime UtcNowAsDateTime { get; }` | Gets the current UTC date and time as a `DateTime`. |
| `DateTime NowAsDateTime { get; }` | Gets the current local date and time as a `DateTime`. |
| `ValueTask Delay(TimeSpan delay, CancellationToken cancellationToken = default)` | Creates a delay for the supplied duration. |
| `TimeZoneInfo GetTimeZone(string id)` | Gets time-zone information for the supplied time-zone ID. |

## SystemTimeProvider

`SystemTimeProvider` is the sealed production implementation backed by the system clock. Its private constructor makes the shared `SystemTimeProvider.Instance` property the way to obtain an instance.

- `UtcNow` returns `DateTimeOffset.UtcNow`, and `Now` returns `DateTimeOffset.Now`.
- `UtcNowAsDateTime` returns `DateTime.UtcNow`, and `NowAsDateTime` returns `DateTime.Now`.
- `Delay` delegates to `Task.Delay`. If the supplied token is already cancelled, it returns a cancelled `ValueTask` immediately.
- `GetTimeZone` rejects a null, empty, or whitespace ID and otherwise calls `TimeZoneInfo.FindSystemTimeZoneById`.

## TestTimeProvider

`TestTimeProvider` is a sealed, controllable implementation intended for tests.

### Construction

```csharp
var clock = new TestTimeProvider(
    new DateTimeOffset(2026, 1, 15, 9, 0, 0, TimeSpan.Zero));
```

The constructor accepts an initial `DateTimeOffset` and an optional time-zone ID. When the ID is omitted or is exactly `"UTC"`, the provider uses `TimeZoneInfo.Utc`; otherwise it resolves the ID with `TimeZoneInfo.FindSystemTimeZoneById`. Both `UtcNow` and `Now` initially contain the supplied value.

### `Advance`

```csharp
clock.Advance(TimeSpan.FromMinutes(10));
```

Adds the supplied `TimeSpan` to both the UTC and local stored values.

### `SetUtcNow`

```csharp
clock.SetUtcNow(new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero));
```

Replaces the stored UTC value and updates `Now` by converting that value to the provider's configured test time zone.

### `Delay`

`Delay` does not wait or advance the stored clock. It returns a completed `ValueTask`, allowing code that awaits delays to finish immediately in tests. If the supplied cancellation token is already cancelled, it returns a cancelled `ValueTask` instead.

### `GetTimeZone`

`GetTimeZone` rejects a null, empty, or whitespace ID. If the requested ID matches the configured test time-zone ID, ignoring case, it returns the configured `TimeZoneInfo`; otherwise it resolves the requested ID with `TimeZoneInfo.FindSystemTimeZoneById`.

### Time properties

- `UtcNow` and `Now` expose the stored `DateTimeOffset` values.
- `UtcNowAsDateTime` returns the stored UTC value's `UtcDateTime`.
- `NowAsDateTime` returns the stored local value's `DateTime`.

## Deterministic test example

The following example injects `ITimeProvider` into code that checks whether a deadline has passed. The test advances the clock explicitly, so it does not depend on wall-clock time or a real delay.

```csharp
using JobScheduler.Core.Abstractions;

public sealed class DeadlineChecker
{
    private readonly ITimeProvider _timeProvider;

    public DeadlineChecker(ITimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public bool HasPassed(DateTimeOffset deadline) =>
        _timeProvider.UtcNow >= deadline;
}

var initialTime = new DateTimeOffset(
    2026, 1, 15, 9, 0, 0, TimeSpan.Zero);
var timeProvider = new TestTimeProvider(initialTime);
var checker = new DeadlineChecker(timeProvider);
var deadline = initialTime.AddMinutes(5);

if (checker.HasPassed(deadline))
{
    throw new InvalidOperationException("The deadline passed too early.");
}

timeProvider.Advance(TimeSpan.FromMinutes(5));

if (!checker.HasPassed(deadline))
{
    throw new InvalidOperationException("The deadline should have passed.");
}
```
