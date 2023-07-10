using System;

namespace NetPad.Events;

public class EnvironmentPropertyChangedEvent : PropertyChangedEvent, IScriptEvent
{
    public EnvironmentPropertyChangedEvent(Guid scriptId, string propertyName, object? oldValue, object? newValue)
        : base(propertyName, oldValue, newValue)
    {
        ScriptId = scriptId;
    }

    public Guid ScriptId { get; }
}
