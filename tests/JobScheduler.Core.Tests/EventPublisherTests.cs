#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JobScheduler.Core.Events;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JobScheduler.Core.Tests;

public sealed class EventPublisherTests
{
    private readonly Mock<ILogger<EventPublisher>> _loggerMock = new();
    private readonly EventPublisher _publisher;

    public EventPublisherTests()
    {
        _publisher = new EventPublisher(_loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new EventPublisher(null!));
    }

    [Fact]
    public async Task PublishAsync_WithValidEvent_CallsSubscribers()
    {
        // Arrange
        var eventReceived = false;
        var subscriptionToken = _publisher.Subscribe<JobCreatedEvent>(async _ =>
        {
            eventReceived = true;
            await Task.CompletedTask;
        });

        var testEvent = new JobCreatedEvent
        {
            JobId = Guid.NewGuid(),
            JobName = "Test Job",
            CreatedBy = "Test User"
        };

        // Act
        await _publisher.PublishAsync(testEvent);

        // Assert
        Assert.True(eventReceived);
        Assert.Equal(1, _publisher.GetSubscriberCount<JobCreatedEvent>());
        subscriptionToken.Dispose();
    }

    [Fact]
    public async Task PublishAsync_WithMultipleSubscribers_CallsAllSubscribers()
    {
        // Arrange
        var receivedCount = 0;
        var subscription1 = _publisher.Subscribe<JobExecutionStartedEvent>(async _ =>
        {
            receivedCount++;
            await Task.CompletedTask;
        });
        var subscription2 = _publisher.Subscribe<JobExecutionStartedEvent>(async _ =>
        {
            receivedCount++;
            await Task.CompletedTask;
        });
        var subscription3 = _publisher.Subscribe<JobExecutionStartedEvent>(async _ =>
        {
            receivedCount++;
            await Task.CompletedTask;
        });

        var testEvent = new JobExecutionStartedEvent
        {
            JobId = Guid.NewGuid(),
            ExecutionId = Guid.NewGuid(),
            JobName = "Test Job"
        };

        // Act
        await _publisher.PublishAsync(testEvent);

        // Assert
        Assert.Equal(3, receivedCount);
        subscription1.Dispose();
        subscription2.Dispose();
        subscription3.Dispose();
    }

