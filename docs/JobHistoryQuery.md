# JobHistoryQuery

A query object used to filter and paginate historical execution records of scheduled jobs. It encapsulates criteria such as time range, execution status, and pagination controls to retrieve job history entries from the scheduler's storage.

## API

### `Status`
Gets or sets the optional execution status to filter job history records. Only records matching this status will be returned.

- **Type:** `ExecutionStatus?`
- **Default:** `null` (no filtering by status)

### `From`
Gets or sets the optional start of the time range (inclusive) for filtering job history records. Only records with an execution timestamp on or after this value will be returned.

- **Type:** `DateTime?`
- **Default:** `null` (no lower bound)

### `To`
Gets or sets the optional end of the time range (inclusive) for filtering job history records. Only records with an execution timestamp on or before this value will be returned.

- **Type:** `DateTime?`
- **Default:** `null` (no upper bound)

### `PageNumber`
Gets or sets the one-based page number for pagination. Must be a positive integer.

- **Type:** `int`
- **Default:** `1`

### `PageSize`
Gets or sets the maximum number of records to return per page. Must be a positive integer.

- **Type:** `int`
- **Default:** `10`

### `Normalize()`
Validates and adjusts the query parameters to ensure they are within acceptable ranges. Ensures `PageNumber` and `PageSize` are positive, and clamps `PageSize` to a reasonable maximum if necessary.

- **Returns:** `JobHistoryQuery` (the normalized instance for method chaining)
- **Throws:** `ArgumentOutOfRangeException` if `PageNumber` or `PageSize` is zero or negative after normalization.

## Usage
