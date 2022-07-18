namespace NetPad.Events;

public class ScriptOutputEmittedEvent : IEvent
{
    public ScriptOutputEmittedEvent(Guid scriptId, string? output)
    {
        ScriptId = scriptId;
        Output = output;
    }

    public Guid ScriptId { get; }
    public string? Output { get; }
}
