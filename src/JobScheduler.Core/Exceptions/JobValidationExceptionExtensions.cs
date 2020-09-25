namespace JobScheduler.Core.Exceptions;

/// <summary>
/// Provides extension methods for <see cref="JobValidationException"/>.
/// </summary>
public static class JobValidationExceptionExtensions
{
    /// <summary>
    /// Gets a formatted error message that includes the property name.
    /// </summary>
    /// <param name="exception">The <see cref="JobValidationException"/> instance.</param>
    /// <returns>A formatted error message.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="exception"/> is null.</exception>
    public static string GetFormattedMessage(this JobValidationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return string.IsNullOrEmpty(exception.PropertyName)
            ? exception.Message
            : $"Validation error on {exception.PropertyName}: {exception.Message}";
    }

    /// <summary>
    /// Creates a new <see cref="JobValidationException"/> with the same message and property name.
    /// </summary>
    /// <param name="exception">The <see cref="JobValidationException"/> instance.</param>
    /// <returns>A new <see cref="JobValidationException"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="exception"/> is null.</exception>
    public static JobValidationException Clone(this JobValidationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return new JobValidationException(exception.Message)
        {
            PropertyName = exception.PropertyName,
        };
    }
}
