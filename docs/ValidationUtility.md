# ValidationUtility

A utility class providing static methods to validate various aspects of job scheduler configuration objects such as job names, cron expressions, handler types, and other configuration parameters. These methods return a `ValidationResult` that indicates whether validation succeeded and includes a descriptive message when it fails.

## API

### `public static ValidationResult ValidateJobName(string jobName)`

Validates a job name according to the scheduler's naming rules.

- **Parameters**
  - `jobName` (string): The name of the job to validate.
- **Return value**
  - A `ValidationResult` indicating whether the job name is valid and containing a message if invalid.
- **Throws**
  - Does not throw exceptions; validation failures are reported via the returned `ValidationResult`.

### `public static ValidationResult ValidateCronExpression(string cronExpression)`

Validates a cron expression string to ensure it conforms to the expected format.

- **Parameters**
  - `cronExpression` (string): The cron expression to validate.
- **Return value**
  - A `ValidationResult` indicating whether the cron expression is valid and containing a message if invalid.
- **Throws**
  - Does not throw exceptions; validation failures are reported via the returned `ValidationResult`.

### `public static ValidationResult ValidateHandlerType(Type handlerType)`

Validates that the provided type is a valid job handler type.

- **Parameters**
  - `handlerType` (Type): The type to validate as a job handler.
- **Return value**
  - A `ValidationResult` indicating whether the type is a valid handler and containing a message if invalid.
- **Throws**
  - Does not throw exceptions; validation failures are reported via the returned `ValidationResult`.

### `public static ValidationResult ValidateJobConfiguration(JobConfiguration config)`

Validates a complete `JobConfiguration` object for structural and semantic correctness.

- **Parameters**
  - `config` (JobConfiguration): The job configuration to validate.
- **Return value**
  - A `ValidationResult` indicating whether the configuration is valid and containing a message if invalid.
- **Throws**
  - Does not throw exceptions; validation failures are reported via the returned `ValidationResult`.

### `public static ValidationResult ValidateJsonParameters(string jsonParameters)`

Validates that the provided JSON string is syntactically valid and optionally conforms to expected schema.

- **Parameters**
  - `jsonParameters` (string): The JSON string to validate.
- **Return value**
  - A `ValidationResult` indicating whether the JSON is valid and containing a message if invalid.
- **Throws**
  - Does not throw exceptions; validation failures are reported via the returned `ValidationResult`.

### `public static ValidationResult ValidatePagination(int page, int pageSize)`

Validates pagination parameters to ensure they are within acceptable bounds.

- **Parameters**
  - `page` (int): The page number to validate.
  - `pageSize` (int): The number of items per page to validate.
- **Return value**
  - A `ValidationResult` indicating whether the pagination parameters are valid and containing a message if invalid.
- **Throws**
  - Does not throw exceptions; validation failures are reported via the returned `ValidationResult`.

### `public static ValidationResult ValidateRetryStrategy(RetryStrategy strategy)`

Validates a retry strategy object for correctness and completeness.

- **Parameters**
  - `strategy` (RetryStrategy): The retry strategy to validate.
- **Return value**
  - A `ValidationResult` indicating whether the retry strategy is valid and containing a message if invalid.
- **Throws**
  - Does not throw exceptions; validation failures are reported via the returned `ValidationResult`.

### `public bool IsValid`

Gets a boolean indicating whether the current validation result represents a successful validation.

- **Return value**
  - `true` if the validation succeeded; otherwise, `false`.

### `public string Message`

Gets the validation message, which is non-empty only when validation failed.

- **Return value**
  - A string containing the validation message, or an empty string if validation succeeded.

### `public ValidationResult`

A struct representing the outcome of a validation operation, containing `IsValid` and `Message` properties.

- **Members**
  - `IsValid` (bool): Indicates whether validation succeeded.
  - `Message` (string): Contains a descriptive message if validation failed.

### `public void ThrowIfInvalid()`

Throws an `InvalidOperationException` if the current validation result indicates failure.

- **Throws**
  - `InvalidOperationException` with the validation message when `IsValid` is `false`.

## Usage
