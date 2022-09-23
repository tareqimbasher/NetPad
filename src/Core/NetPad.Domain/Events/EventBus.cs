using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetPad.Events;

/// <summary>
/// Event bus responsible for taking subscriptions/publications and delivering of Events.
/// </summary>
public sealed class EventBus : IEventBus
{
    readonly ISubscriberErrorHandler _subscriberErrorHandler;

    #region ctor methods

    public EventBus()
    {
        _subscriberErrorHandler = new DefaultSubscriberErrorHandler();
    }

    public EventBus(ISubscriberErrorHandler subscriberErrorHandler)
    {
        _subscriberErrorHandler = subscriberErrorHandler;
    }

    #endregion

    #region Private Types and Interfaces

    private class WeakEventSubscription<TEvent> : IEventSubscription
        where TEvent : class, IEvent
    {
        protected EventSubscriptionToken _subscriptionToken;
        protected WeakReference _deliveryAction;
        protected WeakReference _eventFilter;

        public EventSubscriptionToken SubscriptionToken
        {
            get { return _subscriptionToken; }
        }

        public bool ShouldAttemptDelivery(IEvent @event)
        {
            if (@event == null)
                return false;

            if (!(typeof(TEvent).IsAssignableFrom(@event.GetType())))
                return false;

            if (!_deliveryAction.IsAlive)
                return false;

            if (!_eventFilter.IsAlive)
                return false;

            return ((Func<TEvent, bool>)_eventFilter.Target!).Invoke((TEvent)@event);
        }

        public async Task DeliverAsync(IEvent @event)
        {
            if (!(@event is TEvent))
                throw new ArgumentException("Event is not the correct type");

            if (!_deliveryAction.IsAlive)
                return;

            await ((Func<TEvent, Task>)_deliveryAction.Target!).Invoke((TEvent)@event);
        }

        /// <summary>
        /// Initializes a new instance of the WeakEventSubscription class.
        /// </summary>
        /// <param name="destination">Destination object</param>
        /// <param name="deliveryAction">Delivery action</param>
        /// <param name="EventFilter">Filter function</param>
        public WeakEventSubscription(EventSubscriptionToken subscriptionToken, Func<TEvent, Task> deliveryAction, Func<TEvent, bool> EventFilter)
        {
            if (subscriptionToken == null)
                throw new ArgumentNullException("subscriptionToken");

            if (deliveryAction == null)
                throw new ArgumentNullException("deliveryAction");

            if (EventFilter == null)
                throw new ArgumentNullException("EventFilter");

            _subscriptionToken = subscriptionToken;
            _deliveryAction = new WeakReference(deliveryAction);
            _eventFilter = new WeakReference(EventFilter);
        }
    }

    private class StrongEventSubscription<TEvent> : IEventSubscription
        where TEvent : class, IEvent
    {
        protected EventSubscriptionToken _subscriptionToken;
        protected Func<TEvent, Task> _deliveryAction;
        protected Func<TEvent, bool> _eventFilter;

        public EventSubscriptionToken SubscriptionToken
        {
            get { return _subscriptionToken; }
        }

        public bool ShouldAttemptDelivery(IEvent @event)
        {
            if (@event == null)
                return false;

            if (!(typeof(TEvent).IsAssignableFrom(@event.GetType())))
                return false;

            return _eventFilter.Invoke((TEvent)@event);
        }

        public async Task DeliverAsync(IEvent @event)
        {
            if (!(@event is TEvent))
                throw new ArgumentException("Event is not the correct type");

            await _deliveryAction.Invoke((TEvent)@event);
        }

        /// <summary>
        /// Initializes a new instance of the EventSubscription class.
        /// </summary>
        /// <param name="destination">Destination object</param>
        /// <param name="deliveryAction">Delivery action</param>
        /// <param name="eventFilter">Filter function</param>
        public StrongEventSubscription(EventSubscriptionToken subscriptionToken, Func<TEvent, Task> deliveryAction, Func<TEvent, bool> eventFilter)
        {
            if (subscriptionToken == null)
                throw new ArgumentNullException(nameof(subscriptionToken));

            if (deliveryAction == null)
                throw new ArgumentNullException(nameof(deliveryAction));

            if (eventFilter == null)
                throw new ArgumentNullException(nameof(eventFilter));

            _subscriptionToken = subscriptionToken;
            _deliveryAction = deliveryAction;
            _eventFilter = eventFilter;
        }
    }

