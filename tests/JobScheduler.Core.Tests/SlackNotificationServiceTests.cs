// SPDX-License-Identifier: MIT
// tests for SlackNotificationService
// -------------------------------------------------------------

using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace JobScheduler.Core.Tests;

public sealed class SlackNotificationServiceTests
{
    // -----------------------------------------------------------------
    // Helper HttpMessageHandler that captures the outgoing request.
    // -----------------------------------------------------------------
    private sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage? CapturedRequest { get; private set; }
        public HttpResponseMessage? Response { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedRequest = request;
            var response = Response ?? new HttpResponseMessage(HttpStatusCode.OK);
            return Task.FromResult(response);
        }
    }

    // -----------------------------------------------------------------
    // Helper to deserialize the Slack payload for assertions.
    // -----------------------------------------------------------------
    private static SlackMessage DeserializePayload(HttpContent content)
    {
        var json = content.ReadAsStringAsync().Result;
        return JsonSerializer.Deserialize<SlackMessage>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })!;
    }

    // -----------------------------------------------------------------
    // Happy path – failure notification (retry < max => warning colour)
    // -----------------------------------------------------------------
    [Fact]
    public async Task SendJobFailureNotificationAsync_HappyPath_SendsWarningColour()
    {
        // Arrange
        var job = new Job
        {
            Name = "TestJob",
            MaxRetries = 3
        };
        var execution = new JobExecution
        {
            RetryAttempt = 1,
            ExecutionTimeMs = 150,
            ErrorMessage = "boom"
        };
        var handler = new TestHttpMessageHandler();
        var httpClient = new HttpClient(handler);
        var service = new SlackNotificationService(httpClient, NullLogger<SlackNotificationService>.Instance);
        var webhookUrl = "https://example.com/webhook";

        // Act
        await service.SendJobFailureNotificationAsync(job, execution, webhookUrl);

        // Assert
        Assert.NotNull(handler.CapturedRequest);
        var payload = DeserializePayload(handler.CapturedRequest!.Content);
        Assert.Equal($"Job {job.Name} execution failed", payload.Text);
        Assert.Single(payload.Attachments);
        var attachment = payload.Attachments[0];
        Assert.Equal("warning", attachment.Color);
        Assert.Contains(attachment.Fields, f => f.Title == "Job" && f.Value == job.Name);
        Assert.Contains(attachment.Fields, f => f.Title == "Error" && f.Value == execution.ErrorMessage);
    }

    // -----------------------------------------------------------------
    // Edge case – empty webhook URL results in no HTTP call.
    // -----------------------------------------------------------------
    [Fact]
    public async Task SendJobFailureNotificationAsync_EmptyWebhook_DoesNotSend()
    {
        // Arrange
        var job = new Job { Name = "Job", MaxRetries = 1 };
        var execution = new JobExecution { RetryAttempt = 0 };
        var handler = new TestHttpMessageHandler();
        var service = new SlackNotificationService(new HttpClient(handler), NullLogger<SlackNotificationService>.Instance);

        // Act
        await service.SendJobFailureNotificationAsync(job, execution, string.Empty);

        // Assert
        Assert.Null(handler.CapturedRequest);
    }

    // -----------------------------------------------------------------
    // Happy path – success notification (colour good, includes success rate).
    // -----------------------------------------------------------------
    [Fact]
    public async Task SendJobSuccessNotificationAsync_HappyPath_SendsGoodColour()
    {
        // Arrange
        var job = new Job
        {
            Name = "SuccessJob",
            MaxRetries = 2
        };
        // Assume GetSuccessRate returns a deterministic value; if not, the method
        // will compute based on internal state – we only verify that the field exists.
        var execution = new JobExecution { ExecutionTimeMs = 200 };
        var handler = new TestHttpMessageHandler();
        var service = new SlackNotificationService(new HttpClient(handler), NullLogger<SlackNotificationService>.Instance);
        var webhookUrl = "https://example.com/webhook";

        // Act
        await service.SendJobSuccessNotificationAsync(job, execution, webhookUrl);

        // Assert
        Assert.NotNull(handler.CapturedRequest);
        var payload = DeserializePayload(handler.CapturedRequest!.Content);
        Assert.Equal($"Job {job.Name} executed successfully", payload.Text);
        var attachment = payload.Attachments[0];
        Assert.Equal("good", attachment.Color);
        Assert.Contains(attachment.Fields, f => f.Title == "Status" && f.Value == "Completed");
        Assert.Contains(attachment.Fields, f => f.Title == "Success Rate");
    }

    // -----------------------------------------------------------------
    // Scheduler alert – critical severity maps to danger colour.
    // -----------------------------------------------------------------
    [Fact]
    public async Task SendSchedulerAlertAsync_CriticalSeverity_MapsToDangerColour()
    {
        // Arrange
        var handler = new TestHttpMessageHandler();
        var service = new SlackNotificationService(new HttpClient(handler), NullLogger<SlackNotificationService>.Instance);
        var webhookUrl = "https://example.com/webhook";

        // Act
        await service.SendSchedulerAlertAsync("AlertTitle", "Something bad happened", "Critical", webhookUrl);

        // Assert
        Assert.NotNull(handler.CapturedRequest);
        var payload = DeserializePayload(handler.CapturedRequest!.Content);
        var attachment = payload.Attachments[0];
        Assert.Equal("danger", attachment.Color);
        Assert.Equal("AlertTitle", attachment.Title);
        Assert.Equal("Something bad happened", attachment.Text);
    }

    // -----------------------------------------------------------------
    // Error path – non‑success HTTP response does not throw.
    // -----------------------------------------------------------------
    [Fact]
    public async Task SendJobFailureNotificationAsync_NonSuccessResponse_DoesNotThrow()
    {
        // Arrange
        var job = new Job { Name = "Job", MaxRetries = 1 };
        var execution = new JobExecution { RetryAttempt = 0 };
        var handler = new TestHttpMessageHandler
        {
            Response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        };
        var service = new SlackNotificationService(new HttpClient(handler), NullLogger<SlackNotificationService>.Instance);
        var webhookUrl = "https://example.com/webhook";

        // Act & Assert (no exception should bubble up)
        var exception = await Record.ExceptionAsync(() =>
            service.SendJobFailureNotificationAsync(job, execution, webhookUrl));

        Assert.Null(exception);
        Assert.NotNull(handler.CapturedRequest);
    }
}
