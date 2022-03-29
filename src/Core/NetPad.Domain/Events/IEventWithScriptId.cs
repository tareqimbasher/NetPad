using System;

namespace NetPad.Events;

public interface IEventWithScriptId
{
    Guid ScriptId { get; }
}
