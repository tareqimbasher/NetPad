using NetPad.Events;
using NetPad.Presentation;

namespace NetPad.Scripts.Events;

public class ScriptOutputEmittedEvent(Guid scriptId, ScriptOutput output) : IEvent
{
    public Guid ScriptId { get; } = scriptId;
    public ScriptOutput Output { get; } = output;
    public string OutputType { get; } = output.GetType().Name;
}
