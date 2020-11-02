# TimeUtility

`TimeUtility` is a static utility class providing common date and time operations for the `dotnet-job-scheduler` project. It centralises conversions between Unix timestamps and `DateTime`, ISO 8601 formatting and parsing, rounding operations, business-day calculations, and duration formatting, ensuring consistent time handling across scheduling components.

## API

### `GetUtcNow`
```csharp
public static DateTime GetUtcNow { get; }
```
**Purpose:** Returns the current UTC date and time.  
**Return value:** A `DateTime` with `DateTimeKind.Utc` representing the instant of the call.  
**Throws:** Never throws.

---

### `FromUnixTimestamp`
```csharp
public static DateTime FromUnixTimestamp(long timestamp)
```
**Purpose:** Converts a Unix timestamp (seconds since 1970-01-01T00:00:00Z) to a UTC `DateTime`.  
**Parameters:**  
- `timestamp` — A `long` representing seconds elapsed since the Unix epoch.  
**Return value:** A UTC `DateTime` corresponding to the given timestamp.  
**Throws:** `ArgumentOutOfRangeException` if the timestamp is outside the valid `DateTime` range.

---

### `ToUnixTimestamp`
```csharp
public static long ToUnixTimestamp(DateTime dateTime)
```
**Purpose:** Converts a `DateTime` to a Unix timestamp in seconds.  
**Parameters:**  
- `dateTime` — The `DateTime` to convert. If `DateTimeKind` is `Local` or `Unspecified`, it is treated as UTC.  
**Return value:** The number of whole seconds since the Unix epoch.  
**Throws:** `ArgumentOutOfRangeException` if the resulting value exceeds the `long` range representable by a Unix timestamp.

---

### `ToIso8601`
```csharp
public static string ToIso8601(DateTime dateTime)
```
**Purpose:** Formats a `DateTime` as an ISO 8601 string (`yyyy-MM-ddTHH:mm:ss.fffZ`).  
**Parameters:**  
- `dateTime` — The `DateTime` to format. Converted to UTC if necessary.  
**Return value:** The ISO 8601 representation.  
**Throws:** Never throws.

---

### `ParseIso8601`
```csharp
public static DateTime? ParseIso8601(string isoString)
```
**Purpose:** Parses an ISO 8601 formatted string into a UTC `DateTime`.  
**Parameters:**  
- `isoString` — A string expected to conform to ISO 8601.  
**Return value:** A nullable `DateTime` in UTC on success; `null` if the string is null, empty, or cannot be parsed.  
**Throws:** Never throws.

---

### `RoundDown`
```csharp
public static DateTime RoundDown(DateTime dateTime, TimeSpan interval)
```
**Purpose:** Rounds a `DateTime` down to the nearest interval boundary.  
**Parameters:**  
- `dateTime` — The `DateTime` to round.  
- `interval` — The `TimeSpan` representing the rounding granularity.  
**Return value:** A `DateTime` truncated to the last completed interval.  
**Throws:** `ArgumentException` if `interval` is zero or negative.

---

### `RoundUp`
```csharp
public static DateTime RoundUp(DateTime dateTime, TimeSpan interval)
```
**Purpose:** Rounds a `DateTime` up to the next interval boundary.  
**Parameters:**  
- `dateTime` — The `DateTime` to round.  
- `interval` — The `TimeSpan` representing the rounding granularity.  
**Return value:** A `DateTime` at the start of the next interval. If `dateTime` already falls exactly on a boundary, it is returned unchanged.  
**Throws:** `ArgumentException` if `interval` is zero or negative.

---

### `GetAge`
```csharp
public static int GetAge(DateTime birthDate, DateTime referenceDate)
```
**Purpose:** Calculates the age in full years between a birth date and a reference date.  
**Parameters:**  
- `birthDate` — The date of birth.  
- `referenceDate` — The date at which to compute the age.  
**Return value:** The number of completed years.  
**Throws:** `ArgumentException` if `birthDate` is later than `referenceDate`.

---

### `IsBetweenTimes`
```csharp
public static bool IsBetweenTimes(DateTime target, DateTime start, DateTime end)
```
**Purpose:** Determines whether a target `DateTime` falls between two other `DateTime` values, inclusive of `start` and exclusive of `end`.  
**Parameters:**  
- `target` — The `DateTime` to test.  
- `start` — The inclusive start of the range.  
- `end` — The exclusive end of the range.  
**Return value:** `true` if `target >= start` and `target < end`; otherwise `false`.  
**Throws:** `ArgumentException` if `start` is later than `end`.

---

### `IsBetweenDates`
```csharp
public static bool IsBetweenDates(DateTime target, DateTime start, DateTime end)
```
**Purpose:** Determines whether a target date (ignoring time) falls between two other dates, inclusive of both boundaries.  
**Parameters:**  
- `target` — The date to test.  
- `start` — The inclusive start date.  
- `end` — The inclusive end date.  
**Return value:** `true` if the date portion of `target` is between the date portions of `start` and `end` inclusive; otherwise `false`.  
**Throws:** `ArgumentException` if `start` is later than `end`.

---

### `GetBusinessDaysBetween`
```csharp
public static int GetBusinessDaysBetween(DateTime start, DateTime end)
```
**Purpose:** Counts the number of business days (Monday through Friday) between two dates, inclusive of `start` and exclusive of `end`.  
**Parameters:**  
- `start` — The inclusive start date.  
- `end` — The exclusive end date.  
**Return value:** The count of weekdays in the range.  
**Throws:** `ArgumentException` if `start` is later than `end`.

---

