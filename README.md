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
