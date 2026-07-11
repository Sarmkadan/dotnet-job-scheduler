# GlobalExceptionMiddleware

`GlobalExceptionMiddleware` is an ASP.NET Core middleware component that intercepts unhandled exceptions occurring during HTTP request processing. It catches exceptions thrown by downstream middleware or endpoint logic, serializes them into a structured JSON error response containing a user-facing message, a timestamp, the exception type name, and optionally the stack trace, then writes that response to the client with an appropriate HTTP status code.

## API

### `GlobalExceptionMiddleware`

The constructor for the middleware. It accepts the next delegate in the request pipeline and any configuration required to control error-response formatting.

- **Parameters:**  
  `RequestDelegate next` — the delegate representing the remainder of the request pipeline.  
  Additional parameters may include an `IHostEnvironment` or similar service to determine whether stack traces should be included.

- **Return value:** A new instance of `GlobalExceptionMiddleware`.

- **Throws:** `ArgumentNullException` if `next` is `null`.

### `async Task InvokeAsync(HttpContext context)`

Invokes the middleware for an incoming HTTP context. The method wraps the downstream pipeline invocation in a try/catch block; when an exception is caught, it sets the response status code to an appropriate error code (typically 500), constructs an error payload, serializes it as JSON, and writes it to the response body.

- **Parameters:**  
  `HttpContext context` — the HTTP context for the current request. Must not be `null`.

- **Return value:** A `Task` representing the asynchronous operation.

- **Throws:**  
  `ArgumentNullException` if `context` is `null`.  
  Exceptions thrown during the error-response writing phase (e.g., a broken response stream) may propagate, as they cannot be meaningfully handled.

### `string Message`

Gets the human-readable error message included in the JSON response body. This is typically a generic message such as “An unexpected error occurred” to avoid leaking internal details to clients.

- **Type:** `string` (read-only, set during construction or from configuration).

### `DateTime Timestamp`

Gets the UTC timestamp at which the exception was caught. This value is serialized into the error response so that clients and logs can correlate the failure to a specific point in time.

- **Type:** `DateTime` (read-only, set at the moment the exception is intercepted).

### `string? ExceptionType`

Gets the fully qualified name of the exception type that was caught, or `null` if no exception has been intercepted yet or if the type is intentionally suppressed. In the error response, this field helps consumers understand the category of failure without exposing the full exception object.

- **Type:** `string?` (nullable, read-only).

### `string? StackTrace`

Gets the stack trace string of the caught exception, or `null` if stack traces are disabled (e.g., in production environments) or no exception has been intercepted. When non-null, it provides debugging information in development or staging scenarios.

- **Type:** `string?` (nullable, read-only).

## Usage

### Example 1: Basic registration in the pipeline

```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Register the global exception middleware early in the pipeline.
app.UseMiddleware<GlobalExceptionMiddleware>();

app.MapGet("/", () =>
{
    throw new InvalidOperationException("Something went wrong.");
});

app.Run();
```

When the endpoint throws, the middleware catches the exception and returns a JSON response similar to:

```json
{
  "message": "An unexpected error occurred.",
  "timestamp": "2025-03-15T10:23:45.123Z",
  "exceptionType": "System.InvalidOperationException",
  "stackTrace": null
}
```

### Example 2: Conditional stack-trace inclusion based on environment

```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // In development, register a variant that includes stack traces.
    app.UseMiddleware<GlobalExceptionMiddleware>(includeStackTrace: true);
}
else
{
    app.UseMiddleware<GlobalExceptionMiddleware>(includeStackTrace: false);
}

app.MapGet("/data", async (HttpContext context) =>
{
    await context.Response.WriteAsync("Processing...");
    throw new TimeoutException("The operation timed out.");
});

app.Run();
```

In development, the response includes the `stackTrace` field populated with the full stack trace string. In production, `stackTrace` remains `null`.

## Notes

- **Response already started:** If the downstream pipeline has already begun writing to the response stream before throwing, the middleware cannot alter the status code or body. In such cases, the exception may be logged but the client will receive a truncated or incomplete response.
- **Nested exceptions:** The middleware captures the outermost exception. `ExceptionType` reflects that exception’s type; inner exceptions are not individually serialized unless explicitly implemented in a derived class.
- **Thread safety:** The middleware instance is registered as a singleton or scoped service depending on the DI container configuration. `InvokeAsync` is called concurrently for different requests. The properties `Message`, `Timestamp`, `ExceptionType`, and `StackTrace` reflect the state of the most recently caught exception and are not isolated per request unless the middleware is registered as a transient or scoped service with per-request state. In a typical singleton registration, these properties are overwritten on each exception and should not be relied upon for per-request diagnostics outside of the `InvokeAsync` scope.
- **Empty responses:** If the exception is caught but the response body cannot be written (e.g., the connection has been dropped), the middleware silently fails; no fallback response is sent.
- **Custom status codes:** Derived implementations may map specific exception types to non-500 status codes. The base implementation described here uses a generic server-error status code unless configured otherwise.
