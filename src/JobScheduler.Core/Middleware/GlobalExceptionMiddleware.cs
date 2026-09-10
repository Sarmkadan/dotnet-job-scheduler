#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using JobScheduler.Core.Exceptions;

namespace JobScheduler.Core.Middleware;

/// <summary>
/// Global exception handler middleware that catches all unhandled exceptions.
/// Ensures consistent error responses and prevents sensitive error information leakage.
/// Logs all errors for audit and debugging purposes.
/// </summary>
public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalExceptionMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger instance.</param>
    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Invokes the middleware to process the HTTP request and handle any unhandled exceptions.
    /// </summary>
    /// <param name="context">The HTTP context for the current request.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        _logger.LogInformation("Processing request {Method} {Path}", context.Request.Method, context.Request.Path);
        try
        {
            await _next(context);
            _logger.LogInformation("Finished processing request {Method} {Path} with status {StatusCode}", context.Request.Method, context.Request.Path, context.Response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred: {ExceptionType}", ex.GetType().Name);
            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// Handles the exception by setting the appropriate HTTP status code and writing a JSON error response.
    /// Maps specific exception types to HTTP status codes as follows:
    /// - <see cref="JobValidationException"/> and <see cref="CronExpressionException"/> map to 400 Bad Request.
    /// - <see cref="JobNotFoundException"/> maps to 404 Not Found.
    /// - <see cref="ConcurrencyException"/> maps to 409 Conflict.
    /// - <see cref="ExecutionException"/> and other unhandled exceptions map to 500 Internal Server Error.
    /// </summary>
    /// <param name="context">The HTTP context for the current request.</param>
    /// <param name="exception">The exception that occurred.</param>
    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse
        {
            Message = exception.Message,
            Timestamp = DateTime.UtcNow
        };

        // Map specific exception types to HTTP status codes
        // WHY: Specific exception types need appropriate HTTP status codes for API clients
        context.Response.StatusCode = exception switch
        {
            JobValidationException => StatusCodes.Status400BadRequest,
            CronExpressionException => StatusCodes.Status400BadRequest,
            JobNotFoundException => StatusCodes.Status404NotFound,
            ConcurrencyException => StatusCodes.Status409Conflict,
            ExecutionException => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError
        };

        // Add exception details in development
        if (IsProductionEnvironment(context) == false)
        {
            response.StackTrace = exception.StackTrace;
            response.ExceptionType = exception.GetType().Name;
        }

        return context.Response.WriteAsJsonAsync(response);
    }

    /// <summary>
    /// Determines whether the application is running in a production environment.
    /// </summary>
    /// <param name="context">The HTTP context for the current request.</param>
    /// <returns>True if the environment is production; otherwise, false.</returns>
    private static bool IsProductionEnvironment(HttpContext context)
    {
        var environment = context.RequestServices.GetService(typeof(IHostEnvironment)) as IHostEnvironment;
        return environment?.IsProduction() ?? false;
    }
}

/// <summary>
/// Represents a standardized error response returned by the middleware.
/// </summary>
public sealed class ErrorResponse
{
    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the error occurred.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the fully qualified name of the exception type.
    /// </summary>
    public string? ExceptionType { get; set; }

    /// <summary>
    /// Gets or sets the stack trace of the exception.
    /// </summary>
    public string? StackTrace { get; set; }

    public override string ToString() =>
        $"ErrorResponse {{ Message = {Message}, Timestamp = {Timestamp}, ExceptionType = {ExceptionType}, StackTrace = {StackTrace} }}";
}
