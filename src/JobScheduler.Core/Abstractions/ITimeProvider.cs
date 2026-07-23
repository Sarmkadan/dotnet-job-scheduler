#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;

namespace JobScheduler.Core.Abstractions;

/// <summary>
/// Provides time-related functionality with support for both real-time and test scenarios.
/// This abstraction allows for deterministic testing and proper timezone handling.
/// </summary>
public interface ITimeProvider
{
    /// <summary>
    /// Gets the current UTC date and time.
    /// </summary>
    DateTimeOffset UtcNow { get; }

    /// <summary>
    /// Gets the current local date and time.
    /// </summary>
    DateTimeOffset Now { get; }

    /// <summary>
    /// Gets the current UTC date and time as DateTime (for compatibility).
    /// </summary>
    DateTime UtcNowAsDateTime { get; }

    /// <summary>
    /// Gets the current local date and time as DateTime.
    /// </summary>
    DateTime NowAsDateTime { get; }

    /// <summary>
    /// Creates a delay for the specified time span.
    /// </summary>
    ValueTask Delay(TimeSpan delay, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current time zone information for the specified time zone ID.
    /// </summary>
    TimeZoneInfo GetTimeZone(string id);
}

/// <summary>
/// Default implementation of <see cref="ITimeProvider"/> that uses the system clock.
/// </summary>
public sealed class SystemTimeProvider : ITimeProvider
{
    /// <summary>
    /// Gets the singleton instance of the system time provider.
    /// </summary>
    public static SystemTimeProvider Instance { get; } = new();

    private SystemTimeProvider() { }

    /// <inheritdoc />
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;


    /// <inheritdoc />
    public DateTimeOffset Now => DateTimeOffset.Now;

    /// <inheritdoc />
    public DateTime UtcNowAsDateTime => DateTime.UtcNow;


    /// <inheritdoc />
    public DateTime NowAsDateTime => DateTime.Now;

    /// <inheritdoc />
    public ValueTask Delay(TimeSpan delay, CancellationToken cancellationToken = default)
    {
        return cancellationToken.IsCancellationRequested
            ? ValueTask.FromCanceled(cancellationToken)
            : new ValueTask(Task.Delay(delay, cancellationToken));
    }

    /// <inheritdoc />
    public TimeZoneInfo GetTimeZone(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return TimeZoneInfo.FindSystemTimeZoneById(id);
    }
}

/// <summary>
/// Test implementation of <see cref="ITimeProvider"/> that allows controlling time.
/// </summary>
public sealed class TestTimeProvider : ITimeProvider
{
    private DateTimeOffset _utcNow;
    private DateTimeOffset _now;
    private readonly TimeZoneInfo _testTimeZone;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestTimeProvider"/> class.
    /// </summary>
    /// <param name="initialUtcNow">The initial UTC time to use.</param>
    /// <param name="timeZoneId">The time zone ID to use (defaults to UTC).</param>
    public TestTimeProvider(DateTimeOffset initialUtcNow, string? timeZoneId = null)
    {
        _utcNow = initialUtcNow;
        _now = initialUtcNow;
        _testTimeZone = timeZoneId is null or "UTC"
            ? TimeZoneInfo.Utc
            : TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
    }

    /// <summary>
    /// Advances the current time by the specified time span.
    /// </summary>
    /// <param name="timeSpan">The time span to advance.</param>
    public void Advance(TimeSpan timeSpan)
    {
        _utcNow = _utcNow.Add(timeSpan);
        _now = _now.Add(timeSpan);
    }

    /// <summary>
    /// Sets the current UTC time.
    /// </summary>
    /// <param name="utcNow">The UTC time to set.</param>
    public void SetUtcNow(DateTimeOffset utcNow)
    {
        _utcNow = utcNow;
        _now = TimeZoneInfo.ConvertTime(utcNow, _testTimeZone);
    }

    /// <inheritdoc />
    public DateTimeOffset UtcNow => _utcNow;


    /// <inheritdoc />
    public DateTimeOffset Now => _now;

    /// <inheritdoc />
    public DateTime UtcNowAsDateTime => _utcNow.UtcDateTime;


    /// <inheritdoc />
    public DateTime NowAsDateTime => _now.DateTime;

    /// <inheritdoc />
    public ValueTask Delay(TimeSpan delay, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromCanceled(cancellationToken);
        }

        // For testing, we don't actually delay - just return completed task
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public TimeZoneInfo GetTimeZone(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return _testTimeZone.Id.Equals(id, StringComparison.OrdinalIgnoreCase)
            ? _testTimeZone
            : TimeZoneInfo.FindSystemTimeZoneById(id);
    }
}