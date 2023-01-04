using System;
using System.Threading.Tasks;

namespace NetPad.Events;

/// <summary>
/// Event bus responsible for taking subscriptions/publications and delivering of Events.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Subscribe to a Event type with the given destination and delivery action.
    /// All references are held with WeakReferences
    ///
    /// All Events of this type will be delivered.
    /// </summary>
    /// <typeparam name="TEvent">Type of Event</typeparam>
    /// <param name="deliveryAction">Action to invoke when Event is delivered</param>
    /// <returns>EventSubscription used to unsubscribing</returns>
    EventSubscriptionToken Subscribe<TEvent>(Func<TEvent, Task> deliveryAction) where TEvent : class, IEvent;

    /// <summary>
    /// Subscribe to a Event type with the given destination and delivery action.
    /// Events will be delivered via the specified proxy.
    /// All references (apart from the proxy) are held with WeakReferences
    ///
    /// All Events of this type will be delivered.
    /// </summary>
    /// <typeparam name="TEvent">Type of Event</typeparam>
    /// <param name="deliveryAction">Action to invoke when Event is delivered</param>
    /// <param name="proxy">Proxy to use when delivering the Events</param>
    /// <returns>EventSubscription used to unsubscribing</returns>
    EventSubscriptionToken Subscribe<TEvent>(Func<TEvent, Task> deliveryAction, IEventProxy proxy) where TEvent : class, IEvent;

    /// <summary>
    /// Subscribe to a Event type with the given destination and delivery action.
    ///
    /// All Events of this type will be delivered.
    /// </summary>
    /// <typeparam name="TEvent">Type of Event</typeparam>
    /// <param name="deliveryAction">Action to invoke when Event is delivered</param>
    /// <param name="useStrongReferences">Use strong references to destination and deliveryAction </param>
    /// <returns>EventSubscription used to unsubscribing</returns>
    EventSubscriptionToken Subscribe<TEvent>(Func<TEvent, Task> deliveryAction, bool useStrongReferences) where TEvent : class, IEvent;

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
    EventSubscriptionToken Subscribe<TEvent>(Func<TEvent, Task> deliveryAction, bool useStrongReferences, IEventProxy proxy) where TEvent : class, IEvent;

    /// <summary>
    /// Subscribe to a Event type with the given destination and delivery action with the given filter.
    /// All references are held with WeakReferences
    ///
    /// Only Events that "pass" the filter will be delivered.
    /// </summary>
    /// <typeparam name="TEvent">Type of Event</typeparam>
    /// <param name="deliveryAction">Action to invoke when Event is delivered</param>
    /// <returns>EventSubscription used to unsubscribing</returns>
    EventSubscriptionToken Subscribe<TEvent>(Func<TEvent, Task> deliveryAction, Func<TEvent, bool> eventFilter) where TEvent : class, IEvent;

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
    EventSubscriptionToken Subscribe<TEvent>(Func<TEvent, Task> deliveryAction, Func<TEvent, bool> eventFilter, IEventProxy proxy) where TEvent : class, IEvent;

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
    EventSubscriptionToken Subscribe<TEvent>(Func<TEvent, Task> deliveryAction, Func<TEvent, bool> eventFilter, bool useStrongReferences)
        where TEvent : class, IEvent;

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
    EventSubscriptionToken Subscribe<TEvent>(Func<TEvent, Task> deliveryAction, Func<TEvent, bool> eventFilter, bool useStrongReferences, IEventProxy proxy)
        where TEvent : class, IEvent;

    /// <summary>
    /// Unsubscribe.
    ///
    /// Does not throw an exception if the subscription is not found.
    /// </summary>
    /// <param name="subscriptionToken">Subscription token received from Subscribe</param>
    void Unsubscribe(EventSubscriptionToken subscriptionToken);


    /// <summary>
    /// Publish a Event to any subscribers asynchronously
    /// </summary>
    /// <typeparam name="TEvent">Type of Event</typeparam>
    /// <param name="event">Event to deliver</param>
    Task PublishAsync<TEvent>(TEvent @event) where TEvent : class, IEvent;
}

public interface ISubscriberErrorHandler
{
    void Handle(IEvent @event, Exception exception);
}

/// <summary>
/// Represents a Event subscription
/// </summary>
public interface IEventSubscription
{
    /// <summary>
    /// Token returned to the subscribed to reference this subscription
    /// </summary>
    EventSubscriptionToken SubscriptionToken { get; }

    /// <summary>
    /// Whether delivery should be attempted.
    /// </summary>
    /// <param name="event">Event that may potentially be delivered.</param>
    /// <returns>True - ok to send, False - should not attempt to send</returns>
    bool ShouldAttemptDelivery(IEvent @event);

    /// <summary>
    /// Deliver the Event
    /// </summary>
    /// <param name="event">Event to deliver</param>
    Task DeliverAsync(IEvent @event);
}

/// <summary>
/// Event proxy definition.
///
/// A Event proxy can be used to intercept/alter Events and/or
/// marshall delivery actions onto a particular thread.
/// </summary>
public interface IEventProxy
{
    Task DeliverAsync(IEvent @event, IEventSubscription subscription);
}
