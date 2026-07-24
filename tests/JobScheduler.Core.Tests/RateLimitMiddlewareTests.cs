#nullable enable
using System;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JobScheduler.Core.Middleware;

public sealed class RateLimitMiddlewareTests
{
    private static readonly RequestDelegate _next = (context) => Task.CompletedTask;
    private readonly Mock<ILogger<RateLimitMiddleware>> _loggerMock = new();
    private readonly RateLimitSettings _defaultSettings = new()
    {
        RequestsPerWindow = 10,
        WindowSizeSeconds = 60
    };

    public RateLimitMiddlewareTests()
    {
        // Clear buckets between tests to ensure isolation
        var bucketsField = typeof(RateLimitMiddleware).GetField("_buckets",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        if (bucketsField?.GetValue(null) is ConcurrentDictionary<string, RateLimitBucket> buckets)
        {
            buckets.Clear();
        }
    }

    [Fact]
    public void Constructor_WithNullNext_ThrowsArgumentNullException()
    {
        var loggerMock = new Mock<ILogger<RateLimitMiddleware>>();
        var settings = new RateLimitSettings();

        Assert.Throws<ArgumentNullException>(() => new RateLimitMiddleware(null!, loggerMock.Object, settings));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new RateLimitMiddleware(_next, null!, _defaultSettings));
    }

    [Fact]
    public void Constructor_WithNullSettings_DoesNotThrow()
    {
        var loggerMock = new Mock<ILogger<RateLimitMiddleware>>();
        var middleware = new RateLimitMiddleware(_next, loggerMock.Object, null);
        Assert.NotNull(middleware);
    }

    [Fact]
    public async Task InvokeAsync_WithHealthCheckEndpoint_InvokesNextMiddleware()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/health/live";

        var middleware = new RateLimitMiddleware(_next, _loggerMock.Object, _defaultSettings);
        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task InvokeAsync_WithAuthenticatedUser_UsesUserIdentifier()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/jobs";
        context.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(
            new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "testuser") }));

        var middleware = new RateLimitMiddleware(_next, _loggerMock.Object, _defaultSettings);
        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WithUnauthenticatedUser_UsesIpIdentifier()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/jobs";
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.1");

        var middleware = new RateLimitMiddleware(_next, _loggerMock.Object, _defaultSettings);
        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WithRateLimitExceeded_Returns429Status()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/jobs";
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.100");

        // Create middleware with settings that allow only 1 request
        var settings = new RateLimitSettings { RequestsPerWindow = 1, WindowSizeSeconds = 60 };
        var middleware = new RateLimitMiddleware(_next, _loggerMock.Object, settings);

        // First request should succeed
        await middleware.InvokeAsync(context);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);

        // Second request should be rate limited
        var context2 = new DefaultHttpContext();
        context2.Request.Path = "/api/jobs";
        context2.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.100");
        await middleware.InvokeAsync(context2);

        Assert.Equal(StatusCodes.Status429TooManyRequests, context2.Response.StatusCode);
        Assert.Equal("60", context2.Response.Headers["Retry-After"]);
    }

    [Fact]
    public void RateLimitBucket_AllowRequest_WithEmptyQueue_ReturnsTrue()
    {
        var bucket = new RateLimitBucket(10, 60);
        Assert.True(bucket.AllowRequest());
    }

    [Fact]
    public void RateLimitBucket_AllowRequest_WithRequestsUnderLimit_ReturnsTrue()
    {
        var bucket = new RateLimitBucket(5, 60);

        Assert.True(bucket.AllowRequest());
        Assert.True(bucket.AllowRequest());
        Assert.True(bucket.AllowRequest());
        Assert.True(bucket.AllowRequest());
        Assert.True(bucket.AllowRequest());
    }

    [Fact]
    public void RateLimitBucket_AllowRequest_WithRequestsAtLimit_ReturnsTrue()
    {
        var bucket = new RateLimitBucket(3, 60);

        Assert.True(bucket.AllowRequest());
        Assert.True(bucket.AllowRequest());
        Assert.True(bucket.AllowRequest());
    }

    [Fact]
    public void RateLimitBucket_AllowRequest_WithRequestsOverLimit_ReturnsFalse()
    {
        var bucket = new RateLimitBucket(2, 60);

        Assert.True(bucket.AllowRequest());
        Assert.True(bucket.AllowRequest());
        Assert.False(bucket.AllowRequest());
    }

    [Fact]
    public void RateLimitBucket_AllowRequest_WithExpiredRequests_RemovesOldRequests()
    {
        var bucket = new RateLimitBucket(3, 1); // 1 second window

        // Add requests with timestamps
        var allowRequestMethod = typeof(RateLimitBucket).GetMethod("AllowRequest",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

        // First request
        Assert.True((bool)allowRequestMethod!.Invoke(bucket, null)!);

        // Wait for window to expire
        System.Threading.Thread.Sleep(1500);

        // Should be able to add new requests after expiration
        Assert.True((bool)allowRequestMethod!.Invoke(bucket, null)!);
    }

    [Fact]
    public void RateLimitBucket_IsExpired_WithFreshBucket_ReturnsFalse()
    {
        var bucket = new RateLimitBucket(10, 60);
        var isExpiredProperty = typeof(RateLimitBucket).GetProperty("IsExpired");
        Assert.NotNull(isExpiredProperty);
        var isExpired = (bool)isExpiredProperty.GetValue(bucket)!;
        Assert.False(isExpired);
    }

    [Fact]
    public void RateLimitBucket_IsExpired_WithOldBucket_ReturnsTrue()
    {
        // Create bucket with 1 second window
        var bucket = new RateLimitBucket(10, 1);

        // Wait for it to expire
        System.Threading.Thread.Sleep(2000);

        var isExpiredProperty = typeof(RateLimitBucket).GetProperty("IsExpired");
        Assert.NotNull(isExpiredProperty);
        var isExpired = (bool)isExpiredProperty.GetValue(bucket)!;
        Assert.True(isExpired);
    }

    [Fact]
    public void RateLimitBucket_Constructor_WithZeroRequests_DoesNotThrow()
    {
        // RateLimitBucket doesn't validate constructor parameters
        var bucket = new RateLimitBucket(0, 60);
        Assert.NotNull(bucket);
    }

    [Fact]
    public void RateLimitBucket_Constructor_WithNegativeWindow_DoesNotThrow()
    {
        // RateLimitBucket doesn't validate constructor parameters
        var bucket = new RateLimitBucket(10, -1);
        Assert.NotNull(bucket);
    }

    [Fact]
    public void RateLimitSettings_DefaultValues_AreCorrect()
    {
        var settings = new RateLimitSettings();
        Assert.Equal(1000, settings.RequestsPerWindow);
        Assert.Equal(60, settings.WindowSizeSeconds);
    }

    [Fact]
    public void RateLimitSettings_CustomValues_AreApplied()
    {
        var settings = new RateLimitSettings { RequestsPerWindow = 50, WindowSizeSeconds = 30 };
        Assert.Equal(50, settings.RequestsPerWindow);
        Assert.Equal(30, settings.WindowSizeSeconds);
    }
}
