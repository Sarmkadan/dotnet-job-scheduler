# CleanupResponse

## Overview
The `CleanupResponse` class represents the response model for cleanup operations in the Job Scheduler API. It contains information about deleted items and the cutoff date used for the cleanup operation.

## Source Files
- **Model**: `src/JobScheduler.Core/Domain/Models/CleanupResponse.cs`
- **JSON Extensions**: `src/JobScheduler.Core/Domain/Models/CleanupResponseJsonExtensions.cs`

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `DeletedCount` | `int` | Number of deleted items from the cleanup operation |
| `CutoffDate` | `DateTime` | The cutoff date used for the cleanup operation (executions older than this date were deleted) |
| `Message` | `string` | A descriptive message about the result of the cleanup operation |

## JSON Shape
When serialized using the `ToJson()` extension method, the CleanupResponse produces JSON with camelCase property names:

```json
{
  "deletedCount": 150,
  "cutoffDate": "2026-06-12T10:30:00.0000000Z",
  "message": "Successfully deleted 150 old executions"
}
```

## Endpoint
The `CleanupResponse` is returned by the following API endpoint:

**DELETE** `/api/executions/cleanup`

### Parameters
- `olderThanDays` (query parameter, optional, default: 90) - The number of days old executions must be to be deleted

### Example Request
```
DELETE /api/executions/cleanup?olderThanDays=90
```

### Example Response (Success)
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "deletedCount": 150,
  "cutoffDate": "2026-06-12T10:30:00.0000000Z",
  "message": "Successfully deleted 150 old executions"
}
```

### Example Response (Error)
```http
HTTP/1.1 500 Internal Server Error
Content-Type: application/json

{
  "error": "Failed to cleanup old executions"
}
```

## Usage Example
```csharp
// Using the JSON extension method
var cleanupResponse = new CleanupResponse
{
    DeletedCount = 150,
    CutoffDate = DateTime.UtcNow.AddDays(-90),
    Message = $"Successfully deleted {deletedCount} old executions"
};

string json = cleanupResponse.ToJson(indented: true);
// Returns formatted JSON with camelCase property names
```

## Notes
- The `CutoffDate` is serialized in ISO 8601 format ('O' format specifier) when using the default `ToString()` method
- The JSON extension method uses camelCase naming convention for property names
- The `Message` property defaults to an empty string if not explicitly set
- This response is specifically used for cleaning up old execution records based on retention policy