namespace NetPad.Events;

public class ScriptSqlOutputEmittedEvent
{
    public ScriptSqlOutputEmittedEvent(Guid scriptId, string? output)
    {
        ScriptId = scriptId;
        Output = output;
    }

    public Guid ScriptId { get; }
    public string? Output { get; }
}
