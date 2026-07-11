# JobSchedulerSettingsExtensions

The `JobSchedulerSettingsExtensions` class provides a set of static extension methods designed to simplify the validation, manipulation, and querying of `JobSchedulerSettings` objects within the `dotnet-job-scheduler` framework. These methods centralize common configuration handling logic, ensuring consistent behavior when retrieving effective values or verifying settings integrity across the application.

## API

### Validate
Validates the provided `JobSchedulerSettings` instance against defined configuration constraints.

*   **Parameters:**
    *   `settings` (JobSchedulerSettings): The settings instance to validate.
*   **Returns:** `List<string>`: A list of validation error messages. Returns an empty list if the settings are valid.
*   **Throws:** `ArgumentNullException` if `settings` is null.

### Clone
Creates a deep copy of the provided `JobSchedulerSettings` instance.

*   **Parameters:**
    *   `settings` (JobSchedulerSettings): The settings instance to clone.
*   **Returns:** `JobSchedulerSettings`: A new, independent instance of `JobSchedulerSettings` with identical property values.
*   **Throws:** `ArgumentNullException` if `settings` is null.

### IsCleanupEnabled
Determines whether automatic cleanup tasks are enabled in the provided settings.

*   **Parameters:**
    *   `settings` (JobSchedulerSettings): The settings instance to check.
*   **Returns:** `bool`: True if cleanup is enabled; otherwise, false.
*   **Throws:** `ArgumentNullException` if `settings` is null.

### GetEffectiveTimeoutMs
Retrieves the effective timeout value in milliseconds, applying any necessary default fallbacks defined in the configuration.

*   **Parameters:**
    *   `settings` (JobSchedulerSettings): The settings instance to query.
*   **Returns:** `int`: The configured timeout value in milliseconds.
*   **Throws:** `ArgumentNullException` if `settings` is null.

### GetMaxJobNameLength
Retrieves the maximum allowed length for job names defined in the settings.

*   **Parameters:**
    *   `settings` (JobSchedulerSettings): The settings instance to query.
*   **Returns:** `int`: The maximum allowed job name length.
*   **Throws:** `ArgumentNullException` if `settings` is null.

## Usage

### Validating Configuration
```csharp
using DotNetJobScheduler.Configuration;

var settings = new JobSchedulerSettings { /* Initialize properties */ };

var errors = settings.Validate();
if (errors.Any())
{
    foreach (var error in errors)
    {
        Console.WriteLine($"Configuration error: {error}");
    }
}
else
{
    Console.WriteLine("Configuration is valid.");
}
```

### Cloning and Retrieving Effective Values
```csharp
using DotNetJobScheduler.Configuration;

var originalSettings = new JobSchedulerSettings { /* ... */ };

// Create a safe copy for modification
var workingSettings = originalSettings.Clone();

// Retrieve effective values
int timeout = workingSettings.GetEffectiveTimeoutMs();
bool cleanup = workingSettings.IsCleanupEnabled();

Console.WriteLine($"Effective Timeout: {timeout}ms, Cleanup: {cleanup}");
```

## Notes

*   **Thread Safety:** The extension methods provided by this class are thread-safe for read operations, provided the `JobSchedulerSettings` instance itself is not modified concurrently by another thread.
*   **Null Handling:** All extension methods perform an explicit null check on the `settings` parameter and will throw an `ArgumentNullException` if a null reference is passed.
*   **Cloning:** The `Clone` method performs a deep copy of the settings object, ensuring that modifications made to the returned object do not affect the original source instance.
*   **Edge Cases:** If `GetEffectiveTimeoutMs` or `GetMaxJobNameLength` are called on a `JobSchedulerSettings` object that has not been explicitly configured with valid values, these methods will return the defined internal framework defaults.
