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
    public async Task SendJobFailureNotificationAsync(Job job, JobExecution execution, string webhookUrl)
    {
        if (string.IsNullOrEmpty(webhookUrl))
            return;

        var color = execution.RetryAttempt < job.MaxRetries ? "warning" : "danger";
        var message = new SlackMessage
        {
            Text = $"Job {job.Name} execution failed",
            Attachments = new[]
            {
                new SlackAttachment
                {
                    Color = color,
                    Title = $"{job.Name} - Execution Failed",
                    Fields = new[]
                    {
                        new SlackField { Title = "Job", Value = job.Name, Short = true },
                        new SlackField { Title = "Status", Value = "Failed", Short = true },
                        new SlackField { Title = "Execution Time", Value = $"{execution.ExecutionTimeMs}ms", Short = true },
                        new SlackField { Title = "Retry Attempt", Value = $"{execution.RetryAttempt}/{job.MaxRetries}", Short = true },
                        new SlackField { Title = "Error", Value = execution.ErrorMessage ?? "No error details", Short = false }
                    },
                    Ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()
                }
            }
        };

        await SendSlackMessageAsync(message, webhookUrl);
    }

    /// <summary>
    /// Sends job execution success notification to Slack.
    /// Includes execution time and performance metrics.
    /// </summary>
    public async Task SendJobSuccessNotificationAsync(Job job, JobExecution execution, string webhookUrl)
    {
        if (string.IsNullOrEmpty(webhookUrl))
            return;

        var message = new SlackMessage
        {
            Text = $"Job {job.Name} executed successfully",
            Attachments = new[]
            {
                new SlackAttachment
                {
                    Color = "good",
                    Title = $"{job.Name} - Execution Successful",
                    Fields = new[]
                    {
                        new SlackField { Title = "Job", Value = job.Name, Short = true },
                        new SlackField { Title = "Status", Value = "Completed", Short = true },
                        new SlackField { Title = "Execution Time", Value = $"{execution.ExecutionTimeMs}ms", Short = true },
                        new SlackField { Title = "Success Rate", Value = $"{job.GetSuccessRate():F1}%", Short = true }
                    },
                    Ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()
                }
            }
        };

        await SendSlackMessageAsync(message, webhookUrl);
    }

    /// <summary>
    /// Sends alert for critical scheduler events.
    /// </summary>
    public async Task SendSchedulerAlertAsync(string title, string message, string severity, string webhookUrl)
    {
        if (string.IsNullOrEmpty(webhookUrl))
            return;

        var color = severity switch
        {
            "Critical" => "danger",
            "Warning" => "warning",
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

        await SendSlackMessageAsync(slackMessage, webhookUrl);
    }

    private async Task SendSlackMessageAsync(SlackMessage message, string webhookUrl)
    {
        try
        {
            var json = JsonSerializer.Serialize(message);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
            {
                var response = await _httpClient.PostAsync(webhookUrl, content, cts.Token);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogDebug("Slack notification sent successfully");
                }
                else
                {
                    _logger.LogWarning("Failed to send Slack notification: {StatusCode}", response.StatusCode);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending Slack notification");
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
