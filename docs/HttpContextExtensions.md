# HttpContextExtensions

Provides a set of convenience extension methods for `Microsoft.AspNetCore.Http.HttpContext` that simplify common tasks such as retrieving user information, inspecting requests, and setting response headers within the dotnet‑job‑scheduler project.

## API

### GetUserId
```csharp
public static string? GetUserId(this HttpContext context)
```
- **Purpose**: Returns the identifier of the currently authenticated user, if available.
- **Parameters**: None (operates on the supplied `HttpContext` instance).
- **Return value**: The user ID as a string, or `null` when the user is not authenticated or the ID claim is absent.
- **Exceptions**: None.

### GetClaimValue
```csharp
public static string? GetClaimValue(this HttpContext context)
```
- **Purpose**: Retrieves the value of a claim associated with the current user.
- **Parameters**: None (operates on the supplied `HttpContext` instance).
- **Return value**: The claim value as a string, or `null` if no claim is present.
- **Exceptions**: None.

### HasClaim
```csharp
public static bool HasClaim(this HttpContext context)
```
- **Purpose**: Determines whether the current user possesses any claims.
- **Parameters**: None (operates on the supplied `HttpContext` instance).
- **Return value**: `true` if at least one claim exists; otherwise `false`.
- **Exceptions**: None.

### GetClientIpAddress
```csharp
public static string GetClientIpAddress(this HttpContext context)
```
- **Purpose**: Obtains the IP address of the client that made the request.
- **Parameters**: None (operates on the supplied `HttpContext` instance).
- **Return value**: A string representing the client IP address (e.g., IPv4 or IPv6). Returns an empty string if the address cannot be determined.
- **Exceptions**: None.

### SetCacheControl
```csharp
public static void SetCacheControl(this HttpContext context)
```
- **Purpose**: Applies a default `Cache-Control` header to the response.
- **Parameters**: None (operates on the supplied `HttpContext` instance).
- **Return value**: None.
- **Exceptions**: None.

### SetNoCache
```csharp
public static void SetNoCache(this HttpContext context)
```
- **Purpose**: Configures the response to prohibit caching by setting appropriate `Cache-Control` directives.
- **Parameters**: None (operates on the supplied `HttpContext` instance).
- **Return value**: None.
- **Exceptions**: None.

### SetSecurityHeaders
```csharp
public static void SetSecurityHeaders(this HttpContext context)
```
- **Purpose**: Adds a collection of security‑related HTTP headers (e.g., `X-Content-Type-Options`, `X-Frame-Options`) to the response.
- **Parameters**: None (operates on the supplied `HttpContext` instance).
- **Return value**: None.
- **Exceptions**: None.

### GetCorrelationId
```csharp
public static string GetCorrelationId(this HttpContext context)
```
- **Purpose**: Returns a correlation identifier that can be used to trace a request across services.
- **Parameters**: None (operates on the supplied `HttpContext` instance).
- **Return value**: A string containing the correlation ID, or an empty string if none is available.
- **Exceptions**: None.

### GetQueryParameter<T>
```csharp
public static T? GetQueryParameter<T>(this HttpContext context)
```
- **Purpose**: Attempts to retrieve a query‑string value and convert it to the specified type `T`.
- **Parameters**: None (operates on the supplied `HttpContext` instance).
- **Return value**: The parsed value of type `T`, or `null` if the query string is missing or conversion fails.
- **Exceptions**: May throw a `FormatException` if the value cannot be converted to `T`.

### AcceptsJson
```csharp
public static bool AcceptsJson(this HttpContext context)
```
- **Purpose**: Checks whether the request indicates that JSON is an acceptable response format (based on the `Accept` header).
- **Parameters**: None (operates on the supplied `HttpContext` instance).
- **Return value**: `true` if the client accepts JSON; otherwise `false`.
- **Exceptions**: None.

### IsHttps
```csharp
public static bool IsHttps(this HttpContext context)
```
- **Purpose**: Determines if the current request was made over HTTPS.
- **Parameters**: None (operates on the supplied `HttpContext` instance).
- **Return value**: `true` when the request scheme is `https`; otherwise `false`.
- **Exceptions**: None.

### GetRequestScheme
```csharp
public static string GetRequestScheme(this HttpContext context)
```
- **Purpose**: Retrieves the scheme (`http` or `https`) used for the current request.
- **Parameters**: None (operates on the supplied `HttpContext` instance).
- **Return value**: The request scheme as a string.
- **Exceptions**: None.

### GetFullRequestUrl
```csharp
public static string GetFullRequestUrl(this HttpContext context)
```
- **Purpose**: Builds the absolute URL of the current request, including scheme, host, path, and query string.
- **Parameters**: None (operates on the supplied `HttpContext` instance).
- **Return value**: The full request URL.
- **Exceptions**: None.

## Usage

### Example 1: Securing a response and logging user info
```csharp
using Microsoft.AspNetCore.Http;
using YourNamespace.Extensions; // assumes HttpContextExtensions is here

public class MyMiddleware
{
    private readonly RequestDelegate _next;

    public MyMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        // Apply security headers and prevent caching
        context.SetSecurityHeaders();
        context.SetNoCache();

        // Log user identifier if available
        var userId = context.GetUserId();
        if (userId != null)
        {
            // Logging framework call omitted for brevity
            // Logger.Information("Request from user {UserId}", userId);
        }

        await _next(context);
    }
}
```

### Example 2: Reading a typed query‑string parameter and checking content negotiation
```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using YourNamespace.Extensions;

[ApiController]
[Route("[controller]")]
public class ReportController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        // Expect an optional integer "page" query parameter
        int? page = HttpContext.GetQueryParameter<int>();

        // If the client prefers JSON, format accordingly; otherwise fallback
        if (HttpContext.AcceptsJson())
        {
            return Json(new { Page = page ?? 1, Data = GetData(page) });
        }
        else
        {
            return Content(GetData(page).ToString(), "text/plain");
        }
    }

    private object GetData(int? page) => new { };
}
```

## Notes
- All extension methods are **pure** with respect to the `HttpContext` instance; they do not modify any internal static state and are therefore thread‑safe when called concurrently on different `HttpContext` objects.
- Methods that read request data (`GetUserId`, `GetClaimValue`, `HasClaim`, `GetClientIpAddress`, `GetCorrelationId`, `GetQueryParameter<T>`, `AcceptsJson`, `IsHttps`, `GetRequestScheme`, `GetFullRequestUrl`) will return default values (`null`, `false`, `0`, or empty strings) when the underlying information is not present; they do not throw exceptions for missing data.
- The header‑setting methods (`SetCacheControl`, `SetNoCache`, `SetSecurityHeaders`) mutate the outgoing response headers. Calling them multiple times in the same request pipeline will overwrite previously set values for the same header names.
- `GetQueryParameter<T>` attempts to parse the first query‑string value that matches the implicit key derived from the context; if the value cannot be converted to `T`, a `FormatException` is propagated. Callers should handle this exception when dealing with user‑provided input.
