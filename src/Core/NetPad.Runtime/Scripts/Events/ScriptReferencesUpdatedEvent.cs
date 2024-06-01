using NetPad.DotNet;
using NetPad.Events;

namespace NetPad.Scripts.Events;

public class ScriptReferencesUpdatedEvent(Script script, IEnumerable<Reference> added, IEnumerable<Reference> removed)
    : IEvent
{
    public Script Script { get; } = script;
    public IEnumerable<Reference> Added { get; } = added;
    public IEnumerable<Reference> Removed { get; } = removed;
}
