#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using JobScheduler.Core.Services;
using JobScheduler.Core.Extensions;

namespace JobScheduler.Core.Controllers;

/// <summary>
/// Base controller class providing common functionality for API controllers.
/// WHY: Shared base class reduces duplication and ensures consistent behavior across controllers.
/// </summary>
[ApiController]
public abstract class BaseController : ControllerBase
{
    protected readonly ILogger _logger;
    protected readonly AuditLogger _auditLogger;
    protected readonly CacheService _cacheService;

    protected BaseController(ILogger logger, AuditLogger auditLogger, CacheService cacheService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _auditLogger = auditLogger ?? throw new ArgumentNullException(nameof(auditLogger));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
    }

    /// <summary>
    /// Gets the currently authenticated user ID.
    /// Returns "Anonymous" if not authenticated.
    /// </summary>
    protected string GetUserId()
    {
        return HttpContext.GetUserId() ?? "Anonymous";
    }

    /// <summary>
    /// Gets the client IP address (accounts for proxies).
    /// </summary>
    protected string GetClientIp()
    {
        return HttpContext.GetClientIpAddress();
    }

    /// <summary>
    /// Gets or creates a correlation ID for request tracing.
    /// </summary>
    protected string GetCorrelationId()
    {
        return HttpContext.GetCorrelationId();
    }

    /// <summary>
    /// Logs audit event for controller action.
    /// </summary>
    protected async Task LogAuditAsync(string eventType, string message)
    {
        await _auditLogger.LogSecurityEventAsync(
            eventType,
            GetUserId(),
            message,
            severity: 1);
    }

    /// <summary>
    /// Returns a standardized success response.
    /// </summary>
    protected ActionResult<ApiSuccessResponse<T>> Success<T>(T data, string message = "Operation successful")
    {
        return Ok(new ApiSuccessResponse<T>
        {
            Success = true,
            Message = message,
            Data = data,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Returns a standardized error response.
    /// </summary>
    protected ActionResult<ApiErrorResponse> Error(string message, int statusCode = 400)
    {
        return StatusCode(statusCode, new ApiErrorResponse
        {
            Success = false,
            Message = message,
            Timestamp = DateTime.UtcNow,
            CorrelationId = GetCorrelationId()
        });
    }

    /// <summary>
    /// Returns a validation error response.
    /// </summary>
    protected ActionResult<ApiErrorResponse> ValidationError(Dictionary<string, string[]> errors)
    {
        return BadRequest(new ApiValidationErrorResponse
        {
            Success = false,
            Message = "Validation failed",
            Errors = errors,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Sets response security headers.
    /// </summary>
    protected void SetSecurityHeaders()
    {
        HttpContext.SetSecurityHeaders();
    }

    /// <summary>
    /// Sets response cache headers.
    /// </summary>
    protected void SetCacheControl(int maxAgeSeconds)
    {
        HttpContext.SetCacheControl(maxAgeSeconds);
    }

    /// <summary>
    /// Prevents response caching.
    /// </summary>
    protected void SetNoCache()
    {
        HttpContext.SetNoCache();
    }
}

/// <summary>
/// Standard API success response envelope.
/// </summary>
public class ApiSuccessResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Standard API error response envelope.
/// </summary>
public class ApiErrorResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? CorrelationId { get; set; }
}

/// <summary>
/// Validation error response with field-level errors.
/// </summary>
public class ApiValidationErrorResponse : ApiErrorResponse
{
    public Dictionary<string, string[]> Errors { get; set; } = new();
}
