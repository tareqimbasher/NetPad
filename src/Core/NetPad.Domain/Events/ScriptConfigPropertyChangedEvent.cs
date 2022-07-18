using System;
using NetPad.Scripts;

namespace NetPad.Events;

public class ScriptConfigPropertyChangedEvent : PropertyChangedEvent, IScriptEvent
{
    public ScriptConfigPropertyChangedEvent(Guid scriptId, string propertyName, object? newValue)
        : base(propertyName, newValue)
    {
        ScriptId = scriptId;
    }

    public Guid ScriptId { get; }
}