    [Fact]
    public async Task PublishAsync_WithNoSubscribers_DoesNotThrow()
    {
        // Arrange
        var testEvent = new JobExecutionCompletedEvent
        {
            JobId = Guid.NewGuid(),
            ExecutionId = Guid.NewGuid(),
            JobName = "Test Job",
            Success = true,
            ExecutionTimeMs = 100
        };

        // Act
        var exception = await Record.ExceptionAsync(() => _publisher.PublishAsync(testEvent));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task PublishAsync_WithNullEvent_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _publisher.PublishAsync<JobCreatedEvent>(null!));
    }

    [Fact]
    public async Task Subscribe_WithNullHandler_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            Task.FromResult(_publisher.Subscribe<JobExecutionFailedEvent>(null!)));
    }

    [Fact]
    public void Subscribe_ReturnsDisposableToken()
    {
        // Arrange
        Func<JobDeletedEvent, Task> handler = _ => Task.CompletedTask;

        // Act
        var subscriptionToken = _publisher.Subscribe(handler);

        // Assert
        Assert.NotNull(subscriptionToken);
        Assert.IsAssignableFrom<IDisposable>(subscriptionToken);
    }

    [Fact]
    public void Unsubscribe_WithValidToken_RemovesHandler()
    {
        // Arrange
        var handlerCalled = false;
        var subscriptionToken = _publisher.Subscribe<SchedulerErrorEvent>(async _ =>
        {
            handlerCalled = true;
            await Task.CompletedTask;
        });

        var testEvent = new SchedulerErrorEvent
        {
            ErrorMessage = "Test error",
            Severity = 2
        };

        // Act - publish before unsubscribing
        _publisher.PublishAsync(testEvent).Wait();
        Assert.True(handlerCalled);

        // Reset for second test
        handlerCalled = false;

        // Act - unsubscribe and publish again
        _publisher.Unsubscribe<SchedulerErrorEvent>(subscriptionToken);
        _publisher.PublishAsync(testEvent).Wait();

        // Assert
        Assert.False(handlerCalled);
    }

    [Fact]
    public void Unsubscribe_WithInvalidToken_DoesNotThrow()
    {
        // Arrange
        var invalidToken = new object();

        // Act
        var exception = Record.Exception(() => _publisher.Unsubscribe<JobCreatedEvent>(invalidToken));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task Unsubscribe_WithNullToken_DoesNotThrow()
    {
        // Act
        var exception = Record.Exception(() => _publisher.Unsubscribe<JobExecutionStartedEvent>(null!));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task WaitForEventAsync_WithMatchingEvent_ReturnsEvent()
    {
        // Arrange
        var testEvent = new JobResumedEvent
        {
            JobId = Guid.NewGuid(),
            JobName = "Test Job"
        };

        // Start waiting in background
        var waitTask = _publisher.WaitForEventAsync<JobResumedEvent>(TimeSpan.FromSeconds(5));

        // Give the task a moment to start waiting
        await Task.Delay(10);

        // Act - publish the event
        await _publisher.PublishAsync(testEvent);

        // Assert
        var receivedEvent = await waitTask;
        Assert.NotNull(receivedEvent);
        Assert.Equal(testEvent.EventId, receivedEvent.EventId);
    }

    [Fact]
    public async Task WaitForEventAsync_WithTimeout_ThrowsTimeoutException()
    {
        // Act
        var exception = await Record.ExceptionAsync(() =>
            _publisher.WaitForEventAsync<JobExecutionFailedEvent>(TimeSpan.FromMilliseconds(10)));

        // Assert
        Assert.NotNull(exception);
        Assert.IsType<TimeoutException>(exception);
    }

    [Fact]
    public async Task WaitForEventAsync_WithMultipleEvents_ReturnsFirstEvent()
    {
        // Arrange
        var firstEvent = new JobCreatedEvent
        {
            JobId = Guid.NewGuid(),
            JobName = "First Job",
            CreatedBy = "User1"
        };

        var secondEvent = new JobCreatedEvent
        {
            JobId = Guid.NewGuid(),
            JobName = "Second Job",
            CreatedBy = "User2"
        };

        // Start waiting
        var waitTask = _publisher.WaitForEventAsync<JobCreatedEvent>(TimeSpan.FromSeconds(5));

        // Give the task a moment to start waiting
        await Task.Delay(10);

        // Act - publish first event
        await _publisher.PublishAsync(firstEvent);

        // Assert - should get first event
        var receivedEvent = await waitTask;
        Assert.Equal(firstEvent.EventId, receivedEvent.EventId);

        // Start waiting for second event
        waitTask = _publisher.WaitForEventAsync<JobCreatedEvent>(TimeSpan.FromSeconds(5));

        // Act - publish second event
        await _publisher.PublishAsync(secondEvent);

        // Assert - should get second event
        receivedEvent = await waitTask;
        Assert.Equal(secondEvent.EventId, receivedEvent.EventId);
    }

    [Fact]
    public void GetActiveEventTypes_WithSubscribers_ReturnsEventTypes()
    {
        // Arrange
        _publisher.Subscribe<JobCreatedEvent>(_ => Task.CompletedTask);
        _publisher.Subscribe<JobExecutionStartedEvent>(_ => Task.CompletedTask);
        _publisher.Subscribe<JobExecutionCompletedEvent>(_ => Task.CompletedTask);

        // Act
        var activeTypes = _publisher.GetActiveEventTypes();

        // Assert
        Assert.NotNull(activeTypes);
        Assert.Equal(3, activeTypes.Count);
        Assert.Contains(typeof(JobCreatedEvent).FullName, activeTypes);
        Assert.Contains(typeof(JobExecutionStartedEvent).FullName, activeTypes);
        Assert.Contains(typeof(JobExecutionCompletedEvent).FullName, activeTypes);
    }

    [Fact]
    public void GetActiveEventTypes_WithNoSubscribers_ReturnsEmptyList()
    {
        // Act
        var activeTypes = _publisher.GetActiveEventTypes();

        // Assert
        Assert.NotNull(activeTypes);
        Assert.Empty(activeTypes);
    }

    [Fact]
    public void GetSubscriberCount_WithSubscribers_ReturnsCorrectCount()
    {
        // Arrange
        var sub1 = _publisher.Subscribe<JobSuspendedEvent>(_ => Task.CompletedTask);
        var sub2 = _publisher.Subscribe<JobSuspendedEvent>(_ => Task.CompletedTask);
        var sub3 = _publisher.Subscribe<JobSuspendedEvent>(_ => Task.CompletedTask);
        _publisher.Subscribe<JobResumedEvent>(_ => Task.CompletedTask);

        // Act
        var count = _publisher.GetSubscriberCount<JobSuspendedEvent>();

        // Assert
        Assert.Equal(3, count);
        sub1.Dispose();
        sub2.Dispose();
        sub3.Dispose();
    }

    [Fact]
    public void GetSubscriberCount_WithNoSubscribers_ReturnsZero()
    {
        // Act
        var count = _publisher.GetSubscriberCount<JobDeletedEvent>();

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public void ClearSubscriptions_WithSubscribers_RemovesAllSubscriptions()
    {
        // Arrange
        _publisher.Subscribe<JobCreatedEvent>(_ => Task.CompletedTask);
        _publisher.Subscribe<JobExecutionStartedEvent>(_ => Task.CompletedTask);
        _publisher.Subscribe<JobExecutionCompletedEvent>(_ => Task.CompletedTask);

        Assert.Equal(3, _publisher.GetActiveEventTypes().Count);

        // Act
        _publisher.ClearSubscriptions<JobExecutionStartedEvent>();

        // Assert
        Assert.Equal(2, _publisher.GetActiveEventTypes().Count);
        Assert.Equal(0, _publisher.GetSubscriberCount<JobExecutionStartedEvent>());
    }

    [Fact]
    public void ClearSubscriptions_WithNoSubscribers_DoesNotThrow()
    {
        // Act
        var exception = Record.Exception(() => _publisher.ClearSubscriptions<JobCreatedEvent>());

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void ClearAllSubscriptions_WithMultipleSubscriptions_RemovesAll()
    {
        // Arrange
        _publisher.Subscribe<JobCreatedEvent>(_ => Task.CompletedTask);
        _publisher.Subscribe<JobExecutionStartedEvent>(_ => Task.CompletedTask);
        _publisher.Subscribe<JobExecutionCompletedEvent>(_ => Task.CompletedTask);
        _publisher.Subscribe<JobExecutionFailedEvent>(_ => Task.CompletedTask);
        _publisher.Subscribe<JobSuspendedEvent>(_ => Task.CompletedTask);

        Assert.Equal(5, _publisher.GetActiveEventTypes().Count);

        // Act
        _publisher.ClearAllSubscriptions();

        // Assert
        Assert.Empty(_publisher.GetActiveEventTypes());
        Assert.Equal(0, _publisher.GetSubscriberCount<JobCreatedEvent>());
        Assert.Equal(0, _publisher.GetSubscriberCount<JobExecutionStartedEvent>());
    }

    [Fact]
    public void ClearAllSubscriptions_WithNoSubscriptions_DoesNotThrow()
    {
        // Act
        var exception = Record.Exception(() => _publisher.ClearAllSubscriptions());

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task SubscriptionToken_Dispose_RemovesHandler()
    {
        // Arrange
        var handlerCalled = false;
        var subscriptionToken = _publisher.Subscribe<SchedulerErrorEvent>(async _ =>
        {
            handlerCalled = true;
            await Task.CompletedTask;
        });

        var testEvent = new SchedulerErrorEvent
        {
            ErrorMessage = "Test error",
            Severity = 2
        };

        // Act - publish before disposing
        await _publisher.PublishAsync(testEvent);
        Assert.True(handlerCalled);

        // Reset
        handlerCalled = false;

        // Act - dispose token and publish
        subscriptionToken.Dispose();
        await _publisher.PublishAsync(testEvent);

        // Assert
        Assert.False(handlerCalled);
    }

    [Fact]
    public async Task PublishAsync_WithHandlerException_DoesNotThrowAndContinuesProcessing()
    {
        // Arrange
        var handler1Called = false;
        var handler2Called = false;

        _publisher.Subscribe<JobCreatedEvent>(async _ =>
        {
            handler1Called = true;
            throw new InvalidOperationException("Test exception");
        });

        _publisher.Subscribe<JobCreatedEvent>(async _ =>
        {
            handler2Called = true;
            await Task.CompletedTask;
        });

        var testEvent = new JobCreatedEvent
        {
            JobId = Guid.NewGuid(),
            JobName = "Test Job",
            CreatedBy = "Test User"
        };

        // Act
        var exception = await Record.ExceptionAsync(() => _publisher.PublishAsync(testEvent));

        // Assert
        Assert.Null(exception); // Should not throw
        Assert.True(handler1Called); // First handler was called
        Assert.True(handler2Called); // Second handler was still called despite first failing
    }

    [Fact]
    public void GetActiveEventTypes_ReturnsDistinctEventTypes()
    {
        // Arrange
        _publisher.Subscribe<JobCreatedEvent>(_ => Task.CompletedTask);
        _publisher.Subscribe<JobCreatedEvent>(_ => Task.CompletedTask); // Same type
        _publisher.Subscribe<JobExecutionStartedEvent>(_ => Task.CompletedTask);

        // Act
        var activeTypes = _publisher.GetActiveEventTypes();

        // Assert
        Assert.Equal(2, activeTypes.Count); // Only 2 distinct types
    }

    [Fact]
    public async Task WaitForEventAsync_AfterEventPublished_WaitsForNextEvent()
    {
        // Arrange
        var firstEvent = new JobExecutionFailedEvent
        {
            JobId = Guid.NewGuid(),
            ExecutionId = Guid.NewGuid(),
            ErrorMessage = "First error",
            RetryAttempt = 1,
            WillRetry = true
        };

        // Publish first event
        await _publisher.PublishAsync(firstEvent);

        // Start waiting for second event
        var waitTask = _publisher.WaitForEventAsync<JobExecutionFailedEvent>(TimeSpan.FromSeconds(5));

        // Give the task a moment to start waiting
        await Task.Delay(10);

        // Act - publish second event
        var secondEvent = new JobExecutionFailedEvent
        {
            JobId = Guid.NewGuid(),
            ExecutionId = Guid.NewGuid(),
            ErrorMessage = "Second error",
            RetryAttempt = 2,
            WillRetry = false
        };
        await _publisher.PublishAsync(secondEvent);

        // Assert
        var receivedEvent = await waitTask;
        Assert.Equal(secondEvent.EventId, receivedEvent.EventId);
        Assert.Equal("Second error", receivedEvent.ErrorMessage);
    }
}