    #endregion

    #region Subscription dictionary

    private class SubscriptionItem
    {
        public IEventProxy Proxy { get; private set; }
        public IEventSubscription Subscription { get; private set; }

        public SubscriptionItem(IEventProxy proxy, IEventSubscription subscription)
        {
            Proxy = proxy;
            Subscription = subscription;
        }
    }

    private readonly object _subscriptionsPadlock = new object();
    private readonly List<SubscriptionItem> _subscriptions = new List<SubscriptionItem>();

    #endregion

    #region Public API

    /// <summary>
    /// Subscribe to a Event type with the given destination and delivery action.
    /// All references are held with strong references
    ///
    /// All Events of this type will be delivered.
    /// </summary>
    /// <typeparam name="TEvent">Type of Event</typeparam>
    /// <param name="deliveryAction">Action to invoke when Event is delivered</param>
    /// <returns>EventSubscription used to unsubscribing</returns>
    public EventSubscriptionToken Subscribe<TEvent>(Func<TEvent, Task> deliveryAction) where TEvent : class, IEvent
    {
        return AddSubscriptionInternal<TEvent>(deliveryAction, (m) => true, true, DefaultEventProxy.Instance);
    }

    /// <summary>
    /// Subscribe to a Event type with the given destination and delivery action.
    /// Events will be delivered via the specified proxy.
    /// All references (apart from the proxy) are held with strong references
    ///
    /// All Events of this type will be delivered.
    /// </summary>
    /// <typeparam name="TEvent">Type of Event</typeparam>
    /// <param name="deliveryAction">Action to invoke when Event is delivered</param>
    /// <param name="proxy">Proxy to use when delivering the Events</param>
    /// <returns>EventSubscription used to unsubscribing</returns>
    public EventSubscriptionToken Subscribe<TEvent>(Func<TEvent, Task> deliveryAction, IEventProxy proxy) where TEvent : class, IEvent
    {
        return AddSubscriptionInternal<TEvent>(deliveryAction, (m) => true, true, proxy);
    }

    /// <summary>
    /// Subscribe to a Event type with the given destination and delivery action.
    ///
    /// All Events of this type will be delivered.
    /// </summary>
    /// <typeparam name="TEvent">Type of Event</typeparam>
    /// <param name="deliveryAction">Action to invoke when Event is delivered</param>
    /// <param name="useStrongReferences">Use strong references to destination and deliveryAction </param>
    /// <returns>EventSubscription used to unsubscribing</returns>
    public EventSubscriptionToken Subscribe<TEvent>(Func<TEvent, Task> deliveryAction, bool useStrongReferences) where TEvent : class, IEvent
    {
        return AddSubscriptionInternal<TEvent>(deliveryAction, (m) => true, useStrongReferences, DefaultEventProxy.Instance);
    }

    /// <summary>
    /// Subscribe to a Event type with the given destination and delivery action.
    /// Events will be delivered via the specified proxy.
    ///
    /// All Events of this type will be delivered.
    /// </summary>
    /// <typeparam name="TEvent">Type of Event</typeparam>
    /// <param name="deliveryAction">Action to invoke when Event is delivered</param>
    /// <param name="useStrongReferences">Use strong references to destination and deliveryAction </param>
    /// <param name="proxy">Proxy to use when delivering the Events</param>
    /// <returns>EventSubscription used to unsubscribing</returns>
    public EventSubscriptionToken Subscribe<TEvent>(Func<TEvent, Task> deliveryAction, bool useStrongReferences, IEventProxy proxy) where TEvent : class, IEvent
    {
        return AddSubscriptionInternal<TEvent>(deliveryAction, (m) => true, useStrongReferences, proxy);
    }

