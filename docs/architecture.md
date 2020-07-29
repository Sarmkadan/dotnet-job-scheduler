# Architecture Guide

Comprehensive technical documentation of dotnet-job-scheduler's design, patterns, and internals.

## System Design Principles

1. **Separation of Concerns**: Clear layer boundaries (domain, service, data access, API)
2. **SOLID Principles**: Dependency inversion, single responsibility, open/closed
3. **Async-First**: All I/O operations are non-blocking
4. **Database Agnostic**: Entity Framework Core abstraction supports multiple databases
5. **Testability**: Services depend on abstractions, enabling easy mocking
6. **Scalability**: Designed for horizontal scaling with external coordination

## Layered Architecture

```
┌────────────────────────────────────────┐
│         Application Layer              │
│  Controllers, Background Services      │
│  API Endpoints, Hosted Services        │
└─────────────────┬──────────────────────┘
                  │
┌─────────────────▼──────────────────────┐
│         Domain Layer                   │
│  Entities: Job, JobExecution           │
│  Enums: JobStatus, JobPriority         │
│  Value Objects, Domain Logic           │
└─────────────────┬──────────────────────┘
                  │
┌─────────────────▼──────────────────────┐
│         Service Layer                  │
│  Business Logic & Orchestration        │
│  JobSchedulerService                   │
│  JobExecutorService                    │
│  CronExpressionService                 │
│  RetryService                          │
│  ConcurrencyManager                    │
└─────────────────┬──────────────────────┘
                  │
┌─────────────────▼──────────────────────┐
│      Data Access Layer (Repository)    │
│  IRepository<T>                        │
│  JobRepository                         │
│  ExecutionRepository                   │
│  Entity Framework Core Abstraction     │
└─────────────────┬──────────────────────┘
                  │
┌─────────────────▼──────────────────────┐
│         Persistence Layer              │
│  Entity Framework Core DbContext       │
│  Database Providers                    │
│  Migrations, Schemas                   │
└────────────────────────────────────────┘
```

## Domain Entities

### Job Entity

Core entity representing a scheduled job:

```
Job
├── Identification
│   ├── Id: int
│   ├── Name: string (unique)
│   └── Description: string?
│
├── Scheduling
│   ├── CronExpression: string (POSIX format)
│   ├── NextExecutionTime: DateTime?
│   ├── LastExecutionTime: DateTime?
│   └── TimeZone: string?
│
├── Execution
│   ├── HandlerType: string (fully qualified)
│   ├── ExecutionTimeoutSeconds: int
│   ├── MaxConcurrentExecutions: int
│   └── Status: JobStatus enum
│
├── Retry Policy
│   ├── MaxRetries: int
│   ├── RetryBackoffSeconds: int
│   └── RetryBackoffType: BackoffType enum
│
├── Prioritization
│   ├── Priority: JobPriority enum
│   └── IsActive: bool
│
├── Metadata
│   ├── CreatedAt: DateTime
│   ├── CreatedBy: string?
│   ├── ModifiedAt: DateTime?
│   ├── ModifiedBy: string?
│   └── DeletedAt: DateTime?
│
└── Relations
    ├── JobExecutions: List<JobExecution>
    └── ScheduleHistory: List<JobScheduleHistory>
```

### JobExecution Entity

Represents single job execution attempt:

```
JobExecution
├── Identification
│   ├── Id: int
│   └── JobId: int (foreign key)
│
├── Execution Details
│   ├── ExecutedAt: DateTime (when started)
│   ├── CompletedAt: DateTime? (when finished)
│   ├── Duration: TimeSpan? (computed)
│   └── Status: ExecutionStatus enum
│
├── Results
│   ├── Result: string? (output/message)
│   ├── ErrorMessage: string? (exception details)
│   ├── StackTrace: string? (exception stack)
│   └── ExitCode: int?
│
├── Retry Information
│   ├── RetryAttempt: int (0 = original)
│   ├── NextRetryTime: DateTime?
│   └── IsFinal: bool
│
├── Performance Metrics
│   ├── MemoryUsageMb: int?
│   ├── CpuUsagePercent: double?
│   ├── DatabaseQueryCount: int?
│   └── ExternalApiCallCount: int?
│
└── Operational
    ├── ServerName: string? (which server executed)
    ├── ProcessId: int?
    └── ThreadId: int?
```

### RetryPolicy Value Object

