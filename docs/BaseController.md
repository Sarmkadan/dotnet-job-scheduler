# BaseController

A generic base controller class designed to standardize API responses across the `dotnet-job-scheduler` project. It provides a consistent structure for success/failure states, error details, and payload data, ensuring uniformity in HTTP responses.

## API

### `Success`
- **Purpose**: Indicates whether the operation completed successfully.
- **Type**: `bool`
- **Return Value**: `true` if the operation succeeded; otherwise, `false`.
- **Notes**: Should be set to `false` when errors are present in the `Errors` dictionary.

### `Message`
- **Purpose**: Provides a human-readable status or error message.
- **Type**: `string`
- **Return Value**: A descriptive message explaining the outcome or failure reason.
- **Notes**: May be `null` or empty for successful operations with no additional context.

### `Data`
- **Purpose**: Contains the payload data returned by the operation.
- **Type**: `T?` (generic, nullable)
- **Return Value**: The result of the operation if successful; otherwise, `null`.
- **Notes**: The type `T` is inferred from the generic parameter used when instantiating the controller.

### `Timestamp`
- **Purpose**: Records the UTC date and time when the response was generated.
- **Type**: `DateTime`
- **Return Value**: The current UTC time at the moment of response construction.
- **Notes**: Always expressed in UTC; do not rely on local time zones.

### `CorrelationId`
- **Purpose**: Provides a unique identifier for tracing the request across services.
- **Type**: `string?` (nullable)
- **Return Value**: A GUID or other unique string; may be `null` if not set.
- **Notes**: Useful for correlating logs and debugging distributed operations.

### `Errors`
- **Purpose**: Contains a collection of error details keyed by field names.
- **Type**: `Dictionary<string, string[]>`
- **Return Value**: A dictionary where keys are field names and values are arrays of error messages.
- **Notes**:
  - Populated only when `Success` is `false`.
  - Keys may be `null` or empty to represent general (non-field-specific) errors.
  - Each array may contain multiple error messages for a given field.

## Usage

### Example 1: Successful Response
