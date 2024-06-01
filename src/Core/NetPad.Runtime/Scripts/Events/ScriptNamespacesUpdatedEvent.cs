using NetPad.Events;

namespace NetPad.Scripts.Events;

public class ScriptNamespacesUpdatedEvent(Script script, IEnumerable<string> added, IEnumerable<string> removed)
    : IEvent
{
    public Script Script { get; } = script;
    public IEnumerable<string> Added { get; } = added;
    public IEnumerable<string> Removed { get; } = removed;
}
