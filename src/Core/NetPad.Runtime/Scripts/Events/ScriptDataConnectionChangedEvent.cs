using NetPad.Data;
using NetPad.Events;

namespace NetPad.Scripts.Events;

public class ScriptDataConnectionChangedEvent(Script script, DataConnection? dataConnection) : IEvent
{
    public Script Script { get; } = script;
    public DataConnection? DataConnection { get; } = dataConnection;
}
