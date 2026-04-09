using MediatR;
using NetPad.Events;
using NetPad.Scripts;
using NetPad.Scripts.Events;

namespace NetPad.Apps.CQs;

public class UpdateScriptKindCommand(Script script, ScriptKind kind) : Command
{
    public Script Script { get; } = script;
    public ScriptKind Kind { get; } = kind;

    public class Handler(IEventBus eventBus) : IRequestHandler<UpdateScriptKindCommand>
    {
        public async Task<Unit> Handle(UpdateScriptKindCommand request, CancellationToken cancellationToken)
        {
            var script = request.Script;
            var oldKind = script.Config.Kind;
            var newKind = request.Kind;

            if (oldKind == newKind)
            {
                return Unit.Value;
            }

            script.Config.SetKind(newKind);

            await eventBus.PublishAsync(new ScriptKindUpdatedEvent(script, oldKind, newKind));

            return Unit.Value;
        }
    }
}
