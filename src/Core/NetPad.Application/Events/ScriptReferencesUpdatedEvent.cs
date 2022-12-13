using NetPad.DotNet;
using NetPad.Scripts;

namespace NetPad.Events;

public class ScriptReferencesUpdatedEvent : IEvent
{
    public ScriptReferencesUpdatedEvent(Script script, IEnumerable<Reference> added, IEnumerable<Reference> removed)
    {
        Script = script;
        Added = added;
        Removed = removed;
    }

    public Script Script { get; }
    public IEnumerable<Reference> Added { get; }
    public IEnumerable<Reference> Removed { get; }
}
