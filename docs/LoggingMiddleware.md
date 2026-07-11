# LoggingMiddleware

`LoggingMiddleware` is an ASP.NET Core middleware component that captures and records structured details about incoming HTTP requests and outgoing responses. It extracts the HTTP method, request path, query string, headers, and optional body content at the point of invocation, then logs the corresponding response status code, headers, and optional body after the downstream pipeline completes. This enables comprehensive request/response auditing, diagnostics, and observability without modifying individual endpoint logic.

## API

### `LoggingMiddleware`

The constructor for the middleware. It accepts the standard `RequestDelegate` representing the next middleware in the pipeline, along with any dependencies required for logging or configuration. No public fields or properties are exposed directly on the instance beyond the members documented below; internal state is managed privately.

### `InvokeAsync`

```csharp
public async Task InvokeAsync(HttpContext context)
```

Invokes the middleware logic for a given HTTP context. This method reads request data before passing execution to the next delegate, then captures response data after the delegate completes.

- **Parameters**:
  - `context` (`HttpContext`): The HTTP context for the current request. Must not be `null`.
- **Returns**: A `Task` representing the asynchronous operation.
- **Exceptions**: Throws `ArgumentNullException` if `context` is `null`. May throw `InvalidOperationException` if the request body has already been read and buffering is not enabled; callers should ensure request body buffering is configured upstream when body capture is required.

### Request Members

These members are populated after `InvokeAsync` reads the incoming request and are available for logging or inspection during the request phase.

#### `Method`

```csharp
public string Method { get; }
```

The HTTP method of the request (e.g., `GET`, `POST`, `PUT`). Derived from `HttpContext.Request.Method`.

#### `Path`

```csharp
public string Path { get; }
```

The request path, excluding the query string (e.g., `/api/v1/jobs`). Derived from `HttpContext.Request.Path`.

#### `QueryString`

```csharp
public string QueryString { get; }
```

The raw query string, including the leading `?` if present (e.g., `?status=active&page=1`). Derived from `HttpContext.Request.QueryString`.

#### `Headers`

```csharp
public Dictionary<string, string> Headers { get; }
```

A dictionary of request header names and their values. Header names are stored in their original casing. If a header has multiple values, the behavior (joining or selecting a single value) depends on the internal capture implementation.

#### `Body`

```csharp
public string? Body { get; }
```

The request body captured as a string, or `null` if the body is empty, not readable, or body capture is disabled. Requires request body buffering to be enabled; otherwise, this property may remain `null` or throw during population.

#### `Timestamp`

```csharp
public DateTime Timestamp { get; }
```

The UTC timestamp at which the request was received and logging began. Set at the start of `InvokeAsync` before the downstream pipeline executes.

### Response Members

These members are populated after the downstream pipeline completes and the response is sent.

#### `StatusCode`

```csharp
public int StatusCode { get; }
```

The HTTP status code of the response (e.g., `200`, `404`, `500`). Derived from `HttpContext.Response.StatusCode` after the pipeline returns.

#### `Headers`

```csharp
public Dictionary<string, string> Headers { get; }
```

A dictionary of response header names and their values. Captured after the response is generated. Header names are stored in their original casing.

#### `Body`

```csharp
public string? Body { get; }
```

The response body captured as a string, or `null` if the body is empty, not readable, or body capture is disabled. Requires response body buffering to be enabled; otherwise, this property may remain `null` or throw during population.

## Usage

### Example 1: Basic Registration and Logging

Register the middleware in the application pipeline and configure a simple console logging delegate.

```csharp
var builder = WebApplication.CreateBuilder(args);

// Enable request and response buffering for body capture
builder.Services.AddControllers();

var app = builder.Build();

app.Use(async (context, next) =>
{
    context.Request.EnableBuffering();
    await next();
});

app.UseMiddleware<LoggingMiddleware>();

app.MapControllers();

app.Run();
```

A custom logging handler can be injected into `LoggingMiddleware` to write structured logs:

```csharp
public class RequestResponseLogger
{
    public void Log(LoggingMiddleware middleware)
    {
        Console.WriteLine(
            "[{0:O}] {1} {2}{3} -> {4}",
            middleware.Timestamp,
            middleware.Method,
            middleware.Path,
            middleware.QueryString,
            middleware.StatusCode);
    }
}
```

### Example 2: Conditional Body Capture with Filtering

Capture request and response bodies only for specific endpoints to avoid performance overhead on large payloads.

```csharp
app.UseMiddleware<LoggingMiddleware>();

app.Map("/api/v1/jobs", async (HttpContext context, RequestDelegate next) =>
{
    // Enable buffering only for this branch
    context.Request.EnableBuffering();
    context.Response.OnStarting(() =>
    {
        context.Response.Body = new MemoryStream();
        return Task.CompletedTask;
    });

    await next();

    // After LoggingMiddleware runs, body properties are populated
    var logger = context.RequestServices.GetRequiredService<RequestResponseLogger>();
    // logger.Log(...) has access to Body properties
});
```

## Notes

- **Body buffering requirement**: Both `Body` properties (request and response) depend on the respective streams being buffered and seekable. If `EnableBuffering` is not called on the request or the response stream is not replaced with a buffered stream, attempting to read the body may result in an `InvalidOperationException` or leave the properties as `null`.
- **Header casing**: The `Headers` dictionaries preserve original casing as provided by the HTTP context. Consumers should perform case-insensitive lookups when checking for specific headers.
- **Thread safety**: `LoggingMiddleware` is scoped to a single HTTP request. Each invocation of `InvokeAsync` operates on a distinct instance or a distinct set of properties; no concurrent access occurs within the same request. The type is not designed for reuse across multiple requests without re-initialization.
- **Timestamp precision**: `Timestamp` uses `DateTime.UtcNow` at the moment of capture. For high-resolution timing, consumers should supplement with a `Stopwatch` if elapsed measurement is required.
- **Empty bodies**: For requests or responses with no content (e.g., `GET` requests, `204 No Content` responses), `Body` will be `null` or an empty string depending on internal capture logic. Null checks are recommended before processing body content.
- **Large payloads**: Capturing full request and response bodies in memory can significantly increase memory pressure. Consider selective buffering or truncation strategies for endpoints that handle large payloads.
