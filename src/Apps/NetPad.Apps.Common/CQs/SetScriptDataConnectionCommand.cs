using MediatR;
using NetPad.Data;
using NetPad.Events;
using NetPad.Scripts;
using NetPad.Scripts.Events;

namespace NetPad.Apps.CQs;

public class SetScriptDataConnectionCommand(Script script, DataConnection? connection) : Command
{
    public Script Script { get; } = script;
    public DataConnection? Connection { get; } = connection;

    public class Handler(IEventBus eventBus) : IRequestHandler<SetScriptDataConnectionCommand, Unit>
    {
        public async Task<Unit> Handle(SetScriptDataConnectionCommand request, CancellationToken cancellationToken)
        {
            var script = request.Script;
            var connection = request.Connection;

            if (script.DataConnection?.Id == connection?.Id)
            {
                return Unit.Value;
            }

            script.SetDataConnection(connection);

            await eventBus.PublishAsync(new ScriptDataConnectionChangedEvent(script, connection));

            return Unit.Value;
        }
    }
}
