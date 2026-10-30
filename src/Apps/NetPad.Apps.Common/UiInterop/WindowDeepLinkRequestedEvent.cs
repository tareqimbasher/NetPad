using NetPad.Events;

namespace NetPad.Apps.UiInterop;

/// <summary>
/// Asks a window that is already open to reload with new params or switch to a destination. Shells
/// raise an open window rather than re-creating it, so a deep link to it arrives as this event.
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
