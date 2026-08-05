using NetPad.Events;

namespace NetPad.Apps.UiInterop;

/// <summary>
/// Asks a window that is already open to reload with new params or switch to a sub-view/destination.
/// Shells raise an open window rather than re-creating it, this event allows an already open window
/// to be manipulated.
/// </summary>
/// <param name="windowId">The window the request is for, one of <see cref="WindowIds"/>.</param>
/// <param name="params">
/// The query parameters the request's boot URL would have carried. A null value means the boot URL
/// would have left the parameter out.
/// </param>
public class WindowDeepLinkRequestedEvent(string windowId, IReadOnlyDictionary<string, string?> @params) : IEvent
{
    public string WindowId { get; } = windowId;
    public IReadOnlyDictionary<string, string?> Params { get; } = @params;
}
