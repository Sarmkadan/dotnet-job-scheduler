#nullable enable
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
public sealed class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitMiddleware> _logger;
    private readonly RateLimitSettings _settings;

    // WHY: ConcurrentDictionary for thread-safe access without locks
    private static readonly ConcurrentDictionary<string, RateLimitBucket> _buckets =
        new();

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger instance for logging rate limit events.</param>
    /// <param name="settings">Optional rate limit settings. If not provided, default settings are used.</param>
    public RateLimitMiddleware(RequestDelegate next, ILogger<RateLimitMiddleware> logger, RateLimitSettings? settings = null)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings ?? new RateLimitSettings();
    }

    /// <summary>
    /// Processes the HTTP request to enforce rate limiting based on client identifier.
    /// </summary>
    /// <param name="context">The HTTP context containing the request and response.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        _logger.LogInformation("RateLimitMiddleware invoked for path {Path}", context.Request.Path);

        // Skip rate limiting for health check endpoints
        if (IsHealthCheckEndpoint(context.Request.Path))
        {
            _logger.LogInformation("Skipping rate limiting for health check endpoint {Path}", context.Request.Path);
            await _next(context);
            return;
        }

        var clientId = GetClientIdentifier(context);
        var bucket = GetOrCreateBucket(clientId);

        if (!bucket.AllowRequest())
        {
            _logger.LogWarning("Rate limit exceeded for client {ClientId}", clientId);
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

        _logger.LogInformation("Rate limit check passed for client {ClientId}", clientId);
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

/// <summary>
/// Represents a rate limiting bucket that tracks requests within a time window.
/// Uses a sliding window algorithm to count requests and determine if limit is exceeded.
/// </summary>
public sealed class RateLimitBucket
{
    private readonly int _maxRequests;
    private readonly int _windowSizeSeconds;
    private Queue<DateTime> _requests;
    private DateTime _createdAt;

    /// <summary>
    /// Gets a value indicating whether this bucket has expired and should be recreated.
    /// A bucket expires after twice its window size to prevent memory leaks.
    /// </summary>
    public bool IsExpired => (DateTime.UtcNow - _createdAt).TotalSeconds > _windowSizeSeconds * 2;

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitBucket"/> class.
    /// </summary>
    /// <param name="maxRequests">The maximum number of requests allowed within the window.</param>
    /// <param name="windowSizeSeconds">The size of the sliding window in seconds.</param>
    public RateLimitBucket(int maxRequests, int windowSizeSeconds)
    {
        _maxRequests = maxRequests;
        _windowSizeSeconds = windowSizeSeconds;
        _requests = new Queue<DateTime>();
        _createdAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Determines whether a request is allowed based on the rate limit settings.
    /// Implements a sliding window algorithm: removes old requests outside the window,
    /// then checks if current count is below the maximum allowed.
    /// </summary>
    /// <returns>True if the request is allowed, false if the rate limit has been exceeded.</returns>
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

/// <summary>
/// Configuration settings for the rate limiting middleware.
/// </summary>
public sealed class RateLimitSettings
{
    /// <summary>
    /// Gets or sets the maximum number of requests allowed within the window size period.
    /// Default value is 1000 requests.
    /// </summary>
    public int RequestsPerWindow { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the size of the sliding window in seconds.
    /// Default value is 60 seconds (1 minute).
    /// </summary>
    public int WindowSizeSeconds { get; set; } = 60;
}
