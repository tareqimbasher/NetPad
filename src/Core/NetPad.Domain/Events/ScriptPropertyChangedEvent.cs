using System;

namespace NetPad.Events;

public class ScriptPropertyChangedEvent : PropertyChangedEvent, IScriptEvent
{
    public ScriptPropertyChangedEvent(Guid scriptId, string propertyName, object? newValue)
        : base(propertyName, newValue)
    {
        ScriptId = scriptId;
    }

    public Guid ScriptId { get; }
}