```
RetryPolicy
├── MaxRetries: int (0-10)
├── InitialBackoffSeconds: int (1-3600)
├── BackoffMultiplier: double (for exponential)
├── MaxBackoffSeconds: int (max delay)
└── BackoffType: enum (Exponential, Linear, Fixed)

Calculations:
├── Exponential: delay = min(initial * (2^attempt), max)
├── Linear: delay = initial * attempt (capped at max)
└── Fixed: delay = initial (always)
```

## Service Layer

### JobSchedulerService (Orchestrator)

Central service managing job lifecycle:

**Responsibilities**:
- CRUD operations on jobs
- Cron expression evaluation
- Next execution time calculation
- Execution queue management
- Job status transitions

**Key Methods**:

```csharp
// Job Lifecycle
Task<Job> CreateJobAsync(Job job, string createdBy);
Task<Job> UpdateJobAsync(Job job, string modifiedBy);
Task DeleteJobAsync(int jobId, string deletedBy);
Task<Job?> GetJobByIdAsync(int jobId);

// Execution Management
Task<List<JobExecution>> ExecuteDueJobsAsync();
Task SuspendJobAsync(int jobId, string reason);
Task ResumeJobAsync(int jobId);

// Status & Metrics
Task<SchedulerStatistics> GetSchedulerStatisticsAsync();
Task<PagedResult<Job>> QueryJobsAsync(JobQuery query);
```

### JobExecutorService (Execution Engine)

Responsible for executing individual jobs:

**Responsibilities**:
- Instantiate job handlers via DI
- Execute handler with timeout
- Capture execution results
- Handle exceptions and logging
- Record metrics

**Execution Flow**:
```
1. Receive job and context
2. Validate job can execute (concurrency, status)
3. Create cancellation token with timeout
4. Instantiate handler from DI container
5. Call handler.ExecuteAsync()
6. Capture result, duration, metrics
7. Record execution in database
8. Update job's NextExecutionTime
9. Trigger events (success/failure)
```

### CronExpressionService

Parses and evaluates POSIX cron expressions:

**Features**:
- Full POSIX support (5 fields + optional seconds)
- Timezone-aware calculation
- Next/previous execution time
- Cron expression validation
- Performance optimized with caching

**Expression Parts**:
```
┌─────────────┬──────────┬──────────┬──────────┬──────────────┐
│   Minute    │   Hour   │   Day    │  Month   │  Weekday     │
│   (0-59)    │  (0-23)  │  (1-31)  │  (1-12)  │   (0-6)      │
└─────────────┴──────────┴──────────┴──────────┴──────────────┘
     0          9           *         *            *
```

### RetryService

Manages retry logic and backoff calculations:

**Responsibilities**:
- Determine if execution should retry
- Calculate backoff delay
- Schedule next retry
- Track retry attempts
- Update job status

**Retry Decision Logic**:
```
Is job failed?
├─ No → Mark as Completed
└─ Yes → Exceeded max retries?
    ├─ No → Calculate backoff, schedule retry, mark as "Failed"
    └─ Yes → Mark as "FailedPermanently"
```

### ConcurrencyManager

Enforces concurrency constraints:

**Responsibilities**:
- Check global concurrent job limits
- Check per-job concurrent execution limits
- Prevent overload
- Queue jobs if limits exceeded

**Logic**:
```
Global Limit = MaxConcurrentJobs (e.g., 10)
Per-Job Limit = Job.MaxConcurrentExecutions (e.g., 1)

Can execute?
├─ Running count < Global limit?
├─ AND Job running count < Per-Job limit?
└─ Then execute, else queue
```

### ExecutionStatisticsService

Analyzes execution metrics:

**Calculations**:
- Total executions (by status, time range, job)
- Success rate percentage
- Average/min/max duration
- Error rate by error type
- Execution trends
- Resource utilization metrics

## Repository Pattern

### IRepository<T> (Generic Base)

```csharp
public interface IRepository<T>
{
    Task<T?> GetByIdAsync(int id);
    Task<List<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task DeleteAsync(int id);
    IQueryable<T> AsQueryable();
}
```

### Specialized Repositories

#### JobRepository

```csharp
public interface IJobRepository : IRepository<Job>
{
    Task<Job?> GetByNameAsync(string name);
    Task<List<Job>> GetActiveJobsAsync();
    Task<List<Job>> GetDueJobsAsync(DateTime now);
    Task<PagedResult<Job>> QueryAsync(JobQuery query);
}
```

Implementation:
- Uses `IQueryable` for lazy evaluation
- LINQ expressions compiled to SQL
- Efficient pagination with Skip/Take
- Query optimization with includes

#### ExecutionRepository

