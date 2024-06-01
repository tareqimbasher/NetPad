using MediatR;
using NetPad.Events;
using NetPad.Scripts;

namespace NetPad.Apps.CQs;

/// <summary>
/// Renames a script.
/// </summary>
public class RenameScriptCommand(Script script, string newName) : Command
{
    public Script Script { get; } = script;
    public string NewName { get; } = newName;

    public class Handler(
        IScriptRepository scriptRepository,
        IAutoSaveScriptRepository autoSaveScriptRepository,
        IEventBus eventBus)
        : IRequestHandler<RenameScriptCommand>
    {
        private readonly IEventBus _eventBus = eventBus;

        public async Task<Unit> Handle(RenameScriptCommand request, CancellationToken cancellationToken)
        {
            var script = request.Script;
            var newName = request.NewName;

            scriptRepository.Rename(script, newName);

            if (script.IsDirty)
            {
                await autoSaveScriptRepository.DeleteAsync(script);
                await autoSaveScriptRepository.SaveAsync(script);
            }

            return Unit.Value;
        }
    }
}
