# JobSchedulerContext

The `JobSchedulerContext` class serves as the primary Entity Framework Core database context for the `dotnet-job-scheduler` project, managing the persistence layer for all scheduling entities. It inherits from `DbContext` to provide typed access to tables representing jobs, execution history, retry policies, dependencies, pipelines, and distributed locking mechanisms, ensuring transactional integrity and change tracking for the scheduler's operational state.

## API

### Constructor

#### `public JobSchedulerContext(DbContextOptions<JobSchedulerContext> options)`

Initializes a new instance of the `JobSchedulerContext` class with the specified options required by Entity Framework Core.

*   **Parameters**:
    *   `options`: The `DbContextOptions<JobSchedulerContext>` containing configuration such as the database provider connection string, migration assembly, and behavior settings.
*   **Remarks**: This constructor is typically invoked by the dependency injection container when registering the context. It passes the options to the base `DbContext` constructor.
*   **Exceptions**: Throws `ArgumentNullException` if `options` is null, or provider-specific exceptions if the configuration within `options` is invalid.

### Properties

#### `public DbSet<Job> Jobs`

Gets or sets the collection of `Job` entities representing scheduled tasks.

*   **Purpose**: Provides access to the underlying table storing job definitions, including triggers, payloads, and current status.
*   **Usage**: Used to query, add, update, or remove job definitions.

#### `public DbSet<JobExecution> JobExecutions`

Gets or sets the collection of `JobExecution` entities.

*   **Purpose**: Tracks individual runs of jobs, storing start times, end times, output results, and failure reasons.
*   **Usage**: Essential for auditing, monitoring job health, and implementing retry logic based on past attempts.

#### `public DbSet<JobScheduleHistory> JobScheduleHistories`

Gets or sets the collection of `JobScheduleHistory` entities.

*   **Purpose**: Maintains a historical log of schedule changes or trigger evaluations.
*   **Usage**: Used for debugging scheduling anomalies or analyzing long-term trigger patterns.

#### `public DbSet<RetryPolicy> RetryPolicies`

Gets or sets the collection of `RetryPolicy` entities.

*   **Purpose**: Stores configuration defining how failed jobs should be retried (e.g., max attempts, delay intervals).
*   **Usage**: Referenced by the scheduler engine to determine backoff strategies upon execution failure.

#### `public DbSet<ExecutionMetrics> ExecutionMetrics`

Gets or sets the collection of `ExecutionMetrics` entities.

*   **Purpose**: Aggregates performance data such as execution duration, resource consumption, and throughput statistics.
*   **Usage**: Utilized for system monitoring, alerting, and capacity planning.

#### `public DbSet<JobDependency> JobDependencies`

Gets or sets the collection of `JobDependency` entities.

*   **Purpose**: Defines relationships between jobs where one job must complete successfully before another can start.
*   **Usage**: The scheduler queries this set to resolve execution order and enforce precedence constraints.

#### `public DbSet<SchedulerLeaderLock> SchedulerLeaderLocks`

Gets or sets the collection of `SchedulerLeaderLock` entities.

*   **Purpose**: Manages leader election records in distributed environments to ensure only one scheduler instance processes triggers at a time.
*   **Usage**: Critical for preventing duplicate job executions in scaled-out deployments.

#### `public DbSet<JobPipeline> JobPipelines`

Gets or sets the collection of `JobPipeline` entities.

*   **Purpose**: Represents logical groupings of jobs that form a processing pipeline.
*   **Usage**: Allows for batch management and coordinated lifecycle operations on related jobs.

#### `public DbSet<JobPipelineStep> JobPipelineSteps`

Gets or sets the collection of `JobPipelineStep` entities.

*   **Purpose**: Defines the specific sequence and configuration of jobs within a `JobPipeline`.
*   **Usage**: Determines the execution flow and step-specific parameters within a pipeline context.

#### `public DbSet<DistributedJobLock> DistributedJobLocks`

Gets or sets the collection of `DistributedJobLock` entities.

*   **Purpose**: Stores active locks for specific jobs to prevent concurrent execution of the same job instance across multiple nodes.
*   **Usage**: Checked before job initiation to ensure mutual exclusion.

### Methods

#### `public override async Task<int> SaveChangesAsync()`

Asynchronously saves all changes made in this context to the database.

