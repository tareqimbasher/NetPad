using MediatR;
using NetPad.Events;
using NetPad.Scripts;
using NetPad.Scripts.Events;

namespace NetPad.Apps.CQs;

public class UpdateScriptUseAspNetCommand(Script script, bool useAspNet) : Command
{
    public Script Script { get; } = script;
    public bool UseAspNet { get; } = useAspNet;

    public class Handler(IEventBus eventBus) : IRequestHandler<UpdateScriptUseAspNetCommand>
    {
        public async Task<Unit> Handle(UpdateScriptUseAspNetCommand request, CancellationToken cancellationToken)
        {
            var script = request.Script;
            var oldUseAspNet = script.Config.UseAspNet;
            var newUseAspNet = request.UseAspNet;

            if (oldUseAspNet == newUseAspNet)
            {
                return Unit.Value;
            }

            script.Config.SetUseAspNet(newUseAspNet);

            await eventBus.PublishAsync(new ScriptUseAspNetUpdatedEvent(script, oldUseAspNet, newUseAspNet));

            return Unit.Value;
        }
    }
}
