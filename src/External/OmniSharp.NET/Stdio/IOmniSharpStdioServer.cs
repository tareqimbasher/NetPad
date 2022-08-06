using System;
using System.Text.Json.Nodes;
using OmniSharp.Events;

namespace OmniSharp.Stdio
{
    public interface IOmniSharpStdioServer : IOmniSharpServer
    {
        /// <summary>
        /// Subscribe to an event.
        /// </summary>
        /// <param name="eventType">The event type. See <see cref="OmniSharp.Models.Events.EventTypes"/> for a list of event types.</param>
        /// <param name="handler">A delegate to handle the event. The delegate will be passed the entire event, not just the body.</param>
        SubscriptionToken SubscribeToEvent(string eventType, Action<JsonNode> handler);
    }
}
