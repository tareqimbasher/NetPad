using System;
using NetPad.Scripts;

namespace NetPad.Events;

public class EnvironmentPropertyChangedEvent : PropertyChangedEvent, IScriptEvent
{
    public EnvironmentPropertyChangedEvent(Guid scriptId, string propertyName, object? newValue)
        : base(propertyName, newValue)
    {
        ScriptId = scriptId;
    }

    public Guid ScriptId { get; }
}
