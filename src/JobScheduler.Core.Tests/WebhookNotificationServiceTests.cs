// SPDX-License-Identifier: MIT
// Tests for JobScheduler.Core.Services.WebhookNotificationService
// ---------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JobScheduler.Core.Tests;

public class WebhookNotificationServiceTests
{
    private static HttpClient CreateHttpClient(HttpResponseMessage response, Action<HttpRequestMessage>? requestCallback = null)
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) => requestCallback?.Invoke(req))
            .ReturnsAsync(response)
            .Verifiable();

        return new HttpClient(handlerMock.Object);
    }

    private static Mock<ILogger<WebhookNotificationService>> CreateLoggerMock()
    {
        return new Mock<ILogger<WebhookNotificationService>>();
    }

    private static Mock<CacheService> CreateCacheMock()
    {
        // CacheService is a concrete class in the source tree; we mock its virtual members.
        return new Mock<CacheService>(MockBehavior.Strict);
    }

    [Fact]
    public async Task SendExecutionNotificationAsync_ShouldReturnWhenConfigIsNull()
    {
        // Arrange
        var httpClient = new HttpClient(); // not used
        var logger = CreateLoggerMock().Object;
        var cache = new Mock<CacheService>(MockBehavior.Loose).Object; // not used
        var service = new WebhookNotificationService(httpClient, logger, cache);

        var job = new Job { Id = Guid.NewGuid(), Name = "TestJob" };
        var execution = new JobExecution { Id = Guid.NewGuid(), Status = ExecutionStatus.Completed };

        // Act (config is null)
        await service.SendExecutionNotificationAsync(job, execution, null!);

        // Assert – no exception means pass
    }

    [Fact]
    public async Task SendExecutionNotificationAsync_ShouldPostPayloadAndLogSuccess()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        HttpRequestMessage? capturedRequest = null;
        var httpClient = CreateHttpClient(response, req => capturedRequest = req);

        var loggerMock = CreateLoggerMock();
        var cacheMock = CreateCacheMock();

        var service = new WebhookNotificationService(httpClient, loggerMock.Object, cacheMock.Object);

        var job = new Job { Id = Guid.NewGuid(), Name = "DemoJob" };
        var execution = new JobExecution
        {
            Id = Guid.NewGuid(),
            Status = ExecutionStatus.Failed,
            ExecutionTimeMs = 1234,
            ErrorMessage = "boom",
            RetryAttempt = 2
        };
        var config = new WebhookConfig
        {
            JobId = job.Id,
            WebhookUrl = "https://example.com/webhook",
            Secret = "s3cr3t",
            MaxRetries = 3,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await service.SendExecutionNotificationAsync(job, execution, config);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest!.Method);
        Assert.Equal(config.WebhookUrl, capturedRequest.RequestUri!.ToString());

        var sentJson = await capturedRequest.Content!.ReadAsStringAsync();
        var payload = JsonSerializer.Deserialize<WebhookPayload>(sentJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        Assert.Equal("job.execution.completed", payload.EventType);
        Assert.Equal(job.Id, payload.JobId);
        Assert.Equal(job.Name, payload.JobName);
        Assert.Equal(execution.Id, payload.ExecutionId);
        Assert.Equal(execution.Status.ToString(), payload.Status);
        Assert.Equal(execution.ExecutionTimeMs, payload.ExecutionTimeMs);
        Assert.Equal(execution.ErrorMessage, payload.ErrorMessage);
        Assert.Equal(execution.RetryAttempt, payload.RetryAttempt);

        // Verify that a success log entry was written
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Webhook delivered successfully")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterWebhookAsync_ShouldValidateUrlAndStoreConfig()
    {
        // Arrange
        var httpClient = new HttpClient(); // not used
        var loggerMock = CreateLoggerMock();
        var cacheMock = new Mock<CacheService>(MockBehavior.Strict);
        cacheMock
            .Setup(c => c.SetAsync(
                It.Is<string>(k => k == $"webhook:job:{Guid.Empty}"),
                It.IsAny<WebhookConfig>(),
                It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var service = new WebhookNotificationService(httpClient, loggerMock.Object, cacheMock.Object);
        var jobId = Guid.Empty;
        var url = "https://hooks.example.com/notify";

        // Act
        await service.RegisterWebhookAsync(jobId, url, "secret");

        // Assert
        cacheMock.VerifyAll();

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Webhook registered")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UnregisterWebhookAsync_ShouldRemoveConfigFromCache()
    {
        // Arrange
        var httpClient = new HttpClient(); // not used
        var loggerMock = CreateLoggerMock();
        var cacheMock = new Mock<CacheService>(MockBehavior.Strict);
        var jobId = Guid.NewGuid();
        cacheMock
            .Setup(c => c.RemoveAsync($"webhook:job:{jobId}"))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var service = new WebhookNotificationService(httpClient, loggerMock.Object, cacheMock.Object);

        // Act
        await service.UnregisterWebhookAsync(jobId);

        // Assert
        cacheMock.VerifyAll();
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Webhook unregistered")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetWebhookConfigAsync_ShouldReturnStoredConfig()
    {
        // Arrange
        var httpClient = new HttpClient(); // not used
        var loggerMock = CreateLoggerMock();
        var cacheMock = new Mock<CacheService>(MockBehavior.Strict);
        var jobId = Guid.NewGuid();
        var expectedConfig = new WebhookConfig { JobId = jobId, WebhookUrl = "https://example.com", MaxRetries = 5 };
        cacheMock
            .Setup(c => c.GetAsync<WebhookConfig>($"webhook:job:{jobId}"))
            .ReturnsAsync(expectedConfig)
            .Verifiable();

        var service = new WebhookNotificationService(httpClient, loggerMock.Object, cacheMock.Object);

        // Act
        var result = await service.GetWebhookConfigAsync(jobId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedConfig.WebhookUrl, result!.WebhookUrl);
        cacheMock.VerifyAll();
    }

    [Fact]
    public async Task TestWebhookAsync_ShouldReturnSuccessWhenHttpOk()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        var httpClient = CreateHttpClient(response);
        var loggerMock = CreateLoggerMock();
        var cacheMock = new Mock<CacheService>(MockBehavior.Loose).Object;

        var service = new WebhookNotificationService(httpClient, loggerMock.Object, cacheMock);

        // Act
        var result = await service.TestWebhookAsync("https://example.com/webhook");

        // Assert
        Assert.True(result.Success);
        Assert.Equal((int)HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("Webhook is reachable", result.Message);
    }

    [Fact]
    public async Task TestWebhookAsync_ShouldReturnFailureWhenException()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("Network down"))
            .Verifiable();

        var httpClient = new HttpClient(handlerMock.Object);
        var loggerMock = CreateLoggerMock();
        var cacheMock = new Mock<CacheService>(MockBehavior.Loose).Object;

        var service = new WebhookNotificationService(httpClient, loggerMock.Object, cacheMock);

        // Act
        var result = await service.TestWebhookAsync("https://unreachable.local");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(0, result.StatusCode);
        Assert.Contains("Network down", result.Message);
        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }
}
