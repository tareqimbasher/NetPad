using NetPad.Events;

namespace NetPad.Scripts.Events;

public interface IScriptEvent : IEvent
{
    Guid ScriptId { get; }
}
