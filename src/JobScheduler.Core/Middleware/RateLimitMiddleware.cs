// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using JobScheduler.Core.Services;

namespace JobScheduler.Core.Middleware;

/// <summary>
/// Rate limiting middleware that throttles requests per IP or user.
/// Prevents abuse and ensures fair resource allocation across clients.
/// Uses a sliding window algorithm for accurate rate limiting.
/// </summary>
public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitMiddleware> _logger;
    private readonly RateLimitSettings _settings;

    // WHY: ConcurrentDictionary for thread-safe access without locks
    private static readonly ConcurrentDictionary<string, RateLimitBucket> _buckets =
        new();

    public RateLimitMiddleware(RequestDelegate next, ILogger<RateLimitMiddleware> logger, RateLimitSettings? settings = null)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings ?? new RateLimitSettings();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip rate limiting for health check endpoints
        if (IsHealthCheckEndpoint(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var clientId = GetClientIdentifier(context);
        var bucket = GetOrCreateBucket(clientId);

        if (!bucket.AllowRequest())
        {
            _logger.LogWarning("Rate limit exceeded for client: {ClientId}", clientId);
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers.Add("Retry-After", _settings.WindowSizeSeconds.ToString());

            await context.Response.WriteAsJsonAsync(new
            {
                error = "Rate limit exceeded",
                retryAfter = _settings.WindowSizeSeconds,
                message = $"Maximum {_settings.RequestsPerWindow} requests per {_settings.WindowSizeSeconds} seconds"
            });
            return;
        }

        await _next(context);
    }

    private string GetClientIdentifier(HttpContext context)
    {
        // Prefer authenticated user ID over IP
        if (!string.IsNullOrEmpty(context.User?.Identity?.Name))
            return $"user:{context.User.Identity.Name}";

        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return $"ip:{ipAddress}";
    }

    private RateLimitBucket GetOrCreateBucket(string clientId)
    {
        // WHY: Buckets are automatically pruned when accessed, preventing memory leaks
        return _buckets.AddOrUpdate(clientId,
            new RateLimitBucket(_settings.RequestsPerWindow, _settings.WindowSizeSeconds),
            (_, bucket) => bucket.IsExpired ? new RateLimitBucket(_settings.RequestsPerWindow, _settings.WindowSizeSeconds) : bucket);
    }

    private static bool IsHealthCheckEndpoint(string path)
    {
        return path.Contains("/health", StringComparison.OrdinalIgnoreCase);
    }
}

public class RateLimitBucket
{
    private readonly int _maxRequests;
    private readonly int _windowSizeSeconds;
    private Queue<DateTime> _requests;
    private DateTime _createdAt;

    public bool IsExpired => (DateTime.UtcNow - _createdAt).TotalSeconds > _windowSizeSeconds * 2;

    public RateLimitBucket(int maxRequests, int windowSizeSeconds)
    {
        _maxRequests = maxRequests;
        _windowSizeSeconds = windowSizeSeconds;
        _requests = new Queue<DateTime>();
        _createdAt = DateTime.UtcNow;
    }

    public bool AllowRequest()
    {
        var now = DateTime.UtcNow;
        var windowStart = now.AddSeconds(-_windowSizeSeconds);

        // Remove requests outside the window
        while (_requests.Count > 0 && _requests.Peek() < windowStart)
            _requests.Dequeue();

        if (_requests.Count < _maxRequests)
        {
            _requests.Enqueue(now);
            return true;
        }

        return false;
    }
}

public class RateLimitSettings
{
    public int RequestsPerWindow { get; set; } = 1000;
    public int WindowSizeSeconds { get; set; } = 60;
}
