// ... (rest of README.md content)

## IEventPublisher

The `IEventPublisher` interface is responsible for publishing events to all registered subscribers in the job scheduler. It provides a way to decouple event producers from consumers, allowing for a more flexible and scalable architecture.

Example usage:
```csharp
// Create an instance of IEventPublisher
var eventPublisher = new EventPublisher();

// Publish a JobCreatedEvent
var jobCreatedEvent = new JobCreatedEvent
{
    EventId = Guid.NewGuid(),
    Timestamp = DateTime.UtcNow,
    JobId = Guid.NewGuid(),
    JobName = "My Job",
    CreatedBy = "John Doe"
};
await eventPublisher.PublishAsync(jobCreatedEvent);

// Publish a JobExecutionStartedEvent
var jobExecutionStartedEvent = new JobExecutionStartedEvent
{
    EventId = Guid.NewGuid(),
    Timestamp = DateTime.UtcNow,
    JobId = Guid.NewGuid(),
    ExecutionId = Guid.NewGuid(),
    JobName = "My Job"
};
await eventPublisher.PublishAsync(jobExecutionStartedEvent);

// Publish a JobExecutionCompletedEvent
var jobExecutionCompletedEvent = new JobExecutionCompletedEvent
{
    EventId = Guid.NewGuid(),
    Timestamp = DateTime.UtcNow,
    JobId = Guid.NewGuid(),
    ExecutionId = Guid.NewGuid(),
    JobName = "My Job",
    Success = true,
    ExecutionTimeMs = 1000
};
await eventPublisher.PublishAsync(jobExecutionCompletedEvent);
```

## CollectionExtensions

The `CollectionExtensions` class provides a comprehensive set of extension methods for working with collections, lists, and enumerables in a fluent and memory-efficient way. These utilities are designed to simplify common operations like batching, filtering, and transformation while preventing common exceptions like `NullReferenceException` or `ArgumentOutOfRangeException`.

The extensions include safe accessors, batching operations for pagination, random sampling, and conditional iteration patterns that are particularly useful in job scheduling scenarios where collections need to be processed in chunks or filtered based on conditions.

Example usage:
```csharp
// Sample job data
var jobs = new List<Job>
{
    new Job { Id = Guid.NewGuid(), Name = "Data Sync Job", Priority = 1 },
    new Job { Id = Guid.NewGuid(), Name = "Report Generation Job", Priority = 2 },
    new Job { Id = Guid.NewGuid(), Name = "Cleanup Job", Priority = 3 },
    new Job { Id = Guid.NewGuid(), Name = "Backup Job", Priority = 1 },
    new Job { Id = Guid.NewGuid(), Name = "Indexing Job", Priority = 2 }
};

// Batch jobs for parallel processing (prevents memory exhaustion)
var jobBatches = jobs.Batch(2);
foreach (var batch in jobBatches)
{
    Console.WriteLine($"Processing batch with {batch.Count()} jobs");
    // Process each batch in parallel
}

// Safely get job at specific index
var firstJob = jobs.SafeGetAt(0);
var nonExistentJob = jobs.SafeGetAt(100); // Returns null instead of throwing

// Check if collection is empty or has items
if (jobs.IsEmpty())
{
    Console.WriteLine("No jobs available");
}

if (jobs.HasItems())
{
    Console.WriteLine($"Found {jobs.Count} jobs");
}

// Process only high priority jobs
var highPriorityJobs = jobs.ForEachWhere(
    job => job.Priority > 1,
    job => Console.WriteLine($"Processing high priority job: {job.Name}")
);

// Get random sample of jobs for analysis
var randomJobs = jobs.Random(3);
Console.WriteLine($"Random sample: {string.Join(", ", randomJobs.Select(j => j.Name))}");

// Chunk jobs into fixed-size groups
var jobChunks = jobs.Chunk(2);
foreach (var chunk in jobChunks)
{
    Console.WriteLine($"Chunk with {chunk.Count} jobs");
}

// Distinct jobs by priority
var distinctPriorityJobs = jobs.DistinctBy(job => job.Priority);

// Convert to page for pagination
var page1 = jobs.ToPage(1, 2);
Console.WriteLine($"Page 1 has {page1.Count} jobs");

// Count jobs matching a condition
var highPriorityCount = jobs.CountWhere(job => job.Priority > 1);
Console.WriteLine($"High priority jobs: {highPriorityCount}");

// Safe cast from non-generic collection
var objectList = new ArrayList { new Job { Name = "Test Job" } };
var typedJobs = objectList.SafeCast<Job>();

// Take jobs while condition is true
var priorityJobs = jobs.TakeWhile(job => job.Priority <= 2);
```

## EventPublisher

The `EventPublisher` class implements the `IEventPublisher` interface and provides an in-memory event publishing system using the pub-sub pattern. It enables decoupled communication between components in the job scheduler by allowing event producers to publish events without knowing their consumers.

The publisher maintains a registry of subscribers for each event type and delivers published events to all registered handlers in parallel. It also supports waiting for specific events, which is particularly useful for testing event-driven scenarios.

Example usage:
```csharp
// Create an instance with dependency injection
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var eventPublisher = new EventPublisher(loggerFactory.CreateLogger<EventPublisher>());

// Subscribe to JobCreatedEvent
var subscription = eventPublisher.Subscribe<JobCreatedEvent>(async jobCreatedEvent =>
{
    Console.WriteLine($"Job created: {jobCreatedEvent.JobName} by {jobCreatedEvent.CreatedBy}");
});

// Subscribe with multiple handlers
var count = 0;
var anotherSubscription = eventPublisher.Subscribe<JobExecutionStartedEvent>(async executionEvent =>
{
    count++;
    Console.WriteLine($"Job execution started: {executionEvent.JobName} (count: {count})");
});

// Publish events
var jobCreatedEvent = new JobCreatedEvent
{
    EventId = Guid.NewGuid(),
    Timestamp = DateTime.UtcNow,
    JobId = Guid.NewGuid(),
    JobName = "Data Processing Job",
    CreatedBy = "scheduler-service"
};

var executionStartedEvent = new JobExecutionStartedEvent
{
    EventId = Guid.NewGuid(),
    Timestamp = DateTime.UtcNow,
    JobId = Guid.NewGuid(),
    ExecutionId = Guid.NewGuid(),
    JobName = "Data Processing Job"
};

await eventPublisher.PublishAsync(jobCreatedEvent);
await eventPublisher.PublishAsync(executionStartedEvent);

// Wait for a specific event (useful in tests)
var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
var eventTask = eventPublisher.WaitForEventAsync<JobExecutionCompletedEvent>(TimeSpan.FromSeconds(10));

var completedTask = await Task.WhenAny(timeoutTask, eventTask);
if (completedTask == eventTask)
{
    var completedEvent = await eventTask;
    Console.WriteLine($"Job completed: Success={completedEvent.Success}, Time={completedEvent.ExecutionTimeMs}ms");
}

// Get statistics
var activeTypes = eventPublisher.GetActiveEventTypes();
var subscriberCount = eventPublisher.GetSubscriberCount<JobCreatedEvent>();
Console.WriteLine($"Active event types: {string.Join(", ", activeTypes)}");
Console.WriteLine($"JobCreatedEvent subscribers: {subscriberCount}");

// Unsubscribe when done
subscription.Dispose();
anotherSubscription.Dispose();

// Clear all subscriptions during shutdown
// eventPublisher.ClearAllSubscriptions();
```
