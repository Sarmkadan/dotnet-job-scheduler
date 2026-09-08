# ExecutionRepositoryValidation

The `ExecutionRepositoryValidation` static class provides validation helpers for `ExecutionRepository` to ensure repository instances and their method parameters are valid before operations.

## Validation Methods

| Method | Parameters | Rule Checked | Result/Exception |
|--------|------------|--------------|------------------|
| `Validate(this ExecutionRepository? value)` | `value`: The repository instance to validate | Repository instance not null | List of validation problems; empty if valid<br>Throws `ArgumentNullException` if value is null |
| `IsValid(this ExecutionRepository? value)` | `value`: The repository instance to check | Repository instance not null | True if valid; false otherwise |
| `EnsureValid(this ExecutionRepository? value)` | `value`: The repository instance to validate | Repository instance not null | Throws `ArgumentNullException` if value is null |
| `Validate(this Guid jobId)` | `jobId`: The job identifier | Job ID not empty | List of validation problems; empty if valid<br>Adds "Job ID cannot be empty." if jobId is Guid.Empty |
| `IsValid(this Guid jobId)` | `jobId`: The job identifier to check | Job ID not empty | True if valid; false otherwise |
| `EnsureValid(this Guid jobId)` | `jobId`: The job identifier to validate | Job ID not empty | Throws `ArgumentException` with message "Job ID cannot be empty." if jobId is Guid.Empty |
| `Validate(this ExecutionStatus status)` | `status`: The execution status to filter by | All ExecutionStatus enum values valid | List of validation problems; empty if valid (always returns empty list) |
| `IsValid(this ExecutionStatus status)` | `status`: The status to check | Always true for ExecutionStatus enum values | Always returns true |
| `Validate(this Guid jobId, ExecutionStatus status)` | `jobId`: The job identifier<br>`status`: The execution status to filter by | Job ID not empty (status always valid) | List of validation problems; empty if valid<br>Adds "Job ID cannot be empty." if jobId is Guid.Empty |
| `IsValid(this Guid jobId, ExecutionStatus status)` | `jobId`: The job identifier to check<br>`status`: The status to check | Job ID not empty | True if valid; false otherwise |
| `EnsureValid(this Guid jobId, ExecutionStatus status)` | `jobId`: The job identifier to validate<br>`status`: The status to validate | Job ID not empty | Throws `ArgumentException` with message "Job ID cannot be empty." if jobId is Guid.Empty |
| `ValidateForCurrentlyRunningCount(this Guid jobId)` | `jobId`: The job identifier | Job ID not empty | List of validation problems; empty if valid<br>Adds "Job ID cannot be empty." if jobId is Guid.Empty |
| `IsValidForCurrentlyRunningCount(this Guid jobId)` | `jobId`: The job identifier to check | Job ID not empty | True if valid; false otherwise |
| `EnsureValidForCurrentlyRunningCount(this Guid jobId)` | `jobId`: The job identifier to validate | Job ID not empty | Throws `ArgumentException` with message "Job ID cannot be empty." if jobId is Guid.Empty |
| `Validate(this Guid jobId, int? lastN = null)` | `jobId`: The job identifier<br>`lastN`: Optional limit on number of recent executions to consider | Job ID not empty<br>lastN must be positive if specified | List of validation problems; empty if valid<br>Adds "Job ID cannot be empty." if jobId is Guid.Empty<br>Adds "lastN must be a positive integer if specified." if lastN has value and is <= 0 |
| `IsValid(this Guid jobId, int? lastN = null)` | `jobId`: The job identifier to check<br>`lastN`: Optional limit on number of recent executions | Job ID not empty<br>lastN must be positive if specified | True if valid; false otherwise<br>Returns false if jobId is Guid.Empty or if lastN.HasValue && lastN.Value <= 0 |
| `EnsureValid(this Guid jobId, int? lastN = null)` | `jobId`: The job identifier to validate<br>`lastN`: Optional limit on number of recent executions | Job ID not empty<br>lastN must be positive if specified | Throws `ArgumentException` if validation fails:<br>- "Job ID cannot be empty." if jobId is Guid.Empty<br>- "lastN must be a positive integer if specified." if lastN.HasValue && lastN.Value <= 0 |
| `Validate(this DateTime startDate, DateTime endDate)` | `startDate`: The start date of the range (inclusive)<br>`endDate`: The end date of the range (inclusive)| Start date not default<br>End date not default<br>Start date <= end date | List of validation problems; empty if valid<br>Adds "Start date cannot be default (Unix epoch)." if startDate == default<br>Adds "End date cannot be default (Unix epoch)." if endDate == default<br>Adds "Start date must be less than or equal to end date." if startDate > endDate |
| `IsValid(this DateTime startDate, DateTime endDate)` | `startDate`: The start date to check<br>`endDate`: The end date to check | Start date not default<br>End date not default<br>Start date <= end date | True if valid; false otherwise<br>Returns startDate != default && endDate != default && startDate <= endDate |
| `EnsureValid(this DateTime startDate, DateTime endDate)` | `startDate`: The start date to validate<br>`endDate`: The end date to validate | Start date not default<br>End date not default<br>Start date <= end date | Throws `ArgumentException` if validation fails:<br>- "Start date cannot be default (Unix epoch)." if startDate == default<br>- "End date cannot be default (Unix epoch)." if endDate == default<br>- "Start date must be less than or equal to end date." if startDate > endDate |
| `ValidateForGetByJobId(this Guid jobId)` | `jobId`: The job identifier | Job ID not empty | List of validation problems; empty if valid<br>Adds "Job ID cannot be empty." if jobId is Guid.Empty |
| `IsValidForGetByJobId(this Guid jobId)` | `jobId`: The job identifier to check | Job ID not empty | True if valid; false otherwise |
| `EnsureValidForGetByJobId(this Guid jobId)` | `jobId`: The job identifier to validate | Job ID not empty | Throws `ArgumentException` with message "Job ID cannot be empty." if jobId is Guid.Empty |

## Example Usage

```csharp
using JobScheduler.Core.Data.Repositories;
using JobScheduler.Core.Domain.Entities;

// Validate repository instance
var repository = new ExecutionRepository(context);
var validationProblems = repository.Validate();
if (validationProblems.Any())
{
    // Handle validation problems
}

// Validate job ID parameter
Guid jobId = Guid.NewGuid();
if (jobId.IsValid())
{
    // Proceed with operation
}

// Ensure parameters are valid (throws exception if invalid)
jobId.EnsureValid(); // Throws ArgumentException if jobId is Guid.Empty

// Validate date range
DateTime startDate = DateTime.UtcNow.AddDays(-7);
DateTime endDate = DateTime.UtcNow;
var dateRangeProblems = startDate.Validate(endDate);
if (!dateRangeProblems.Any())
{
    // Date range is valid
}
```

See [ExecutionRepository.md](ExecutionRepository.md) for the main repository documentation.