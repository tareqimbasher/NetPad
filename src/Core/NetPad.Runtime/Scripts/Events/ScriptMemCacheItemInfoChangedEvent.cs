using NetPad.Events;
using NetPad.ExecutionModel.ClientServer.ScriptServices;

namespace NetPad.Scripts.Events;

public class ScriptMemCacheItemInfoChangedEvent(Guid scriptId, MemCacheItemInfo[] items): IEvent
{
    public Guid ScriptId { get; } = scriptId;
    public MemCacheItemInfo[] Items { get; } = items;
}
