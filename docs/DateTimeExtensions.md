# DateTimeExtensions

A utility class providing common date and time manipulation and comparison operations for `System.DateTime` values. Designed to simplify recurring tasks such as checking temporal relationships, rounding timestamps, and computing start or end boundaries of calendar units.

## API

### `public static bool IsInThePast(DateTime value)`

Determines whether the specified `DateTime` instance represents a moment that has already occurred relative to the current system time (`DateTime.UtcNow`).
- **Parameters**
  - `value` – The `DateTime` to evaluate.
- **Return value**
  - `true` if `value` is earlier than `DateTime.UtcNow`; otherwise, `false`.
- **Exceptions**
  - Throws `System.ArgumentOutOfRangeException` if `value.Kind` is `DateTimeKind.Local` or `DateTimeKind.Unspecified` and the system clock is not in UTC.

### `public static bool IsInTheFuture(DateTime value)`

Determines whether the specified `DateTime` instance represents a moment that has not yet occurred relative to the current system time (`DateTime.UtcNow`).
- **Parameters**
  - `value` – The `DateTime` to evaluate.
- **Return value**
  - `true` if `value` is later than `DateTime.UtcNow`; otherwise, `false`.
- **Exceptions**
  - Throws `System.ArgumentOutOfRangeException` if `value.Kind` is `DateTimeKind.Local` or `DateTimeKind.Unspecified` and the system clock is not in UTC.

### `public static TimeSpan TimeUntil(DateTime future)`

Computes the duration remaining between the current system time (`DateTime.UtcNow`) and the specified future `DateTime`.
- **Parameters**
  - `future` – The `DateTime` representing a moment in the future.
- **Return value**
  - A `TimeSpan` representing the difference between `future` and `DateTime.UtcNow`.
- **Exceptions**
  - Throws `System.ArgumentOutOfRangeException` if `future` is not later than `DateTime.UtcNow`.
  - Throws `System.ArgumentOutOfRangeException` if `future.Kind` is `DateTimeKind.Local` or `DateTimeKind.Unspecified` and the system clock is not in UTC.

### `public static TimeSpan TimeSince(DateTime past)`

Computes the duration elapsed between the specified past `DateTime` and the current system time (`DateTime.UtcNow`).
- **Parameters**
  - `past` – The `DateTime` representing a moment in the past.
- **Return value**
  - A `TimeSpan` representing the difference between `DateTime.UtcNow` and `past`.
- **Exceptions**
  - Throws `System.ArgumentOutOfRangeException` if `past` is not earlier than `DateTime.UtcNow`.
  - Throws `System.ArgumentOutOfRangeException` if `past.Kind` is `DateTimeKind.Local` or `DateTimeKind.Unspecified` and the system clock is not in UTC.

### `public static bool IsSameDay(DateTime date1, DateTime date2)`

Determines whether two `DateTime` instances fall on the same calendar day in UTC.
- **Parameters**
  - `date1` – The first `DateTime`.
  - `date2` – The second `DateTime`.
- **Return value**
  - `true` if both dates represent the same UTC day; otherwise, `false`.
- **Exceptions**
  - Throws `System.ArgumentOutOfRangeException` if either `date1.Kind` or `date2.Kind` is not `DateTimeKind.Utc`.

### `public static DateTime RoundToNearestMinute(DateTime value)`

Rounds the given `DateTime` to the nearest whole minute, preserving the `DateTimeKind`.
- **Parameters**
  - `value` – The `DateTime` to round.
- **Return value**
  - A `DateTime` with seconds and milliseconds set to zero and the minute value adjusted to the nearest whole minute.
- **Exceptions**
  - Throws `System.ArgumentOutOfRangeException` if `value.Kind` is `DateTimeKind.Local` or `DateTimeKind.Unspecified`.

### `public static DateTime RoundToNearestHour(DateTime value)`

Rounds the given `DateTime` to the nearest whole hour, preserving the `DateTimeKind`.
- **Parameters**
  - `value` – The `DateTime` to round.
- **Return value**
  - A `DateTime` with minutes, seconds, and milliseconds set to zero and the hour value adjusted to the nearest whole hour.
- **Exceptions**
  - Throws `System.ArgumentOutOfRangeException` if `value.Kind` is `DateTimeKind.Local` or `DateTimeKind.Unspecified`.

### `public static DateTime StartOfDay(DateTime value)`

Truncates the given `DateTime` to the start of its calendar day in UTC.
- **Parameters**
  - `value` – The `DateTime` to truncate.
- **Return value**
  - A `DateTime` representing midnight (00:00:00) of the same UTC day.
- **Exceptions**
  - Throws `System.ArgumentOutOfRangeException` if `value.Kind` is not `DateTimeKind.Utc`.

### `public static DateTime EndOfDay(DateTime value)`

Truncates the given `DateTime` to the end of its calendar day in UTC.
- **Parameters**
  - `value` – The `DateTime` to truncate.
- **Return value**
  - A `DateTime` representing one tick before midnight (23:59:59.9999999) of the same UTC day.
- **Exceptions**
  - Throws `System.ArgumentOutOfRangeException` if `value.Kind` is not `DateTimeKind.Utc`.

### `public static DateTime StartOfWeek(DateTime value)`

Truncates the given `DateTime` to the start of its calendar week in UTC, where the week begins on Monday.
- **Parameters**
  - `value` – The `DateTime` to truncate.
- **Return value**
  - A `DateTime` representing midnight (00:00:00) of the Monday on or before the given date.
- **Exceptions**
  - Throws `System.ArgumentOutOfRangeException` if `value.Kind` is not `DateTimeKind.Utc`.

### `public static DateTime StartOfMonth(DateTime value)`

Truncates the given `DateTime` to the start of its calendar month in UTC.
- **Parameters**
  - `value` – The `DateTime` to truncate.
- **Return value**
  - A `DateTime` representing midnight (00:00:00) on the first day of the same UTC month.
- **Exceptions**
  - Throws `System.ArgumentOutOfRangeException` if `value.Kind` is not `DateTimeKind.Utc`.

### `public static DateTime EndOfMonth(DateTime value)`

Truncates the given `DateTime` to the end of its calendar month in UTC.
- **Parameters**
  - `value` – The `DateTime` to truncate.
- **Return value**
  - A `DateTime` representing one tick before midnight (23:59:59.9999999) on the last day of the same UTC month.
- **Exceptions**
  - Throws `System.ArgumentOutOfRangeException` if `value.Kind` is not `DateTimeKind.Utc`.

## Usage
