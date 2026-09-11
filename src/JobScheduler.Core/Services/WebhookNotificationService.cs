#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using Microsoft.Extensions.Logging;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Utilities;

namespace JobScheduler.Core.Services;

/// <summary>
/// Service for sending webhook notifications when jobs execute.
/// Supports delivery retry, signature verification, and event filtering.
/// WHY: Webhooks enable external systems to react to job events in real-time.
/// </summary>
public sealed class WebhookNotificationService
{
    /// <summary>
    /// Initial delay in milliseconds before retrying a webhook delivery.
    /// </summary>
    private const int InitialBackoffMs = 1000;

    /// <summary>
    /// Maximum delay in milliseconds between webhook delivery attempts.
    /// </summary>
    private const int MaxBackoffMs = 30000;

    /// <summary>
    /// Maximum duration allowed for a webhook request.
    /// </summary>
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Duration for which webhook configuration is retained in the cache.
    /// </summary>
    private static readonly TimeSpan WebhookConfigTtl = TimeSpan.FromDays(365);

    private readonly HttpClient _httpClient;
    private readonly ILogger<WebhookNotificationService> _logger;
    private readonly CacheService _cacheService;

    public WebhookNotificationService(HttpClient httpClient, ILogger<WebhookNotificationService> logger, CacheService cacheService)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
    }

    /// <summary>
    /// Sends webhook notification for job execution event.
    /// Includes retry logic and delivery status tracking.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="job"/> is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="execution"/> is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="config"/> is null.</exception>
    public async Task SendExecutionNotificationAsync(Job job, JobExecution execution, WebhookConfig config)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(execution);
        ArgumentNullException.ThrowIfNull(config);
        if (config is null || string.IsNullOrEmpty(config.WebhookUrl))
            return;

        var payload = new WebhookPayload
        {
            EventType = "job.execution.completed",
            Timestamp = DateTime.UtcNow,
            JobId = job.Id,
            JobName = job.Name,
            ExecutionId = execution.Id,
            Status = execution.Status.ToString(),
            ExecutionTimeMs = execution.ExecutionTimeMs,
            ErrorMessage = execution.ErrorMessage,
            RetryAttempt = execution.RetryAttempt
        };

        await SendWebhookWithRetryAsync(config.WebhookUrl, payload, config.Secret, config.MaxRetries);
    }

    /// <summary>
    /// Sends webhook with exponential backoff retry strategy.
    /// WHY: Network failures are transient; retries improve delivery reliability.
    /// </summary>
    private async Task SendWebhookWithRetryAsync(string url, WebhookPayload payload, string? secret, int maxRetries)
    {
        var json = JsonSerializer.Serialize(payload);
        var attempt = 0;
        var backoffMs = InitialBackoffMs;

        while (attempt < maxRetries)
        {
            try
            {
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                // Add HMAC signature if secret provided
                if (!string.IsNullOrEmpty(secret))
                {
                    var signature = CryptoUtility.ComputeHmacSha256(json, secret);
                    content.Headers.Add("X-Webhook-Signature", signature);
                    content.Headers.Add("X-Signature-Algorithm", "HMAC-SHA256");
                }

                using (var cts = new CancellationTokenSource(RequestTimeout))
                {
                    var response = await _httpClient.PostAsync(url, content, cts.Token);

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Webhook delivered successfully to {Url}", url);
                        return;
                    }

                    if (response.StatusCode >= System.Net.HttpStatusCode.BadRequest &&
                        response.StatusCode < System.Net.HttpStatusCode.InternalServerError)
                    {
                        // Client error - don't retry
                        _logger.LogWarning("Webhook rejected by {Url} with status {StatusCode}", url, response.StatusCode);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Webhook delivery attempt {Attempt} failed to {Url}", attempt + 1, url);
            }

            attempt++;
            if (attempt < maxRetries)
            {
                await Task.Delay(backoffMs);
                backoffMs = Math.Min(backoffMs * 2, MaxBackoffMs);
            }
        }

        _logger.LogError("Webhook delivery failed to {Url} after {MaxRetries} attempts", url, maxRetries);
    }

    /// <summary>
    /// Registers a webhook endpoint for job events.
    /// Stores configuration with validation.
    /// </summary>
    public async Task RegisterWebhookAsync(Guid jobId, string webhookUrl, string? secret = null)
    {
        if (string.IsNullOrEmpty(webhookUrl))
            throw new ArgumentException("Webhook URL is required", nameof(webhookUrl));

        if (!Uri.TryCreate(webhookUrl, UriKind.Absolute, out var uri) || (uri.Scheme != "http" && uri.Scheme != "https"))
            throw new ArgumentException("Invalid webhook URL format", nameof(webhookUrl));

        var config = new WebhookConfig
        {
            JobId = jobId,
            WebhookUrl = webhookUrl,
            Secret = secret,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            MaxRetries = 5
        };

        var key = $"webhook:job:{jobId}";
        await _cacheService.SetAsync(key, config, WebhookConfigTtl);

        _logger.LogInformation("Webhook registered for job {JobId}: {Url}", jobId, webhookUrl);
    }

    /// <summary>
    /// Unregisters a webhook for a job.
    /// </summary>
    public async Task UnregisterWebhookAsync(Guid jobId)
    {
        var key = $"webhook:job:{jobId}";
        await _cacheService.RemoveAsync(key);

        _logger.LogInformation("Webhook unregistered for job {JobId}", jobId);
    }

    /// <summary>
    /// Retrieves webhook configuration for a job.
    /// </summary>
    public async Task<WebhookConfig?> GetWebhookConfigAsync(Guid jobId)
    {
        var key = $"webhook:job:{jobId}";
        return await _cacheService.GetAsync<WebhookConfig>(key);
    }

    /// <summary>
    /// Tests webhook connectivity and response.
    /// Used for configuration validation.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="webhookUrl"/> is null, empty, or invalid.</exception>
    public async Task<WebhookTestResult> TestWebhookAsync(string webhookUrl, string? secret = null)
    {
        if (string.IsNullOrEmpty(webhookUrl))
            throw new ArgumentException("Webhook URL is required", nameof(webhookUrl));

        if (!Uri.TryCreate(webhookUrl, UriKind.Absolute, out var uri) || (uri.Scheme != "http" && uri.Scheme != "https"))
            throw new ArgumentException("Invalid webhook URL format", nameof(webhookUrl));
        var testPayload = new WebhookPayload
        {
            EventType = "webhook.test",
            Timestamp = DateTime.UtcNow,
            JobId = Guid.NewGuid(),
            JobName = "Test Job"
        };

        var json = JsonSerializer.Serialize(testPayload);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        if (!string.IsNullOrEmpty(secret))
        {
            var signature = CryptoUtility.ComputeHmacSha256(json, secret);
            content.Headers.Add("X-Webhook-Signature", signature);
        }

        try
        {
            using (var cts = new CancellationTokenSource(RequestTimeout))
            {
                var response = await _httpClient.PostAsync(webhookUrl, content, cts.Token);

                return new WebhookTestResult
                {
                    Success = response.IsSuccessStatusCode,
                    StatusCode = (int)response.StatusCode,
                    Message = response.IsSuccessStatusCode ? "Webhook is reachable" : $"HTTP {response.StatusCode}"
                };
            }
        }
        catch (Exception ex)
        {
            return new WebhookTestResult
            {
                Success = false,
                StatusCode = 0,
                Message = $"Error: {ex.Message}"
            };
        }
    }
}

