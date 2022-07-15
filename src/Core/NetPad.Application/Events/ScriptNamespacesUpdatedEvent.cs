using NetPad.Scripts;

namespace NetPad.Events;

public class ScriptNamespacesUpdatedEvent : IEvent
{
    public ScriptNamespacesUpdatedEvent(Script script, IEnumerable<string> added, IEnumerable<string> removed)
    {
        Script = script;
        Added = added;
        Removed = removed;
    }

    public Script Script { get; }
    public IEnumerable<string> Added { get; }
    public IEnumerable<string> Removed { get; }
}
