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
public class LoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LoggingMiddleware> _logger;

    public LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context, AuditLogger auditLogger)
    {
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

    private static bool IsHealthCheckEndpoint(string path)
    {
        return path.Contains("/health", StringComparison.OrdinalIgnoreCase) ||
               path.Contains("/live", StringComparison.OrdinalIgnoreCase) ||
               path.Contains("/ready", StringComparison.OrdinalIgnoreCase);
    }
}

public class RequestDetails
{
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string QueryString { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = new();
    public string? Body { get; set; }
    public DateTime Timestamp { get; set; }
}

public class ResponseDetails
{
    public int StatusCode { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
    public string? Body { get; set; }
}
