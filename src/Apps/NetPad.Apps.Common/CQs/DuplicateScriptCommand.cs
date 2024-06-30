using MediatR;
using NetPad.Events;
using NetPad.Scripts;
using NetPad.Scripts.Events;

namespace NetPad.Apps.CQs;

/// <summary>
/// Duplicates a script.
/// </summary>
public class DuplicateScriptCommand(Script script) : Command<Script>
{
    public Script Script { get; } = script;

    public class Handler(
        IScriptRepository scriptRepository,
        IScriptNameGenerator scriptNameGenerator,
        IEventBus eventBus)
        : IRequestHandler<DuplicateScriptCommand, Script>
    {
        public async Task<Script> Handle(DuplicateScriptCommand request, CancellationToken cancellationToken)
        {
            var name = scriptNameGenerator.Generate(request.Script.Name);
            var script = await scriptRepository.CreateAsync(name);
            await eventBus.PublishAsync(new ScriptCreatedEvent(script));

            script.SetDataConnection(request.Script.DataConnection);
            script.UpdateConfig(request.Script.Config);
            script.UpdateCode(request.Script.Code);

            return script;
        }
    }
}
