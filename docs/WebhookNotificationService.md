# WebhookNotificationService

`WebhookNotificationService` manages the lifecycle of webhook registrations and dispatches execution-related notifications to registered HTTP endpoints. It handles registration, deregistration, configuration retrieval, test invocation, and the delivery of job-execution events—including status, timing, and error details—to external systems.

## API

### Constructors

#### `public WebhookNotificationService`

Creates a new instance of the service. Implementation details (e.g., dependency injection of HTTP clients or storage backends) are internal; the constructor is parameterless from the public surface.

### Methods

#### `public async Task SendExecutionNotificationAsync`

Dispatches a webhook notification for a job execution event. The payload is derived from the public properties of the service instance (`EventType`, `Timestamp`, `JobId`, `JobName`, `ExecutionId`, `Status`, `ExecutionTimeMs`, `ErrorMessage`, `RetryAttempt`). The method resolves the registered webhook configuration for the given job, serializes the payload, applies any configured secret for HMAC signing, and POSTs to the registered URL.

- **Parameters:** none (uses instance state).
- **Returns:** `Task` representing the asynchronous send operation.
- **Throws:** May throw if no active webhook is registered for the job, if the HTTP request fails, or if signing/configuration is invalid.

#### `public async Task RegisterWebhookAsync`

Registers or updates a webhook configuration for a specific job. The configuration is built from the instance properties `JobId`, `WebhookUrl`, `Secret`, and `IsActive`.

- **Parameters:** none (uses instance state).
- **Returns:** `Task` representing the asynchronous registration.
- **Throws:** May throw if `WebhookUrl` is invalid or unreachable during validation, or if a persistence conflict occurs.

#### `public async Task UnregisterWebhookAsync`

Removes the webhook configuration associated with the `JobId` held by the instance. After this call, no further notifications will be sent for that job until a new registration is made.

- **Parameters:** none (uses instance state).
- **Returns:** `Task` representing the asynchronous removal.
- **Throws:** May throw if no configuration exists for the given `JobId`.

#### `public async Task<WebhookConfig?> GetWebhookConfigAsync`

Retrieves the stored webhook configuration for the `JobId` held by the instance.

- **Parameters:** none (uses instance state).
- **Returns:** A `WebhookConfig` object if one is registered and active; `null` otherwise.
- **Throws:** Typically does not throw; returns `null` for missing configurations.

#### `public async Task<WebhookTestResult> TestWebhookAsync`

Sends a synthetic test payload to the webhook endpoint defined by the instance properties `WebhookUrl` and `Secret`. This validates reachability, authentication, and the shape of the payload without requiring a real job execution.

- **Parameters:** none (uses instance state).
- **Returns:** A `WebhookTestResult` indicating success or failure, along with HTTP status and any error details.
- **Throws:** May throw if `WebhookUrl` is malformed; network-level exceptions are typically captured in the result object rather than propagated.

### Properties

#### `public string EventType`

The type of execution event being reported (e.g., `"JobCompleted"`, `"JobFailed"`). Set before calling `SendExecutionNotificationAsync`.

#### `public DateTime Timestamp`

The UTC timestamp of the event occurrence. Set before calling `SendExecutionNotificationAsync`.

#### `public Guid JobId`

The unique identifier of the job this notification pertains to. Used by `RegisterWebhookAsync`, `UnregisterWebhookAsync`, `GetWebhookConfigAsync`, and `SendExecutionNotificationAsync`.

#### `public string JobName`

The human-readable name of the job. Included in notification payloads.

#### `public Guid? ExecutionId`

The unique identifier of the specific execution instance. Nullable; may be `null` for events not tied to a single execution.

#### `public string? Status`

The execution status (e.g., `"Running"`, `"Succeeded"`, `"Failed"`). Nullable.

#### `public long ExecutionTimeMs`

The execution duration in milliseconds. Set before sending a completion notification.

