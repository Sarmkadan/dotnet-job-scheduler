# Repository Interfaces Documentation

This document describes the three repository interfaces in `src/JobScheduler.Core/Data/Repositories/`: `IRepository<T>`, `IJobRepository`, and `IExecutionRepository`.

## IRepository<T> - Base Repository Interface

The generic base repository interface defining standard CRUD operations and query methods.

| Method Signature | Purpose |
|------------------|---------|
| `Task<T?> GetByIdAsync(Guid id)` | Retrieves an entity by its unique identifier |
| `Task<IEnumerable<T>> GetAllAsync()` | Retrieves all entities of type T |
| `Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)` | Finds entities matching a predicate expression |
| `Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)` | Returns the first entity matching a predicate or default |
| `Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)` | Counts entities, optionally filtered by predicate |
| `Task AddAsync(T entity)` | Adds a new entity to the repository |
| `Task AddRangeAsync(IEnumerable<T> entities)` | Adds multiple entities to the repository |
| `void Update(T entity)` | Updates an existing entity |
| `void UpdateRange(IEnumerable<T> entities)` | Updates multiple existing entities |
| `void Remove(T entity)` | Removes an entity from the repository |
| `void RemoveRange(IEnumerable<T> entities)` | Removes multiple entities from the repository |
| `Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)` | Checks if any entities match the predicate |
| `Task SaveChangesAsync()` | Persists all changes to the underlying data store |

**Implementing Class:** `Repository<T>` (in `Repository.cs`)

**Registration:** Registered as `services.AddScoped(typeof(IRepository<>), typeof(Repository<>));` in `DependencyInjectionExtensions.cs`

---

## IJobRepository - Job-Specific Repository Interface

Extends `IRepository<Job>` with job domain-specific query methods.

| Method Signature | Purpose |
|------------------|---------|
| `Task<Job?> GetByNameAsync(string name)` | Retrieves a job by its name |
| `Task<IEnumerable<Job>> GetActiveJobsAsync()` | Gets all active jobs (not cancelled/suspended) |
| `Task<IEnumerable<Job>> GetJobsByStatusAsync(JobStatus status)` | Gets jobs filtered by status |
| `Task<IEnumerable<Job>> GetJobsByPriorityAsync(JobPriority priority)` | Gets active jobs filtered by priority level |
| `Task<IEnumerable<Job>> GetScheduledJobsForExecutionAsync()` | Gets jobs due for execution (next execution time <= now) |
| `Task<IEnumerable<Job>> GetMisfiredJobsAsync()` | Gets jobs that missed their scheduled execution time |
| `Task<IEnumerable<Job>> GetFailedJobsAsync()` | Gets jobs with failed status |
| `Task<IEnumerable<Job>> GetLongRunningJobsAsync(int thresholdSeconds)` | Gets jobs running longer than threshold |
| `Task<IEnumerable<Job>> GetJobsWithoutRecentExecutionAsync(int minutesThreshold)` | Gets active jobs without recent execution |

**Implementing Class:** `JobRepository` (in `JobRepository.cs`)

**Registration:** Registered as `services.AddScoped<IJobRepository, JobRepository>();` in `DependencyInjectionExtensions.cs`

---

## IExecutionRepository - Job Execution Tracking Interface

Extends `IRepository<JobExecution>` with execution history and status query methods.

| Method Signature | Purpose |
|------------------|---------|
| `Task<JobExecution?> GetLatestExecutionAsync(Guid jobId)` | Gets the most recent execution for a job |
| `Task<IEnumerable<JobExecution>> GetExecutionsByJobAsync(Guid jobId)` | Gets all executions for a specific job |
| `Task<IEnumerable<JobExecution>> GetExecutionsByStatusAsync(ExecutionStatus status)` | Gets executions filtered by status |
| `Task<IEnumerable<JobExecution>> GetExecutionsByJobAndStatusAsync(Guid jobId, ExecutionStatus status)` | Gets executions for a job filtered by status |
| `Task<int> GetCurrentlyRunningCountAsync(Guid jobId)` | Gets count of currently running executions for a job |
| `Task<int> GetConcurrentRunningCountAsync()` | Gets total count of currently running executions across all jobs |
| `Task<IEnumerable<JobExecution>> GetRunningExecutionsAsync()` | Gets all currently running executions |
| `Task<IEnumerable<JobExecution>> GetFailedExecutionsRequiringRetryAsync()` | Gets failed executions that are eligible for retry |
| `Task<IEnumerable<JobExecution>> GetExecutionsByDateRangeAsync(DateTime startDate, DateTime endDate)` | Gets executions within a date range |
| `Task<long> GetAverageExecutionTimeAsync(Guid jobId, int? lastN = null)` | Gets average execution time for a job (optionally for last N executions) |
| `Task<List<JobExecution>> GetByJobIdAsync(Guid jobId)` | Gets all executions for a job as a materialized list (newest first) |

**Implementing Class:** `ExecutionRepository` (in `ExecutionRepository.cs`)

**Registration:** Registered as `services.AddScoped<IExecutionRepository, ExecutionRepository>();` in `DependencyInjectionExtensions.cs`

---

## Dependency Injection Registration Summary

All three repository interfaces are registered in `src/JobScheduler.Core/Configuration/DependencyInjectionExtensions.cs` within the `AddJobScheduler` method:

```csharp
// Register repositories
services.AddScoped<IJobRepository, JobRepository>();
services.AddScoped<IExecutionRepository, ExecutionRepository>();
services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
```

This registration follows the scoped lifetime pattern, ensuring each HTTP request or processing scope gets its own repository instances with isolated DbContext objects.