public sealed class WebhookPayload
{
    /// <summary>
    /// Gets or sets the type of webhook event (e.g., job.execution.completed).
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the webhook event occurred.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the job associated with the webhook.
    /// </summary>
    public Guid JobId { get; set; }

    /// <summary>
    /// Gets or sets the name of the job associated with the webhook.
    /// </summary>
    public string JobName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique identifier of the job execution (if applicable).
    /// </summary>
    public Guid? ExecutionId { get; set; }

    /// <summary>
    /// Gets or sets the status of the job execution (e.g., Success, Failed).
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Gets or sets the execution time in milliseconds.
    /// </summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the error message if the job execution failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the number of retry attempts for this webhook delivery.
    /// </summary>
    public int RetryAttempt { get; set; }
}

public sealed class WebhookConfig
{
    /// <summary>
    /// Gets or sets the unique identifier of the job associated with the webhook.
    /// </summary>
    public Guid JobId { get; set; }

    /// <summary>
    /// Gets or sets the URL of the webhook endpoint.
    /// </summary>
    public string WebhookUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the secret used for HMAC signature verification (optional).
    /// </summary>
    public string? Secret { get; set; }

    /// <summary>
    /// Gets or sets whether the webhook is active and should be used for notifications.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the webhook configuration was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for failed webhook deliveries.
    /// </summary>
    public int MaxRetries { get; set; } = 5;
}

public sealed class WebhookTestResult
{
    /// <summary>
    /// Gets or sets whether the webhook test was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the HTTP status code returned by the webhook endpoint.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the message describing the test result.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
