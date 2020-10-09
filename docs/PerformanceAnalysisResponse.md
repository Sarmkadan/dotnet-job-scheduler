# PerformanceAnalysisResponse

Represents performance metrics for a scheduled job, including execution-time statistics and timestamps for the slowest and fastest executions.

## API

### JobId
- **Purpose**: Uniquely identifies the job whose performance is being analyzed.
- **Type**: `Guid`
- **Remarks**: Read-only; never `null` or empty.

### AverageExecutionTimeMs
- **Purpose**: The arithmetic mean of all recorded execution times for the job, in milliseconds.
- **Type**: `long`
- **Remarks**: Always non-negative; may be zero if no executions were recorded.

### MedianExecutionTimeMs
- **Purpose**: The median execution time for the job, in milliseconds.
- **Type**: `long`
- **Remarks**: Always non-negative; may be zero if no executions were recorded.

### P95ExecutionTimeMs
- **Purpose**: The 95th percentile execution time for the job, in milliseconds.
- **Type**: `long`
- **Remarks**: Always non-negative; may be zero if fewer than 20 executions were recorded.

### P99ExecutionTimeMs
- **Purpose**: The 99th percentile execution time for the job, in milliseconds.
- **Type**: `long`
- **Remarks**: Always non-negative; may be zero if fewer than 100 executions were recorded.

### SlowestExecutionTimeMs
- **Purpose**: The longest recorded execution time for the job, in milliseconds.
- **Type**: `long`
- **Remarks**: Always non-negative; may be zero if no executions were recorded.

### FastestExecutionTimeMs
- **Purpose**: The shortest recorded execution time for the job, in milliseconds.
- **Type**: `long`
- **Remarks**: Always non-negative; may be zero if no executions were recorded.

### SlowestExecutionAt
- **Purpose**: The timestamp when the slowest execution occurred, if any.
- **Type**: `DateTime?`
- **Remarks**: `null` if no executions were recorded or if the slowest execution time is zero.

### FastestExecutionAt
- **Purpose**: The timestamp when the fastest execution occurred, if any.
- **Type**: `DateTime?`
- **Remarks**: `null` if no executions were recorded or if the fastest execution time is zero.

## Usage
