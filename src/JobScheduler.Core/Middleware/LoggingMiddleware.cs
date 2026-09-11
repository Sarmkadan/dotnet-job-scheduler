#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using JobScheduler.Core.Services;

namespace JobScheduler.Core.Middleware;

/// <summary>
/// Logging middleware that tracks all HTTP requests and responses.
/// Records request/response details including headers, body, and execution time.
/// Critical for debugging and audit trail purposes.
/// </summary>
public sealed class LoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LoggingMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger instance.</param>
    public LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Processes the HTTP request, logs the request and response, and invokes the next middleware.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="auditLogger">The audit logger service.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context, AuditLogger auditLogger)
    {
        _logger.LogInformation("InvokeAsync started for {Method} {Path}", context.Request.Method, context.Request.Path);
        var stopwatch = Stopwatch.StartNew();
        var request = await CaptureRequestAsync(context);

        // Capture original response stream to allow reading response body
        var originalBodyStream = context.Response.Body;
        using (var responseBody = new MemoryStream())
        {
            context.Response.Body = responseBody;

            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception while processing {Method} {Path}", request.Method, request.Path);
                throw;
            }
            finally
            {
                stopwatch.Stop();
                var response = await CaptureResponseAsync(context);

                // Log request and response details
                LogRequestResponse(request, response, stopwatch.ElapsedMilliseconds, context);

                // Audit log for API operations (non-health checks)
                if (!IsHealthCheckEndpoint(request.Path))
                {
                    await auditLogger.LogApiCallAsync(new ApiCallAudit
                    {
                        Method = request.Method,
                        Path = request.Path,
                        StatusCode = context.Response.StatusCode,
                        ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                        UserId = context.User?.Identity?.Name,
                        Timestamp = DateTime.UtcNow
                    });
                }

                // Copy response to original stream
                await responseBody.CopyToAsync(originalBodyStream);
            }
        }
    }

    /// <summary>
    /// Captures the request details from the HTTP context.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The request details.</returns>
    private async Task<RequestDetails> CaptureRequestAsync(HttpContext context)
    {
        var request = context.Request;
        var details = new RequestDetails
        {
            Method = request.Method,
            Path = request.Path,
            QueryString = request.QueryString.ToString(),
            Headers = ExtractSafeHeaders(request.Headers),
            Timestamp = DateTime.UtcNow
        };

        // Capture body for non-GET requests
        // WHY: GET requests shouldn't have bodies, and reading them can cause issues
        if (request.Method != "GET" && request.ContentLength > 0)
        {
            request.EnableBuffering();
            var reader = new StreamReader(request.Body, leaveOpen: true);
            details.Body = await reader.ReadToEndAsync();
            request.Body.Position = 0;
        }

        return details;
    }

    /// <summary>
    /// Captures the response details from the HTTP context.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The response details.</returns>
    private async Task<ResponseDetails> CaptureResponseAsync(HttpContext context)
    {
        var response = context.Response;
        var body = string.Empty;

        if (response.Body.CanSeek)
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            using (var reader = new StreamReader(response.Body, leaveOpen: true))
            {
                body = await reader.ReadToEndAsync();
            }
            response.Body.Seek(0, SeekOrigin.Begin);
        }

        return new ResponseDetails
        {
            StatusCode = response.StatusCode,
            Headers = ExtractSafeHeaders(response.Headers),
            Body = body.Length > 1000 ? body.Substring(0, 1000) + "..." : body
        };
    }

    /// <summary>
    /// Logs the request and response details.
    /// </summary>
    /// <param name="request">The request details.</param>
    /// <param name="response">The response details.</param>
    /// <param name="elapsedMs">The elapsed time in milliseconds.</param>
    /// <param name="context">The HTTP context.</param>
    private void LogRequestResponse(RequestDetails request, ResponseDetails response, long elapsedMs, HttpContext context)
    {
        var logLevel = response.StatusCode >= 500 ? LogLevel.Error :
                      response.StatusCode >= 400 ? LogLevel.Warning :
                      LogLevel.Information;

        _logger.Log(logLevel,
            "HTTP {Method} {Path} - {StatusCode} ({ExecutionTimeMs}ms) - User: {UserId}",
            request.Method,
            request.Path,
            response.StatusCode,
            elapsedMs,
            context.User?.Identity?.Name ?? "Anonymous");
    }

    /// <summary>
    /// Extracts headers from the header dictionary, excluding sensitive ones.
    /// </summary>
    /// <param name="headers">The header dictionary.</param>
    /// <returns>A dictionary of safe headers.</returns>
    private static Dictionary<string, string> ExtractSafeHeaders(IHeaderDictionary headers)
    {
        var safeHeaders = new Dictionary<string, string>();
        var sensitiveHeaders = new[] { "Authorization", "X-API-Key", "Cookie", "Password" };

        foreach (var header in headers)
        {
            if (!sensitiveHeaders.Contains(header.Key, StringComparer.OrdinalIgnoreCase))
            {
                safeHeaders[header.Key] = header.Value.ToString();
            }
        }

        return safeHeaders;
    }

    /// <summary>
    /// Determines whether the specified path is a health check endpoint.
    /// </summary>
    /// <param name="path">The request path.</param>
    /// <returns>True if the path is a health check endpoint; otherwise, false.</returns>
    private static bool IsHealthCheckEndpoint(string path)
    {
        return path.Contains("/health", StringComparison.OrdinalIgnoreCase) ||
               path.Contains("/live", StringComparison.OrdinalIgnoreCase) ||
               path.Contains("/ready", StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Contains details about an HTTP request.
/// </summary>
public sealed class RequestDetails
{
    /// <summary>
    /// Gets or sets the HTTP method.
    /// </summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the request path.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the query string.
    /// </summary>
    public string QueryString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the request headers.
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>
    /// Gets or sets the request body.
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the request.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Returns a concise summary of the request details.
    /// </summary>
    /// <returns>A string summarizing the request method, path, query string, and timestamp.</returns>
    public override string ToString()
    {
        return $"Request: {Method} {Path}{QueryString} at {Timestamp:O}";
    }
}

/// <summary>
/// Contains details about an HTTP response.
/// </summary>
public sealed class ResponseDetails
{
    /// <summary>
    /// Gets or sets the HTTP status code.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the response headers.
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>
    /// Gets or sets the response body (truncated if too long).
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// Returns a concise summary of the response details.
    /// </summary>
    /// <returns>A string summarizing the response status code and body length.</returns>
    public override string ToString()
    {
        return $"Response: {StatusCode} (body length: {Body?.Length ?? 0})";
    }
}