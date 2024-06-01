using NetPad.Events;

namespace NetPad.Plugins.OmniSharp.Events;

public class OmniSharpAsyncBufferUpdateCompletedEvent(Guid scriptId) : IEvent
{
    public Guid ScriptId { get; } = scriptId;
}
