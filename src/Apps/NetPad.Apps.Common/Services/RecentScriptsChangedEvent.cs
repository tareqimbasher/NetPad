using NetPad.Events;

namespace NetPad.Apps.Services;

/// <summary>
/// Fired when the list of recently opened scripts changes (added or cleared).
/// The payload is the new full list, most-recent first.
/// </summary>
public class RecentScriptsChangedEvent(IReadOnlyList<string> recentScripts) : IEvent
{
    public IReadOnlyList<string> RecentScripts { get; } = recentScripts;
}
