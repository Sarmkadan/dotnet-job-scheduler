# PaginatedResponseExtensions Documentation

## Overview

This document describes the `PaginatedResponse<T>` class and the `PaginatedResponseExtensions` static class, which provide functionality for handling paginated API responses in the JobScheduler.Core.Controllers namespace.

## PaginatedResponse<T> Class

A generic class representing a paginated response containing a subset of data from a larger dataset.

### Namespace
`JobScheduler.Core.Controllers`

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Data` | `List<T>` | Gets or sets the collection of items for the current page. Initialized as an empty list. |
| `TotalCount` | `int` | Gets or sets the total number of items available across all pages. |
| `PageNumber` | `int` | Gets or sets the current page number (1-based index). |
| `PageSize` | `int` | Gets or sets the number of items per page. |

### Methods

#### `ToString()`
Returns a string representation of the paginated response in the format:
```
PaginatedResponse: Page = {PageNumber}, PageSize = {PageSize}, TotalCount = {TotalCount}, ItemCount = {Data.Count}
```

### Usage Example
```csharp
var response = new PaginatedResponse<JobResponse>
{
    Data = jobResponses,
    TotalCount = totalJobs,
    PageNumber = 1,
    PageSize = 10
};
```

## PaginatedResponseExtensions Class

A static class providing extension methods for working with `PaginatedResponse<T>` objects.

### Namespace
`JobScheduler.Core.Controllers`

### Extension Methods

#### `GetTotalPages<T>(this PaginatedResponse<T> response)`
Calculates the total number of pages represented by the paginated response.

##### Parameters
| Name | Type | Description |
|------|------|-------------|
| `response` | `PaginatedResponse<T>` | The paginated response whose total page count is calculated. |

##### Returns
- `int`: The total number of pages, rounded up to include a partially filled page.
- Returns `0` when the page size is not positive.

##### Exceptions
- `ArgumentNullException`: Thrown when `response` is `null`.

##### Remarks
The calculation uses the formula: `Math.Ceiling((double)response.TotalCount / response.PageSize)`

##### Usage Example
```csharp
var paginatedResponse = GetJobs(status, pageNumber, pageSize);
int totalPages = paginatedResponse.GetTotalPages();
// If TotalCount = 25 and PageSize = 10, totalPages will be 3
```