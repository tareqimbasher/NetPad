using MediatR;
using NetPad.Events;
using NetPad.Scripts;
using NetPad.Scripts.Events;

namespace NetPad.Apps.CQs;

public class UpdateScriptCodeCommand(Script script, string? code) : Command
{
    public Script Script { get; } = script;
    public string? Code { get; } = code;

    public class Handler(IEventBus eventBus) : IRequestHandler<UpdateScriptCodeCommand>
    {
        public async Task<Unit> Handle(UpdateScriptCodeCommand request, CancellationToken cancellationToken)
        {
            var script = request.Script;

            var oldCode = script.Code;

            if (oldCode == request.Code)
            {
                return Unit.Value;
            }

            request.Script.UpdateCode(request.Code);

            await eventBus.PublishAsync(new ScriptCodeUpdatedEvent(script, script.Code, oldCode));

            return Unit.Value;
        }
    }
}
