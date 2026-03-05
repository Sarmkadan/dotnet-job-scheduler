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

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred: {ExceptionType}", ex.GetType().Name);
            await HandleExceptionAsync(context, ex);
        }
    }

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

    private static bool IsProductionEnvironment(HttpContext context)
    {
        var environment = context.RequestServices.GetService(typeof(IHostEnvironment)) as IHostEnvironment;
        return environment?.IsProduction() ?? false;
    }
}

public sealed class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? ExceptionType { get; set; }
    public string? StackTrace { get; set; }
}
