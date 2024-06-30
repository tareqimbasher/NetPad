using MediatR;
using NetPad.Events;
using NetPad.Scripts;
using NetPad.Scripts.Events;

namespace NetPad.Apps.CQs;

/// <summary>
/// Saves a script. Returns true if script was saved, false otherwise.
/// </summary>
public class SaveScriptCommand(Script script) : Command
{
    public Script Script { get; } = script;

    public class Handler(
        IScriptRepository scriptRepository,
        IAutoSaveScriptRepository autoSaveScriptRepository,
        IEventBus eventBus)
        : IRequestHandler<SaveScriptCommand>
    {
        public async Task<Unit> Handle(SaveScriptCommand request, CancellationToken cancellationToken)
        {
            var script = request.Script;

            await scriptRepository.SaveAsync(script);

            await autoSaveScriptRepository.DeleteAsync(script);

            await eventBus.PublishAsync(new ScriptSavedEvent(script));

            return Unit.Value;
        }
    }
}
