// SPDX-License-Identifier: MIT
// Tests for JobScheduler.Core.Services.WebhookNotificationService
// ---------------------------------------------------------------

using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using JobScheduler.Core.Domain.Entities;
using JobScheduler.Core.Services;
using JobScheduler.Core.Constants;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace JobScheduler.Core.Tests;

public class WebhookNotificationServiceTests
{
    // -----------------------------------------------------------------
    // Helper HttpMessageHandler that captures the outgoing request.
    // -----------------------------------------------------------------
    private sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage? CapturedRequest { get; private set; }
        public HttpResponseMessage? Response { get; set; }
        public bool ThrowException { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedRequest = request;
            if (ThrowException)
            {
                throw new InvalidOperationException("Simulated exception");
            }
            var response = Response ?? new HttpResponseMessage(HttpStatusCode.OK);
            return Task.FromResult(response);
        }
    }

    // -----------------------------------------------------------------
    // Helper to deserialize the webhook payload for assertions.
    // -----------------------------------------------------------------
    private static WebhookPayload DeserializePayload(HttpContent content)
    {
        var json = content.ReadAsStringAsync().Result;
        return JsonSerializer.Deserialize<WebhookPayload>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })!;
    }

    [Fact]
    public async Task SendExecutionNotificationAsync_ShouldThrowWhenConfigIsNull()
    {
        // Arrange
        var httpClient = new HttpClient(); // not used
        var logger = NullLogger<WebhookNotificationService>.Instance;
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cacheService = new CacheService(memoryCache, NullLogger<CacheService>.Instance);
        var service = new WebhookNotificationService(httpClient, logger, cacheService);

        var job = new Job { Id = Guid.NewGuid(), Name = "TestJob" };
        var execution = new JobExecution { Id = Guid.NewGuid(), Status = ExecutionStatus.Success };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.SendExecutionNotificationAsync(job, execution, null!));
    }

    [Fact]
    public async Task SendExecutionNotificationAsync_ShouldThrowWhenWebhookUrlIsNullOrEmpty()
    {
        // Arrange
        var httpClient = new HttpClient(); // not used
        var logger = NullLogger<WebhookNotificationService>.Instance;
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cacheService = new CacheService(memoryCache, NullLogger<CacheService>.Instance);
        var service = new WebhookNotificationService(httpClient, logger, cacheService);

        var job = new Job { Id = Guid.NewGuid(), Name = "TestJob" };
        var execution = new JobExecution { Id = Guid.NewGuid(), Status = ExecutionStatus.Success };
        var config = new WebhookConfig { JobId = job.Id, WebhookUrl = null! };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SendExecutionNotificationAsync(job, execution, config));
    }

    [Fact]
    public async Task SendExecutionNotificationAsync_ShouldThrowWhenWebhookUrlIsEmpty()
    {
        // Arrange
        var httpClient = new HttpClient(); // not used
        var logger = NullLogger<WebhookNotificationService>.Instance;
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cacheService = new CacheService(memoryCache, NullLogger<CacheService>.Instance);
        var service = new WebhookNotificationService(httpClient, logger, cacheService);

        var job = new Job { Id = Guid.NewGuid(), Name = "TestJob" };
        var execution = new JobExecution { Id = Guid.NewGuid(), Status = ExecutionStatus.Success };
        var config = new WebhookConfig { JobId = job.Id, WebhookUrl = string.Empty };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SendExecutionNotificationAsync(job, execution, config));
    }

    [Fact]
    public async Task SendExecutionNotificationAsync_ShouldPostPayloadAndLogSuccess()
    {
        // Arrange
        var handler = new TestHttpMessageHandler();
        var httpClient = new HttpClient(handler);
        var logger = NullLogger<WebhookNotificationService>.Instance;
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cacheService = new CacheService(memoryCache, NullLogger<CacheService>.Instance);
        var service = new WebhookNotificationService(httpClient, logger, cacheService);

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
        Assert.NotNull(handler.CapturedRequest);
        Assert.Equal(HttpMethod.Post, handler.CapturedRequest!.Method);
        Assert.Equal(config.WebhookUrl, handler.CapturedRequest.RequestUri!.ToString());

        var sentJson = await handler.CapturedRequest.Content!.ReadAsStringAsync();
        var payload = DeserializePayload(handler.CapturedRequest.Content);
        Assert.Equal("job.execution.completed", payload.EventType);
        Assert.Equal(job.Id, payload.JobId);
        Assert.Equal(job.Name, payload.JobName);
        Assert.Equal(execution.Id, payload.ExecutionId);
        Assert.Equal(execution.Status.ToString(), payload.Status);
        Assert.Equal(execution.ExecutionTimeMs, payload.ExecutionTimeMs);
        Assert.Equal(execution.ErrorMessage, payload.ErrorMessage);
        Assert.Equal(execution.RetryAttempt, payload.RetryAttempt);
    }

    [Fact]
    public async Task RegisterWebhookAsync_ShouldValidateUrlAndStoreConfig()
    {
        // Arrange
        var httpClient = new HttpClient(); // not used
        var logger = NullLogger<WebhookNotificationService>.Instance;
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cacheService = new CacheService(memoryCache, NullLogger<CacheService>.Instance);
        var service = new WebhookNotificationService(httpClient, logger, cacheService);
        var jobId = Guid.Empty;
        var url = "https://hooks.example.com/notify";

        // Act
        await service.RegisterWebhookAsync(jobId, url, "secret");

        // Assert - Verify that an entry was added to the cache
        // Since we can't directly verify the internal _keys field, we'll check that GetAsync returns something
        var result = await cacheService.GetAsync<WebhookConfig>($"webhook:job:{jobId}");
        Assert.NotNull(result);
        Assert.Equal(url, result!.WebhookUrl);
    }

    [Fact]
    public async Task UnregisterWebhookAsync_ShouldRemoveConfigFromCache()
    {
        // Arrange
        var httpClient = new HttpClient(); // not used
        var logger = NullLogger<WebhookNotificationService>.Instance;
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cacheService = new CacheService(memoryCache, NullLogger<CacheService>.Instance);
        var service = new WebhookNotificationService(httpClient, logger, cacheService);
        var jobId = Guid.NewGuid();
        var url = "https://example.com/webhook";

        // First register a webhook so we have something to unregister
        await service.RegisterWebhookAsync(jobId, url, "secret");

        // Act
        await service.UnregisterWebhookAsync(jobId);

        // Assert - Verify that the entry was removed from the cache
        var result = await cacheService.GetAsync<WebhookConfig>($"webhook:job:{jobId}");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetWebhookConfigAsync_ShouldReturnStoredConfig()
    {
        // Arrange
        var httpClient = new HttpClient(); // not used
        var logger = NullLogger<WebhookNotificationService>.Instance;
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cacheService = new CacheService(memoryCache, NullLogger<CacheService>.Instance);
        var jobId = Guid.NewGuid();
        var expectedConfig = new WebhookConfig { JobId = jobId, WebhookUrl = "https://example.com", MaxRetries = 5 };

        // First store the config in the cache
        await cacheService.SetAsync($"webhook:job:{jobId}", expectedConfig);

        var service = new WebhookNotificationService(httpClient, logger, cacheService);

        // Act
        var result = await service.GetWebhookConfigAsync(jobId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedConfig.WebhookUrl, result!.WebhookUrl);
    }

    [Fact]
    public async Task TestWebhookAsync_ShouldReturnSuccessWhenHttpOk()
    {
        // Arrange
        var handler = new TestHttpMessageHandler();
        handler.Response = new HttpResponseMessage(HttpStatusCode.OK);
        var httpClient = new HttpClient(handler);
        var logger = NullLogger<WebhookNotificationService>.Instance;
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cacheService = new CacheService(memoryCache, NullLogger<CacheService>.Instance);
        var service = new WebhookNotificationService(httpClient, logger, cacheService);

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
        var handler = new TestHttpMessageHandler { ThrowException = true };
        var httpClient = new HttpClient(handler);
        var logger = NullLogger<WebhookNotificationService>.Instance;
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cacheService = new CacheService(memoryCache, NullLogger<CacheService>.Instance);
        var service = new WebhookNotificationService(httpClient, logger, cacheService);

        // Act
        var result = await service.TestWebhookAsync("https://unreachable.local");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(0, result.StatusCode);
        Assert.Contains("Error:", result.Message);
    }
}