using System;

namespace NetPad.Events;

public class ScriptConfigPropertyChangedEvent : PropertyChangedEvent, IScriptEvent
{
    public ScriptConfigPropertyChangedEvent(Guid scriptId, string propertyName, object? oldValue, object? newValue)
        : base(propertyName, oldValue, newValue)
    {
        ScriptId = scriptId;
    }

    public Guid ScriptId { get; }
}
