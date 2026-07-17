# HealthControllerValidation

Provides centralized validation logic for health-check endpoint configurations within the `dotnet-job-scheduler` project. This static utility class exposes methods to validate health controller parameters, check validity, and enforce correctness through guard-style assertions. It is designed to ensure that health-check configurations meet the required constraints before being processed by the scheduler's health monitoring infrastructure.

## API

### Validate

```csharp
public static IReadOnlyList<string> Validate(HealthControllerOptions options)
public static IReadOnlyList<string> Validate(HealthCheckConfiguration configuration)
public static IReadOnlyList<string> Validate(HealthEndpointSettings settings)
public static IReadOnlyList<string> Validate(HealthProbeDefinition probe)
public static IReadOnlyList<string> Validate(HealthCheckInterval interval)
public static IReadOnlyList<string> Validate(HealthTimeoutPolicy policy)
public static IReadOnlyList<string> Validate(HealthRetryStrategy strategy)
```

Validates the provided health-related object and returns a read-only list of error messages. An empty list indicates successful validation. Each overload targets a specific configuration type relevant to health controller setup.

**Parameters:**
- `options` / `configuration` / `settings` / `probe` / `interval` / `policy` / `strategy` — The object to validate.

**Return Value:**
- `IReadOnlyList<string>` — A list of validation error messages. Returns an empty list when the object is valid.

**Exceptions:**
- No exceptions are thrown by these methods; all errors are returned as strings.

---

### IsValid

```csharp
public static bool IsValid(HealthControllerOptions options)
public static bool IsValid(HealthCheckConfiguration configuration)
public static bool IsValid(HealthEndpointSettings settings)
public static bool IsValid(HealthProbeDefinition probe)
public static bool IsValid(HealthCheckInterval interval)
public static bool IsValid(HealthTimeoutSeconds timeout)
public static bool IsValid(HealthRetryPolicy retryPolicy)
```

Determines whether the provided object passes all validation rules. Returns `true` if no errors are found; otherwise `false`.

**Parameters:**
- `options` / `configuration` / `settings` / `probe` / `interval` / `timeout` / `retryPolicy` — The object to check.

**Return Value:**
- `bool` — `true` when the object is valid, `false` otherwise.

**Exceptions:**
- None.

### EnsureValid

```csharp
public static void EnsureValid(HealthControllerOptions options)
public static void EnsureValid(HealthCheckConfiguration configuration)
public static void EnsureValid(HealthEndpointSettings settings)
public static void EnsureValid(HealthProbeDefinition probe)
public static void EnsureValid(HealthCheckInterval interval)
public static void EnsureValid(HealthTimeoutSeconds timeout)
public static void EnsureValid(HealthRetryPolicy retryPolicy)
```

Performs validation and throws an exception if any errors are detected. This acts as a guard clause, ensuring that only valid objects proceed through the application pipeline.

**Parameters:**
- `options` / `configuration` / `settings` / `probe` / `interval` / `timeout` / `retryPolicy` — The object to validate.

**Exceptions:**
- `ArgumentException` — Thrown when validation fails, with a message aggregating all detected errors.

## Usage

### Example 1: Validating a HealthControllerOptions instance before registration

```csharp
var options = new HealthControllerOptions
{
    Endpoint = "/health",
    TimeoutSeconds = 5,
    RetryCount = 3
};

if (HealthControllerValidation.IsValid(options))
{
    services.AddHealthController(options);
}
else
{
    var errors = HealthControllerValidation.Validate(options);
    foreach (var error in errors)
    {
        Console.WriteLine($"Configuration error: {error}");
    }
}
```

### Example 2: Using EnsureValid as a guard in a service constructor

```csharp
public class HealthMonitorService
{
    private readonly HealthCheckConfiguration _config;

    public HealthMonitorService(HealthCheckConfiguration config)
    {
        HealthControllerValidation.EnsureValid(config);
        _config = config;
    }

    public async Task RunChecksAsync()
    {
        // Configuration is guaranteed valid at this point.
        foreach (var probe in _config.Probes)
        {
            HealthControllerValidation.EnsureValid(probe);
            await ExecuteProbeAsync(probe);
        }
    }
}
```

## Notes

- All methods are static and stateless, making them safe for concurrent access from multiple threads without any synchronization.
- The `Validate` methods never throw; they always return a list, even for null inputs (which typically produce a single error message indicating the object is required).
- `EnsureValid` throws `ArgumentException` with a message that concatenates all validation errors, separated by newlines. Callers should be prepared to catch this exception at appropriate boundaries.
- `IsValid` is a convenience wrapper around `Validate` and is equivalent to checking whether the returned list is empty.
- The overloads cover the full set of health-related configuration types used by the scheduler. Passing an unsupported type will result in a compile-time error, as no generic or object-accepting overloads are exposed.
- Validation rules are applied consistently across all overloads for the same type, ensuring uniform behavior regardless of which method is called.
