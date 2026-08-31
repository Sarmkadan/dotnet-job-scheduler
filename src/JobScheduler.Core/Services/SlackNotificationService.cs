#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using Microsoft.Extensions.Logging;
using JobScheduler.Core.Domain.Entities;

namespace JobScheduler.Core.Services;

/// <summary>
/// Service for sending Slack notifications when critical job events occur.
/// Integrates with Slack webhooks for real-time alerts.
/// WHY: Slack integration enables DevOps teams to monitor jobs directly in their workflow.
/// </summary>
public sealed class SlackNotificationService
{
    private const string WarningColor = "warning";
    private const string DangerColor = "danger";
    private const string GoodColor = "good";

    private static readonly JsonSerializerOptions SerializerOptions = new();

    private readonly HttpClient _httpClient;
    private readonly ILogger<SlackNotificationService> _logger;

    public SlackNotificationService(HttpClient httpClient, ILogger<SlackNotificationService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Sends job execution failure notification to Slack.
    /// Includes error message and retry information.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="job"/> or <paramref name="execution"/> is <see langword="null"/>.</exception>
    public async Task SendJobFailureNotificationAsync(Job job, JobExecution execution, string webhookUrl)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(execution);

        _logger.LogInformation("Sending job failure notification for Job {JobName} (ExecutionId: {ExecutionId}, Attempt: {Attempt}/{MaxRetries})", job.Name, execution.Id, execution.RetryAttempt, job.MaxRetries);

        if (string.IsNullOrEmpty(webhookUrl))
        {
            _logger.LogWarning("Webhook URL is null or empty for job {JobName}, skipping Slack notification", job.Name);
            return;
        }

        var color = execution.RetryAttempt < job.MaxRetries ? WarningColor : DangerColor;
        var message = new SlackMessage
        {
            Text = $"Job {job.Name} execution failed",
            Attachments = new[]
            {
                BuildJobAttachment(
                    job,
                    execution,
                    color,
                    $"{job.Name} - Execution Failed",
                    "Failed",
                    new SlackField { Title = "Retry Attempt", Value = $"{execution.RetryAttempt}/{job.MaxRetries}", Short = true },
                    new SlackField { Title = "Error", Value = execution.ErrorMessage ?? "No error details", Short = false })
            }
        };

        await SendSlackMessageAsync(message, webhookUrl, job.Name);
        _logger.LogInformation("Job failure notification sent successfully for Job {JobName}", job.Name);
    }

    /// <summary>
    /// Sends job execution success notification to Slack.
    /// Includes execution time and performance metrics.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="job"/> or <paramref name="execution"/> is <see langword="null"/>.</exception>
    public async Task SendJobSuccessNotificationAsync(Job job, JobExecution execution, string webhookUrl)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(execution);

        _logger.LogInformation("Sending job success notification for Job {JobName} (ExecutionId: {ExecutionId}, ExecutionTime: {ExecutionTimeMs}ms)", job.Name, execution.Id, execution.ExecutionTimeMs);

        if (string.IsNullOrEmpty(webhookUrl))
        {
            _logger.LogWarning("Webhook URL is null or empty for job {JobName}, skipping Slack notification", job.Name);
            return;
        }

        var message = new SlackMessage
        {
            Text = $"Job {job.Name} executed successfully",
            Attachments = new[]
            {
                BuildJobAttachment(
                    job,
                    execution,
                    GoodColor,
                    $"{job.Name} - Execution Successful",
                    "Completed",
                    new SlackField { Title = "Success Rate", Value = $"{job.GetSuccessRate():F1}%", Short = true })
            }
        };

        await SendSlackMessageAsync(message, webhookUrl, job.Name);
        _logger.LogInformation("Job success notification sent successfully for Job {JobName}", job.Name);
    }

    /// <summary>
    /// Sends alert for critical scheduler events.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="title"/> or <paramref name="message"/> is <see langword="null"/>, empty, or consists only of white-space characters.</exception>
    public async Task SendSchedulerAlertAsync(string title, string message, string severity, string webhookUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        _logger.LogInformation("Sending scheduler alert: {Title} with severity {Severity}", title, severity);

        if (string.IsNullOrEmpty(webhookUrl))
        {
            _logger.LogWarning("Webhook URL is null or empty for scheduler alert: {Title}", title);
            return;
        }

        var color = severity switch
        {
            "Critical" => DangerColor,
            "Warning" => WarningColor,
            _ => "#808080"
        };

        var slackMessage = new SlackMessage
        {
            Text = title,
            Attachments = new[]
            {
                new SlackAttachment
                {
                    Color = color,
                    Title = title,
                    Text = message,
                    Fields = new[]
                    {
                        new SlackField { Title = "Severity", Value = severity, Short = true },
                        new SlackField { Title = "Time", Value = DateTime.UtcNow.ToString("o"), Short = true }
                    }
                }
            }
        };

        await SendSlackMessageAsync(slackMessage, webhookUrl, title);
        _logger.LogInformation("Scheduler alert sent successfully: {Title}", title);
    }

    private static SlackAttachment BuildJobAttachment(
        Job job,
        JobExecution execution,
        string color,
        string title,
        string status,
        params SlackField[] extraFields)
    {
        var fields = new SlackField[3 + extraFields.Length];
        fields[0] = new SlackField { Title = "Job", Value = job.Name, Short = true };
        fields[1] = new SlackField { Title = "Status", Value = status, Short = true };
        fields[2] = new SlackField { Title = "Execution Time", Value = $"{execution.ExecutionTimeMs}ms", Short = true };
        extraFields.CopyTo(fields, 3);

        return new SlackAttachment
        {
            Color = color,
            Title = title,
            Fields = fields,
            Ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()
        };
    }

    private async Task SendSlackMessageAsync(SlackMessage message, string webhookUrl, string context)
    {
        try
        {
            var json = JsonSerializer.Serialize(message, SerializerOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
            {
                using var response = await _httpClient.PostAsync(webhookUrl, content, cts.Token);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogDebug("Slack notification sent successfully");
                }
                else
                {
                    _logger.LogError(
                        "Failed to send Slack notification with status code {StatusCode} for {Context}",
                        response.StatusCode,
                        context);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending Slack notification: {ExceptionMessage}", ex.Message);
        }
    }
}

public sealed class SlackMessage
{
    [System.Text.Json.Serialization.JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("attachments")]
    public SlackAttachment[] Attachments { get; set; } = Array.Empty<SlackAttachment>();
}

public sealed class SlackAttachment
{
    [System.Text.Json.Serialization.JsonPropertyName("color")]
    public string Color { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("text")]
    public string? Text { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("fields")]
    public SlackField[] Fields { get; set; } = Array.Empty<SlackField>();

    [System.Text.Json.Serialization.JsonPropertyName("ts")]
    public string Ts { get; set; } = string.Empty;
}

public sealed class SlackField
{
    [System.Text.Json.Serialization.JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("short")]
    public bool Short { get; set; }
}
