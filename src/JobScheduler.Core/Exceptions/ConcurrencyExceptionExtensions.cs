namespace JobScheduler.Core.Exceptions;

public static class ConcurrencyExceptionExtensions
{
    /// <summary>
    /// Determines whether the concurrency exception indicates that the job is currently running at maximum allowed concurrency.
    /// </summary>
    /// <param name="exception">The concurrency exception to check.</param>
    /// <returns><see langword="true"/> if the job is currently running at maximum allowed concurrency; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is <see langword="null"/>.</exception>
    public static bool IsAtMaxConcurrency(this ConcurrencyException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return exception.CurrentConcurrentExecutions >= exception.MaxAllowed;
    }

    /// <summary>
    /// Calculates the number of additional executions that can be started without exceeding the maximum allowed concurrency.
    /// </summary>
    /// <param name="exception">The concurrency exception to check.</param>
    /// <returns>The number of additional executions that can be started. Returns 0 if current executions exceed maximum.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is <see langword="null"/>.</exception>
    public static int GetAvailableConcurrencySlots(this ConcurrencyException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return Math.Max(exception.MaxAllowed - exception.CurrentConcurrentExecutions, 0);
    }
}
