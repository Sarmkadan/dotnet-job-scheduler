#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace JobScheduler.Core.Extensions;

/// <summary>
/// Extension methods for HttpContext for common request/response operations.
/// WHY: These extensions reduce boilerplate in controllers and middleware.
/// </summary>
public static class HttpContextExtensions
{
    /// <summary>
    /// Gets the authenticated user ID from claims.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The user ID if available; otherwise <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    public static string? GetUserId(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
               context.User?.FindFirst("sub")?.Value ??
               context.User?.Identity?.Name;
    }

    /// <summary>
    /// Gets a specific claim value.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="claimType">The claim type to retrieve.</param>
    /// <returns>The claim value if found; otherwise <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="claimType"/> is <see langword="null"/>.</exception>
    public static string? GetClaimValue(this HttpContext context, string claimType)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(claimType);

        return context.User?.FindFirst(claimType)?.Value;
    }

    /// <summary>
    /// Checks if user has a specific claim value.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="claimType">The claim type to check.</param>
    /// <param name="claimValue">Optional claim value to match. If <see langword="null"/>, checks for existence only.</param>
    /// <returns><see langword="true"/> if the claim exists (and optionally matches the value); otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="claimType"/> is <see langword="null"/>.</exception>
    public static bool HasClaim(this HttpContext context, string claimType, string? claimValue = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(claimType);

        return claimValue is null
            ? context.User?.HasClaim(c => c.Type == claimType) == true
            : context.User?.HasClaim(claimType, claimValue) == true;
    }

    /// <summary>
    /// Gets the client IP address (handles proxy headers).
    /// WHY: Some deployments use reverse proxies that require X-Forwarded-For header.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The client IP address.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    public static string GetClientIpAddress(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Check for X-Forwarded-For header first (common with proxies)
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            var addresses = forwardedFor.ToString().Split(',');
            if (addresses.Length > 0)
                return addresses[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    /// <summary>
    /// Sets a response header for caching control.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="maxAgeSeconds">The max-age value in seconds.</param>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    public static void SetCacheControl(this HttpContext context, int maxAgeSeconds)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Response.Headers.CacheControl = $"public, max-age={maxAgeSeconds}";
    }

    /// <summary>
    /// Sets a response header to prevent caching.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    public static void SetNoCache(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
        context.Response.Headers.Pragma = "no-cache";
        context.Response.Headers.Expires = "0";
    }

    /// <summary>
    /// Sets security headers to prevent common web attacks.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    public static void SetSecurityHeaders(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Response.Headers.XContentTypeOptions = "nosniff";
        context.Response.Headers.XFrameOptions = "DENY";
        context.Response.Headers.XXSSProtection = "1; mode=block";
        context.Response.Headers.StrictTransportSecurity = "max-age=31536000; includeSubDomains";
    }

    /// <summary>
    /// Gets the request correlation ID (for tracing).
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The correlation ID.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    public static string GetCorrelationId(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        const string correlationIdHeader = "X-Correlation-ID";

        if (context.Request.Headers.TryGetValue(correlationIdHeader, out var correlationId))
            return correlationId.ToString();

        var id = Guid.NewGuid().ToString();
        context.Request.Headers[correlationIdHeader] = id;
        return id;
    }

    /// <summary>
    /// Gets a query parameter with type conversion.
    /// </summary>
    /// <typeparam name="T">The target type (int, bool, Guid, etc.).</typeparam>
    /// <param name="context">The HTTP context.</param>
    /// <param name="paramName">The query parameter name.</param>
    /// <returns>The parsed value if successful; otherwise <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="paramName"/> is <see langword="null"/>.</exception>
    public static T? GetQueryParameter<T>(this HttpContext context, string paramName) where T : struct
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(paramName);

        if (!context.Request.Query.TryGetValue(paramName, out var value))
            return null;

        return typeof(T) switch
        {
            Type t when t == typeof(int) && int.TryParse(value, out var intValue) => (T)(object)intValue,
            Type t when t == typeof(bool) && bool.TryParse(value, out var boolValue) => (T)(object)boolValue,
            Type t when t == typeof(Guid) && Guid.TryParse(value, out var guidValue) => (T)(object)guidValue,
            _ => null
        };
    }

    /// <summary>
    /// Checks if the request accepts JSON response.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns><see langword="true"/> if the request accepts or expects JSON; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    public static bool AcceptsJson(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Request.Headers.Accept.Contains("application/json") ||
               context.Request.ContentType?.Contains("application/json") == true;
    }

    /// <summary>
    /// Checks if the request is HTTPS.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns><see langword="true"/> if the request is HTTPS or behind a proxy that indicates HTTPS; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    public static bool IsHttps(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Request.IsHttps ||
               context.Request.Headers.TryGetValue("X-Forwarded-Proto", out var proto) &&
               proto.ToString().Equals("https", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the request scheme (http or https).
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The scheme (<c>"http"</c> or <c>"https"</c>).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    public static string GetRequestScheme(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.IsHttps() ? "https" : "http";
    }

    /// <summary>
    /// Gets the full request URL.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The complete URL including scheme, host, port, path, and query string.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    public static string GetFullRequestUrl(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var scheme = context.GetRequestScheme();
        var host = context.Request.Host.Host;
        var port = context.Request.Host.Port;
        var path = context.Request.Path;
        var query = context.Request.QueryString;

        var url = $"{scheme}://{host}";
        if (port.HasValue)
            url += $":{port}";
        url += $"{path}{query}";

        return url;
    }
}