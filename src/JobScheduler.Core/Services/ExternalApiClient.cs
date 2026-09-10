#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace JobScheduler.Core.Services;

/// <summary>
/// Generic HTTP client for calling external APIs.
/// Provides retry logic, timeout management, and error handling.
/// WHY: Centralized API client ensures consistent handling of external calls and retries.
/// </summary>
public sealed class ExternalApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExternalApiClient> _logger;

    /// <summary>
    /// Default timeout in seconds for HTTP requests.
    /// </summary>
    public const int DefaultTimeoutSeconds = 30;

    /// <summary>
    /// Default timeout in seconds for API availability checks.
    /// </summary>
    private const int AvailabilityTimeoutSeconds = 5;

    /// <summary>
    /// Bearer token scheme for authorization headers.
    /// </summary>
    private const string BearerScheme = "Bearer";

    /// <summary>
    /// JSON content type for HTTP requests.
    /// </summary>
    private const string JsonContentType = "application/json";

    /// <summary>
    /// Default error message for request timeouts.
    /// </summary>
    private const string RequestTimeoutMessage = "Request timeout";

    /// <summary>
    /// Default error message when max retries are exceeded.
    /// </summary>
    private const string MaxRetriesExceededMessage = "Max retries exceeded";

    public ExternalApiClient(HttpClient httpClient, ILogger<ExternalApiClient> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(logger);
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Makes a GET request to an external API.
    /// Includes timeout and error handling.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="url"/> is null or whitespace.</exception>
    public async Task<ApiResponse<T>> GetAsync<T>(string url, string? authToken = null, int timeoutSeconds = DefaultTimeoutSeconds) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);

            if (!string.IsNullOrEmpty(authToken))
                request.Headers.Authorization = new AuthenticationHeaderValue(BearerScheme, authToken);

            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
            {
                using var response = await _httpClient.SendAsync(request, cts.Token);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize<T>(content);
                    return new ApiResponse<T>(data, true);
                }
                else
                {
                    _logger.LogWarning("GET request failed to {Url} with status {StatusCode}", url, response.StatusCode);
                    return new ApiResponse<T>(null, false, $"HTTP {response.StatusCode}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("GET request timed out to {Url}", url);
            return new ApiResponse<T>(null, false, RequestTimeoutMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error making GET request to {Url}", url);
            return new ApiResponse<T>(null, false, ex.Message);
        }
    }

    /// <summary>
    /// Makes a POST request to an external API with JSON body.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="url"/> is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="data"/> is null.</exception>
    public async Task<ApiResponse<TResponse>> PostAsync<TRequest, TResponse>(
        string url, TRequest data, string? authToken = null, int timeoutSeconds = DefaultTimeoutSeconds)
        where TRequest : class
        where TResponse : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentNullException.ThrowIfNull(data);
        try
        {
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, System.Text.Encoding.UTF8, JsonContentType);

            using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };

            if (!string.IsNullOrEmpty(authToken))
                request.Headers.Authorization = new AuthenticationHeaderValue(BearerScheme, authToken);

            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
            {
                using var response = await _httpClient.SendAsync(request, cts.Token);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<TResponse>(responseContent);
                    return new ApiResponse<TResponse>(result, true);
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("POST request failed to {Url} with status {StatusCode}: {Error}",
                        url, response.StatusCode, error);
                    return new ApiResponse<TResponse>(null, false, $"HTTP {response.StatusCode}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("POST request timed out to {Url}", url);
            return new ApiResponse<TResponse>(null, false, RequestTimeoutMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error making POST request to {Url}", url);
            return new ApiResponse<TResponse>(null, false, ex.Message);
        }
    }

    /// <summary>
    /// Makes a PUT request to an external API.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="url"/> is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="data"/> is null.</exception>
    public async Task<ApiResponse<TResponse>> PutAsync<TRequest, TResponse>(
        string url, TRequest data, string? authToken = null, int timeoutSeconds = DefaultTimeoutSeconds)
        where TRequest : class
        where TResponse : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentNullException.ThrowIfNull(data);
        try
        {
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, System.Text.Encoding.UTF8, JsonContentType);

            using var request = new HttpRequestMessage(HttpMethod.Put, url) { Content = content };

            if (!string.IsNullOrEmpty(authToken))
                request.Headers.Authorization = new AuthenticationHeaderValue(BearerScheme, authToken);

            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
            {
                using var response = await _httpClient.SendAsync(request, cts.Token);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<TResponse>(responseContent);
                    return new ApiResponse<TResponse>(result, true);
                }
                else
                {
                    _logger.LogWarning("PUT request failed to {Url} with status {StatusCode}", url, response.StatusCode);
                    return new ApiResponse<TResponse>(null, false, $"HTTP {response.StatusCode}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("PUT request timed out to {Url}", url);
            return new ApiResponse<TResponse>(null, false, RequestTimeoutMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error making PUT request to {Url}", url);
            return new ApiResponse<TResponse>(null, false, ex.Message);
        }
    }

    /// <summary>
    /// Makes a DELETE request to an external API.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="url"/> is null or whitespace.</exception>
    public async Task<ApiResponse<bool>> DeleteAsync(string url, string? authToken = null, int timeoutSeconds = DefaultTimeoutSeconds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, url);

            if (!string.IsNullOrEmpty(authToken))
                request.Headers.Authorization = new AuthenticationHeaderValue(BearerScheme, authToken);

            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
            {
                using var response = await _httpClient.SendAsync(request, cts.Token);

                if (response.IsSuccessStatusCode)
                {
                    return new ApiResponse<bool>(true, true);
                }
                else
                {
                    _logger.LogWarning("DELETE request failed to {Url} with status {StatusCode}", url, response.StatusCode);
                    return new ApiResponse<bool>(false, false, $"HTTP {response.StatusCode}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("DELETE request timed out to {Url}", url);
            return new ApiResponse<bool>(false, false, "Request timeout");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error making DELETE request to {Url}", url);
            return new ApiResponse<bool>(false, false, ex.Message);
        }
    }

    /// <summary>
    /// Makes a request with automatic retry on transient failures.
    /// WHY: Network failures are often temporary; retries improve reliability.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="url"/> is null or whitespace.</exception>
    public async Task<ApiResponse<T>> GetWithRetryAsync<T>(
        string url, int maxRetries = 3, string? authToken = null) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            var response = await GetAsync<T>(url, authToken);

            if (response.Success)
                return response;

            if (attempt < maxRetries - 1)
            {
                var backoffMs = (int)Math.Pow(2, attempt) * 1000; // Exponential backoff
                await Task.Delay(backoffMs);
            }
        }

        return new ApiResponse<T>(null, false, MaxRetriesExceededMessage);
    }

    /// <summary>
    /// Checks if an external API endpoint is reachable.
    /// Useful for health checks and connectivity monitoring.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="url"/> is null or whitespace.</exception>
    public async Task<bool> IsApiAvailableAsync(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        try
        {
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(AvailabilityTimeoutSeconds)))
            {
                var response = await _httpClient.GetAsync(url, cts.Token);
                return response.IsSuccessStatusCode;
            }
        }
        catch
        {
            return false;
        }
    }
}

public sealed class ApiResponse<T>
{
    public T? Data { get; }
    public bool Success { get; }
    public string? Error { get; }

    public ApiResponse(T? data, bool success, string? error = null)
    {
        Data = data;
        Success = success;
        Error = error;
    }
}
