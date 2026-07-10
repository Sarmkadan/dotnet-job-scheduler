# ExecutionStatsResponse

A data transfer object that encapsulates execution statistics for a scheduled job, including counts of total, successful, and failed executions, timing metrics, and the most recent execution timestamp.

## API

### JobId
The unique identifier of the job to which these statistics apply.
Type: `System.Guid`
This member is required and must not be the default `Guid.Empty` value.

### TotalExecutions
The total number of times the job has been executed.
Type: `System.Int32`
This value is non-negative and reflects the cumulative count of all executions, including those that succeeded and failed.

### SuccessfulExecutions
The number of executions that completed without errors.
Type: `System.Int32`
This value cannot exceed `TotalExecutions` and is non-negative.

### FailedExecutions
The number of executions that terminated with an error.
Type: `System.Int32`
This value cannot exceed `TotalExecutions` and is non-negative.

### SuccessRate
The ratio of successful executions to total executions, expressed as a value between 0.0 and 1.0.
Type: `System.Double`
Calculated as `SuccessfulExecutions / TotalExecutions` when `TotalExecutions > 0`; otherwise, this value is 0.0.

### AverageExecutionTimeMs
The arithmetic mean of all recorded execution durations, in milliseconds.
Type: `System.Int64`
This value is non-negative and reflects the average of all execution times, including those that failed.

### MinExecutionTimeMs
The shortest recorded execution duration, in milliseconds.
Type: `System.Int64`
This value is non-negative and less than or equal to `MaxExecutionTimeMs`.

### MaxExecutionTimeMs
The longest recorded execution duration, in milliseconds.
Type: `System.Int64`
This value is non-negative and greater than or equal to `MinExecutionTimeMs`.

### LastExecutionAt
The date and time of the most recent job execution, or `null` if the job has never been executed.
Type: `System.DateTime?`
This value is `null` only when `TotalExecutions` is zero.

## Usage
