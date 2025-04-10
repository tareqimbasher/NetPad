using NetPad.Events;

namespace NetPad.ExecutionModel.ClientServer.Events;

public class ScriptHostProcessLifetimeEvent(Guid scriptId, bool isRunning) : IEvent
{
    public Guid ScriptId { get; } = scriptId;
    public bool IsRunning { get; } = isRunning;
}
