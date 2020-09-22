# JobValidationException

Exception thrown when a job fails validation in the dotnet-job-scheduler system. This exception is used to indicate that a job's configuration, parameters, or state do not meet the required criteria for execution.

## API

### `public string? PropertyName`

Gets the name of the property that failed validation, if applicable. Returns `null` when the validation failure is not tied to a specific property.

### `public JobValidationException(string message) : base(message, "JOB_VALIDATION_ERROR")`

Constructs a new `JobValidationException` with the specified error message. The exception is initialized with the provided message and a predefined error code `"JOB_VALIDATION_ERROR"`.

- **Parameters**
  - `message` (string): The error message describing the validation failure.
- **Return value**
  - N/A (constructor)
- **Throws**
  - N/A

### `public JobValidationException()`

Constructs a new `JobValidationException` with a default error message. The exception is initialized with a generic message and the predefined error code `"JOB_VALIDATION_ERROR"`.

- **Parameters**
  - N/A
- **Return value**
  - N/A (constructor)
- **Throws**
  - N/A

### `public JobValidationException(string? message, string? propertyName)`

Constructs a new `JobValidationException` with a custom error message and property name. The exception is initialized with the provided message and error code `"JOB_VALIDATION_ERROR"`, and associates the failure with the specified property.

- **Parameters**
  - `message` (string?): The error message describing the validation failure.
  - `propertyName` (string?): The name of the property that failed validation.
- **Return value**
  - N/A (constructor)
- **Throws**
  - N/A

## Usage