#### `public string? ErrorMessage`

The error message associated with a failed execution. Nullable; set only for failure events.

#### `public int RetryAttempt`

The zero-based retry attempt number for this notification delivery. Used internally for retry tracking.

#### `public string WebhookUrl`

The target HTTP endpoint for webhook delivery. Used by `RegisterWebhookAsync` and `TestWebhookAsync`.

#### `public string? Secret`

An optional shared secret used to sign webhook payloads (typically HMAC-SHA256). Nullable; when set, the signature is included in the `X-Webhook-Signature` header.

#### `public bool IsActive`

Indicates whether the webhook configuration is active. Inactive webhooks are not triggered by `SendExecutionNotificationAsync`.

#### `public DateTime CreatedAt`

The UTC timestamp when the webhook configuration was first created. Set by `RegisterWebhookAsync`.

## Usage

### Example 1: Registering a webhook and sending a completion notification

```csharp
var service = new WebhookNotificationService();

// Register a webhook for job "a1b2c3d4-..."
service.JobId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
service.WebhookUrl = "https://example.com/webhooks/job-events";
service.Secret = "whsec_shared_secret";
service.IsActive = true;

await service.RegisterWebhookAsync();

// Later, after job execution completes:
service.EventType = "JobCompleted";
service.Timestamp = DateTime.UtcNow;
service.JobName = "NightlyBatchExport";
service.ExecutionId = Guid.Parse("f9e8d7c6-b5a4-3210-fedc-ba9876543210");
service.Status = "Succeeded";
service.ExecutionTimeMs = 12_340;
service.ErrorMessage = null;
service.RetryAttempt = 0;

await service.SendExecutionNotificationAsync();
```

### Example 2: Testing a webhook and conditionally unregistering

```csharp
var service = new WebhookNotificationService();
service.JobId = Guid.Parse("11111111-2222-3333-4444-555555555555");
service.WebhookUrl = "https://staging.example.com/hooks";
service.Secret = null; // no signing

WebhookTestResult result = await service.TestWebhookAsync();

if (!result.Success)
{
    Console.WriteLine($"Test failed: {result.Error}");
    // Remove the broken configuration
    await service.UnregisterWebhookAsync();
}
else
{
    // Retrieve and confirm the stored config
    WebhookConfig? config = await service.GetWebhookConfigAsync();
    Console.WriteLine(config != null
        ? $"Active webhook created at {config.CreatedAt}"
        : "No configuration found.");
}
```

## Notes

- **Mutable instance state:** The service uses its own properties as both input and output carriers across method calls. This design is not thread-safe; a single instance must not be shared concurrently without external synchronization. For multi-threaded scenarios, create separate instances or serialize access.
- **Null handling:** `ExecutionId`, `Status`, `ErrorMessage`, and `Secret` are nullable. `SendExecutionNotificationAsync` must tolerate `null` values in the payload (e.g., omit fields or serialize as JSON `null`). `GetWebhookConfigAsync` returns `null` when no configuration exists—callers must guard against this before accessing members of `WebhookConfig`.
- **Test vs. send:** `TestWebhookAsync` uses the instance’s `WebhookUrl` and `Secret` directly, independent of any stored registration. It does not require `RegisterWebhookAsync` to have been called first. Conversely, `SendExecutionNotificationAsync` relies on a previously registered, active configuration.
- **Retry behavior:** The `RetryAttempt` property is set by the caller (or an internal retry loop) before each delivery attempt. The service itself does not automatically increment or persist this counter; it is included in the payload for the receiver’s idempotency logic.
- **Idempotency:** `RegisterWebhookAsync` acts as an upsert—calling it multiple times with the same `JobId` overwrites the existing configuration and updates `CreatedAt`. `UnregisterWebhookAsync` on an already-removed job may throw or no-op depending on implementation; callers should check existence via `GetWebhookConfigAsync` first if strict control is needed.
