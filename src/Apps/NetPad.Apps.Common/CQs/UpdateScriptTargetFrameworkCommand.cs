using MediatR;
using NetPad.DotNet;
using NetPad.Events;
using NetPad.Scripts;
using NetPad.Scripts.Events;

namespace NetPad.Apps.CQs;

public class UpdateScriptTargetFrameworkCommand(Script script, DotNetFrameworkVersion targetFrameworkVersion)
    : Command
{
    public Script Script { get; } = script;
    public DotNetFrameworkVersion TargetFrameworkVersion { get; } = targetFrameworkVersion;

    public class Handler(IEventBus eventBus) : IRequestHandler<UpdateScriptTargetFrameworkCommand>
    {
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

            await eventBus.PublishAsync(new ScriptTargetFrameworkVersionUpdatedEvent(script, oldTargetFramework, newTargetFramework));

            return Unit.Value;
        }
    }
}
