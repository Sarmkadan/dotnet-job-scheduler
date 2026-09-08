# RateLimitMiddlewareValidation

`RateLimitMiddlewareValidation` provides extension methods for checking a `RateLimitMiddleware` instance. The implementation uses reflection to inspect the runtime type's public instance properties and returns human-readable problems for supported property types.

> **Current behavior:** `RateLimitMiddleware` is sealed and currently has no public instance properties. Consequently, every non-null `RateLimitMiddleware` instance produces an empty problem list, `IsValid` returns `true`, and `EnsureValid` returns without throwing. The middleware constructor and `InvokeAsync` do not call these validation methods automatically.

## Public methods

### `Validate(this RateLimitMiddleware value)`

```csharp
public static IReadOnlyList<string> Validate(this RateLimitMiddleware value)
```

Throws `ArgumentNullException` when `value` is null. Otherwise, it enumerates public instance properties, skips indexers, and returns all problems it finds. An empty list means that the instance is considered valid.

For each inspected property, the coded rules and exact problem messages are:

| Property type | Invalid condition | Problem added |
|---|---|---|
| `string` | Null, empty, or whitespace | ``{PropertyName} must not be null or empty.`` |
| Integer (`int`, `long`, `short`, `byte`, `uint`, `ulong`, `ushort`, or `sbyte`) | Value is less than or equal to zero | ``{PropertyName} must be greater than zero (current value: {value}).`` |
| `TimeSpan` | Value is less than or equal to `TimeSpan.Zero` | ``{PropertyName} must be a positive TimeSpan.`` |
| `DateTime` | Value equals `default(DateTime)` | ``{PropertyName} must be a valid (non-default) DateTime.`` |
| Nullable integer | No value | ``{PropertyName} must have a value.`` |
| Nullable integer | Value is less than or equal to zero | ``{PropertyName} must be greater than zero (current value: {value}).`` |
| `TimeSpan?` | No value | ``{PropertyName} must have a value.`` |
| `TimeSpan?` | Value is less than or equal to `TimeSpan.Zero` | ``{PropertyName} must be a positive TimeSpan.`` |
| `DateTime?` | No value | ``{PropertyName} must have a value.`` |
| `DateTime?` | Value equals `default(DateTime)` | ``{PropertyName} must be a valid (non-default) DateTime.`` |
| Other reference type | Value is null | ``{PropertyName} must not be null.`` |

Other non-nullable value types, including `bool`, floating-point types, decimal types, enums, and structs other than `TimeSpan` and `DateTime`, have no validation rule. Despite the source comment referring to collections, the code does not explicitly exempt collection properties: any other reference-typed property is checked for null, including a collection.

### `IsValid(this RateLimitMiddleware value)`

```csharp
public static bool IsValid(this RateLimitMiddleware value)
```

Calls `Validate` and returns `true` when no problems are returned; otherwise it returns `false`. It throws `ArgumentNullException` when `value` is null because `Validate` performs the null check.

### `EnsureValid(this RateLimitMiddleware value)`

```csharp
public static void EnsureValid(this RateLimitMiddleware value)
```

Calls `Validate`. When one or more problems exist, it joins them with `"; "` and throws an `ArgumentException` whose parameter name is `value` and whose message begins:

```text
RateLimitMiddleware configuration is invalid:
```

It throws `ArgumentNullException` instead when `value` is null. With the current `RateLimitMiddleware` shape, a non-null instance has no properties to report and this method does not throw.

## Relationship to `RateLimitSettings`

`RateLimitSettings`, declared in `RateLimitMiddleware.cs`, contains the actual public configuration properties:

```csharp
public int RequestsPerWindow { get; set; } = 1000;
public int WindowSizeSeconds { get; set; } = 60;
```

The middleware stores its settings in the private `_settings` field. Because `Validate` reflects only public properties on the `RateLimitMiddleware` instance, it does not reach `_settings` and does not validate either setting. `RateLimitSettings` also has no validation of its own, so zero and negative values can be assigned and passed to the middleware without an exception from this validation class.

During request processing, the middleware uses these values to construct a client-specific `RateLimitBucket`. It also uses `WindowSizeSeconds` for the `Retry-After` response header and uses both values in the rate-limit response body.

## Relationship to `RateLimitBucket`

`RateLimitBucket`, also declared in `RateLimitMiddleware.cs`, receives `RequestsPerWindow` and `WindowSizeSeconds` as `maxRequests` and `windowSizeSeconds`. It keeps them in private fields, uses them to enforce the sliding request window, and exposes only the public `IsExpired` property and `AllowRequest()` method.

The validation extensions do not inspect buckets: buckets are stored in the middleware's private static dictionary, and `Validate` does not traverse fields or nested objects. `RateLimitBucket` itself does not reject zero or negative constructor arguments. Its `IsExpired` property is therefore unrelated to the `DateTime`, integer, and other property rules encoded in `RateLimitMiddlewareValidation`.

See [RateLimitMiddleware](./RateLimitMiddleware.md) for the middleware, settings, and request-processing documentation.
