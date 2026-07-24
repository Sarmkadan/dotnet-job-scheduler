using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using JobScheduler.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace JobScheduler.Core.Tests
{
    public class ExternalApiClientTests
    {
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly HttpClient _httpClient;
        private readonly Mock<ILogger<ExternalApiClient>> _mockLogger;
        private readonly ExternalApiClient _client;

        public ExternalApiClientTests()
        {
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
            _mockLogger = new Mock<ILogger<ExternalApiClient>>();
            _client = new ExternalApiClient(_httpClient, _mockLogger.Object);
        }

        [Fact]
        public async Task GetAsync_ReturnsSuccess_WhenResponseIsSuccess()
        {
            // Arrange
            var url = "https://api.example.com/data";
            var responseData = new MyData { Id = 1, Name = "Test" };
            var json = JsonSerializer.Serialize(responseData);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    It.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            // Act
            var result = await _client.GetAsync<MyData>(url);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(responseData.Id, result.Data.Id);
            Assert.Equal(responseData.Name, result.Data.Name);
            Assert.Null(result.Error);
        }

        [Fact]
        public async Task GetAsync_ReturnsFailure_WhenResponseIsNotSuccess()
        {
            // Arrange
            var url = "https://api.example.com/data";
            var response = new HttpResponseMessage(HttpStatusCode.NotFound);

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    It.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            // Act
            var result = await _client.GetAsync<MyData>(url);

            // Assert
            Assert.False(result.Success);
            Assert.Null(result.Data);
            Assert.Equal("HTTP NotFound", result.Error);
        }

        [Fact]
        public async Task GetAsync_ReturnsFailure_OnTimeout()
        {
            // Arrange
            var url = "https://api.example.com/data";

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    It.IsAny<CancellationToken>())
                .ThrowsAsync(new OperationCanceledException());

            // Act
            var result = await _client.GetAsync<MyData>(url);

            // Assert
            Assert.False(result.Success);
            Assert.Null(result.Data);
            Assert.Equal("Request timeout", result.Error);
        }

        [Fact]
        public async Task PostAsync_ReturnsSuccess_WhenResponseIsSuccess()
        {
            // Arrange
            var url = "https://api.example.com/data";
            var requestData = new MyData { Name = "Test" };
            var responseData = new MyData { Id = 1, Name = "Test" };
            var json = JsonSerializer.Serialize(responseData);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    It.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            // Act
            var result = await _client.PostAsync<MyData, MyData>(url, requestData);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(responseData.Id, result.Data.Id);
            Assert.Equal(responseData.Name, result.Data.Name);
            Assert.Null(result.Error);
        }

        [Fact]
        public async Task PostAsync_ReturnsFailure_WhenResponseIsNotSuccess()
        {
            // Arrange
            var url = "https://api.example.com/data";
            var requestData = new MyData { Name = "Test" };
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest);

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    It.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            // Act
            var result = await _client.PostAsync<MyData, MyData>(url, requestData);

            // Assert
            Assert.False(result.Success);
            Assert.Null(result.Data);
            Assert.Equal("HTTP BadRequest", result.Error);
        }

        [Fact]
        public async Task PutAsync_ReturnsSuccess_WhenResponseIsSuccess()
        {
            // Arrange
            var url = "https://api.example.com/data";
            var requestData = new MyData { Name = "Updated" };
            var responseData = new MyData { Id = 1, Name = "Updated" };
            var json = JsonSerializer.Serialize(responseData);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    It.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            // Act
            var result = await _client.PutAsync<MyData, MyData>(url, requestData);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(responseData.Id, result.Data.Id);
            Assert.Equal(responseData.Name, result.Data.Name);
            Assert.Null(result.Error);
        }

        [Fact]
        public async Task DeleteAsync_ReturnsSuccess_WhenResponseIsSuccess()
        {
            // Arrange
            var url = "https://api.example.com/data/1";
            var response = new HttpResponseMessage(HttpStatusCode.OK);

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    It.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            // Act
            var result = await _client.DeleteAsync(url);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.Data);
            Assert.Null(result.Error);
        }

        [Fact]
        public async Task GetWithRetryAsync_ReturnsSuccess_OnFirstAttempt()
        {
            // Arrange
            var url = "https://api.example.com/data";
            var responseData = new { Id = 1 };
            var json = JsonSerializer.Serialize(responseData);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    It.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            // Act
            var result = await _client.GetWithRetryAsync<MyData>(url, maxRetries: 3);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(responseData.Id, result.Data.Id);
            Assert.Null(result.Error);
        }

        [Fact]
        public async Task GetWithRetryAsync_ReturnsSuccess_AfterRetry()
        {
            // Arrange
            var url = "https://api.example.com/data";
            var responseData = new { Id = 1 };
            var json = JsonSerializer.Serialize(responseData);

            // First two attempts fail, third succeeds
            var failResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            var successResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            _mockHttpMessageHandler
                .Protected()
                .SetupSequence<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    It.IsAny<CancellationToken>())
                .ReturnsAsync(failResponse)
                .ReturnsAsync(failResponse)
                .ReturnsAsync(successResponse);

            // Act
            var result = await _client.GetWithRetryAsync<MyData>(url, maxRetries: 3);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(responseData.Id, result.Data.Id);
            Assert.Null(result.Error);
        }

        [Fact]
        public async Task GetWithRetryAsync_ReturnsFailure_AfterMaxRetries()
        {
            // Arrange
            var url = "https://api.example.com/data";
            var failResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError);

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    It.IsAny<CancellationToken>())
                .ReturnsAsync(failResponse);

            // Act
            var result = await _client.GetWithRetryAsync<MyData>(url, maxRetries: 2);

            // Assert
            Assert.False(result.Success);
            Assert.Null(result.Data);
            Assert.Equal("Max retries exceeded", result.Error);
        }

        [Fact]
        public async Task IsApiAvailableAsync_ReturnsTrue_WhenApiIsAvailable()
        {
            // Arrange
            var url = "https://api.example.com/health";
            var response = new HttpResponseMessage(HttpStatusCode.OK);

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    It.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            // Act
            var result = await _client.IsApiAvailableAsync(url);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task IsApiAvailableAsync_ReturnsFalse_WhenApiIsNotAvailable()
        {
            // Arrange
            var url = "https://api.example.com/health";
            var response = new HttpResponseMessage(HttpStatusCode.NotFound);

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    It.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            // Act
            var result = await _client.IsApiAvailableAsync(url);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task IsApiAvailableAsync_ReturnsFalse_OnException()
        {
            // Arrange
            var url = "https://api.example.com/health";

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    It.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException());

            // Act
            var result = await _client.IsApiAvailableAsync(url);

            // Assert
            Assert.False(result);
        }

        private class MyData
        {
            public int Id { get; set; }
            public string Name { get; set; } = default!;
        }
    }
}