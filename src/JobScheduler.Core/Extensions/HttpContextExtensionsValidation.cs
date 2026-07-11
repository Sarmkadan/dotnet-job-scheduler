#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Globalization;

namespace JobScheduler.Core.Extensions;

/// <summary>
/// Validation extensions for HttpContext to validate request state.
/// WHY: Centralized validation prevents runtime errors and provides clear error messages.
/// </summary>
public static class HttpContextExtensionsValidation
{
    /// <summary>
    /// Validates the HttpContext instance and returns any problems found.
    /// </summary>
    /// <param name="context">The HttpContext instance to validate.</param>
    /// <returns>A list of human-readable validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if context is null.</exception>
    public static IReadOnlyList<string> Validate(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var problems = new List<string>();

        // Validate GetUserId - should not be empty if present
        if (context.GetUserId() is string userId && string.IsNullOrWhiteSpace(userId))
        {
            problems.Add("User ID is present but empty or whitespace");
        }

        // Validate GetClientIpAddress - should be a valid IP address
        var ipAddress = context.GetClientIpAddress();
        if (ipAddress is not ("Unknown" or "::1" or "127.0.0.1") &&
            !Uri.CheckHostName(ipAddress.Split(':')[0]).Equals(UriHostNameType.IPv4) &&
            !Uri.CheckHostName(ipAddress.Split(':')[0]).Equals(UriHostNameType.IPv6))
        {
            problems.Add($"Client IP address '{ipAddress}' is not a valid IP address");
        }

        // Validate GetCorrelationId - should be a valid GUID
        if (!Guid.TryParse(context.GetCorrelationId(), out _))
        {
            problems.Add("Correlation ID is not a valid GUID");
        }

        // Validate GetRequestScheme - should be either "http" or "https"
        var scheme = context.GetRequestScheme();
        if (scheme is not ("http" or "https") && !Uri.CheckSchemeName(scheme))
        {
            problems.Add($"Request scheme '{scheme}' is not a valid URI scheme");
        }

        // Validate GetFullRequestUrl - should be a valid absolute URL
        var fullUrl = context.GetFullRequestUrl();
        if (!Uri.IsWellFormedUriString(fullUrl, UriKind.Absolute))
        {
            problems.Add($"Full request URL '{fullUrl}' is not a valid absolute URI");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Checks if the HttpContext instance is valid.
    /// </summary>
    /// <param name="context">The HttpContext instance to check.</param>
    /// <returns>True if valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if context is null.</exception>
    public static bool IsValid(this HttpContext context)
    {
        return Validate(context).Count == 0;
    }

    /// <summary>
    /// Ensures the HttpContext instance is valid, throwing an exception if not.
    /// </summary>
    /// <param name="context">The HttpContext instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if context is null.</exception>
    /// <exception cref="ArgumentException">Thrown if validation fails, containing the list of problems.</exception>
    public static void EnsureValid(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var problems = Validate(context);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"HttpContext validation failed:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", problems)}");
        }
    }
}