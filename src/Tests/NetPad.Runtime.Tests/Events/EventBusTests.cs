using NetPad.Events;

namespace NetPad.Runtime.Tests.Events;

public class EventBusTests
{
    private class TestEvent(string message) : IEvent
    {
        public string Message { get; } = message;
    }

    private class OtherEvent : IEvent;

    private class RecordingErrorHandler : ISubscriberErrorHandler
    {
        public List<(IEvent Event, Exception Exception)> Errors { get; } = [];

        public void Handle(IEvent @event, Exception exception)
        {
            Errors.Add((@event, exception));
        }
    }

    private class RecordingProxy : IEventProxy
    {
        public int DeliveryCount { get; private set; }

        public async Task DeliverAsync(IEvent @event, IEventSubscription subscription)
        {
            DeliveryCount++;
            await subscription.DeliverAsync(@event);
        }
    }

    // Basic subscribe/publish

    [Fact]
    public async Task Publish_DeliversEventToSubscriber()
    {
        var bus = new EventBus();
        TestEvent? received = null;
        bus.Subscribe<TestEvent>(e =>
        {
            received = e;
            return Task.CompletedTask;
        });

        await bus.PublishAsync(new TestEvent("hello"));

        Assert.NotNull(received);
        Assert.Equal("hello", received!.Message);
    }

    [Fact]
    public async Task Publish_DeliversToMultipleSubscribers()
    {
        var bus = new EventBus();
        int count = 0;
        bus.Subscribe<TestEvent>(_ =>
        {
            count++;
            return Task.CompletedTask;
        });
        bus.Subscribe<TestEvent>(_ =>
        {
            count++;
            return Task.CompletedTask;
        });
        bus.Subscribe<TestEvent>(_ =>
        {
            count++;
            return Task.CompletedTask;
        });

        await bus.PublishAsync(new TestEvent("test"));

        Assert.Equal(3, count);
    }

    [Fact]
    public async Task Publish_DoesNotDeliverToWrongEventType()
    {
        var bus = new EventBus();
        bool called = false;
        bus.Subscribe<OtherEvent>(_ =>
        {
            called = true;
            return Task.CompletedTask;
        });

        await bus.PublishAsync(new TestEvent("test"));

        Assert.False(called);
    }

    [Fact]
    public async Task Publish_WithNoSubscribers_DoesNotThrow()
    {
        var bus = new EventBus();
        await bus.PublishAsync(new TestEvent("test"));
    }

    // Unsubscribe

    [Fact]
    public async Task Unsubscribe_StopsDelivery()
    {
        var bus = new EventBus();
        int count = 0;
        var token = bus.Subscribe<TestEvent>(_ =>
        {
            count++;
            return Task.CompletedTask;
        });

        await bus.PublishAsync(new TestEvent("1"));
        bus.Unsubscribe(token);
        await bus.PublishAsync(new TestEvent("2"));

        Assert.Equal(1, count);
    }

    [Fact]
    public void Unsubscribe_WithUnknownToken_DoesNotThrow()
    {
        var bus = new EventBus();
        var token = new EventSubscriptionToken(bus, typeof(TestEvent));

        bus.Unsubscribe(token);
    }

    [Fact]
    public void Unsubscribe_ThrowsForNullToken()
    {
        var bus = new EventBus();

        Assert.Throws<ArgumentNullException>(() => bus.Unsubscribe(null!));
    }

    // EventSubscriptionToken disposal auto-unsubscribes

    [Fact]
    public async Task TokenDispose_Unsubscribes()
    {
        var bus = new EventBus();
        int count = 0;
        var token = bus.Subscribe<TestEvent>(_ =>
        {
            count++;
            return Task.CompletedTask;
        });

        await bus.PublishAsync(new TestEvent("1"));
        token.Dispose();
        await bus.PublishAsync(new TestEvent("2"));

        Assert.Equal(1, count);
    }

    // Event filtering

    [Fact]
    public async Task Subscribe_WithFilter_OnlyDeliversMatchingEvents()
    {
        var bus = new EventBus();
        var received = new List<string>();
        bus.Subscribe<TestEvent>(
            e =>
            {
                received.Add(e.Message);
                return Task.CompletedTask;
            },
            e => e.Message.StartsWith("keep"));

        await bus.PublishAsync(new TestEvent("keep-1"));
        await bus.PublishAsync(new TestEvent("skip-1"));
        await bus.PublishAsync(new TestEvent("keep-2"));

        Assert.Equal(["keep-1", "keep-2"], received);
    }

    // Strong vs weak references

