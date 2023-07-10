using System;

namespace NetPad.Events;

public class ScriptPropertyChangedEvent : PropertyChangedEvent, IScriptEvent
{
    public ScriptPropertyChangedEvent(Guid scriptId, string propertyName, object? oldValue, object? newValue)
        : base(propertyName, oldValue, newValue)
    {
        ScriptId = scriptId;
    }

    public Guid ScriptId { get; }
}
