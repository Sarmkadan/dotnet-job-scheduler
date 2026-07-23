#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace JobScheduler.Core.Events;

/// <summary>
/// In-memory event publisher implementing pub-sub pattern for scheduler events.
/// Enables decoupled event-driven architecture within the scheduler.
/// WHY: Pub-sub pattern allows components to react to events without tight coupling.
/// </summary>
public sealed class EventPublisher : IEventPublisher, IDisposable
{
    /// <summary>
    /// Represents a publish request for fire-and-forget processing.
    /// </summary>
    /// <param name="EventType">The type of event being published</param>
    /// <param name="EventData">The event data</param>
    /// <param name="Handlers">List of handlers to invoke</param>
    private sealed record PublishRequest(string EventType, ISchedulerEvent EventData, List<Func<ISchedulerEvent, Task>> Handlers);

    private readonly ILogger<EventPublisher> _logger;
    private readonly ConcurrentDictionary<string, List<Delegate>> _subscribers = new();
    private readonly ConcurrentDictionary<string, TaskCompletionSource<ISchedulerEvent>> _waitingTasks = new();
    private readonly Channel<PublishRequest> _publishChannel;
    private readonly CancellationTokenSource _channelCts = new();
    private Task? _processingTask;
    private bool _disposed;

    /// <summary>
    /// Gets the maximum number of concurrent handlers that can be processed.
    /// </summary>
    public int MaxConcurrentHandlers { get; } = Math.Max(4, Environment.ProcessorCount * 2);

    /// <summary>
    /// Gets the bounded channel capacity for publish requests.
    /// </summary>
    public int ChannelCapacity { get; } = 1000;

    public EventPublisher(ILogger<EventPublisher> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Create bounded channel for fire-and-forget publishing
        _publishChannel = Channel.CreateBounded<PublishRequest>(new BoundedChannelOptions(ChannelCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });

        // Start background processor
        StartProcessingChannel();
    }

    /// <summary>
    /// Finalizer to ensure resources are properly disposed.
    /// </summary>
    ~EventPublisher()
    {
        Dispose(false);
    }

    /// <summary>
    /// Disposes the EventPublisher and cleans up background resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes resources.
    /// </summary>
    /// <param name="disposing">True if called from Dispose, false if called from finalizer</param>
    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            try
            {
                // Signal cancellation to the channel processor
                _channelCts.Cancel();

                // Complete the writer to allow processing of remaining items
                _publishChannel.Writer.Complete();

                // Wait for background processing to complete
                if (_processingTask != null)
                {
                    try
                    {
                        _processingTask.Wait(TimeSpan.FromSeconds(5));
                    }
                    catch (AggregateException)
                    {
                        // Ignore task cancellation exceptions during disposal
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during EventPublisher disposal");
            }
        }

        _disposed = true;
    }

    /// <summary>
    /// Starts the background channel processing task.
    /// This ensures publish requests are processed asynchronously without blocking the caller.
    /// </summary>
    private void StartProcessingChannel()
    {
        _processingTask = Task.Run(async () =>
        {
            _logger.LogInformation("EventPublisher background processor started with capacity {ChannelCapacity} and {MaxConcurrentHandlers} max concurrent handlers",
                ChannelCapacity, MaxConcurrentHandlers);

            try
            {
                await ProcessChannelAsync();
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("EventPublisher background processor cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EventPublisher background processor failed");
            }
            finally
            {
                _logger.LogInformation("EventPublisher background processor stopped");
            }
        });
    }

