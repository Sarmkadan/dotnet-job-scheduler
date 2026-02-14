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
public class ExternalApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExternalApiClient> _logger;

    public ExternalApiClient(HttpClient httpClient, ILogger<ExternalApiClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Makes a GET request to an external API.
    /// Includes timeout and error handling.
    /// </summary>
    public async Task<ApiResponse<T>> GetAsync<T>(string url, string? authToken = null, int timeoutSeconds = 30) where T : class
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            if (!string.IsNullOrEmpty(authToken))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
            {
                var response = await _httpClient.SendAsync(request, cts.Token);

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
            return new ApiResponse<T>(null, false, "Request timeout");
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
    public async Task<ApiResponse<TResponse>> PostAsync<TRequest, TResponse>(
        string url, TRequest data, string? authToken = null, int timeoutSeconds = 30)
        where TRequest : class
        where TResponse : class
    {
        try
        {
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };

            if (!string.IsNullOrEmpty(authToken))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
            {
                var response = await _httpClient.SendAsync(request, cts.Token);

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
            return new ApiResponse<TResponse>(null, false, "Request timeout");
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
    public async Task<ApiResponse<TResponse>> PutAsync<TRequest, TResponse>(
        string url, TRequest data, string? authToken = null, int timeoutSeconds = 30)
        where TRequest : class
        where TResponse : class
    {
        try
        {
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Put, url) { Content = content };

            if (!string.IsNullOrEmpty(authToken))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
            {
                var response = await _httpClient.SendAsync(request, cts.Token);

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
            return new ApiResponse<TResponse>(null, false, "Request timeout");
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
    public async Task<ApiResponse<bool>> DeleteAsync(string url, string? authToken = null, int timeoutSeconds = 30)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, url);

            if (!string.IsNullOrEmpty(authToken))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
            {
                var response = await _httpClient.SendAsync(request, cts.Token);

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
    public async Task<ApiResponse<T>> GetWithRetryAsync<T>(
        string url, int maxRetries = 3, string? authToken = null) where T : class
    {
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

        return new ApiResponse<T>(null, false, "Max retries exceeded");
    }

    /// <summary>
    /// Checks if an external API endpoint is reachable.
    /// Useful for health checks and connectivity monitoring.
    /// </summary>
    public async Task<bool> IsApiAvailableAsync(string url)
    {
        try
        {
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
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

public class ApiResponse<T> where T : class
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
