using NetPad.Data;
using NetPad.Scripts;

namespace NetPad.Events;

public class ScriptDataConnectionChangedEvent : IEvent
{
    public ScriptDataConnectionChangedEvent(Script script, DataConnection? dataConnection)
    {
        Script = script;
        DataConnection = dataConnection;
    }

    public Script Script { get; }
    public DataConnection? DataConnection { get; }
}
