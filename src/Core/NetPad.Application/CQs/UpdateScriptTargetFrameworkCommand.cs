using MediatR;
using NetPad.DotNet;
using NetPad.Events;
using NetPad.Scripts;

namespace NetPad.CQs;

public class UpdateScriptTargetFrameworkCommand : Command
{

    public UpdateScriptTargetFrameworkCommand(Script script, DotNetFrameworkVersion targetFrameworkVersion)
    {
        Script = script;
        TargetFrameworkVersion = targetFrameworkVersion;
    }

    public Script Script { get; }
    public DotNetFrameworkVersion TargetFrameworkVersion { get; }

    public class Handler : IRequestHandler<UpdateScriptTargetFrameworkCommand>
    {
        private readonly IEventBus _eventBus;

        public Handler(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public async Task<Unit> Handle(UpdateScriptTargetFrameworkCommand request, CancellationToken cancellationToken)
        {
            var script = request.Script;
            var oldTargetFramework = script.Config.TargetFrameworkVersion;
            var newTargetFramework = request.TargetFrameworkVersion;

            if (oldTargetFramework == newTargetFramework)
            {
                return Unit.Value;
            }

            script.Config.SetTargetFrameworkVersion(newTargetFramework);

            await _eventBus.PublishAsync(new ScriptTargetFrameworkVersionUpdatedEvent(script, oldTargetFramework, newTargetFramework));

            return Unit.Value;
        }
    }
}
