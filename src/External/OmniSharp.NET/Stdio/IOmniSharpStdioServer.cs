using System;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using OmniSharp.Events;

namespace OmniSharp.Stdio
{
    /// <summary>
    /// An OmniSharp client that interfaces with an external OmniSharp process via standard input/output (STDIO).
    /// </summary>
    public interface IOmniSharpStdioServer : IOmniSharpServer
    {
        /// <summary>
        /// Subscribe to an event.
        /// </summary>
        /// <param name="eventType">The event type. See <see cref="OmniSharp.Models.Events.EventTypes"/> for a list of event types.</param>
        /// <param name="handler">A delegate to handle the event. The delegate will be passed the entire event, not just the body.</param>
        SubscriptionToken SubscribeToEvent(string eventType, Func<JsonNode, Task> handler);

        /// <summary>
        /// Adds a handler that will receive raw OmniSharp process standard output.
        /// </summary>
        void AddOnProcessOutputReceivedHandler(Func<string, Task> handler);

        /// <summary>
        /// Removes a previously added raw standard output handler.
        /// </summary>
        /// <param name="handler"></param>
        void RemoveOnProcessOutputReceivedHandler(Func<string, Task> handler);

        /// <summary>
        /// Adds a handler that will receive raw OmniSharp process error output.
        /// </summary>
        void AddOnProcessErrorReceivedHandler(Func<string, Task> handler);

        /// <summary>
        /// Removes a previously added raw error output handler.
        /// </summary>
        /// <param name="handler"></param>
        void RemoveOnProcessErrorReceivedHandler(Func<string, Task> handler);
    }
}
