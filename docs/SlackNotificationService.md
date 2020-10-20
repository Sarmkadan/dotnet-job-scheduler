# SlackNotificationService

The `SlackNotificationService` is a core component within the `dotnet-job-scheduler` project responsible for dispatching asynchronous notifications to Slack channels regarding job execution states and scheduler health. It encapsulates the logic required to format and transmit messages for job failures, successful completions, and critical system alerts, utilizing a structured payload model that supports rich text, attachments, and field metadata to ensure clarity in operational monitoring.

## API

### Constructors

#### `public SlackNotificationService()`
Initializes a new instance of the `SlackNotificationService` class. This constructor prepares the internal HTTP clients and configuration handlers required to communicate with the Slack Web API.

### Methods

#### `public async Task SendJobFailureNotificationAsync`
Asynchronously sends a formatted notification to the configured Slack channel indicating that a scheduled job has failed.
*   **Parameters**: Accepts context regarding the failed job (typically job name, exception details, and timestamp) inferred from the service's internal configuration or passed via overloaded implementations not listed here; based on the signature, it relies on internal state or default configuration for the target channel.
*   **Return Value**: Returns a `Task` that completes when the notification has been successfully dispatched or when the retry policy is exhausted.
*   **Exceptions**: Throws `HttpRequestException` if the Slack API endpoint is unreachable or returns a non-success status code. Throws `InvalidOperationException` if the service is not properly configured with a webhook URL or bot token.

#### `public async Task SendJobSuccessNotificationAsync`
Asynchronously sends a formatted notification to the configured Slack channel confirming the successful completion of a scheduled job.
*   **Parameters**: Relies on internal context or configuration to identify the completed job and relevant metrics.
*   **Return Value**: Returns a `Task` representing the asynchronous operation.
*   **Exceptions**: Throws `HttpRequestException` on network failures or API errors. Throws `InvalidOperationException` if required configuration properties are missing.

#### `public async Task SendSchedulerAlertAsync`
Asynchronously broadcasts a critical alert regarding the scheduler's operational status (e.g., startup failure, resource exhaustion, or configuration errors).
*   **Parameters**: Uses internal state to determine the severity and content of the alert.
*   **Return Value**: Returns a `Task` that completes upon transmission.
*   **Exceptions**: Throws `HttpRequestException` if the message cannot be delivered. Throws `InvalidOperationException` if the service is uninitialized.

### Data Structures and Properties

The following types and properties define the schema for Slack message payloads used by the service.

#### `public string Text`
Represents the primary body text of a Slack message. This property holds the main narrative content displayed in the channel.

#### `public string? Text`
Represents an optional primary body text field. The nullable variant allows for messages that rely entirely on attachments or blocks without a top-level text summary.

#### `public SlackAttachment[] Attachments`
An array of `SlackAttachment` objects associated with a message. Attachments provide structured sidebars containing additional context, graphs, or action buttons.

#### `public string Color`
Specifies the hex color code (e.g., "#FF0000" or "good") used for the left border of a `SlackAttachment`. This visually indicates the severity or status of the attached content.

#### `public string Title`
Defines the bolded header text for a `SlackAttachment`. In the context of the service, this often contains the job name or alert type.

#### `public string Title` (Duplicate Signature Context)
In specific overloads or nested structures within the payload model, this property may refer to the title of an individual field or a secondary header context, maintaining consistency with Slack's block kit or attachment legacy formats.

#### `public SlackField[] Fields`
An array of `SlackField` objects contained within an attachment. Fields allow for key-value pair formatting (e.g., "Duration: 5s", "Status: Failed") to present structured data compactly.

#### `public string Ts`
Represents the timestamp string of the message. This is typically populated by the Slack API response upon successful delivery and can be used for threading or message updates.

#### `public string Value`
Contains the actual content of a `SlackField`. This is the data corresponding to the field's title.

#### `public bool Short`
A boolean flag indicating whether a `SlackField` should be displayed in a compact, side-by-side layout (`true`) or take up the full width of the attachment (`false`).

## Usage

### Example 1: Handling a Job Failure
This example demonstrates how the service is utilized within a job execution wrapper to notify administrators immediately upon catching an unhandled exception.

```csharp
public class DataSyncJob
{
    private readonly SlackNotificationService _notificationService;

    public DataSyncJob(SlackNotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task ExecuteAsync()
    {
        try
        {
            // Simulate job logic
            await PerformDataSync();
        }
        catch (Exception ex)
        {
            // Trigger failure notification with context
            await _notificationService.SendJobFailureNotificationAsync();
            throw; // Re-throw if the scheduler needs to know the job failed technically
        }
    }

    private Task PerformDataSync()
    {
        // Implementation details
        return Task.CompletedTask;
    }
}
```

### Example 2: Dispatching a Critical Scheduler Alert
This example illustrates sending a high-priority alert when the scheduler detects a systemic issue, such as a database connection pool exhaustion.

```csharp
public class SchedulerHealthMonitor
{
    private readonly SlackNotificationService _alertService;

    public SchedulerHealthMonitor(SlackNotificationService alertService)
    {
        _alertService = alertService;
    }

    public async Task CheckResourcesAsync()
    {
        if (!await DatabasePool.IsHealthyAsync())
        {
            // Send immediate alert to ops channel
            await _alertService.SendSchedulerAlertAsync();
            
            // Optional: Trigger fallback logic
            await DatabasePool.AttemptRecoveryAsync();
        }
    }
}
```

## Notes

*   **Thread Safety**: The `SlackNotificationService` methods are asynchronous and designed to be thread-safe for concurrent invocations. Multiple jobs failing simultaneously will result in parallel HTTP requests to the Slack API. However, consumers should implement rate-limiting logic at the call site if the volume of notifications exceeds Slack's API rate limits, as the service itself does not appear to expose a configurable throttling mechanism in its public signature.
*   **Nullability**: The presence of both `public string Text` and `public string? Text` suggests distinct usage contexts or potential legacy overloads within the payload construction. Care must be taken to ensure that when `Text` is null, the `Attachments` array is populated; sending a message with null text and no attachments may result in an API rejection depending on the Slack workspace configuration.
*   **Timestamp Handling**: The `Ts` property is typically read-only after receipt from the API. Attempting to manually set this property before sending a message may have no effect or cause validation errors, as Slack generates timestamps server-side.
*   **Attachment Layout**: When populating the `Fields` array, the `Short` property should be set consistently. Mixing `Short = true` and `Short = false` fields in the same attachment can lead to unpredictable rendering across different Slack clients (desktop vs. mobile).
*   **Error Propagation**: Since the methods throw `HttpRequestException` directly, calling code must wrap invocations in try-catch blocks if a notification failure should not crash the underlying job or scheduler process.
