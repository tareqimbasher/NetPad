using MediatR;
using NetPad.Events;
using NetPad.Scripts;

namespace NetPad.CQs;

public class UpdateScriptUseAspNetCommand : Command
{

    public UpdateScriptUseAspNetCommand(Script script, bool useAspNet)
    {
        Script = script;
        UseAspNet = useAspNet;
    }

    public Script Script { get; }
    public bool UseAspNet { get; }

    public class Handler : IRequestHandler<UpdateScriptUseAspNetCommand>
    {
        private readonly IEventBus _eventBus;

        public Handler(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

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

            await _eventBus.PublishAsync(new ScriptUseAspNetUpdatedEvent(script, oldUseAspNet, newUseAspNet));

            return Unit.Value;
        }
    }
}