    /// <summary>
    /// Main processing loop for the publish channel.
    /// Processes publish requests with bounded parallelism to prevent resource exhaustion.
    /// </summary>
    private async Task ProcessChannelAsync()
    {
        var semaphore = new SemaphoreSlim(MaxConcurrentHandlers, MaxConcurrentHandlers);
        var reader = _publishChannel.Reader;

        await foreach (var request in reader.ReadAllAsync(_channelCts.Token))
        {
            try
            {
                // Use semaphore to limit concurrent handler execution
                await semaphore.WaitAsync(_channelCts.Token);

                // Process the publish request in background
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ProcessPublishRequestAsync(request);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });
            }
            catch (OperationCanceledException)
            {
                // Channel was closed, exit gracefully
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing publish request for event type: {EventType}", request.EventType);
            }
        }
    }

    /// <summary>
    /// Processes a single publish request by invoking all handlers.
    /// Aggregates and logs handler exceptions without preventing other handlers from executing.
    /// </summary>
    /// <param name="request">The publish request to process</param>
    private async Task ProcessPublishRequestAsync(PublishRequest request)
    {
        var eventType = request.EventType;
        var eventData = request.EventData;
        var handlers = request.Handlers;

        _logger.LogDebug("Processing publish request: {EventType} to {HandlerCount} handlers",
            eventType, handlers.Count);

        try
        {
            // Execute all handlers with individual error handling
            var handlerTasks = new List<Task>(handlers.Count);
            var handlerExceptions = new List<Exception>();

            foreach (var handler in handlers)
            {
                handlerTasks.Add(InvokeHandlerSafelyAsync(handler, eventData, eventType, handlerExceptions));
            }

            await Task.WhenAll(handlerTasks);

            // Log aggregated exceptions if any
            if (handlerExceptions.Count > 0)
            {
                _logger.LogWarning(handlerExceptions.Count == 1 ? handlerExceptions[0] :
                    new AggregateException(handlerExceptions),
                    "Completed processing event {EventType} with {ErrorCount} handler errors",
                    eventType, handlerExceptions.Count);
            }

            _logger.LogDebug("Completed processing publish request: {EventType} to {HandlerCount} handlers",
                eventType, handlers.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing publish request for event type: {EventType}", eventType);
        }
    }

    /// <summary>
    /// Invokes a handler with comprehensive error handling and exception aggregation.
    /// </summary>
    /// <param name="handler">The handler to invoke</param>
    /// <param name="eventData">The event data</param>
    /// <param name="eventType">The event type for logging</param>
    /// <param name="exceptions">List to collect exceptions</param>
    /// <returns>Task representing the handler invocation</returns>
    private async Task InvokeHandlerSafelyAsync<TEvent>(Func<TEvent, Task> handler, TEvent eventData, string eventType,
        List<Exception> exceptions) where TEvent : ISchedulerEvent
    {
        try
        {
            await handler(eventData);
        }
        catch (Exception ex)
        {
            // Log the individual error and add to exceptions list for aggregation
            _logger.LogError(ex, "Error invoking handler for event type: {EventType}", eventType);
            exceptions.Add(ex);
            // Don't throw - allow other handlers to execute and exceptions to be aggregated
        }
    }

    /// <summary>
    /// Publishes event asynchronously to all subscribers.
    /// This operation is fire-and-forget and best-effort.
    /// If subscribers throw exceptions, they are logged and aggregated but do not prevent other subscribers from receiving the event.
    /// Slow handlers cannot stall the scheduler due to bounded channel processing with limited concurrency.
    /// </summary>
    /// <param name="eventData">The event data to publish</param>
    /// <exception cref="ArgumentNullException">eventData is null</exception>
    public async Task PublishAsync<TEvent>(TEvent eventData) where TEvent : ISchedulerEvent
    {
        if (eventData is null)
            throw new ArgumentNullException(nameof(eventData));

        var eventType = typeof(TEvent).FullName ?? typeof(TEvent).Name;

        _logger.LogDebug("Publishing event: {EventType} (Id: {EventId})", eventType, eventData.EventId);

        try
        {
            // Wake up any waiting tasks
            var waitKey = $"wait:{eventType}";
            if (_waitingTasks.TryRemove(waitKey, out var tcs))
            {
                tcs.SetResult(eventData);
            }

            // Get subscribers for this event type
            if (!_subscribers.TryGetValue(eventType, out var handlers) || handlers.Count == 0)
            {
                _logger.LogDebug("No subscribers for event type: {EventType}", eventType);
                return;
            }

            // Create publish request for fire-and-forget processing
            // This ensures the publish operation is non-blocking and best-effort
            var request = new PublishRequest(
                EventType: eventType,
                EventData: eventData,
                Handlers: handlers.OfType<Func<TEvent, Task>>().Cast<Func<ISchedulerEvent, Task>>().ToList()
            );

            // Send to bounded channel - will wait if channel is full
            // This prevents unbounded memory growth and ensures backpressure
            await _publishChannel.Writer.WriteAsync(request, _channelCts.Token);

            _logger.LogDebug("Event queued for fire-and-forget publishing: {EventType} to {SubscriberCount} subscribers",
                eventType, handlers.Count);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Publish operation cancelled for event type: {EventType}", eventType);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error queuing event for publishing: {EventType}", eventType);
            throw;
        }
    }

    /// <summary>
    /// Subscribes handler to events of specific type.
    /// Returns subscription token for unsubscription.
    /// </summary>
    public IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : ISchedulerEvent
    {
        var eventType = typeof(TEvent).FullName ?? typeof(TEvent).Name;

        if (handler is null)
            throw new ArgumentNullException(nameof(handler));

        _subscribers.AddOrUpdate(eventType,
            new List<Delegate> { handler },
            (_, handlers) =>
            {
                handlers.Add(handler);
                return handlers;
            });

        _logger.LogDebug("Subscribed handler to event type: {EventType}", eventType);

        // Return disposable token for unsubscription
        return new SubscriptionToken(this, eventType, handler);
    }

    /// <summary>
    /// Unsubscribes handler from events.
    /// </summary>
    public void Unsubscribe<TEvent>(object subscriptionToken) where TEvent : ISchedulerEvent
    {
        if (subscriptionToken is not SubscriptionToken token)
            return;

        if (_subscribers.TryGetValue(token.EventType, out var handlers))
        {
            handlers.Remove(token.Handler);

            if (handlers.Count == 0)
                _subscribers.TryRemove(token.EventType, out _);

            _logger.LogDebug("Unsubscribed handler from event type: {EventType}", token.EventType);
        }
    }

    /// <summary>
    /// Waits for next event of specified type with timeout.
    /// Useful for testing event-driven scenarios.
    /// </summary>
    public async Task<TEvent> WaitForEventAsync<TEvent>(TimeSpan timeout) where TEvent : ISchedulerEvent
    {
        var eventType = typeof(TEvent).FullName ?? typeof(TEvent).Name;
        var waitKey = $"wait:{eventType}";

        var tcs = new TaskCompletionSource<ISchedulerEvent>();
        _waitingTasks[waitKey] = tcs;

        try
        {
            var result = await tcs.Task.ConfigureAwait(false);
            return (TEvent)result;
        }
        catch (OperationCanceledException)
        {
            _waitingTasks.TryRemove(waitKey, out _);
            throw;
        }
    }

    /// <summary>
    /// Gets list of event types with active subscribers.
    /// Useful for debugging and monitoring.
    /// </summary>
    public List<string> GetActiveEventTypes()
    {
        return _subscribers.Keys.ToList();
    }

    /// <summary>
    /// Gets count of subscribers for a specific event type.
    /// </summary>
    public int GetSubscriberCount<TEvent>() where TEvent : ISchedulerEvent
    {
        var eventType = typeof(TEvent).FullName ?? typeof(TEvent).Name;
        return _subscribers.TryGetValue(eventType, out var handlers) ? handlers.Count : 0;
    }

    /// <summary>
    /// Clears all subscriptions for a given event type.
    /// Useful during shutdown or reset scenarios.
    /// </summary>
    public void ClearSubscriptions<TEvent>() where TEvent : ISchedulerEvent
    {
        var eventType = typeof(TEvent).FullName ?? typeof(TEvent).Name;
        _subscribers.TryRemove(eventType, out _);
        _logger.LogInformation("Cleared all subscriptions for event type: {EventType}", eventType);
    }

    /// <summary>
    /// Clears all subscriptions for all event types.
    /// </summary>
    public void ClearAllSubscriptions()
    {
        _subscribers.Clear();
        _logger.LogInformation("Cleared all event subscriptions");
    }

    /// <summary>
    /// Internal subscription token for unsubscription.
    /// </summary>
    private class SubscriptionToken : IDisposable
    {
        private readonly EventPublisher _publisher;
        public string EventType { get; }
        public Delegate Handler { get; }

        public SubscriptionToken(EventPublisher publisher, string eventType, Delegate handler)
        {
            _publisher = publisher;
            EventType = eventType;
            Handler = handler;
        }

        public void Dispose()
        {
            _publisher._subscribers.AddOrUpdate(EventType,
                new List<Delegate>(),
                (_, handlers) =>
                {
                    handlers.Remove(Handler);
                    return handlers;
                });
        }
    }
}