#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

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
    public static string? GetUserId(this HttpContext context)
    {
        return context?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
               context?.User?.FindFirst("sub")?.Value ??
               context?.User?.Identity?.Name;
    }

    /// <summary>
    /// Gets a specific claim value.
    /// </summary>
    public static string? GetClaimValue(this HttpContext context, string claimType)
    {
        return context?.User?.FindFirst(claimType)?.Value;
    }

    /// <summary>
    /// Checks if user has a specific claim value.
    /// </summary>
    public static bool HasClaim(this HttpContext context, string claimType, string? claimValue = null)
    {
        if (context?.User is null)
            return false;

        if (claimValue is null)
            return context.User.HasClaim(c => c.Type == claimType);

        return context.User.HasClaim(claimType, claimValue);
    }

    /// <summary>
    /// Gets the client IP address (handles proxy headers).
    /// WHY: Some deployments use reverse proxies that require X-Forwarded-For header.
    /// </summary>
    public static string GetClientIpAddress(this HttpContext context)
    {
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
    public static void SetCacheControl(this HttpContext context, int maxAgeSeconds)
    {
        context.Response.Headers.Add("Cache-Control", $"public, max-age={maxAgeSeconds}");
    }

    /// <summary>
    /// Sets a response header to prevent caching.
    /// </summary>
    public static void SetNoCache(this HttpContext context)
    {
        context.Response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");
        context.Response.Headers.Add("Pragma", "no-cache");
        context.Response.Headers.Add("Expires", "0");
    }

    /// <summary>
    /// Sets security headers to prevent common web attacks.
    /// </summary>
    public static void SetSecurityHeaders(this HttpContext context)
    {
        context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Add("X-Frame-Options", "DENY");
        context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    }

    /// <summary>
    /// Gets the request correlation ID (for tracing).
    /// </summary>
    public static string GetCorrelationId(this HttpContext context)
    {
        const string correlationIdHeader = "X-Correlation-ID";

        if (context.Request.Headers.TryGetValue(correlationIdHeader, out var correlationId))
            return correlationId.ToString();

        var id = Guid.NewGuid().ToString();
        context.Request.Headers.Add(correlationIdHeader, id);
        return id;
    }

    /// <summary>
    /// Gets a query parameter with type conversion.
    /// </summary>
    public static T? GetQueryParameter<T>(this HttpContext context, string paramName) where T : struct
    {
        if (!context.Request.Query.TryGetValue(paramName, out var value))
            return null;

        if (typeof(T) == typeof(int) && int.TryParse(value, out var intValue))
            return (T)(object)intValue;

        if (typeof(T) == typeof(bool) && bool.TryParse(value, out var boolValue))
            return (T)(object)boolValue;

        if (typeof(T) == typeof(Guid) && Guid.TryParse(value, out var guidValue))
            return (T)(object)guidValue;

        return null;
    }

    /// <summary>
    /// Checks if the request accepts JSON response.
    /// </summary>
    public static bool AcceptsJson(this HttpContext context)
    {
        return context.Request.Headers.Accept.Contains("application/json") ||
               context.Request.ContentType?.Contains("application/json") == true;
    }

    /// <summary>
    /// Checks if the request is HTTPS.
    /// </summary>
    public static bool IsHttps(this HttpContext context)
    {
        return context.Request.IsHttps ||
               context.Request.Headers.ContainsKey("X-Forwarded-Proto") &&
               context.Request.Headers["X-Forwarded-Proto"].ToString() == "https";
    }

    /// <summary>
    /// Gets the request scheme (http or https).
    /// </summary>
    public static string GetRequestScheme(this HttpContext context)
    {
        return context.IsHttps() ? "https" : "http";
    }

    /// <summary>
    /// Gets the full request URL.
    /// </summary>
    public static string GetFullRequestUrl(this HttpContext context)
    {
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
