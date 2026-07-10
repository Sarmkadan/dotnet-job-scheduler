# EventPublisher
Lightweight in‑memory publish/subscribe helper used by the scheduler to distribute typed events to interested listeners.

## API
### `EventPublisher()`
Initializes a new instance. No parameters. Does not throw.

### `public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)`
Publishes an event of type `TEvent` to all current subscribers.

- **Parameters**
  - `@event`: The event instance to publish. Must not be `null`.
  - `cancellationToken`: Optional token to observe cancellation requests.
- **Return Value**: A `Task` that completes when all synchronous handlers have finished.
- **Exceptions**
  - `ArgumentNullException` if `@event` is `null`.
  - `OperationCanceledException` if `cancellationToken` is triggered before publishing completes.
  - `ObjectDisposedException` if the publisher has been disposed.

### `public IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler)`
Registers an asynchronous handler for events of type `TEvent`.

- **Parameters**
  - `handler`: Async delegate invoked when an event of type `TEvent` is published. Must not be `null`.
- **Return Value**: An `IDisposable` that, when disposed, unsubscribes the handler.
- **Exceptions**
  - `ArgumentNullException` if `handler` is `null`.
  - `ObjectDisposedException` if the publisher has been disposed.

### `public void Unsubscribe<TEvent>(IDisposable token)`
Removes the subscription represented by `token`.

- **Parameters**
  - `token`: The `IDisposable` returned by `Subscribe<TEvent>`. Must not be `null` and must have been obtained from this publisher.
- **Exceptions**
  - `ArgumentNullException` if `token` is `null`.
  - `InvalidOperationException` if `token` was not created by this `EventPublisher`.
  - `ObjectDisposedException` if the publisher has been disposed.

### `public async Task<TEvent> WaitForEventAsync<TEvent>(CancellationToken cancellationToken = default)`
Creates a task that completes when the next event of type `TEvent` is published.

- **Parameters**
  - `cancellationToken`: Optional token to cancel the wait.
- **Return Value**: A `Task<TEvent>` yielding the received event instance.
- **Exceptions**
  - `OperationCanceledException` if `cancellationToken` is triggered before an event arrives.
  - `ObjectDisposedException` if the publisher has been disposed.

### `public List<string> GetActiveEventTypes()`
Returns the names of event types that currently have at least one subscriber.

- **Return Value**: A list of strings; empty if no subscribers exist.
- **Exceptions**
  - `ObjectDisposedException` if the publisher has been disposed.

### `public int GetSubscriberCount<TEvent>()`
Returns the number of subscribers registered for `TEvent`.

- **Return Value**: Integer count (zero if none).
- **Exceptions**
  - `ObjectDisposedException` if the publisher has been disposed.

### `public void ClearSubscriptions<TEvent>()`
Removes all subscribers for the specific event type `TEvent`.

- **Exceptions**
  - `ObjectDisposedException` if the publisher has been disposed.

### `public void ClearAllSubscriptions()`
Removes all subscribers for every event type.

- **Exceptions**
  - `ObjectDisposedException` if the publisher has been disposed.

### `public string EventType { get; }`
Gets the name of the event type that the publisher is currently associated with (if any); returns `null` when no event type is set.

- **Exceptions**
  - `ObjectDisposedException` if the publisher has been disposed.

### `public Delegate Handler { get; }`
Gets the combined delegate of all subscribers for the current event type; returns `null` when there are no subscribers.

- **Exceptions**
  - `ObjectDisposedException` if the publisher has been disposed.

### `public SubscriptionToken SubscriptionToken { get; }`
Gets an opaque token representing the current subscription (if any); can be passed to `Unsubscribe<TEvent>` to detach it. Returns `null` when no subscription exists.

- **Exceptions**
  - `ObjectDisposedException` if the publisher has been disposed.

### `public void Dispose()`
Releases all internal resources. After disposal, any further call to a member of this type throws `ObjectDisposedException`.

- **Exceptions**
  - None.

## Usage
### Example 1: Basic publish/subscribe
```csharp
var publisher = new EventPublisher();

using var subscription = publisher.Subscribe<OrderPlaced>(async e =>
{
    await ProcessOrderAsync(e);
});

// Somewhere else in the code
await publisher.PublishAsync(new OrderPlaced { OrderId = 123 });
```
The handler is invoked for each published `OrderPlaced` event and is automatically unsubscribed when the `using` block ends.

### Example 2: Waiting for a specific event
```csharp
var publisher = new EventPublisher();

// Start a background task that waits for the next "JobCompleted" event
var waitTask = publisher.WaitForEventAsync<JobCompleted>();

// Simulate work that eventually publishes the event
await DoWorkAsync();
await publisher.PublishAsync(new JobCompleted { JobId = 42, Succeeded = true });

// The waiting task now completes with the received event
JobCompleted completed = await waitTask;
Console.WriteLine($"Job {completed.JobId} finished with success: {completed.Succeeded}");
```
`WaitForEventAsync<TEvent>` enables a consumer to pause execution until a particular event occurs, without polling.

## Notes
- All members are thread‑safe; subscriptions may be added, removed, or queried from any thread concurrently.
- `PublishAsync` does not block awaiting asynchronous handlers; it returns once all handlers have been invoked.
- If a subscription is disposed while a `WaitForEventAsync<TEvent>` operation is pending, the waiting task is **not** cancelled; it will still complete when the next matching event is published.
- Calling `ClearSubscriptions<TEvent>` or `ClearAllSubscriptions` does not affect already‑created `WaitForEventAsync<TEvent>` tasks; they continue to await future events.
- The `EventType`, `Handler`, and `SubscriptionToken` properties reflect the state at the moment of access and may change concurrently; callers should not rely on their values remaining stable across calls.
- Disposing the publisher while a `WaitForEventAsync<TEvent>` task is pending causes that task to throw `ObjectDisposedException`. Likewise, any subsequent call to `PublishAsync`, `Subscribe`, `Unsubscribe`, `GetActiveEventTypes`, `GetSubscriberCount`, `ClearSubscriptions`, `ClearAllSubscriptions`, or access to the properties throws `ObjectDisposedException`.