    [Fact]
    public async Task Subscribe_WithStrongReferences_DeliversEvents()
    {
        var bus = new EventBus();
        int count = 0;
        bus.Subscribe<TestEvent>(_ =>
        {
            count++;
            return Task.CompletedTask;
        }, useStrongReferences: true);

        await bus.PublishAsync(new TestEvent("test"));

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Subscribe_WithWeakReferences_DeliversEvents()
    {
        var bus = new EventBus();
        int count = 0;
        // Keep a reference to the delegate so it doesn't get GC'd
        Func<TestEvent, Task> handler = _ =>
        {
            count++;
            return Task.CompletedTask;
        };
        bus.Subscribe(handler, useStrongReferences: false);

        await bus.PublishAsync(new TestEvent("test"));

        Assert.Equal(1, count);
        GC.KeepAlive(handler);
    }

    // Custom proxy

    [Fact]
    public async Task Subscribe_WithProxy_UsesProxyForDelivery()
    {
        var bus = new EventBus();
        var proxy = new RecordingProxy();
        bus.Subscribe<TestEvent>(_ => Task.CompletedTask, proxy);

        await bus.PublishAsync(new TestEvent("test"));
        await bus.PublishAsync(new TestEvent("test2"));

        Assert.Equal(2, proxy.DeliveryCount);
    }

    // Error handling

    [Fact]
    public async Task Publish_WhenSubscriberThrows_ContinuesDeliveryToOthers()
    {
        var errorHandler = new RecordingErrorHandler();
        var bus = new EventBus(errorHandler);
        int successCount = 0;

        bus.Subscribe<TestEvent>(_ => throw new InvalidOperationException("boom"));
        bus.Subscribe<TestEvent>(_ =>
        {
            successCount++;
            return Task.CompletedTask;
        });

        await bus.PublishAsync(new TestEvent("test"));

        Assert.Equal(1, successCount);
        Assert.Single(errorHandler.Errors);
        Assert.IsType<InvalidOperationException>(errorHandler.Errors[0].Exception);
    }

    // Null validation

    [Fact]
    public async Task Publish_ThrowsForNullEvent()
    {
        var bus = new EventBus();

        await Assert.ThrowsAsync<ArgumentNullException>(() => bus.PublishAsync<TestEvent>(null!));
    }

    [Fact]
    public void Subscribe_ThrowsForNullDeliveryAction()
    {
        var bus = new EventBus();

        Assert.Throws<ArgumentNullException>(() => bus.Subscribe<TestEvent>(null!));
    }

    [Fact]
    public void Subscribe_ThrowsForNullProxy()
    {
        var bus = new EventBus();

        Assert.Throws<ArgumentNullException>(() =>
            bus.Subscribe<TestEvent>(_ => Task.CompletedTask, (IEventProxy)null!));
    }

    [Fact]
    public void Subscribe_ThrowsForNullFilter()
    {
        var bus = new EventBus();

        Assert.Throws<ArgumentNullException>(() =>
            bus.Subscribe<TestEvent>(_ => Task.CompletedTask, (Func<TestEvent, bool>)null!));
    }

    // Combined overloads

    [Fact]
    public async Task Subscribe_WithFilterAndProxy_Works()
    {
        var bus = new EventBus();
        var proxy = new RecordingProxy();
        var received = new List<string>();

        bus.Subscribe<TestEvent>(
            e =>
            {
                received.Add(e.Message);
                return Task.CompletedTask;
            },
            e => e.Message == "match",
            proxy);

        await bus.PublishAsync(new TestEvent("match"));
        await bus.PublishAsync(new TestEvent("skip"));

        Assert.Single(received);
        Assert.Equal(1, proxy.DeliveryCount);
    }

    [Fact]
    public async Task Subscribe_WithFilterStrongRefAndProxy_Works()
    {
        var bus = new EventBus();
        var proxy = new RecordingProxy();
        int count = 0;

        bus.Subscribe<TestEvent>(
            _ =>
            {
                count++;
                return Task.CompletedTask;
            },
            _ => true,
            useStrongReferences: true,
            proxy: proxy);

        await bus.PublishAsync(new TestEvent("test"));

        Assert.Equal(1, count);
        Assert.Equal(1, proxy.DeliveryCount);
    }
}

public class EventSubscriptionTokenTests
{
    private class TestEvent : IEvent;

    [Fact]
    public void Constructor_SetsEventType()
    {
        var bus = new EventBus();
        var token = new EventSubscriptionToken(bus, typeof(TestEvent));

        Assert.Equal(typeof(TestEvent), token.EventType);
    }

    [Fact]
    public void Constructor_ThrowsForNullHub()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new EventSubscriptionToken(null!, typeof(TestEvent)));
    }

    [Fact]
    public void Constructor_ThrowsForNonEventType()
    {
        var bus = new EventBus();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new EventSubscriptionToken(bus, typeof(string)));
    }
}

public class DisposableTokenTests
{
    [Fact]
    public void Dispose_ExecutesAction()
    {
        bool executed = false;
        var token = new DisposableToken(() => executed = true);

        token.Dispose();

        Assert.True(executed);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        int count = 0;
        var token = new DisposableToken(() => count++);

        token.Dispose();
        token.Dispose();

        Assert.Equal(2, count);
    }
}