    /// <summary>
    /// Subscribe to a Event type with the given destination and delivery action with the given filter.
    /// All references are held with WeakReferences
    ///
    /// Only Events that "pass" the filter will be delivered.
    /// </summary>
    /// <typeparam name="TEvent">Type of Event</typeparam>
    /// <param name="deliveryAction">Action to invoke when Event is delivered</param>
    /// <returns>EventSubscription used to unsubscribing</returns>
    public EventSubscriptionToken Subscribe<TEvent>(Func<TEvent, Task> deliveryAction, Func<TEvent, bool> eventFilter) where TEvent : class, IEvent
    {
        return AddSubscriptionInternal<TEvent>(deliveryAction, eventFilter, true, DefaultEventProxy.Instance);
    }

    /// <summary>
    /// Subscribe to a Event type with the given destination and delivery action with the given filter.
    /// Events will be delivered via the specified proxy.
    /// All references (apart from the proxy) are held with WeakReferences
    ///
    /// Only Events that "pass" the filter will be delivered.
    /// </summary>
    /// <typeparam name="TEvent">Type of Event</typeparam>
    /// <param name="deliveryAction">Action to invoke when Event is delivered</param>
    /// <param name="proxy">Proxy to use when delivering the Events</param>
    /// <returns>EventSubscription used to unsubscribing</returns>
    public EventSubscriptionToken Subscribe<TEvent>(Func<TEvent, Task> deliveryAction, Func<TEvent, bool> eventFilter, IEventProxy proxy)
        where TEvent : class, IEvent
    {
        return AddSubscriptionInternal<TEvent>(deliveryAction, eventFilter, true, proxy);
    }

    /// <summary>
    /// Subscribe to a Event type with the given destination and delivery action with the given filter.
    /// All references are held with WeakReferences
    ///
    /// Only Events that "pass" the filter will be delivered.
    /// </summary>
    /// <typeparam name="TEvent">Type of Event</typeparam>
    /// <param name="deliveryAction">Action to invoke when Event is delivered</param>
    /// <param name="useStrongReferences">Use strong references to destination and deliveryAction </param>
    /// <returns>EventSubscription used to unsubscribing</returns>
    public EventSubscriptionToken Subscribe<TEvent>(Func<TEvent, Task> deliveryAction, Func<TEvent, bool> eventFilter, bool useStrongReferences)
        where TEvent : class, IEvent
    {
        return AddSubscriptionInternal<TEvent>(deliveryAction, eventFilter, useStrongReferences, DefaultEventProxy.Instance);
    }

    /// <summary>
    /// Subscribe to a Event type with the given destination and delivery action with the given filter.
    /// Events will be delivered via the specified proxy.
    /// All references are held with WeakReferences
    ///
    /// Only Events that "pass" the filter will be delivered.
    /// </summary>
    /// <typeparam name="TEvent">Type of Event</typeparam>
    /// <param name="deliveryAction">Action to invoke when Event is delivered</param>
    /// <param name="useStrongReferences">Use strong references to destination and deliveryAction </param>
    /// <param name="proxy">Proxy to use when delivering the Events</param>
    /// <returns>EventSubscription used to unsubscribing</returns>
    public EventSubscriptionToken Subscribe<TEvent>(Func<TEvent, Task> deliveryAction, Func<TEvent, bool> eventFilter, bool useStrongReferences,
        IEventProxy proxy) where TEvent : class, IEvent
    {
        return AddSubscriptionInternal<TEvent>(deliveryAction, eventFilter, useStrongReferences, proxy);
    }

    /// <summary>
    /// Unsubscribe.
    ///
    /// Does not throw an exception if the subscription is not found.
    /// </summary>
    /// <param name="subscriptionToken">Subscription token received from Subscribe</param>
    public void Unsubscribe(EventSubscriptionToken subscriptionToken)
    {
        RemoveSubscriptionInternal(subscriptionToken);
    }

    /// <summary>
    /// Publish a Event to any subscribers asynchronously
    /// </summary>
    /// <typeparam name="TEvent">Type of Event</typeparam>
    /// <param name="event">Event to deliver</param>
    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : class, IEvent
    {
        await PublishInternalAsync<TEvent>(@event);
    }

    #endregion

    #region Internal Methods