```csharp
public interface IExecutionRepository : IRepository<JobExecution>
{
    Task<List<JobExecution>> GetByJobIdAsync(int jobId);
    Task<JobExecution?> GetLastExecutionAsync(int jobId);
    Task<ExecutionMetrics> GetMetricsAsync(int? jobId, 
        DateTime startDate, DateTime endDate);
    IAsyncEnumerable<JobExecution> StreamByJobAsync(int jobId);
}
```

Implementation:
- Query optimization with filtered includes
- Metrics aggregation with GROUP BY
- Streaming for large result sets
- Index utilization for date ranges

## Data Access Layer (EF Core)

### JobSchedulerContext

```csharp
public class JobSchedulerContext : DbContext
{
    public DbSet<Job> Jobs { get; set; }
    public DbSet<JobExecution> JobExecutions { get; set; }
    public DbSet<JobScheduleHistory> ScheduleHistory { get; set; }
    public DbSet<ExecutionMetrics> ExecutionMetrics { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Fluent API configuration
        // Entity relationships
        // Index definitions
        // Constraints
    }
}
```

### Key Configurations

**Relationships**:
- Job → JobExecutions (1:N, cascade delete)
- Job → ScheduleHistory (1:N)

**Indices**:
- `Job.Name` (unique)
- `Job.Status, IsActive` (query optimization)
- `JobExecution.JobId, ExecutedAt` (history queries)
- `JobExecution.Status, ExecutedAt` (dashboard)

**Constraints**:
- Job.Name: NOT NULL, UNIQUE
- Job.CronExpression: NOT NULL, validated
- Job.MaxConcurrentExecutions: >= 1
- JobExecution.Status: enum validation

## Execution Flow Diagram

### Job Scheduling and Execution

```
Timer triggers (every 5 seconds)
          │
          ▼
┌──────────────────────────────┐
│ ExecuteDueJobsAsync()        │
│ (JobSchedulerService)        │
└──────────────────────────────┘
          │
          ▼ Get all active jobs
┌──────────────────────────────┐
│ Query due jobs               │
│ WHERE NextExecutionTime      │
│   <= DateTime.UtcNow         │
│ ORDER BY Priority DESC       │
└──────────────────────────────┘
          │
          ▼ For each due job
┌──────────────────────────────┐
│ ConcurrencyManager           │
│ Check execution limits       │
└──────────────────────────────┘
          │
     ┌────┴────┐
     │          │
     ▼          ▼
  Can Execute   Queue Job
     │
     ▼
┌──────────────────────────────┐
│ JobExecutorService           │
│ ExecuteAsync(job)            │
└──────────────────────────────┘
     │
     ├─ Create CancellationToken(timeout)
     ├─ Resolve handler from DI
     ├─ Call handler.ExecuteAsync()
     └─ Capture result
     │
     ▼
┌──────────────────────────────┐
│ Execution completed/failed   │
│ Record in JobExecutions      │
│ Update Job.LastExecutionTime │
└──────────────────────────────┘
     │
     ▼ Publish events
┌──────────────────────────────┐
│ EventPublisher               │
│ OnJobCompleted/OnJobFailed   │
└──────────────────────────────┘
     │
     ▼ Calculate next execution
┌──────────────────────────────┐
│ CronExpressionService        │
│ GetNextOccurrence()          │
└──────────────────────────────┘
     │
     ▼
┌──────────────────────────────┐
│ Update Job.NextExecutionTime │
│ Save to database             │
└──────────────────────────────┘
```

## Dependency Injection

### Service Registration

```csharp
public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddJobScheduler(
        this IServiceCollection services,
        Action<JobSchedulerSettings> configure)
    {
        // Register DbContext
        services.AddDbContext<JobSchedulerContext>(
            options => options.UseSqlite(connectionString)
        );

        // Register repositories
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IJobRepository, JobRepository>();
        services.AddScoped<IExecutionRepository, ExecutionRepository>();

        // Register services
        services.AddScoped<JobSchedulerService>();
        services.AddScoped<JobExecutorService>();
        services.AddScoped<CronExpressionService>();
        services.AddScoped<RetryService>();
        services.AddScoped<ConcurrencyManager>();
        
        // Register background services
        services.AddScoped<JobSchedulerBackgroundService>();

        return services;
    }
}
```

### DI Patterns Used

1. **Constructor Injection**: All dependencies required at construction
2. **Service Locator**: Via `IServiceProvider` for runtime resolution
3. **Factory Pattern**: For creating handler instances
4. **Transient Services**: Stateless utilities
5. **Scoped Services**: Per-request/execution lifetime
6. **Singleton Services**: Shared configurations

## Error Handling Strategy

