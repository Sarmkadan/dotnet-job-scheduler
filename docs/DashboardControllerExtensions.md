# Dashboard Controller Extensions

This document describes the 14 public extension methods in `DashboardControllerExtensions.cs` that enhance the functionality of the `DashboardController` class.

## Extension Methods

### 1. CalculateHealthScoreAsync

```csharp
public static async Task<int> CalculateHealthScoreAsync(this DashboardController controller)
```

**Purpose**: Calculates the system health score (0-100) based on current system metrics. A score below 70 indicates potential issues requiring attention.

**Parameters**:
- `controller`: The dashboard controller instance.

**Returns**: A health score between 0 and 100.

**Exceptions**:
- `ArgumentNullException`: Thrown when controller is null.
- `InvalidOperationException`: Thrown when any of the controller methods return null.

### 2. GetHealthStatusAsync

```csharp
public static async Task<(string Status, string Color)> GetHealthStatusAsync(this DashboardController controller)
```

**Purpose**: Gets a simplified health status summary for quick dashboard indicators.

**Parameters**:
- `controller`: The dashboard controller instance.

**Returns**: A tuple containing (Status: "Good", "Warning", or "Critical", Color: "green", "yellow", or "red").

**Exceptions**:
- `ArgumentNullException`: Thrown when controller is null.
- `InvalidOperationException`: Thrown when GetHealthReport returns null.

### 3. GetFormattedStatisticsAsync

```csharp
public static async Task<IReadOnlyDictionary<string, string>> GetFormattedStatisticsAsync(this DashboardController controller)
```

**Purpose**: Gets job execution statistics formatted for display with human-readable units.

**Parameters**:
- `controller`: The dashboard controller instance.

**Returns**: A dictionary containing formatted display values for key metrics.

**Exceptions**:
- `ArgumentNullException`: Thrown when controller is null.
- `InvalidOperationException`: Thrown when any of the controller methods return null.

### 4. GetTopFailingJobsWithAnalysisAsync

```csharp
public static async Task<IReadOnlyList<JobFailureAnalysis>> GetTopFailingJobsWithAnalysisAsync(
    this DashboardController controller,
    int count = 5)
```

**Purpose**: Gets the top N jobs by failure rate with additional context.

**Parameters**:
- `controller`: The dashboard controller instance.
- `count`: Number of jobs to return (default: 5).

**Returns**: A list of job failure analysis objects with additional metrics.

**Exceptions**:
- `ArgumentNullException`: Thrown when controller is null.
- `ArgumentOutOfRangeException`: Thrown when count is less than 1.
- `InvalidOperationException`: Thrown when GetMostFailingJobs or GetSlowestJobs return null.

## JobFailureAnalysis Class

The `JobFailureAnalysis` class represents a detailed analysis of job failures with additional context:

- `JobId`: Gets the unique identifier of the job.
- `JobName`: Gets the name of the job.
- `FailureRate`: Gets the failure rate percentage (0-100).
- `FailedCount`: Gets the total number of failed executions.
- `SuccessRate`: Gets the success rate percentage (0-100).
- `IsSlow`: Gets whether the job is also among the slowest jobs.
- `FailureImpactScore`: Gets a calculated impact score based on failure count and rate.
- `Status`: Gets a human-readable status based on the failure impact.

## Usage Example

```csharp
// Assuming you have a DashboardController instance
var dashboardController = new DashboardController();

// Calculate health score
int healthScore = await dashboardController.CalculateHealthScoreAsync();
Console.WriteLine($"Health Score: {healthScore}");

// Get health status
var (status, color) = await dashboardController.GetHealthStatusAsync();
Console.WriteLine($"Status: {status}, Color: {color}");

// Get formatted statistics
var stats = await dashboardController.GetFormattedStatisticsAsync();
foreach (var stat in stats)
{
    Console.WriteLine($"{stat.Key}: {stat.Value}");
}

// Get top failing jobs with analysis
var failingJobs = await dashboardController.GetTopFailingJobsWithAnalysisAsync(3);
foreach (var job in failingJobs)
{
    Console.WriteLine($"Job: {job.JobName}, Failure Rate: {job.FailureRate}%, Status: {job.Status}");
}
```

## Related Documentation

See [DashboardController.md](./DashboardController.md) for the base controller implementation that these extension methods enhance.