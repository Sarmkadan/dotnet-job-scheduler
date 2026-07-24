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
        private readonly Mock<HttpMessageHandler> _handlerMock;
        private readonly HttpClient _httpClient;
        private readonly Mock<ILogger<ExternalApiClient>> _loggerMock;
        private readonly ExternalApiClient _client;

        public ExternalApiClientTests()
        {
            _handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            _httpClient = new HttpClient(_handlerMock.Object);
            _loggerMock = new Mock<ILogger<ExternalApiClient>>();
            _client = new ExternalApiClient(_httpClient, _loggerMock.Object);
        }

        private void SetupHandler(HttpResponseMessage response, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? func = null)
        {
            var setup = _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>());

            if (func != null)
                setup.Returns(func);
            else
                setup.ReturnsAsync(response);
        }

        [Fact]
        public async Task GetAsync_ReturnsSuccess_WhenResponseIsOk()
        {
            var url = "https://api.example.com/data";
            var expected = new MyData { Id = 1, Name = "Test" };
            var json = JsonSerializer.Serialize(expected);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            SetupHandler(response);

            var result = await _client.GetAsync<MyData>(url);

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(expected.Id, result.Data!.Id);
            Assert.Equal(expected.Name, result.Data.Name);
            Assert.Null(result.Error);
        }

        [Fact]
        public async Task GetAsync_ReturnsFailure_WhenStatusNotSuccess()
        {
            var url = "https://api.example.com/data";
            var response = new HttpResponseMessage(HttpStatusCode.NotFound);
            SetupHandler(response);

            var result = await _client.GetAsync<MyData>(url);

            Assert.False(result.Success);
            Assert.Null(result.Data);
            Assert.Equal("HTTP NotFound", result.Error);
        }

        [Fact]
        public async Task GetAsync_ReturnsFailure_OnTimeout()
        {
            var url = "https://api.example.com/data";
            SetupHandler(null, (req, ct) => throw new OperationCanceledException());

            var result = await _client.GetAsync<MyData>(url);

            Assert.False(result.Success);
            Assert.Null(result.Data);
            Assert.Equal("Request timeout", result.Error);
        }

        [Fact]
        public async Task PostAsync_ReturnsSuccess_WhenResponseIsOk()
        {
            var url = "https://api.example.com/data";
            var request = new MyData { Name = "Req" };
            var responseData = new MyData { Id = 2, Name = "Resp" };
            var json = JsonSerializer.Serialize(responseData);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            SetupHandler(response);

            var result = await _client.PostAsync<MyData, MyData>(url, request);

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(responseData.Id, result.Data!.Id);
            Assert.Equal(responseData.Name, result.Data.Name);
            Assert.Null(result.Error);
        }

        [Fact]
        public async Task PostAsync_ReturnsFailure_WhenStatusNotSuccess()
        {
            var url = "https://api.example.com/data";
            var request = new MyData { Name = "Req" };
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            SetupHandler(response);

            var result = await _client.PostAsync<MyData, MyData>(url, request);

            Assert.False(result.Success);
            Assert.Null(result.Data);
            Assert.Equal("HTTP BadRequest", result.Error);
        }

        [Fact]
        public async Task PutAsync_ReturnsSuccess_WhenResponseIsOk()
        {
            var url = "https://api.example.com/data";
            var request = new MyData { Name = "Update" };
            var responseData = new MyData { Id = 3, Name = "Update" };
            var json = JsonSerializer.Serialize(responseData);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            SetupHandler(response);

            var result = await _client.PutAsync<MyData, MyData>(url, request);

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(responseData.Id, result.Data!.Id);
            Assert.Equal(responseData.Name, result.Data.Name);
            Assert.Null(result.Error);
        }

        [Fact]
        public async Task DeleteAsync_ReturnsSuccess_WhenResponseIsOk()
        {
            var url = "https://api.example.com/data/1";
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            SetupHandler(response);

            var result = await _client.DeleteAsync(url);

            Assert.True(result.Success);
            Assert.True(result.Data);
            Assert.Null(result.Error);
        }

        [Fact]
        public async Task GetWithRetryAsync_ReturnsSuccess_OnFirstAttempt()
        {
            var url = "https://api.example.com/data";
            var expected = new MyData { Id = 4, Name = "Retry" };
            var json = JsonSerializer.Serialize(expected);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            SetupHandler(response);

            var result = await _client.GetWithRetryAsync<MyData>(url, maxRetries: 3);

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(expected.Id, result.Data!.Id);
            Assert.Equal(expected.Name, result.Data.Name);
            Assert.Null(result.Error);
        }

        [Fact]
        public async Task GetWithRetryAsync_ReturnsSuccess_AfterRetry()
        {
            var url = "https://api.example.com/data";
            var expected = new MyData { Id = 5, Name = "RetryLater" };
            var json = JsonSerializer.Serialize(expected);
            var failResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            var successResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            _handlerMock
                .Protected()
                .SetupSequence<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(failResponse)
                .ReturnsAsync(failResponse)
                .ReturnsAsync(successResponse);

            var result = await _client.GetWithRetryAsync<MyData>(url, maxRetries: 3);

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(expected.Id, result.Data!.Id);
            Assert.Equal(expected.Name, result.Data.Name);
            Assert.Null(result.Error);
        }

        [Fact]
        public async Task GetWithRetryAsync_ReturnsFailure_AfterMaxRetries()
        {
            var url = "https://api.example.com/data";
            var failResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            SetupHandler(failResponse);

            var result = await _client.GetWithRetryAsync<MyData>(url, maxRetries: 2);

            Assert.False(result.Success);
            Assert.Null(result.Data);
            Assert.Equal("Max retries exceeded", result.Error);
        }

        [Fact]
        public async Task IsApiAvailableAsync_ReturnsTrue_WhenSuccess()
        {
            var url = "https://api.example.com/health";
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            SetupHandler(response);

            var result = await _client.IsApiAvailableAsync(url);

            Assert.True(result);
        }

        [Fact]
        public async Task IsApiAvailableAsync_ReturnsFalse_WhenNotSuccess()
        {
            var url = "https://api.example.com/health";
            var response = new HttpResponseMessage(HttpStatusCode.NotFound);
            SetupHandler(response);

            var result = await _client.IsApiAvailableAsync(url);

            Assert.False(result);
        }

        [Fact]
        public async Task IsApiAvailableAsync_ReturnsFalse_OnException()
        {
            var url = "https://api.example.com/health";
            SetupHandler(null, (req, ct) => throw new HttpRequestException());

            var result = await _client.IsApiAvailableAsync(url);

            Assert.False(result);
        }

        private class MyData
        {
            public int Id { get; set; }
            public string Name { get; set; } = default!;
        }
    }
}
