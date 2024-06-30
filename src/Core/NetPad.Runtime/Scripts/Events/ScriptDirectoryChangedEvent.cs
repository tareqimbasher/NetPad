using NetPad.Events;

namespace NetPad.Scripts.Events;

public class ScriptDirectoryChangedEvent(IEnumerable<ScriptSummary> scripts) : IEvent
{
    public IEnumerable<ScriptSummary> Scripts { get; } = scripts;
}
