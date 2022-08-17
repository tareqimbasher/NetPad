using MediatR;
using NetPad.Data;
using NetPad.Scripts;

namespace NetPad.CQs;

public class SetScriptDataConnectionCommand : Command
{

    public SetScriptDataConnectionCommand(Script script, DataConnection? connection)
    {
        Script = script;
        Connection = connection;
    }

    public Script Script { get; }
    public DataConnection? Connection { get; }

    public class Handler : IRequestHandler<SetScriptDataConnectionCommand, Unit>
    {
        public Task<Unit> Handle(SetScriptDataConnectionCommand request, CancellationToken cancellationToken)
        {
            request.Script.SetDataConnection(request.Connection);
            
            return Task.FromResult(Unit.Value);
        }
    }
}