### `GetNextBusinessDay`
```csharp
public static DateTime GetNextBusinessDay(DateTime date)
```
**Purpose:** Returns the next business day following the given date. If the date is a weekday, the next weekday is returned; if Friday, the following Monday.  
**Parameters:**  
- `date` — The reference date.  
**Return value:** A `DateTime` representing the next weekday.  
**Throws:** Never throws.

---

### `GetPreviousBusinessDay`
```csharp
public static DateTime GetPreviousBusinessDay(DateTime date)
```
**Purpose:** Returns the previous business day before the given date. If the date is a weekday, the preceding weekday is returned; if Monday, the previous Friday.  
**Parameters:**  
- `date` — The reference date.  
**Return value:** A `DateTime` representing the previous weekday.  
**Throws:** Never throws.

---

### `GetStartOfWeek`
```csharp
public static DateTime GetStartOfWeek(DateTime date, DayOfWeek startOfWeek = DayOfWeek.Monday)
```
**Purpose:** Returns the start of the week containing the given date, according to the specified first day of the week.  
**Parameters:**  
- `date` — Any date within the target week.  
- `startOfWeek` — The day considered the first day of the week (default `Monday`).  
**Return value:** A `DateTime` at midnight on the first day of the week.  
**Throws:** Never throws.

---

### `GetEndOfWeek`
```csharp
public static DateTime GetEndOfWeek(DateTime date, DayOfWeek startOfWeek = DayOfWeek.Monday)
```
**Purpose:** Returns the end of the week containing the given date, according to the specified first day of the week. The end is the last moment of the last day of the week.  
**Parameters:**  
- `date` — Any date within the target week.  
- `startOfWeek` — The day considered the first day of the week (default `Monday`).  
**Return value:** A `DateTime` representing the last tick of the last day of the week.  
**Throws:** Never throws.

---

### `GetStartOfMonth`
```csharp
public static DateTime GetStartOfMonth(DateTime date)
```
**Purpose:** Returns the first moment of the month containing the given date.  
**Parameters:**  
- `date` — Any date within the target month.  
**Return value:** A `DateTime` at midnight on the first day of the month.  
**Throws:** Never throws.

---

### `GetEndOfMonth`
```csharp
public static DateTime GetEndOfMonth(DateTime date)
```
**Purpose:** Returns the last moment of the month containing the given date.  
**Parameters:**  
- `date` — Any date within the target month.  
**Return value:** A `DateTime` representing the last tick of the last day of the month.  
**Throws:** Never throws.

---

### `FormatDuration`
```csharp
public static string FormatDuration(TimeSpan duration)
```
**Purpose:** Formats a `TimeSpan` into a human-readable duration string (e.g., "2h 30m 15s").  
**Parameters:**  
- `duration` — The `TimeSpan` to format.  
**Return value:** A string representation of the duration. Negative durations are formatted with a leading minus sign.  
**Throws:** Never throws.

---

### `IsLeapYear`
```csharp
public static bool IsLeapYear(int year)
```
**Purpose:** Determines whether the given year is a leap year according to Gregorian calendar rules.  
**Parameters:**  
- `year` — The year to test.  
**Return value:** `true` if the year is a leap year; otherwise `false`.  
**Throws:** `ArgumentOutOfRangeException` if `year` is less than 1 or greater than 9999.

## Usage

### Example 1: Scheduling a job on the next business day
```csharp
DateTime now = TimeUtility.GetUtcNow;
DateTime nextRun = TimeUtility.GetNextBusinessDay(now);
long nextRunTimestamp = TimeUtility.ToUnixTimestamp(nextRun);

Console.WriteLine($"Next run: {TimeUtility.ToIso8601(nextRun)} (Unix: {nextRunTimestamp})");
```

### Example 2: Validating a time window and computing age
```csharp
DateTime birthDate = new DateTime(1990, 5, 15, 0, 0, 0, DateTimeKind.Utc);
DateTime reference = TimeUtility.GetUtcNow;

int age = TimeUtility.GetAge(birthDate, reference);

DateTime windowStart = TimeUtility.GetStartOfMonth(reference);
DateTime windowEnd = TimeUtility.GetEndOfMonth(reference);

bool inWindow = TimeUtility.IsBetweenDates(reference, windowStart, windowEnd);

Console.WriteLine($"Age: {age}, In current month window: {inWindow}");
```

## Notes

- **Thread safety:** All members are static and operate on immutable input parameters or return new values. The type is inherently thread-safe and safe for concurrent use without external synchronisation.
- **Edge cases in rounding:** `RoundDown` and `RoundUp` require a positive `TimeSpan`; a zero or negative interval throws `ArgumentException`. When the input `DateTime` falls exactly on an interval boundary, `RoundUp` returns the same value.
- **Age calculation:** `GetAge` uses completed years; if the reference date has not yet reached the birth date's month and day in the reference year, one year is subtracted. A `birthDate` later than `referenceDate` throws.
- **Business-day methods:** `GetBusinessDaysBetween` excludes the end date. `GetNextBusinessDay` and `GetPreviousBusinessDay` skip weekends only; they do not account for public holidays.
- **ISO 8601 parsing:** `ParseIso8601` returns `null` for any unparseable input rather than throwing, allowing safe inline use in scheduling pipelines.
- **Unix timestamp range:** Conversions validate that the resulting `DateTime` or timestamp fits within the representable range of the target type; out-of-range values throw `ArgumentOutOfRangeException`.
- **`DateTimeKind` handling:** Methods that produce UTC values (`FromUnixTimestamp`, `ToIso8601`, `ParseIso8601`) explicitly set `DateTimeKind.Utc`. Inputs with `Local` or `Unspecified` kind are converted to UTC before processing where necessary.