*   **Return Value**: A `Task<int>` representing the asynchronous operation. The result contains the number of state entries written to the database (i.e., the number of entities inserted, updated, or deleted).
*   **Purpose**: Commits pending changes tracked by the context, ensuring atomicity for complex scheduling operations.
*   **Exceptions**:
    *   `DbUpdateException`: Thrown if an error occurs while saving changes to the database (e.g., constraint violations, concurrency conflicts).
    *   `OperationCanceledException`: Thrown if the cancellation token (if applicable via overload) is canceled.
    *   Provider-specific exceptions may occur if the database connection is lost or the transaction fails.

## Usage

### Example 1: Registering and Seeding a New Job

This example demonstrates registering the context in a service collection and adding a new job definition with an associated retry policy.

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using dotnet_job_scheduler;

// Registration in Program.cs or Startup.cs
services.AddDbContext<JobSchedulerContext>(options =>
    options.UseSqlServer("Server=localhost;Database=SchedulerDb;Trusted_Connection=True;"));

// Usage within a service
public class JobRegistrationService
{
    private readonly JobSchedulerContext _context;

    public JobRegistrationService(JobSchedulerContext context)
    {
        _context = context;
    }

    public async Task RegisterMaintenanceJobAsync()
    {
        var retryPolicy = new RetryPolicy 
        { 
            Name = "DefaultRetry", 
            MaxAttempts = 3, 
            DelaySeconds = 60 
        };

        var job = new Job 
        { 
            Name = "NightlyCleanup", 
            CronExpression = "0 2 * * *", 
            RetryPolicyId = retryPolicy.Id 
        };

        _context.RetryPolicies.Add(retryPolicy);
        _context.Jobs.Add(job);

        // Persist changes to the database
        int affectedRows = await _context.SaveChangesAsync();
        
        Console.WriteLine($"Seeded {affectedRows} entities.");
    }
}
```

### Example 2: Querying Execution Metrics and Handling Concurrency

This example illustrates querying execution metrics for a specific job and updating the distributed lock state atomically.

```csharp
using Microsoft.EntityFrameworkCore;
using dotnet_job_scheduler;

public class MonitoringService
{
    private readonly JobSchedulerContext _context;

    public MonitoringService(JobSchedulerContext context)
    {
        _context = context;
    }

    public async Task<bool> TryAcquireLockAndLogMetricsAsync(string jobId)
    {
        // Check for existing active lock
        var existingLock = await _context.DistributedJobLocks
            .FirstOrDefaultAsync(l => l.JobId == jobId && l.ExpiresAt > DateTime.UtcNow);

        if (existingLock != null)
        {
            return false; // Lock already held
        }

        // Record metrics before attempting execution
        var metrics = new ExecutionMetrics
        {
            JobId = jobId,
            Timestamp = DateTime.UtcNow,
            Event = "LockAcquisitionAttempt"
        };
        
        _context.ExecutionMetrics.Add(metrics);

        // Create new lock entry
        var newLock = new DistributedJobLock
        {
            JobId = jobId,
            OwnerId = Environment.MachineName,
            AcquiredAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };
        
        _context.DistributedJobLocks.Add(newLock);

        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            // Handle potential race condition where another node acquired the lock simultaneously
            return false;
        }
    }
}
```

## Notes

*   **Thread Safety**: `JobSchedulerContext` instances are not thread-safe. A single instance should not be shared across multiple threads performing concurrent database operations. In ASP.NET Core applications, the context is typically registered with a scoped lifetime, ensuring a new instance per request.
*   **Concurrency Conflicts**: When calling `SaveChangesAsync`, be aware of potential `DbUpdateConcurrencyException` scenarios, particularly when updating `DistributedJobLocks` or `SchedulerLeaderLocks` in high-contention distributed environments. Implement retry logic or use optimistic concurrency tokens on relevant entities if not already configured.
*   **Transaction Scope**: The `SaveChangesAsync` method automatically wraps changes in a database transaction. If multiple `DbSet` modifications (e.g., adding a `Job` and its `JobDependencies`) must succeed or fail together, rely on this default behavior. For operations spanning multiple contexts or external resources, utilize an explicit `IDbContextTransaction`.
*   **Disposable Resource**: As `JobSchedulerContext` implements `IDisposable` (inherited from `DbContext`), it should be used within a `using` statement or managed by a dependency injection container that handles disposal to ensure database connections are properly released.