    private EventSubscriptionToken AddSubscriptionInternal<TEvent>(Func<TEvent, Task> deliveryAction, Func<TEvent, bool> eventFilter, bool strongReference,
        IEventProxy proxy)
        where TEvent : class, IEvent
    {
        if (deliveryAction == null)
            throw new ArgumentNullException(nameof(deliveryAction));

        if (eventFilter == null)
            throw new ArgumentNullException(nameof(eventFilter));

        if (proxy == null)
            throw new ArgumentNullException(nameof(proxy));

        lock (_subscriptionsPadlock)
        {
            var subscriptionToken = new EventSubscriptionToken(this, typeof(TEvent));

            IEventSubscription subscription;
            if (strongReference)
                subscription = new StrongEventSubscription<TEvent>(subscriptionToken, deliveryAction, eventFilter);
            else
                subscription = new WeakEventSubscription<TEvent>(subscriptionToken, deliveryAction, eventFilter);

            _subscriptions.Add(new SubscriptionItem(proxy, subscription));

            return subscriptionToken;
        }
    }

    private void RemoveSubscriptionInternal(EventSubscriptionToken subscriptionToken)
    {
        if (subscriptionToken == null)
            throw new ArgumentNullException(nameof(subscriptionToken));

        lock (_subscriptionsPadlock)
        {
            var currentlySubscribed = (from sub in _subscriptions
                where object.ReferenceEquals(sub.Subscription.SubscriptionToken, subscriptionToken)
                select sub).ToList();

            currentlySubscribed.ForEach(sub => _subscriptions.Remove(sub));
        }
    }

    private async Task PublishInternalAsync<TEvent>(TEvent Event) where TEvent : class, IEvent
    {
        if (Event == null)
            throw new ArgumentNullException(nameof(Event));

        List<SubscriptionItem> currentlySubscribed;
        lock (_subscriptionsPadlock)
        {
            currentlySubscribed = (from sub in _subscriptions
                where sub.Subscription.ShouldAttemptDelivery(Event)
                select sub).ToList();
        }

        foreach (var sub in currentlySubscribed)
        {
            try
            {
                await sub.Proxy.DeliverAsync(Event, sub.Subscription);
            }
            catch (Exception exception)
            {
                // By default ignore any errors and carry on
                _subscriberErrorHandler.Handle(Event, exception);
            }
        }
    }

    #endregion
}

public class DefaultSubscriberErrorHandler : ISubscriberErrorHandler
{
    public void Handle(IEvent @event, Exception exception)
    {
        //default behaviour is to do nothing
    }
}

/// <summary>
/// Represents an active subscription to a Event
/// </summary>
public sealed class EventSubscriptionToken : IDisposable
{
    private readonly WeakReference _hub;
    private readonly Type _eventType;

    /// <summary>
    /// Initializes a new instance of the EventSubscriptionToken class.
    /// </summary>
    public EventSubscriptionToken(IEventBus hub, Type eventType)
    {
        if (hub == null)
            throw new ArgumentNullException(nameof(hub));

        if (!typeof(IEvent).IsAssignableFrom(eventType))
            throw new ArgumentOutOfRangeException(nameof(eventType));

        _hub = new WeakReference(hub);
        _eventType = eventType;
    }

    public Type EventType => _eventType;

    public void Dispose()
    {
        if (_hub.IsAlive)
        {
            var hub = _hub.Target as IEventBus;

            if (hub != null)
            {
                hub.Unsubscribe(this);
            }
        }

        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Default "pass through" proxy.
///
/// Does nothing other than deliver the Event.
/// </summary>
public sealed class DefaultEventProxy : IEventProxy
{
    private static readonly DefaultEventProxy _instance = new DefaultEventProxy();

    static DefaultEventProxy()
    {
    }

    /// <summary>
    /// Singleton instance of the proxy.
    /// </summary>
    public static DefaultEventProxy Instance
    {
        get
        {
            return _instance;
        }
    }

    private DefaultEventProxy()
    {
    }

    public async Task DeliverAsync(IEvent @event, IEventSubscription subscription)
    {
        await subscription.DeliverAsync(@event);
    }
}
