namespace NetPad.Scripts.Events;

public class ScriptConfigPropertyChangedEvent(Guid scriptId, string propertyName, object? oldValue, object? newValue)
    : PropertyChangedEvent(propertyName, oldValue, newValue), IScriptEvent
{
    public Guid ScriptId { get; } = scriptId;
}
