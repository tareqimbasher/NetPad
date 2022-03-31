using System;

namespace NetPad.Events;

public interface IScriptEvent : IEvent
{
    Guid ScriptId { get; }
}
