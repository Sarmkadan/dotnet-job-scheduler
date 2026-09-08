# CronExpressionException

The `CronExpressionException` is thrown when a cron expression is invalid, cannot be parsed, or cannot produce a future occurrence. It derives from `JobSchedulerException` and carries the expression that caused the failure, a stable error code, and, when available, the underlying parsing exception.

## API

### `CronExpressionException(string cronExpression, string message)`

Initializes a new instance with the invalid cron expression and a description of the failure. The exception message is formatted as `Invalid cron expression '<expression>': <message>`, and the inherited `ErrorCode` property is set to `INVALID_CRON_EXPRESSION`.

### `CronExpressionException(string cronExpression, string message, Exception innerException)`

Initializes a new instance with the invalid cron expression, a description of the failure, and the exception that caused it. The message and error code use the same format and value as the two-argument constructor, while `InnerException` preserves the original failure.

### `string CronExpression`

Gets or sets the cron expression that failed validation or evaluation. Both constructors initialize this property from their `cronExpression` argument.

### `string? ErrorCode`

Gets or sets the error code inherited from `JobSchedulerException`. Both constructors set it to `INVALID_CRON_EXPRESSION`, allowing callers to classify the failure without parsing the exception message.

## Where It Is Thrown

`CronExpressionService` throws `CronExpressionException` in the following cases:

- `ParseCronExpression` receives a null, empty, or whitespace expression.
- `ParseCronExpression` cannot parse an expression with NCrontab. This path uses the constructor that preserves the parsing exception as `InnerException`.
- `GetNextExecutionTime` cannot find a valid future occurrence within its retry range.
- `GetNextExecutionTimeInZone` cannot find a valid future occurrence in the requested timezone within its retry range.

Other `CronExpressionService` methods call `ParseCronExpression` and may therefore propagate the exception. `IsValidCronExpression` is the exception: it catches parsing failures and returns `false`.

`ValidationUtility.ValidateCronExpression` does not throw `CronExpressionException`. It performs basic five-field validation and returns a `ValidationResult` whose `IsValid` value is `false` and whose error message describes the problem. Use `CronExpressionService.ParseCronExpression` when full parsing and exception-based error handling are required.

## Usage

The following example catches a cron parsing failure and uses the expression and inherited error code to report it.

```csharp
var cronService = new CronExpressionService();

try
{
    var schedule = cronService.ParseCronExpression("not a cron expression");
    // Use the parsed schedule...
}
catch (CronExpressionException ex)
{
    logger.LogError(
        ex,
        "Cron expression {CronExpression} failed with error code {ErrorCode}: {Message}",
        ex.CronExpression,
        ex.ErrorCode,
        ex.Message);
}
```

## Notes

- The exception message includes the supplied cron expression and reason. A null or empty expression is rendered as `Invalid cron expression '': <message>`.
- `CronExpression` and the inherited `ErrorCode` are mutable. Treat an exception instance as immutable after construction when rethrowing, logging, or storing it.
- Catch `CronExpressionException` around parsing or occurrence calculation when the caller needs failure details. Use `IsValidCronExpression` or `ValidationUtility.ValidateCronExpression` when a Boolean or result-based validation flow is more appropriate.
