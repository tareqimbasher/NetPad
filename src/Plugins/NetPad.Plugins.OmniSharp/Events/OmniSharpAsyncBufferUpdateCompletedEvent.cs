using NetPad.Events;

namespace NetPad.Plugins.OmniSharp.Events;

public class OmniSharpAsyncBufferUpdateCompletedEvent : IEvent
{
    public OmniSharpAsyncBufferUpdateCompletedEvent(Guid scriptId)
    {
        ScriptId = scriptId;
    }

    public Guid ScriptId { get; }
}
