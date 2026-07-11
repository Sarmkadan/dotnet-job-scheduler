# RateLimitMiddleware

The `RateLimitMiddleware` class implements an ASP.NET Core middleware that enforces rate limiting on incoming HTTP requests using a token bucket algorithm. It tracks the number of requests within a sliding time window and rejects requests that exceed the configured limit. The middleware is configurable through its public properties and exposes the underlying bucket for inspection.

## API

### `public RateLimitMiddleware()`

Initializes a new instance of the `RateLimitMiddleware` class. The middleware is typically registered in the ASP.NET Core pipeline via `UseMiddleware<RateLimitMiddleware>()`. Configuration properties should be set before the first request is processed.

### `public async Task InvokeAsync(HttpContext context)`

Processes an incoming HTTP request. Checks whether the request is allowed based on the current rate limit state. If allowed, the request is passed to the next middleware in the pipeline; otherwise, a `429 Too Many Requests` response is returned.

- **Parameters**  
  `context` – The `HttpContext` for the current request.

- **Returns**  
  A `Task` representing the asynchronous operation.

- **Throws**  
  `ArgumentNullException` – if `context` is `null`.

### `public RateLimitBucket RateLimitBucket { get; }`

Gets the `RateLimitBucket` instance used to track request counts and enforce the rate limit. The bucket maintains the current count of requests within the active window and is reset automatically when the window expires.

### `public bool AllowRequest { get; set; }`

Gets or sets a value indicating whether rate limiting is enabled. When set to `false`, all requests are allowed without restriction. Default is `true`.

### `public int RequestsPerWindow { get; set; }`

Gets or sets the maximum number of requests allowed within a single time window. Must be greater than zero when rate limiting is active.

- **Throws**  
  `ArgumentOutOfRangeException` – if set to a value less than or equal to zero while `AllowRequest` is `true`.

### `public int WindowSizeSeconds { get; set; }`

Gets or sets the duration of the rate limiting window in seconds. Must be greater than zero when rate limiting is active.

- **Throws**  
  `ArgumentOutOfRangeException` – if set to a value less than or equal to zero while `AllowRequest` is `true`.

## Usage

### Example 1: Basic middleware registration with default settings

```csharp
// In Program.cs or Startup.Configure
app.UseMiddleware<RateLimitMiddleware>();
```

This registers the middleware with default values (`AllowRequest = true`, `RequestsPerWindow = 100`, `WindowSizeSeconds = 60`). Requests exceeding 100 per minute will be rejected.

### Example 2: Custom configuration for a high‑traffic endpoint

```csharp
// In Startup.ConfigureServices or a custom middleware configuration method
var rateLimitMiddleware = new RateLimitMiddleware
{
    AllowRequest = true,
    RequestsPerWindow = 500,
    WindowSizeSeconds = 10
};

// Register the middleware instance (requires custom factory or direct use)
app.Use(next =>
{
    return async context =>
    {
        // Optionally set properties per request context
        rateLimitMiddleware.RequestsPerWindow = 500;
        rateLimitMiddleware.WindowSizeSeconds = 10;
        await rateLimitMiddleware.InvokeAsync(context);
    };
});
```

This configuration allows up to 500 requests every 10 seconds. The properties can be adjusted dynamically before each invocation, though thread‑safety considerations apply (see Notes).

## Notes

- **Thread safety**: The `RateLimitBucket` is designed to be thread‑safe for concurrent requests. However, modifying `RequestsPerWindow`, `WindowSizeSeconds`, or `AllowRequest` after the middleware has started processing requests may lead to inconsistent behavior. It is recommended to set these properties during application startup, before any requests are handled.
- **Edge cases**:  
  - Setting `WindowSizeSeconds` to zero or negative while `AllowRequest` is `true` throws `ArgumentOutOfRangeException`.  
  - Setting `RequestsPerWindow` to zero or negative while `AllowRequest` is `true` throws `ArgumentOutOfRangeException`.  
  - If `AllowRequest` is set to `false`, the middleware passes all requests through without checking the bucket, and the `RequestsPerWindow` and `WindowSizeSeconds` values are ignored.
- **Bucket state**: The `RateLimitBucket` property exposes the internal bucket for diagnostic or monitoring purposes. Direct manipulation of the bucket is not supported and may cause unexpected rate limiting behavior.
