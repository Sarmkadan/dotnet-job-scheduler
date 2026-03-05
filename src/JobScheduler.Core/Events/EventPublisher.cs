#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace JobScheduler.Core.Events;

/// <summary>
/// In-memory event publisher implementing pub-sub pattern for scheduler events.
/// Enables decoupled event-driven architecture within the scheduler.
/// WHY: Pub-sub pattern allows components to react to events without tight coupling.
/// </summary>
public class EventPublisher : IEventPublisher
{
    private readonly ILogger<EventPublisher> _logger;
    private readonly ConcurrentDictionary<string, List<Delegate>> _subscribers = new();
    private readonly ConcurrentDictionary<string, TaskCompletionSource<ISchedulerEvent>> _waitingTasks = new();

    public EventPublisher(ILogger<EventPublisher> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Publishes event asynchronously to all subscribers.
    /// Runs subscribers in parallel to prevent one from blocking others.
    /// </summary>
    public async Task PublishAsync<TEvent>(TEvent eventData) where TEvent : ISchedulerEvent
    {
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

            // Execute all handlers in parallel
            // WHY: Parallel execution prevents slow subscribers from blocking others
            var tasks = handlers
                .OfType<Func<TEvent, Task>>()
                .Select(handler => InvokeHandlerSafelyAsync(handler, eventData, eventType))
                .ToList();

            await Task.WhenAll(tasks);

            _logger.LogDebug("Event published successfully: {EventType} to {SubscriberCount} subscribers",
                eventType, handlers.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing event: {EventType}", eventType);
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
    /// Invokes handler with error handling and logging.
    /// Prevents one handler exception from affecting others.
    /// </summary>
    private async Task InvokeHandlerSafelyAsync<TEvent>(Func<TEvent, Task> handler, TEvent eventData, string eventType) where TEvent : ISchedulerEvent
    {
        try
        {
            await handler(eventData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking handler for event type: {EventType}", eventType);
            // Don't throw - allow other handlers to execute
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
