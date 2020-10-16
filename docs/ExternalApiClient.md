# ExternalApiClient

The `ExternalApiClient` class provides a typed HTTP client abstraction for interacting with external RESTful services within the `dotnet-job-scheduler` project. It encapsulates standard HTTP verbs (GET, POST, PUT, DELETE) into strongly-typed asynchronous methods that return a unified `ApiResponse<T>` result, handling serialization and basic error state propagation without throwing exceptions for non-successful HTTP status codes. The client also includes built-in retry logic for read operations and a connectivity check mechanism to verify service availability before executing critical jobs.

## API

### Constructors

#### `public ExternalApiClient()`
Initializes a new instance of the `ExternalApiClient` class. This constructor sets up the underlying HTTP handler and configures default headers required for the job scheduler's communication protocol.

### Methods

#### `public async Task<ApiResponse<T>> GetAsync<T>(string endpoint)`
Executes an HTTP GET request to the specified endpoint and deserializes the response body to type `T`.
*   **Parameters**:
    *   `endpoint`: The relative URI path to the resource.
*   **Return Value**: A `Task` representing the asynchronous operation, containing an `ApiResponse<T>` with the deserialized data or error details.
*   **Exceptions**: Throws network-level exceptions (e.g., `HttpRequestException`) if the request fails to reach the server or times out. Does not throw for non-2xx HTTP status codes; these are captured in the returned `ApiResponse`.

#### `public async Task<ApiResponse<TResponse>> PostAsync<TRequest, TResponse>(string endpoint, TRequest payload)`
Executes an HTTP POST request with the provided payload serialized as JSON to the specified endpoint, expecting a response of type `TResponse`.
*   **Parameters**:
    *   `endpoint`: The relative URI path to the resource.
    *   `payload`: The object to be serialized and sent in the request body.
*   **Return Value**: A `Task` representing the asynchronous operation, containing an `ApiResponse<TResponse>` with the created resource or error details.
*   **Exceptions**: Throws network-level exceptions if connectivity fails. Serialization errors or non-successful HTTP statuses are reflected in the `ApiResponse`.

#### `public async Task<ApiResponse<TResponse>> PutAsync<TRequest, TResponse>(string endpoint, TRequest payload)`
Executes an HTTP PUT request to update a resource at the specified endpoint with the provided payload, expecting a response of type `TResponse`.
*   **Parameters**:
    *   `endpoint`: The relative URI path to the resource.
    *   `payload`: The object to be serialized and sent in the request body.
*   **Return Value**: A `Task` representing the asynchronous operation, containing an `ApiResponse<TResponse>` with the updated resource or error details.
*   **Exceptions**: Throws network-level exceptions if connectivity fails. Non-successful HTTP statuses are captured in the `ApiResponse`.

#### `public async Task<ApiResponse<bool>> DeleteAsync(string endpoint)`
Executes an HTTP DELETE request to the specified endpoint.
*   **Parameters**:
    *   `endpoint`: The relative URI path to the resource.
*   **Return Value**: A `Task` representing the asynchronous operation, containing an `ApiResponse<bool>`. If successful, the `Data` property is `true`; otherwise, it is `false` or populated with error context.
*   **Exceptions**: Throws network-level exceptions if connectivity fails.

#### `public async Task<ApiResponse<T>> GetWithRetryAsync<T>(string endpoint, int maxRetries = 3)`
Executes an HTTP GET request with automatic retry logic for transient failures.
*   **Parameters**:
    *   `endpoint`: The relative URI path to the resource.
    *   `maxRetries`: The maximum number of retry attempts (default is 3).
*   **Return Value**: A `Task` representing the asynchronous operation, containing an `ApiResponse<T>`.
*   **Exceptions**: Throws only after all retry attempts have been exhausted due to persistent network failures.

#### `public async Task<bool> IsApiAvailableAsync()`
Performs a lightweight connectivity check (typically a HEAD or OPTIONS request) to determine if the external API is reachable.
*   **Return Value**: A `Task` resulting in `true` if the API responds with a success status code, or `false` if the API is unreachable or returns an error status.
*   **Exceptions**: Generally suppresses exceptions, returning `false` on failure, unless a critical configuration error exists.

### Properties

#### `public T? Data`
Gets the deserialized payload from the last successful API response. Returns `default(T)` if the request failed or no data was returned.

#### `public bool Success`
Gets a value indicating whether the last API operation completed with a successful HTTP status code (2xx).

#### `public string? Error`
Gets the error message or diagnostic details if the last API operation failed. Returns `null` if the operation was successful.

### Nested Types

#### `public ApiResponse`
Represents the standard envelope for all API results. This type ensures consistent handling of success states, data payloads, and error messages across all client methods.

## Usage

### Example 1: Fetching Job Configuration with Retry Logic
This example demonstrates retrieving a configuration object from an external service, utilizing the built-in retry mechanism to handle transient network instability common in distributed job environments.

```csharp
var client = new ExternalApiClient();
var endpoint = "/config/job-settings";

// Attempt to fetch settings with automatic retries on failure
var response = await client.GetWithRetryAsync<JobSettings>(endpoint);

if (response.Success && response.Data != null)
{
    var settings = response.Data;
    Console.WriteLine($"Polling interval: {settings.PollingIntervalSeconds}s");
}
else
{
    // Handle failure without catching an exception
    Console.WriteLine($"Failed to load config: {response.Error}");
}
```

### Example 2: Submitting a New Job Task
This example illustrates posting a new job definition to the scheduler API and verifying the creation result.

```csharp
var client = new ExternalApiClient();
var newJob = new JobDefinition 
{ 
    Name = "NightlyBackup", 
    Schedule = "0 2 * * *" 
};

var response = await client.PostAsync<JobDefinition, JobCreationResult>("/jobs", newJob);

if (response.Success)
{
    Console.WriteLine($"Job created with ID: {response.Data?.Id}");
}
else
{
    // Log the specific API error message
    Logger.LogError($"Job submission failed: {response.Error}");
}
```

## Notes

*   **Exception Handling**: The client methods are designed to not throw exceptions for standard HTTP error responses (4xx, 5xx). Instead, these states are captured within the `ApiResponse` object, setting `Success` to `false` and populating the `Error` property. Callers must inspect the `Success` property before accessing `Data`. Network-level exceptions (DNS failure, connection timeout) will still propagate and should be caught by the caller.
*   **Thread Safety**: The `ExternalApiClient` instance relies on an underlying `HttpClient`. While `HttpClient` is generally thread-safe for concurrent requests, the stateful properties (`Data`, `Success`, `Error`) on the `ExternalApiClient` instance itself reflect the result of the *most recently awaited* operation. In multi-threaded scenarios, do not rely on these instance properties to correlate results to specific calls; instead, use the `ApiResponse<T>` returned directly by the method invocation.
*   **Nullability**: The `Data` property is nullable (`T?`). Even if `Success` is `true`, `Data` may be `null` if the API returns a 204 No Content or an empty body. Always check for nullability before dereferencing the `Data` property.
*   **Retry Behavior**: `GetWithRetryAsync` implements an exponential backoff strategy for retries. It is intended only for idempotent GET operations. Do not use this method for state-changing operations.