### Custom Exception Hierarchy

```
JobSchedulerException (base)
├── JobNotFoundException
├── JobValidationException
├── CronExpressionException
├── ExecutionException
└── ConcurrencyException
```

### Exception Handling Flow

```
Request → Controller
            │
            ▼
        Service Layer
            │
     ┌──────┴──────┐
     │             │
     ▼             ▼
  Success      Exception
     │             │
     │             ▼
     │        Create JobExecution
     │        with ErrorMessage
     │             │
     │             ▼
     │        RetryService decision
     │             │
     │        ┌────┴────┐
     │        │          │
     │        ▼          ▼
     │     Retry    Permanent Failure
     │        │
     └────────┴─────────────┐
                            │
                            ▼
                    GlobalExceptionMiddleware
                    → Log error
                    → Return 500 with error details
```

## Caching Strategy

### Cache Keys

- **Job Configuration**: `job:{jobId}`
- **Cron Patterns**: `cron:{expression}`
- **Metrics**: `metrics:{jobId}:{date}`
- **Status Counts**: `status:counts`

### Cache Invalidation

- Job updated → Invalidate `job:{jobId}`
- Execution completed → Invalidate `metrics:*`
- Job deleted → Invalidate `job:{jobId}`, `status:counts`

### TTLs

- Job config: 1 hour
- Cron patterns: 24 hours
- Metrics: 5 minutes
- Status counts: 1 minute

## Performance Optimization

### Database Optimization

1. **Connection Pooling**: EF Core default (5-100 connections)
2. **Query Caching**: DbQuery results cached per DbContext
3. **Batch Operations**: Insert/update multiple records at once
4. **Soft Deletes**: Logical deletion via `DeletedAt`
5. **Pagination**: Always use Skip/Take for large result sets

### Memory Optimization

1. **Stream Large Results**: `IAsyncEnumerable<T>` for large datasets
2. **Release Context**: Dispose DbContext after operations
3. **Limit Cache**: TTL-based expiration, size limits
4. **Lazy Loading Disabled**: Always use `.Include()` for needed relations

### CPU Optimization

1. **Async All the Way**: Non-blocking I/O
2. **Index Usage**: Database level filtering
3. **Compiled Queries**: For repeated queries
4. **Parallel Execution**: Multiple jobs concurrently (up to limit)

## Scalability Considerations

### Horizontal Scaling

Multiple scheduler instances:

```
┌─────────────────┐
│  Instance 1     │
│  JobScheduler   │──┐
│  Service        │  │
└─────────────────┘  │
                     ├──→ Shared Database
┌─────────────────┐  │
│  Instance 2     │  │
│  JobScheduler   │──┘
│  Service        │
└─────────────────┘

Coordination:
- Distributed locking for due job selection
- Database constraints prevent duplicate execution
- Each instance processes different jobs
```

### Database Constraints

- Unique constraint on `(JobId, ExecutedAt)` prevents duplicates
- Version field prevents concurrent updates
- Transactional updates ensure consistency

### Partitioning Strategy

For very large deployments:

- **By Job ID**: Each instance handles jobs with IDs % instance_count == id
- **By Status**: Some instances handle scheduled, others handle failed
- **By Priority**: High-priority jobs on dedicated instances

## Extension Points

### Custom Job Handlers

Implement `IJobHandler` interface:

```csharp
public interface IJobHandler
{
    Task<string> ExecuteAsync(Job job, CancellationToken cancellationToken);
}
```

### Event Subscribers

Listen to execution events:

```csharp
public interface IJobEventSubscriber
{
    Task OnJobStartedAsync(Job job);
    Task OnJobCompletedAsync(Job job, JobExecution execution);
    Task OnJobFailedAsync(Job job, JobExecution execution);
}
```

### Custom Repositories

Extend `IRepository<T>` for specialized queries:

```csharp
public interface ICustomRepository : IRepository<Job>
{
    Task<List<Job>> GetSlowJobsAsync(TimeSpan threshold);
}
```

## Testing Architecture

### Unit Testing

```
Service Tests
├── Mock repositories
├── Mock dependencies
└── Test business logic

Repository Tests
├── In-memory DbContext
├── SQLite test database
└── Query correctness
```

### Integration Testing

```
Database Tests
├── Real database (SQLite)
├── EF Core migrations
└── End-to-end job execution
```

### Performance Testing

```
Load Tests
├── Multiple concurrent jobs
├── High concurrency scenarios
└── Database throughput
```

This architecture provides a solid foundation for a production-grade job scheduler with clear separation of concerns, extensibility, and performance optimization opportunities.
