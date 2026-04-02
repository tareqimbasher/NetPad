using MediatR;
using NetPad.Common;
using NetPad.DotNet;
using NetPad.Events;
using NetPad.Scripts;
using NetPad.Scripts.Events;

namespace NetPad.Apps.CQs;

public class CreateScriptCommand(string? scriptName) : Command<Script>
{
    public string? ScriptName { get; } = scriptName;

    public class Handler(
        IScriptNameGenerator scriptNameGenerator,
        IScriptRepository scriptRepository,
        IDotNetInfo dotNetInfo,
        IEventBus eventBus)
        : IRequestHandler<CreateScriptCommand, Script>
    {
        public async Task<Script> Handle(CreateScriptCommand request, CancellationToken cancellationToken)
        {
            var name = !string.IsNullOrWhiteSpace(request.ScriptName)
                ? request.ScriptName
                : scriptNameGenerator.Generate();

            var targetFrameworkVersion = dotNetInfo.GetLatestSupportedDotNetSdkVersion()?.GetFrameworkVersion()
                                         ?? GlobalConsts.AppDotNetFrameworkVersion;

            var script = await scriptRepository.CreateAsync(name, targetFrameworkVersion);

            script.Config.SetNamespaces(ScriptConfigDefaults.DefaultNamespaces);

            await eventBus.PublishAsync(new ScriptCreatedEvent(script));

            return script;
        }
    }
}
